import { createContext, useContext } from 'react'

export const AGENCY_ROLES = ['AgencyAdmin', 'Agent', 'Accounts', 'ReadOnly'] as const
export type AgencyRole = (typeof AGENCY_ROLES)[number]

export type AuthMode = 'dev' | 'entra'

export interface AuthState {
  mode: AuthMode
  isSignedIn: boolean
  /** Dev mode: user name and roles to impersonate. Entra mode: arguments are ignored. */
  signIn: (user?: string, roles?: AgencyRole[]) => Promise<void>
  signOut: () => Promise<void>
}

export const AuthContext = createContext<AuthState | null>(null)

export const useAuth = (): AuthState => {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used inside <AuthProvider>')
  return ctx
}
