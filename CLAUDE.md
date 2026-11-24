# CLAUDE.md — TenancyManager

Guidance for Claude Code (and humans) working in this repository.

## What this is

A web application for **UK letting agents** to manage landlords (owners), properties, tenants,
tenancies, deposits, compliance and rent. It covers **England, Wales and Scotland**, which have
different tenancy law.

- **Today:** single agency, single-tenant Microsoft Entra ID.
- **Later:** multi-agency SaaS. Design every feature so that move needs no schema change (see *Agency scoping*).
- **Planned integrations:** deposit scheme APIs (TDS, DPS, mydeposits, SafeDeposits Scotland, LPS Scotland), Open Banking rent collection.
- **Hosting:** Azure (UK South primary, UK West DR).

## Stack

| Area | Choice |
|---|---|
| API | .NET 10, ASP.NET Core minimal APIs, OpenAPI (built-in) + Scalar UI |
| Data | PostgreSQL via EF Core 10 + Npgsql — **or** JSON seed files (`DataProvider:Type = Json`) |
| Auth | Entra ID via `Microsoft.Identity.Web`; `Auth:Mode = Dev` header-based auth for local/test only |
| Web | React + TypeScript + Vite, React Router, TanStack Query, MUI, MSAL React |
| Specs | **OpenSpec** (`openspec/`) — the source of truth for behaviour |
| BDD | **Reqnroll** (`tests/TenancyManager.AcceptanceTests`) for API, **Playwright** (`tests/e2e`) for UI |
| Unit tests | xUnit (API), Vitest + React Testing Library (web) |
| Infra | Bicep (`infra/`, `main.bicepparam`). GitHub Actions for CI and CD (`.github/workflows`); `deploy.yml` pushes the API image to ghcr.io and deploys via OIDC. `USE_POSTGRES=false` (the PoC default) runs the JSON provider in Azure. |

## Repository layout

```
openspec/                    specs/ (current truth), changes/ (proposals), config.yaml
src/api/
  TenancyManager.Domain/        entities, enums, value objects — no dependencies
  TenancyManager.Application/   use cases, repository interfaces, ITenantContext
  TenancyManager.Infrastructure/ Json/ and Postgres/ providers, EF Core, auth helpers
  TenancyManager.Api/           endpoints, auth wiring, Program.cs
src/web/                     React SPA
tests/
  TenancyManager.UnitTests/
  TenancyManager.AcceptanceTests/   Reqnroll .feature files + step definitions
  TenancyManager.IntegrationTests/  repository contract tests (Json + Postgres/Testcontainers)
  e2e/                              Playwright
seed/agencies/<agencyId>/*.json     stub data, also used to seed an empty Postgres
infra/                       Bicep
scripts/                     traceability check, Entra app registration
```

## Commands

```bash
# API (JSON stub, dev auth — no Postgres/Entra needed)
dotnet run --project src/api/TenancyManager.Api            # http://localhost:5080, docs at /scalar

# API against Postgres
docker compose up -d postgres
DataProvider__Type=Postgres dotnet run --project src/api/TenancyManager.Api

# Web
cd src/web && npm install && npm run dev                    # http://localhost:5173
npm run api:generate                                        # regenerate typed client from OpenAPI

# Tests
dotnet test                                                 # unit + acceptance (+ Postgres tests if Docker present)
cd src/web && npm test && npm run lint && npm run typecheck
cd tests/e2e && npx playwright test                         # starts API + web itself

# Specs
npx @fission-ai/openspec@2.1.1 validate --all --strict
node scripts/check-traceability.mjs                         # every requirement ID has a test

# EF Core migrations (tool restored via dotnet tool restore)
dotnet ef migrations add <Name> -p src/api/TenancyManager.Infrastructure -s src/api/TenancyManager.Api -o Postgres/Migrations

# Full stack
docker compose up --build
```

## How we build features (spec-first BDD)

1. **Propose** with OpenSpec: `/openspec-propose "<idea>"` → `openspec/changes/<id>/` (proposal, spec deltas, design, tasks). Wait for human approval before implementing.
2. **Write failing acceptance tests**: a Reqnroll `.feature` scenario for each OpenSpec scenario, tagged with the requirement ID.
3. **Implement**: domain → application → infrastructure (**both** providers) → endpoint → UI.
4. **UI journeys** get a Playwright test when the change is user-facing.
5. **Verify**: `dotnet test`, web tests, `openspec validate --strict`, traceability script.
6. **Archive**: `/openspec-archive-change <id>` merges the deltas into `openspec/specs/`.

### Requirement IDs and traceability
- Every OpenSpec requirement header carries an ID: `### Requirement: TEN-003 Deposit cap`.
- Prefixes: `IAM` identity/access, `AGY` agency scoping, `SYS` system/health, `DAT` data provider,
  `PRP` properties, `LLD` landlords, `TNT` tenants, `TEN` tenancies, `DEP` deposits, `CMP` compliance,
  `PAY` payments/ledger, `JUR` jurisdiction rules (use `ENG`/`WAL`/`SCO` for country-only rules).
- Each Reqnroll scenario is tagged `@<ID>`. `scripts/check-traceability.mjs` fails CI on an untested ID.
- Use `Scenario Outline` + `Examples` with a `jurisdiction` column when behaviour differs by country.

