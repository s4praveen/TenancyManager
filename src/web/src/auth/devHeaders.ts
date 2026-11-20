import type { AgencyRole } from './AuthContext'

export interface DevIdentity {
  user: string
  roles: AgencyRole[]
}

/** Builds the headers the API's Dev authentication handler trusts. */
export const devHeaders = (identity: DevIdentity): Record<string, string> => ({
  'X-Dev-User': identity.user,
  'X-Dev-Roles': identity.roles.join(','),
})
