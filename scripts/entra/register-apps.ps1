<#
.SYNOPSIS
  Creates the two single-tenant Entra ID app registrations TenancyManager needs.

.DESCRIPTION
  TenancyManager-API  : exposes scope 'access_as_user' and app roles AgencyAdmin, Agent, Accounts, ReadOnly.
  TenancyManager-Web  : SPA (auth code + PKCE) that requests the API scope.

  Safe to re-run: it finds existing apps by display name and updates them (ids stay stable).
  Works in Windows PowerShell 5.1 and PowerShell 7. Requires the Azure CLI, signed in to the tenant,
  and permission to create app registrations.

.EXAMPLE
  ./scripts/entra/register-apps.ps1 -TenantId <tenant-guid> -AssignMeRole AgencyAdmin -GrantAdminConsent

.EXAMPLE
  # After the first deployment, add the Static Web App URL as a redirect URI:
  ./scripts/entra/register-apps.ps1 -TenantId <tenant-guid> -SpaRedirectUris http://localhost:5173, https://<your-app>.azurestaticapps.net
#>
[CmdletBinding()]
param(
  [Parameter(Mandatory)] [string] $TenantId,
  [string[]] $SpaRedirectUris = @('http://localhost:5173'),
  [string] $ApiName = 'TenancyManager-API',
  [string] $WebName = 'TenancyManager-Web',
  [ValidateSet('', 'AgencyAdmin', 'Agent', 'Accounts', 'ReadOnly')] [string] $AssignMeRole = '',
  [switch] $GrantAdminConsent
)

$ErrorActionPreference = 'Stop'
$graph = 'https://graph.microsoft.com/v1.0'

function Invoke-Graph([string] $Method, [string] $Uri, $Body) {
  $azArgs = @('rest', '--method', $Method, '--uri', $Uri, '--headers', 'Content-Type=application/json')
  $file = $null
  if ($null -ne $Body) {
    # UTF-8 *without* BOM: Windows PowerShell's Set-Content adds a BOM that breaks JSON parsing.
    $file = [IO.Path]::GetTempFileName()
    [IO.File]::WriteAllText($file, ($Body | ConvertTo-Json -Depth 20), (New-Object Text.UTF8Encoding $false))
    $azArgs += @('--body', "@$file")
  }
  try {
    $out = az @azArgs
    if ($LASTEXITCODE -ne 0) { throw "Graph $Method $Uri failed (see az output above)." }
  } finally {
    if ($file) { Remove-Item $file -ErrorAction SilentlyContinue }
  }
  # az prints JSON over several lines; join before parsing (PS 5.1 parses line by line otherwise).
  $text = ($out -join "`n").Trim()
  if ($text) { return ($text | ConvertFrom-Json) }
}

function Get-ByFilter([string] $Collection, [string] $Filter) {
  $encoded = [Uri]::EscapeDataString($Filter)
  return @((Invoke-Graph GET "$graph/$Collection`?`$filter=$encoded").value)
}

function Get-OrCreateApp([string] $Name) {
  $existing = Get-ByFilter 'applications' "displayName eq '$Name'"
  if ($existing.Count -gt 0) { Write-Host "Found app registration $Name"; return $existing[0] }
  Write-Host "Creating app registration $Name"
  return Invoke-Graph POST "$graph/applications" @{ displayName = $Name; signInAudience = 'AzureADMyOrg' }
}

function Get-OrCreateServicePrincipal([string] $AppId) {
  $sp = Get-ByFilter 'servicePrincipals' "appId eq '$AppId'"
  if ($sp.Count -gt 0) { return $sp[0] }
  return Invoke-Graph POST "$graph/servicePrincipals" @{ appId = $AppId }
}

# ---- Sign-in check ----------------------------------------------------------------------------
$current = az account show --query tenantId -o tsv 2>$null
if ($LASTEXITCODE -ne 0 -or $current -ne $TenantId) {
  Write-Host "Signing in to tenant $TenantId ..."
  az login --tenant $TenantId --allow-no-subscriptions | Out-Null
  $current = az account show --query tenantId -o tsv
}
if ($current -ne $TenantId) { throw "Azure CLI is signed in to tenant $current, expected $TenantId." }

# ---- API --------------------------------------------------------------------------------------
$api = Get-OrCreateApp $ApiName

