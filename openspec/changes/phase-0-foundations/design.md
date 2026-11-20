# Design: Phase 0 — Foundations

## Context

Greenfield repository. See `proposal.md` for scope and `CLAUDE.md` for conventions. Constraints:
one agency today, multi-agency SaaS later. Entra ID single tenant. Azure hosting. A JSON stub must work
without Docker or a database.

## Goals / Non-Goals

**Goals:** layering that later phases extend without restructuring; one test suite that proves both
data providers behave the same; local start-up with zero external dependencies.

**Non-goals:** writes and CRUD, row-level security, deployment to a real subscription.

## Decisions

### Solution layering
`Domain` → `Application` → `Infrastructure` → `Api`. Domain has no package references. Application owns
repository interfaces (`IPropertyRepository`, `IAgencyRepository`) and `ITenantContext`.
*Alternative:* vertical slices in one project. Rejected for now: two persistence providers benefit from
an explicit infrastructure boundary.

No MediatR (its licence changed and it adds indirection). Endpoints call small application services directly.

### Data provider switch
`DataProvider:Type` is bound at start-up. `AddInfrastructure()` registers either the Json or the Postgres
repositories, and an unknown value throws (DAT-001).
- **Json:** `JsonDataStore` (singleton) loads `seed/agencies/*/{agency,landlords,properties}.json` at start-up into memory. Repositories filter by `ITenantContext.AgencyId`.
- **Postgres:** `TenancyDbContext` uses global query filters `e => e.AgencyId == tenant.AgencyId` on every `AgencyOwnedEntity`. `numeric(18,2)` for money. snake_case naming. Migrations live in `Infrastructure/Postgres/Migrations`. `PostgresSeeder` applies migrations and, if the database is empty, loads the **same** seed files through the same `SeedReader`.

*Alternative:* EF Core InMemory as the stub. Rejected: it doesn't let people hand-edit fixtures as JSON, and it hides relational behaviour.

### Tenant context
`ConfigurationTenantContext` reads `Agency:DefaultAgencyId`. Later a `ClaimsTenantContext` will replace it
without touching repositories. Seed data includes a second agency ("Southbank Homes") purely to prove isolation (AGY-001/002).

### Authentication
`Auth:Mode` is `Entra` (default) or `Dev`.
- **Entra:** `AddMicrosoftIdentityWebApi` against the `AzureAd` section. Roles come from app-role claims (`roles`).
- **Dev:** a custom `DevAuthenticationHandler` reads the `X-Dev-User` and `X-Dev-Roles` headers. With no header the request is anonymous, which keeps IAM-001 testable.
- Startup guard: Dev mode + Production environment → `InvalidOperationException` (IAM-004).
- Authorisation: a fallback policy requires an authenticated user. The `/api` group requires the `AgencyUser` policy (any agency role).

### API surface
Minimal APIs:
- `GET /health` (anonymous): custom JSON writer `{ status, dataProvider }`. On Postgres it includes a DbContext check.
- `GET /api/me`
- `GET /api/properties`
- `GET /api/properties/{id}`

The OpenAPI document is generated at build time (`Microsoft.Extensions.ApiDescription.Server`) into
`src/api/TenancyManager.Api/openapi/`. Orval reads that file to generate the web client, so no running API is needed.

### Testing strategy
- **AcceptanceTests:** Reqnroll + xUnit with `WebApplicationFactory<Program>` on the Json provider and Dev auth. Steps send the Dev headers. Start-up failures (IAM-004, DAT-001) are tested by building the factory with overridden settings.
- **IntegrationTests:** an abstract `PropertyRepositoryContract` run by a `Json` subclass and a `Postgres` subclass (Testcontainers `postgres:17`). Postgres tests are skipped automatically when Docker isn't available (DAT-002 is then enforced in CI).
- **Web:** Vitest + React Testing Library. Playwright smoke test starts the API (Json + Dev) and Vite.

### Web app
Vite + React + TS, MUI, React Router, TanStack Query. An `AuthProvider` abstraction has two implementations:
`DevAuthProvider` (role picker → headers) and `EntraAuthProvider` (MSAL, `acquireTokenSilent` → bearer).
The mode is chosen by `VITE_AUTH_MODE`. Orval generates a fetch client with a custom mutator that adds auth.

### Azure (authored, not deployed)
`azd` + Bicep:
- Log Analytics, Application Insights
- Container Registry, Container Apps environment, API container app with user-assigned managed identity
- PostgreSQL Flexible Server (Entra auth + password stored in Key Vault)
- Key Vault (RBAC), Static Web App for the SPA

Region parameter defaults to `uksouth`.

## Risks / Trade-offs

- [Json and Postgres drift apart] → shared contract tests, one `SeedReader`.
- [Dev auth leaks into prod] → startup guard plus a Bicep-set `Auth__Mode=Entra`.
- [Docker not available locally] → Postgres tests skip locally and are mandatory in CI.
- [Global query filter forgotten on a new entity] → a convention in `OnModelCreating` applies it to every `AgencyOwnedEntity`, and a unit test asserts every such entity has the filter.

## Migration Plan

Greenfield, nothing to migrate. Rollback is a git revert.

## Open Questions

- Exact Entra tenant and client IDs (supplied by the user later). Dev mode covers development until then.