## Non-negotiable conventions

### Agency scoping (SaaS-ready)
- Every agency-owned entity derives from `AgencyOwnedEntity` and has a non-null `AgencyId`.
- Never filter by agency by hand in endpoints. Isolation is enforced centrally: EF Core global query filters (Postgres) and `ITenantContext`-aware repositories (Json).
- Today `ITenantContext` reads `Agency:DefaultAgencyId` from config. Later it will read a token claim. Code must not assume which.
- A record from another agency returns **404**, never 403, so its existence isn't revealed.

### Both data providers, always
- Every repository interface has **both** a Json and a Postgres implementation, and both pass the shared contract tests in `TenancyManager.IntegrationTests`.
- Seed data lives only in `seed/`. Postgres seeding reads the same files. Don't duplicate fixtures.

### Jurisdiction (England / Wales / Scotland)
- `Property.Jurisdiction` drives the legal rules. Never hard-code a rule with an `if (England)` in an endpoint or the UI. Put it in the jurisdiction rules component (introduced in Phase 2) with an **effective-from date**.
- Use the right terminology per country (Wales: *contract-holder*, *occupation contract*; Scotland: *PRT*, *Notice to Leave*).
- **Do not invent legal rules.** A legal value (deadline, cap, notice period) must cite its source in the spec. If unsure, leave a `TODO(legal-verify)` and flag it to the user.

### Money and dates
- Money is `decimal` in GBP with explicit rounding (`MidpointRounding.AwayFromZero`) and never `double`. Postgres uses `numeric(18,2)`.
- Use `DateOnly` for legal dates (tenancy start, deadlines) and `DateTimeOffset` (UTC) for timestamps.
- Working-day calculations must use the jurisdiction's bank-holiday calendar (Scotland differs).

### API
- Minimal API endpoint groups under `/api/...`, one file per feature in `TenancyManager.Api/Endpoints`.
- Errors use RFC 7807 `ProblemDetails`. Validation errors return 400 with field errors.
- Every endpoint except `/health` requires authentication, and every `/api` endpoint requires an agency role (`AgencyAdmin`, `Agent`, `Accounts`, `ReadOnly`).
- `src/api/TenancyManager.Api/openapi/openapi.json` is committed and generated **only** on demand (`OpenApiGenerateDocumentsOnBuild=true`, which `npm run api:generate` sets). When the API surface changes, run `npm run api:generate` in `src/web` and commit both the JSON and `src/web/src/api/generated`. CI fails if either is stale.
- JSON uses string enums and strict number handling, so integers stay `integer` in OpenAPI.

### Web
- Call the API only through the generated client in `src/web/src/api/generated` (wrapped by TanStack Query hooks). No ad-hoc `fetch`.
- `VITE_AUTH_MODE=dev` uses a role picker that sends `X-Dev-User`/`X-Dev-Roles`. `entra` uses MSAL.
- Accessibility target is WCAG 2.2 AA: label every form control and keep everything keyboard-navigable.

### Security and privacy
- **Dev auth must never run in Production.** The API refuses to start if `Auth:Mode=Dev` and the environment is Production.
- No secrets in git. Use `dotnet user-secrets` locally and Key Vault plus Managed Identity in Azure.
- Treat tenant and landlord data as personal data (UK GDPR). Don't log names, emails, bank details or full addresses.
- Future payment and deposit integrations go behind adapter interfaces with stub implementations, idempotency keys and webhook signature verification.

## Environment gotchas
- **Windows Smart App Control** on the main dev machine blocks some locally built, unsigned .NET assemblies ("An Application Control policy has blocked this file", 0x800711C7). This affects `dotnet test`, `dotnet run`, `dotnet ef` and OpenAPI generation, but not `dotnet build`. It is not a code bug. Run those in the dev container or rely on CI. Don't try to work around it by changing OS security settings.
- Docker isn't installed on that machine, so Postgres contract tests skip locally (`REQUIRE_DOCKER=1` in CI makes them mandatory).
- Pin OpenSpec: `npx @fission-ai/openspec@2.1.1 ...` (set `OPENSPEC_TELEMETRY=0`).
- NuGet "latest" now resolves to .NET 11 packages. Pin Microsoft.* / EF Core / Npgsql packages to **10.x** in `Directory.Packages.props`.

## Coding style
- C#: nullable enabled, warnings as errors, file-scoped namespaces, `sealed` by default, records for DTOs. Central package versions live in `Directory.Packages.props`.
- TypeScript: `strict`, oxlint clean (`npm run lint`), functional components, no `any`.
- Keep comments for *why*, not *what*.

## Roadmap (from the agreed plan)
0. Foundations ← **current** (`openspec/changes/phase-0-foundations`)
1. Core records: landlords (with country registration numbers), properties (with jurisdiction), tenants
2. Jurisdiction rules engine + tenancies
3. Deposits (manual, then stub, then a real scheme sandbox)
4. Compliance certificates and reminders
5. Money: ledger, Open Banking pay-by-bank, reconciliation, Direct Debit, landlord statements
6. Maintenance and documents
7. SaaS hardening: multi-agency onboarding, External ID / multi-tenant Entra, Postgres row-level security, billing, audit, GDPR tooling
