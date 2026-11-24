# Proposal: Phase 0 — Foundations

## Why

TenancyManager is a greenfield product. Before any letting features are built we need a running,
tested skeleton: authenticated API and web app, agency-scoped data that works on either a JSON stub
or PostgreSQL, and a spec-first BDD pipeline. Without these, every later phase would re-decide them.

## What Changes

- New .NET 10 API (Domain / Application / Infrastructure / Api) with OpenAPI + Scalar.
- Authentication through single-tenant Microsoft Entra ID, plus a Dev mode for local use and tests.
  Agency roles: `AgencyAdmin`, `Agent`, `Accounts`, `ReadOnly`.
- Agency scoping: every agency-owned record carries an `AgencyId`. The current agency comes from configuration.
- A switchable data provider: `Json` (seed files) or `Postgres` (EF Core, migrations, seeded from the same files).
- A minimal read-only property list (properties plus landlord name and jurisdiction), so the full stack is exercised end to end.
- A `/health` endpoint.
- New React + TypeScript SPA with sign-in (Dev role picker or MSAL), a "who am I" header and a property list.
- Tests: xUnit unit tests, Reqnroll acceptance tests, provider contract tests (Postgres via Testcontainers), Vitest, a Playwright smoke test.
- Tooling: Docker Compose, Dockerfiles, Bicep for Azure, GitHub Actions CI and CD (image on ghcr.io), an OpenSpec requirement→test traceability check, an Entra app registration script.

## Capabilities

### New Capabilities
- `identity-access`: authentication, Dev-mode safety, agency roles, current-user endpoint.
- `agency-scoping`: data is visible only to the agency it belongs to.
- `system-health`: unauthenticated health endpoint reporting status and data provider.
- `data-provider`: switchable JSON / PostgreSQL storage with identical seeded data.
- `properties`: read-only property list and detail (full CRUD comes in Phase 1).

### Modified Capabilities
None (greenfield).

## Out of scope

- Creating or editing landlords, properties and tenants (Phase 1).
- Tenancies, deposits, compliance, payments and the jurisdiction rules engine (Phases 2–5).
- Multi-agency onboarding, Entra External ID, Postgres row-level security and billing (Phase 7).
- Actual Azure deployment. Bicep is authored and validated but not run against a subscription.

## Jurisdiction impact

`Property.Jurisdiction` (England / Wales / Scotland) is introduced as data only. No country-specific
rules are implemented in this phase.

## Impact

New code throughout `src/`, `tests/`, `infra/`, `.github/`, `seed/`, `scripts/`. New dependencies:
EF Core + Npgsql, Microsoft.Identity.Web, Scalar, Reqnroll, Testcontainers, React/Vite/MUI/TanStack Query/MSAL, Playwright.