# Reuse existing ids on re-run so tokens and role assignments keep working.
$scopeId = ($api.api.oauth2PermissionScopes | Where-Object { $_.value -eq 'access_as_user' }).id
if (-not $scopeId) { $scopeId = [guid]::NewGuid().ToString() }

$roleDefs = @(
  @{ value = 'AgencyAdmin'; displayName = 'Agency administrator'; description = 'Manages agency settings and users.' },
  @{ value = 'Agent';       displayName = 'Letting agent';        description = 'Manages landlords, properties and tenancies.' },
  @{ value = 'Accounts';    displayName = 'Accounts';             description = 'Manages rent, ledgers and statements.' },
  @{ value = 'ReadOnly';    displayName = 'Read only';            description = 'Views records without changing them.' }
)
$appRoles = @(foreach ($r in $roleDefs) {
  $existingId = ($api.appRoles | Where-Object { $_.value -eq $r.value }).id
  [ordered]@{
    id = $(if ($existingId) { $existingId } else { [guid]::NewGuid().ToString() })
    value = $r.value; displayName = $r.displayName; description = $r.description
    allowedMemberTypes = @('User'); isEnabled = $true
  }
})

Invoke-Graph PATCH "$graph/applications/$($api.id)" @{
  identifierUris = @("api://$($api.appId)")
  appRoles = $appRoles
  api = @{
    requestedAccessTokenVersion = 2
    oauth2PermissionScopes = @(@{
      id = $scopeId; value = 'access_as_user'; type = 'User'; isEnabled = $true
      adminConsentDisplayName = 'Access TenancyManager'
      adminConsentDescription = 'Allows the app to call the TenancyManager API as the signed-in user.'
      userConsentDisplayName = 'Access TenancyManager'
      userConsentDescription = 'Allows the app to call TenancyManager as you.'
    })
  }
} | Out-Null
$apiSp = Get-OrCreateServicePrincipal $api.appId

# ---- Web (SPA) --------------------------------------------------------------------------------
$web = Get-OrCreateApp $WebName
Invoke-Graph PATCH "$graph/applications/$($web.id)" @{
  spa = @{ redirectUris = @($SpaRedirectUris) }
  requiredResourceAccess = @(@{
    resourceAppId = $api.appId
    resourceAccess = @(@{ id = $scopeId; type = 'Scope' })
  })
} | Out-Null
$webSp = Get-OrCreateServicePrincipal $web.appId

# ---- Optional: admin consent (no consent prompt for agents) ----------------------------------
if ($GrantAdminConsent) {
  $grants = Get-ByFilter 'oauth2PermissionGrants' "clientId eq '$($webSp.id)' and resourceId eq '$($apiSp.id)'"
  if ($grants.Count -eq 0) {
    Invoke-Graph POST "$graph/oauth2PermissionGrants" @{
      clientId = $webSp.id; consentType = 'AllPrincipals'; resourceId = $apiSp.id; scope = 'access_as_user'
    } | Out-Null
    Write-Host 'Granted admin consent for access_as_user.'
  } else {
    Write-Host 'Admin consent already granted.'
  }
}

# ---- Optional: give the signed-in user a role -------------------------------------------------
if ($AssignMeRole) {
  $me = az ad signed-in-user show --query id -o tsv
  $roleId = ($appRoles | Where-Object { $_.value -eq $AssignMeRole }).id
  $assigned = @((Invoke-Graph GET "$graph/servicePrincipals/$($apiSp.id)/appRoleAssignedTo").value |
    Where-Object { $_.principalId -eq $me -and $_.appRoleId -eq $roleId })
  if ($assigned.Count -eq 0) {
    Invoke-Graph POST "$graph/servicePrincipals/$($apiSp.id)/appRoleAssignedTo" @{
      principalId = $me; resourceId = $apiSp.id; appRoleId = $roleId
    } | Out-Null
    Write-Host "Assigned role $AssignMeRole to you."
  } else {
    Write-Host "You already have role $AssignMeRole."
  }
}

Write-Host ''
Write-Host '=== Copy these values ===' -ForegroundColor Green
Write-Host "ENTRA_TENANT_ID     = $TenantId"
Write-Host "ENTRA_API_CLIENT_ID = $($api.appId)"
Write-Host "ENTRA_WEB_CLIENT_ID = $($web.appId)"
Write-Host ''
Write-Host 'Add them as GitHub repository variables (Settings > Secrets and variables > Actions > Variables).'
