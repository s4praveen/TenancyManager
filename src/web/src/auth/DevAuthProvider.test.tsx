import { describe, expect, it, vi } from 'vitest'
import { act, render, screen } from '@testing-library/react'
import { apiFetch } from '../api/http'
import { useAuth } from './AuthContext'
import { DevAuthProvider } from './DevAuthProvider'
import { devHeaders } from './devHeaders'

function SignInButton() {
  const { isSignedIn, signIn } = useAuth()
  return <button onClick={() => void signIn('admin@example.test', ['AgencyAdmin'])}>{String(isSignedIn)}</button>
}

describe('DevAuthProvider', () => {
  it('builds the headers the API Dev handler expects', () => {
    expect(devHeaders({ user: 'a@example.test', roles: ['Agent', 'Accounts'] })).toEqual({
      'X-Dev-User': 'a@example.test',
      'X-Dev-Roles': 'Agent,Accounts',
    })
  })

  it('sends Dev headers on API calls after sign-in', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response('[]', { status: 200 }))
    vi.stubGlobal('fetch', fetchMock)

    render(
      <DevAuthProvider>
        <SignInButton />
      </DevAuthProvider>,
    )
    expect(screen.getByRole('button')).toHaveTextContent('false')

    await act(async () => screen.getByRole('button').click())
    expect(screen.getByRole('button')).toHaveTextContent('true')

    await apiFetch('/api/properties')
    const headers = fetchMock.mock.calls[0][1].headers as Headers
    expect(headers.get('X-Dev-User')).toBe('admin@example.test')
    expect(headers.get('X-Dev-Roles')).toBe('AgencyAdmin')
  })
})
