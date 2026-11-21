#!/bin/sh
# azd prepackage hook (macOS/Linux/CI): writes the SPA's build-time settings from azd environment values.
set -e
cat > .env.production.local <<ENV
VITE_AUTH_MODE=entra
VITE_API_BASE_URL=$API_URL
VITE_ENTRA_TENANT_ID=$ENTRA_TENANT_ID
VITE_ENTRA_CLIENT_ID=$ENTRA_WEB_CLIENT_ID
VITE_ENTRA_API_SCOPE=api://$ENTRA_API_CLIENT_ID/access_as_user
ENV
echo "Wrote .env.production.local (API: $API_URL)"
