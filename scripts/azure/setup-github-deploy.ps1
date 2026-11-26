<#
.SYNOPSIS
  Lets GitHub Actions deploy to your Azure subscription without storing any Azure password (OIDC).

.DESCRIPTION
  Creates (or reuses) an app registration "TenancyManager-GitHub-Deploy", adds a federated credential
  that trusts only this repository's GitHub Environment (e.g. "dev"), and gives it the Contributor role
  on the subscription so it can create the resource group and resources.

  -AllowRoleAssignments also grants "Role Based Access Control Administrator". That is only needed
  when USE_POSTGRES=true, because the Key Vault module grants the API's identity access to its secret.

  Safe to re-run. Works in Windows PowerShell 5.1 and PowerShell 7. Requires the Azure CLI, signed in as
  someone who can create app registrations and assign roles on the subscription (the subscription owner).

.EXAMPLE
  ./scripts/azure/setup-github-deploy.ps1 -SubscriptionId <subscription-guid> -Repo s4praveen/TenancyManager
#>
[CmdletBinding()]
param(
  [Parameter(Mandatory)] [string] $SubscriptionId,
  [Parameter(Mandatory)] [ValidatePattern('^[^/]+/[^/]+$')] [string] $Repo,
  [string] $Environment = 'dev',
  [string] $AppName = 'TenancyManager-GitHub-Deploy',
  [switch] $AllowRoleAssignments
)

$ErrorActionPreference = 'Stop'

function Invoke-Az {
  $out = az @args
  if ($LASTEXITCODE -ne 0) { throw "az $($args -join ' ') failed (see output above)." }
  $text = ($out -join "`n").Trim()
  if ($text -and ($text.StartsWith('{') -or $text.StartsWith('['))) { return ($text | ConvertFrom-Json) }
  return $text
}

az account set --subscription $SubscriptionId
if ($LASTEXITCODE -ne 0) { throw "Can't select subscription $SubscriptionId. Run 'az login' first." }
$tenantId = Invoke-Az account show --query tenantId -o tsv

# ---- App registration + service principal -------------------------------------------------------
$app = @(Invoke-Az ad app list --display-name $AppName -o json)
if ($app.Count -gt 0) { $app = $app[0]; Write-Host "Found app registration $AppName" }
else { Write-Host "Creating app registration $AppName"; $app = Invoke-Az ad app create --display-name $AppName -o json }

$sp = @(Invoke-Az ad sp list --filter "appId eq '$($app.appId)'" -o json)
if ($sp.Count -gt 0) { $sp = $sp[0] } else { $sp = Invoke-Az ad sp create --id $app.appId -o json }

# ---- Federated credentials: trust GitHub tokens for this repo + environment ---------------------
# GitHub may present the subject in either format, so register both:
#   classic:   repo:<owner>/<repo>:environment:<env>
#   immutable: repo:<owner>@<ownerId>/<repo>@<repoId>:environment:<env>  (survives renames; can't be hijacked)
$subjects = [ordered]@{ "github-$Environment" = "repo:${Repo}:environment:$Environment" }
try {
  $gh = Invoke-RestMethod "https://api.github.com/repos/$Repo" -Headers @{ 'User-Agent' = 'tenancymanager-setup' }
  $subjects["github-$Environment-immutable"] =
    "repo:$($gh.owner.login)@$($gh.owner.id)/$($gh.name)@$($gh.id):environment:$Environment"
} catch {
  Write-Warning "Couldn't read the repository's ids from GitHub ($($_.Exception.Message)). Only the classic subject is registered; if sign-in fails with AADSTS700213, add the subject from the error as a federated credential."
}

$existing = @(Invoke-Az ad app federated-credential list --id $app.appId -o json)
foreach ($name in $subjects.Keys) {
  $subject = $subjects[$name]
  if ($existing | Where-Object { $_.subject -eq $subject }) { Write-Host "Federated credential for $subject already exists."; continue }
  $file = [IO.Path]::GetTempFileName()
  $body = @{
    name = $name
    issuer = 'https://token.actions.githubusercontent.com'
    subject = $subject
    audiences = @('api://AzureADTokenExchange')
  } | ConvertTo-Json
  [IO.File]::WriteAllText($file, $body, (New-Object Text.UTF8Encoding $false))
  try { Invoke-Az ad app federated-credential create --id $app.appId --parameters "@$file" -o none | Out-Null }
  finally { Remove-Item $file -ErrorAction SilentlyContinue }
  Write-Host "Added federated credential for $subject"
}

# ---- Role assignments ------------------------------------------------------------------------------
$scope = "/subscriptions/$SubscriptionId"
$roles = @('Contributor')
if ($AllowRoleAssignments) { $roles += 'Role Based Access Control Administrator' }
foreach ($role in $roles) {
  $has = @(Invoke-Az role assignment list --assignee $sp.id --role $role --scope $scope -o json)
  if ($has.Count -gt 0) { Write-Host "Already has '$role' on the subscription."; continue }
  # --assignee-principal-type avoids a Graph lookup that fails while a new principal replicates.
  Invoke-Az role assignment create --assignee-object-id $sp.id --assignee-principal-type ServicePrincipal `
    --role $role --scope $scope -o none | Out-Null
  Write-Host "Granted '$role' on the subscription."
}

Write-Host ''
Write-Host '=== Add these as GitHub repository VARIABLES ===' -ForegroundColor Green
Write-Host "(https://github.com/$Repo/settings/variables/actions > New repository variable)"
Write-Host "AZURE_CLIENT_ID       = $($app.appId)"
Write-Host "AZURE_TENANT_ID       = $tenantId"
Write-Host "AZURE_SUBSCRIPTION_ID = $SubscriptionId"
Write-Host ''
Write-Host "Also create the GitHub Environment '$Environment' (https://github.com/$Repo/settings/environments)."
