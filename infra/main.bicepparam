// Values come from environment variables, which the deploy workflow sets from GitHub
// Environment variables/secrets. To deploy from your own PC, set the same variables and run:
//   az deployment sub create --location uksouth --parameters infra/main.bicepparam
using 'main.bicep'

param environmentName = readEnvironmentVariable('ENVIRONMENT_NAME', 'dev')
param location = readEnvironmentVariable('AZURE_LOCATION', 'uksouth')
param entraTenantId = readEnvironmentVariable('ENTRA_TENANT_ID')
param entraApiClientId = readEnvironmentVariable('ENTRA_API_CLIENT_ID')
param apiImage = readEnvironmentVariable('API_IMAGE')
param registryUsername = readEnvironmentVariable('REGISTRY_USERNAME', '')
param registryPassword = readEnvironmentVariable('REGISTRY_PASSWORD', '')
param usePostgres = bool(readEnvironmentVariable('USE_POSTGRES', 'false'))
param postgresAdminPassword = readEnvironmentVariable('POSTGRES_ADMIN_PASSWORD', '')
param principalId = readEnvironmentVariable('AZURE_PRINCIPAL_ID', '')
param staticWebAppLocation = readEnvironmentVariable('STATIC_WEB_APP_LOCATION', 'westeurope')
