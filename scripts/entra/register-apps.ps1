<#
.SYNOPSIS
  Creates the two single-tenant Entra ID app registrations TenancyManager needs.

.DESCRIPTION
  TenancyManager-API  : exposes scope 'access_as_user' and app roles AgencyAdmin, Agent, Accounts, ReadOnly.
  TenancyManager-Web  : SPA (auth code + PKCE) that requests the API scope.

  Idempotent-ish: re-running finds existing apps by display name and updates them.
  Requires Azure CLI and permission to create app registrations in the tenant.
  Review before running. Nothing here is executed by CI.

.EXAMPLE
  ./scripts/entra/register-apps.ps1 -TenantId <tenant-guid> -AssignMeRole AgencyAdmin
#>
[CmdletBinding()]
param(
  [Parameter(Mandatory)] [string] $TenantId,
  [string[]] $SpaRedirectUris = @('http://localhost:5173'),
  [string] $ApiName = 'TenancyManager-API',
  [string] $WebName = 'TenancyManager-Web',
  [ValidateSet('', 'AgencyAdmin', 'Agent', 'Accounts', 'ReadOnly')] [string] $AssignMeRole = ''
)

$ErrorActionPreference = 'Stop'
$graph = 'https://graph.microsoft.com/v1.0'

function Invoke-Graph([string] $Method, [string] $Uri, $Body) {
  $args = @('rest', '--method', $Method, '--uri', $Uri, '--headers', 'Content-Type=application/json')
  if ($null -ne $Body) {
    $file = New-TemporaryFile
    ($Body | ConvertTo-Json -Depth 20) | Set-Content -Path $file -Encoding utf8
    $args += @('--body', "@$file")
  }
  $out = az @args
  if ($LASTEXITCODE -ne 0) { throw "Graph $Method $Uri failed" }
  if ($out) { return ($out | ConvertFrom-Json) }
}

function Get-OrCreateApp([string] $Name) {
  $existing = (Invoke-Graph GET "$graph/applications?`$filter=displayName eq '$Name'").value
  if ($existing.Count -gt 0) { Write-Host "Found $Name"; return $existing[0] }
  Write-Host "Creating $Name"
  return Invoke-Graph POST "$graph/applications" @{ displayName = $Name; signInAudience = 'AzureADMyOrg' }
}

function Get-OrCreateServicePrincipal([string] $AppId) {
  $sp = (Invoke-Graph GET "$graph/servicePrincipals?`$filter=appId eq '$AppId'").value
  if ($sp.Count -gt 0) { return $sp[0] }
  return Invoke-Graph POST "$graph/servicePrincipals" @{ appId = $AppId }
}

az account show --query tenantId -o tsv | Out-Null
if ($LASTEXITCODE -ne 0) { az login --tenant $TenantId | Out-Null }
$current = az account show --query tenantId -o tsv
if ($current -ne $TenantId) { throw "Azure CLI is signed in to tenant $current, expected $TenantId. Run: az login --tenant $TenantId" }

# ---- API ------------------------------------------------------------------------------------
$api = Get-OrCreateApp $ApiName

# Keep existing ids stable on re-run.
$scopeId = ($api.api.oauth2PermissionScopes | Where-Object value -eq 'access_as_user').id
if (-not $scopeId) { $scopeId = [guid]::NewGuid().ToString() }

$roleDefs = @(
  @{ value = 'AgencyAdmin'; displayName = 'Agency administrator'; description = 'Manages agency settings and users.' },
  @{ value = 'Agent';       displayName = 'Letting agent';        description = 'Manages landlords, properties and tenancies.' },
  @{ value = 'Accounts';    displayName = 'Accounts';             description = 'Manages rent, ledgers and statements.' },
  @{ value = 'ReadOnly';    displayName = 'Read only';            description = 'Views records without changing them.' }
)
$appRoles = foreach ($r in $roleDefs) {
  $id = ($api.appRoles | Where-Object value -eq $r.value).id
  @{
    id = $(if ($id) { $id } else { [guid]::NewGuid().ToString() })
    value = $r.value; displayName = $r.displayName; description = $r.description
    allowedMemberTypes = @('User'); isEnabled = $true
  }
}

Invoke-Graph PATCH "$graph/applications/$($api.id)" @{
  identifierUris = @("api://$($api.appId)")
  appRoles = @($appRoles)
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

# ---- Web (SPA) ------------------------------------------------------------------------------
$web = Get-OrCreateApp $WebName
Invoke-Graph PATCH "$graph/applications/$($web.id)" @{
  spa = @{ redirectUris = @($SpaRedirectUris) }
  requiredResourceAccess = @(@{
    resourceAppId = $api.appId
    resourceAccess = @(@{ id = $scopeId; type = 'Scope' })
  })
} | Out-Null
Get-OrCreateServicePrincipal $web.appId | Out-Null

# ---- Optional: give the signed-in user a role -----------------------------------------------
if ($AssignMeRole) {
  $me = az ad signed-in-user show --query id -o tsv
  $roleId = ($appRoles | Where-Object value -eq $AssignMeRole).id
  try {
    Invoke-Graph POST "$graph/servicePrincipals/$($apiSp.id)/appRoleAssignedTo" @{
      principalId = $me; resourceId = $apiSp.id; appRoleId = $roleId
    } | Out-Null
    Write-Host "Assigned role $AssignMeRole to you."
  } catch { Write-Warning "Role assignment skipped (it may already exist): $_" }
}

Write-Host ''
Write-Host 'Done. Grant admin consent for the Web app in the portal if your tenant requires it:'
Write-Host "  Entra ID > App registrations > $WebName > API permissions > Grant admin consent"
Write-Host ''
Write-Host '--- API (dotnet user-secrets, run in src/api/TenancyManager.Api) ---'
Write-Host "dotnet user-secrets set AzureAd:TenantId $TenantId"
Write-Host "dotnet user-secrets set AzureAd:ClientId $($api.appId)"
Write-Host "dotnet user-secrets set AzureAd:Audience api://$($api.appId)"
Write-Host ''
Write-Host '--- Web (src/web/.env.local) ---'
Write-Host 'VITE_AUTH_MODE=entra'
Write-Host "VITE_ENTRA_TENANT_ID=$TenantId"
Write-Host "VITE_ENTRA_CLIENT_ID=$($web.appId)"
Write-Host "VITE_ENTRA_API_SCOPE=api://$($api.appId)/access_as_user"
Write-Host ''
Write-Host '--- azd / GitHub environment variables ---'
Write-Host "ENTRA_TENANT_ID=$TenantId"
Write-Host "ENTRA_API_CLIENT_ID=$($api.appId)"
Write-Host "ENTRA_WEB_CLIENT_ID=$($web.appId)"
Write-Host ''
Write-Host 'After deploying, add the Static Web App URL as an extra SPA redirect URI (re-run with -SpaRedirectUris).'
