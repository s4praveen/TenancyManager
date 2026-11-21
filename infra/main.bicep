// TenancyManager Azure environment (one resource group per azd environment).
// Provision with `azd up` (or `azd provision`). Region defaults to UK South for UK data residency.
targetScope = 'subscription'

@minLength(1)
@maxLength(32)
@description('azd environment name, e.g. dev, test, prod. Used in resource names.')
param environmentName string

@description('Primary region for all data-bearing resources.')
param location string = 'uksouth'

@description('Object id of the deploying user; granted Key Vault secrets access for troubleshooting.')
param principalId string = ''

@description('Entra ID tenant that signs agents in (single tenant).')
param entraTenantId string

@description('Client id of the TenancyManager-API app registration (token audience).')
param entraApiClientId string

@secure()
@description('PostgreSQL administrator password. Phase 7 replaces password auth with Entra ID auth.')
param postgresAdminPassword string

@description('Static Web Apps is not offered in UK regions; it serves only static files, no personal data.')
param staticWebAppLocation string = 'westeurope'

var token = toLower(uniqueString(subscription().id, environmentName, location))
var tags = { 'azd-env-name': environmentName, application: 'tenancymanager' }

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

module registry 'modules/registry.bicep' = {
  scope: rg
  params: { location: location, tags: tags, token: token, pullPrincipalId: identity.outputs.principalId }
}

module postgres 'modules/postgres.bicep' = {
  scope: rg
  params: { location: location, tags: tags, token: token, adminPassword: postgresAdminPassword }
}

module keyVault 'modules/keyvault.bicep' = {
  scope: rg
  params: {
    location: location
    tags: tags
    token: token
    readerPrincipalIds: [identity.outputs.principalId]
    adminPrincipalId: principalId
    postgresConnectionString: postgres.outputs.connectionStringTemplate
    postgresAdminPassword: postgresAdminPassword
    enablePurgeProtection: environmentName == 'prod'
  }
}

module web 'modules/staticwebapp.bicep' = {
  scope: rg
  params: { location: staticWebAppLocation, tags: union(tags, { 'azd-service-name': 'web' }), token: token }
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
    registryLoginServer: registry.outputs.loginServer
    postgresConnectionSecretUri: keyVault.outputs.postgresConnectionSecretUri
    entraTenantId: entraTenantId
    entraApiClientId: entraApiClientId
    allowedOrigin: 'https://${web.outputs.defaultHostname}'
  }
}

output AZURE_RESOURCE_GROUP string = rg.name
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = registry.outputs.loginServer
output API_URL string = api.outputs.url
output WEB_URL string = 'https://${web.outputs.defaultHostname}'
