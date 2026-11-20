param location string
param tags object
param token string

@description('Principals (managed identities) that may read secrets.')
param readerPrincipalIds array

@description('Optional user principal that may manage secrets (the deploying developer).')
param adminPrincipalId string = ''

param postgresConnectionString string

@secure()
param postgresAdminPassword string

resource vault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: 'kv-tm-${token}'
  location: location
  tags: tags
  properties: {
    tenantId: subscription().tenantId
    sku: { family: 'A', name: 'standard' }
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 30
    enablePurgeProtection: true
  }
}

resource postgresSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: vault
  name: 'postgres-connection-string'
  properties: { value: '${postgresConnectionString}${postgresAdminPassword}' }
}

var secretsUserRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
var secretsOfficerRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'b86a8fe4-44ce-4948-aee5-eccb2c155cd7')

resource readers 'Microsoft.Authorization/roleAssignments@2022-04-01' = [for id in readerPrincipalIds: {
  scope: vault
  name: guid(vault.id, id, secretsUserRoleId)
  properties: { roleDefinitionId: secretsUserRoleId, principalId: id, principalType: 'ServicePrincipal' }
}]

resource admin 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(adminPrincipalId)) {
  scope: vault
  name: guid(vault.id, adminPrincipalId, secretsOfficerRoleId)
  properties: { roleDefinitionId: secretsOfficerRoleId, principalId: adminPrincipalId, principalType: 'User' }
}

output postgresConnectionSecretUri string = postgresSecret.properties.secretUri
