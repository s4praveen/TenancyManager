import { InteractionRequiredAuthError, type AccountInfo } from '@azure/msal-browser'
import { MsalProvider, useIsAuthenticated, useMsal } from '@azure/msal-react'
import { useEffect, useMemo, type ReactNode } from 'react'
import { setAuthHeaderProvider } from '../api/http'
import { AuthContext, type AuthState } from './AuthContext'
import { apiScope, msalInstance } from './msalInstance'

function EntraAuthBridge({ children }: { children: ReactNode }) {
  const { instance, accounts } = useMsal()
  const isSignedIn = useIsAuthenticated()
  const account: AccountInfo | undefined = accounts[0]

  useEffect(() => {
    setAuthHeaderProvider(async (): Promise<Record<string, string>> => {
      if (!account) return {}
      try {
        const result = await instance.acquireTokenSilent({ scopes: [apiScope], account })
        return { Authorization: `Bearer ${result.accessToken}` }
      } catch (error) {
        if (error instanceof InteractionRequiredAuthError) {
          await instance.acquireTokenRedirect({ scopes: [apiScope], account })
        }
        throw error
      }
    })
  }, [instance, account])

  const value = useMemo<AuthState>(
    () => ({
      mode: 'entra',
      isSignedIn,
      signIn: async () => instance.loginRedirect({ scopes: [apiScope] }),
      signOut: async () => instance.logoutRedirect(),
    }),
    [instance, isSignedIn],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function EntraAuthProvider({ children }: { children: ReactNode }) {
  return (
    <MsalProvider instance={msalInstance}>
      <EntraAuthBridge>{children}</EntraAuthBridge>
    </MsalProvider>
  )
}
