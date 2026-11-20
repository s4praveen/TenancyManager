// Azure Database for PostgreSQL Flexible Server. Burstable tier for dev/test; raise sku + HA for prod.
param location string
param tags object
param token string

@secure()
param adminPassword string

param adminLogin string = 'tmadmin'
param databaseName string = 'tenancymanager'
param skuName string = 'Standard_B1ms'
param skuTier string = 'Burstable'

resource server 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' = {
  name: 'psql-tm-${token}'
  location: location
  tags: tags
  sku: { name: skuName, tier: skuTier }
  properties: {
    version: '17'
    administratorLogin: adminLogin
    administratorLoginPassword: adminPassword
    storage: { storageSizeGB: 32, autoGrow: 'Enabled' }
    backup: { backupRetentionDays: 7, geoRedundantBackup: 'Disabled' }
    highAvailability: { mode: 'Disabled' }
    authConfig: { activeDirectoryAuth: 'Disabled', passwordAuth: 'Enabled' }
  }
}

resource database 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2024-08-01' = {
  parent: server
  name: databaseName
  properties: { charset: 'UTF8', collation: 'en_US.utf8' }
}

// Allows Azure services (Container Apps) to connect. Replace with VNet integration before go-live.
resource allowAzure 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2024-08-01' = {
  parent: server
  name: 'AllowAzureServices'
  properties: { startIpAddress: '0.0.0.0', endIpAddress: '0.0.0.0' }
}

// Password is added by keyvault.bicep so it never appears in deployment outputs.
output connectionStringTemplate string = 'Host=${server.properties.fullyQualifiedDomainName};Database=${databaseName};Username=${adminLogin};Ssl Mode=Require;Password='
