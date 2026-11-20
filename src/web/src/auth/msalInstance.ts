import { PublicClientApplication } from '@azure/msal-browser'

const env = import.meta.env

export const apiScope = env.VITE_ENTRA_API_SCOPE ?? ''

export const msalInstance = new PublicClientApplication({
  auth: {
    clientId: env.VITE_ENTRA_CLIENT_ID ?? '',
    // Single-tenant: only users in the agency's own Entra tenant can sign in.
    authority: `https://login.microsoftonline.com/${env.VITE_ENTRA_TENANT_ID ?? 'organizations'}`,
    redirectUri: window.location.origin,
  },
  cache: { cacheLocation: 'sessionStorage' },
})
