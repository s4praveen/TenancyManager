// User-assigned managed identity for the API: reads Key Vault secrets (when Postgres is on).
param location string
param tags object
param token string

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'id-tm-api-${token}'
  location: location
  tags: tags
}

output id string = identity.id
output clientId string = identity.properties.clientId
output principalId string = identity.properties.principalId
