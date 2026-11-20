import { defineConfig } from 'orval'

// Generates typed TanStack Query hooks from the API's build-time OpenAPI document.
// Regenerate with `npm run api:generate` whenever the API surface changes.
export default defineConfig({
  tenancyManager: {
    input: '../api/TenancyManager.Api/openapi/openapi.json',
    output: {
      mode: 'tags-split',
      target: 'src/api/generated/endpoints.ts',
      schemas: 'src/api/generated/model',
      client: 'react-query',
      httpClient: 'fetch',
      clean: true,
      override: {
        mutator: { path: 'src/api/http.ts', name: 'apiFetch' },
        query: { useQuery: true, useSuspenseQuery: false },
      },
    },
  },
})
