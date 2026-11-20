// Fetch wrapper used by every Orval-generated call. Adds the base URL and auth headers,
// and turns non-2xx responses into ApiError so TanStack Query reports them as errors.

export type AuthHeaderProvider = () => Promise<Record<string, string>>

let authHeaders: AuthHeaderProvider = async () => ({})

/** Called by the active auth provider (Dev or Entra) on sign-in. */
export const setAuthHeaderProvider = (provider: AuthHeaderProvider): void => {
  authHeaders = provider
}

export class ApiError extends Error {
  readonly status: number
  readonly problem: unknown

  constructor(status: number, problem: unknown) {
    super(`API request failed with status ${status}`)
    this.status = status
    this.problem = problem
  }
}

const baseUrl = (import.meta.env.VITE_API_BASE_URL ?? '').replace(/\/$/, '')

export const apiFetch = async <T>(url: string, init?: RequestInit): Promise<T> => {
  const headers = new Headers(init?.headers)
  for (const [key, value] of Object.entries(await authHeaders())) headers.set(key, value)
  headers.set('Accept', 'application/json')

  const res = await fetch(`${baseUrl}${url}`, { ...init, headers })
  const text = await res.text()
  const body: unknown = text ? JSON.parse(text) : undefined

  if (!res.ok) throw new ApiError(res.status, body)

  // Shape expected by Orval's fetch client: { data, status, headers }.
  return { data: body, status: res.status, headers: res.headers } as T
}
