// User-assigned managed identity for the API: pulls images, reads Key Vault secrets.
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
