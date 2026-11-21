# azd prepackage hook (Windows): writes the SPA's build-time settings from azd environment values.
# Vite reads .env.production.local during `npm run build`. The file is git-ignored (*.local).
$ErrorActionPreference = 'Stop'
$lines = @(
  'VITE_AUTH_MODE=entra'
  "VITE_API_BASE_URL=$env:API_URL"
  "VITE_ENTRA_TENANT_ID=$env:ENTRA_TENANT_ID"
  "VITE_ENTRA_CLIENT_ID=$env:ENTRA_WEB_CLIENT_ID"
  "VITE_ENTRA_API_SCOPE=api://$env:ENTRA_API_CLIENT_ID/access_as_user"
)
[IO.File]::WriteAllLines((Join-Path $PWD '.env.production.local'), $lines)
Write-Host "Wrote .env.production.local (API: $env:API_URL)"
