// TenancyManager Azure environment (one resource group per environment name).
// Deployed by .github/workflows/deploy.yml with `az deployment sub create --parameters infra/main.bicepparam`.
// Region defaults to UK South for UK data residency.
targetScope = 'subscription'

@minLength(1)
@maxLength(32)
@description('Environment name, e.g. dev, test, prod. Used in resource names.')
param environmentName string

@description('Primary region for all data-bearing resources.')
param location string = 'uksouth'

@description('Entra ID tenant that signs agents in (single tenant).')
param entraTenantId string

@description('Client id of the TenancyManager-API app registration (token audience).')
param entraApiClientId string

@description('Full API image reference, e.g. ghcr.io/<owner>/tenancymanager-api:<git-sha>.')
param apiImage string

@description('Registry login for a private image. Leave the password empty when the image is public.')
param registryServer string = 'ghcr.io'
param registryUsername string = ''
@secure()
param registryPassword string = ''

@description('false = the API serves the JSON seed data (cheapest; read-only PoC). true = PostgreSQL + Key Vault.')
param usePostgres bool = false

@secure()
@description('PostgreSQL administrator password. Required only when usePostgres is true.')
param postgresAdminPassword string = ''

@description('Optional user object id granted Key Vault secrets access for troubleshooting (usePostgres only).')
param principalId string = ''

@description('Static Web Apps is not offered in UK regions; it serves only static files, no personal data.')
@allowed(['westeurope', 'eastus2', 'centralus', 'westus2', 'eastasia'])
param staticWebAppLocation string = 'westeurope'

var token = toLower(uniqueString(subscription().id, environmentName, location))
var tags = { environment: environmentName, application: 'tenancymanager' }

resource rg 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: 'rg-tenancymanager-${environmentName}'
  location: location
  tags: tags
}

module monitoring 'modules/monitoring.bicep' = {
  scope: rg
  params: { location: location, tags: tags, token: token }
}

module identity 'modules/identity.bicep' = {
  scope: rg
  params: { location: location, tags: tags, token: token }
}

module postgres 'modules/postgres.bicep' = if (usePostgres) {
  scope: rg
  params: { location: location, tags: tags, token: token, adminPassword: postgresAdminPassword }
}

module keyVault 'modules/keyvault.bicep' = if (usePostgres) {
  scope: rg
  params: {
    location: location
    tags: tags
    token: token
    readerPrincipalIds: [identity.outputs.principalId]
    adminPrincipalId: principalId
    postgresConnectionString: postgres!.outputs.connectionStringTemplate
    postgresAdminPassword: postgresAdminPassword
    enablePurgeProtection: environmentName == 'prod'
  }
}

module web 'modules/staticwebapp.bicep' = {
  scope: rg
  params: { location: staticWebAppLocation, tags: tags, token: token }
}

module api 'modules/containerapps.bicep' = {
  scope: rg
  params: {
    location: location
    tags: tags
    token: token
    logAnalyticsWorkspaceId: monitoring.outputs.logAnalyticsWorkspaceId
    appInsightsConnectionString: monitoring.outputs.appInsightsConnectionString
    identityId: identity.outputs.id
    identityClientId: identity.outputs.clientId
    image: apiImage
    registryServer: registryServer
    registryUsername: registryUsername
    registryPassword: registryPassword
    postgresConnectionSecretUri: usePostgres ? keyVault!.outputs.postgresConnectionSecretUri : ''
    entraTenantId: entraTenantId
    entraApiClientId: entraApiClientId
    allowedOrigin: 'https://${web.outputs.defaultHostname}'
  }
}

output AZURE_RESOURCE_GROUP string = rg.name
output API_URL string = api.outputs.url
output WEB_URL string = 'https://${web.outputs.defaultHostname}'
output STATIC_WEB_APP_NAME string = web.outputs.name
output DATA_PROVIDER string = usePostgres ? 'Postgres' : 'Json'
