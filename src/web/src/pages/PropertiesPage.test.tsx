import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, within } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import type { PropertyResponse } from '../api/generated/model'
import { PropertiesPage } from './PropertiesPage'

const property = (overrides: Partial<PropertyResponse>): PropertyResponse => ({
  id: crypto.randomUUID(),
  line1: '14 Castle Street',
  line2: null,
  town: 'Cardiff',
  postcode: 'CF10 1BU',
  jurisdiction: 'Wales',
  bedrooms: 2,
  landlordId: crypto.randomUUID(),
  landlordName: 'Gwen Davies',
  ...overrides,
})

const renderPage = () =>
  render(
    <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
      <PropertiesPage />
    </QueryClientProvider>,
  )

describe('PropertiesPage', () => {
  it('lists properties with jurisdiction and landlord', async () => {
    const data = [property({}), property({ line1: '3/2 Marchmont Road', town: 'Edinburgh', jurisdiction: 'Scotland' })]
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify(data), { status: 200 })))

    renderPage()

    const table = await screen.findByRole('table', { name: 'Properties' })
    const rows = within(table).getAllByRole('row')
    expect(rows).toHaveLength(3) // header + 2
    expect(within(rows[1]).getByText('14 Castle Street, Cardiff')).toBeInTheDocument()
    expect(within(rows[1]).getByText('Wales')).toBeInTheDocument()
    expect(within(rows[2]).getByText('Scotland')).toBeInTheDocument()
  })

  it('shows an error when the API fails', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('', { status: 500 })))

    renderPage()

    expect(await screen.findByText('Could not load properties.')).toBeInTheDocument()
  })
})
