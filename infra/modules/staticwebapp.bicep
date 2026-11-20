param location string
param tags object
param token string

resource site 'Microsoft.Web/staticSites@2024-04-01' = {
  name: 'stapp-tm-${token}'
  location: location
  tags: tags
  sku: { name: 'Free', tier: 'Free' }
  properties: {}
}

output defaultHostname string = site.properties.defaultHostname
