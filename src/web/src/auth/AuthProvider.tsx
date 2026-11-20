import { lazy, Suspense, type ReactNode } from 'react'
import { DevAuthProvider } from './DevAuthProvider'

// MSAL is only loaded in Entra mode, keeping it out of the dev bundle path.
const EntraAuthProvider = lazy(async () => {
  const [{ EntraAuthProvider }, { msalInstance }] = await Promise.all([
    import('./EntraAuthProvider'),
    import('./msalInstance'),
  ])
  await msalInstance.initialize()
  await msalInstance.handleRedirectPromise()
  return { default: EntraAuthProvider }
})

export function AuthProvider({ children }: { children: ReactNode }) {
  if (import.meta.env.VITE_AUTH_MODE === 'entra') {
    return (
      <Suspense fallback={null}>
        <EntraAuthProvider>{children}</EntraAuthProvider>
      </Suspense>
    )
  }
  return <DevAuthProvider>{children}</DevAuthProvider>
}
