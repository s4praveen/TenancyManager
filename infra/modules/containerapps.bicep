// Container Apps environment + the API container app.
param location string
param tags object
param token string
param logAnalyticsWorkspaceId string
param appInsightsConnectionString string
param identityId string
param identityClientId string
param registryLoginServer string
param postgresConnectionSecretUri string
param entraTenantId string
param entraApiClientId string
param allowedOrigin string

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

resource api 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'ca-tm-api-${token}'
  location: location
  tags: union(tags, { 'azd-service-name': 'api' })
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
      registries: [{ server: registryLoginServer, identity: identityId }]
      secrets: [
        { name: 'postgres-connection-string', keyVaultUrl: postgresConnectionSecretUri, identity: identityId }
      ]
    }
    template: {
      containers: [
        {
          name: 'api'
          // Placeholder until `azd deploy` pushes the real image.
          image: 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'
          resources: { cpu: json('0.5'), memory: '1Gi' }
          env: [
            { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
            { name: 'Auth__Mode', value: 'Entra' } // Dev auth is refused in Production (IAM-004)
            { name: 'AzureAd__TenantId', value: entraTenantId }
            { name: 'AzureAd__ClientId', value: entraApiClientId }
            { name: 'AzureAd__Audience', value: 'api://${entraApiClientId}' }
            { name: 'DataProvider__Type', value: 'Postgres' }
            { name: 'ConnectionStrings__Postgres', secretRef: 'postgres-connection-string' }
            { name: 'Cors__AllowedOrigins__0', value: allowedOrigin }
            { name: 'AZURE_CLIENT_ID', value: identityClientId }
            { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: appInsightsConnectionString }
          ]
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
