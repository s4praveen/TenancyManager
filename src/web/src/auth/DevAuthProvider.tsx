import { useCallback, useMemo, useState, type ReactNode } from 'react'
import { setAuthHeaderProvider } from '../api/http'
import { AuthContext, type AgencyRole, type AuthState } from './AuthContext'
import { devHeaders, type DevIdentity } from './devHeaders'

const STORAGE_KEY = 'tm.devAuth'

const load = (): DevIdentity | null => {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    return raw ? (JSON.parse(raw) as DevIdentity) : null
  } catch {
    return null
  }
}

const save = (identity: DevIdentity | null): void => {
  try {
    if (identity) localStorage.setItem(STORAGE_KEY, JSON.stringify(identity))
    else localStorage.removeItem(STORAGE_KEY)
  } catch {
    // Storage unavailable (private mode); the session just won't survive a reload.
  }
}

const register = (identity: DevIdentity | null): void =>
  setAuthHeaderProvider(async () => (identity ? devHeaders(identity) : {}))

/** Local-only sign-in: pick a user and roles. The API refuses Dev auth in Production. */
export function DevAuthProvider({ children }: { children: ReactNode }) {
  const [identity, setIdentity] = useState<DevIdentity | null>(() => {
    const initial = load()
    register(initial)
    return initial
  })

  const signIn = useCallback(async (user = 'agent@example.test', roles: AgencyRole[] = ['Agent']) => {
    const next = { user, roles }
    save(next)
    register(next)
    setIdentity(next)
  }, [])

  const signOut = useCallback(async () => {
    save(null)
    register(null)
    setIdentity(null)
  }, [])

  const value = useMemo<AuthState>(
    () => ({ mode: 'dev', isSignedIn: identity !== null, signIn, signOut }),
    [identity, signIn, signOut],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
