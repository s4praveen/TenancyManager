// Container Apps environment + the API container app.
param location string
param tags object
param token string
param logAnalyticsWorkspaceId string
param appInsightsConnectionString string
param identityId string
param identityClientId string
param image string
param registryServer string
param registryUsername string
@secure()
param registryPassword string
@description('Key Vault secret holding the Postgres connection string. Empty = JSON seed data.')
param postgresConnectionSecretUri string
param entraTenantId string
param entraApiClientId string
param allowedOrigin string

var usePostgres = !empty(postgresConnectionSecretUri)
var useRegistryLogin = !empty(registryPassword)

resource workspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' existing = {
  name: last(split(logAnalyticsWorkspaceId, '/'))
}

resource environment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: 'cae-tm-${token}'
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: workspace.properties.customerId
        sharedKey: workspace.listKeys().primarySharedKey
      }
    }
  }
}

var dataEnv = usePostgres
  ? [
      { name: 'DataProvider__Type', value: 'Postgres' }
      { name: 'ConnectionStrings__Postgres', secretRef: 'postgres-connection-string' }
    ]
  : [{ name: 'DataProvider__Type', value: 'Json' }]

resource api 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'ca-tm-api-${token}'
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: { '${identityId}': {} }
  }
  properties: {
    managedEnvironmentId: environment.id
    configuration: {
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'
        allowInsecure: false
      }
      registries: useRegistryLogin
        ? [{ server: registryServer, username: registryUsername, passwordSecretRef: 'registry-password' }]
        : []
      secrets: concat(
        usePostgres
          ? [{ name: 'postgres-connection-string', keyVaultUrl: postgresConnectionSecretUri, identity: identityId }]
          : [],
        useRegistryLogin ? [{ name: 'registry-password', value: registryPassword }] : []
      )
    }
    template: {
      containers: [
        {
          name: 'api'
          image: image
          resources: { cpu: json('0.5'), memory: '1Gi' }
          env: concat(
            [
              { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
              { name: 'Auth__Mode', value: 'Entra' } // Dev auth is refused in Production (IAM-004)
              { name: 'AzureAd__TenantId', value: entraTenantId }
              // No AzureAd__Audience: Microsoft.Identity.Web then accepts both the client id (v2 tokens,
              // which the app registration issues) and api://<client id> (v1 tokens).
              { name: 'AzureAd__ClientId', value: entraApiClientId }
              { name: 'Cors__AllowedOrigins__0', value: allowedOrigin }
              { name: 'AZURE_CLIENT_ID', value: identityClientId }
              { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: appInsightsConnectionString }
            ],
            dataEnv
          )
          probes: [
            { type: 'Liveness', httpGet: { path: '/health', port: 8080 }, periodSeconds: 30 }
            { type: 'Readiness', httpGet: { path: '/health', port: 8080 }, periodSeconds: 10 }
          ]
        }
      ]
      scale: { minReplicas: 0, maxReplicas: 3 }
    }
  }
}

output url string = 'https://${api.properties.configuration.ingress.fqdn}'
