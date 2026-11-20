# Tasks: Phase 0 — Foundations

> Status note (2026-11-20): all code is written. Tasks whose verification needs the .NET tests or
> the running API are left unticked. Windows Smart App Control on the dev machine blocks locally built
> assemblies, so they await the first CI or dev-container run.

## 1. Repository and tooling
- [x] 1.1 Add `.gitignore`, `.editorconfig`, `global.json`, `Directory.Build.props`, `Directory.Packages.props`, dotnet tool manifest (verify: `dotnet build` succeeds with warnings as errors)
- [x] 1.2 Create the solution with Domain, Application, Infrastructure and Api projects plus Unit, Acceptance and Integration test projects (verify: `dotnet build TenancyManager.slnx`)

## 2. Domain, seed data and providers
- [ ] 2.1 Domain: `Agency`, `AgencyOwnedEntity`, `Landlord`, `Property`, `Jurisdiction` (verify: unit tests)
- [ ] 2.2 Seed files for Northgate Lettings (4 properties across England, Wales and Scotland) and Southbank Homes (verify: files load in `SeedReader` unit test)
- [x] 2.3 Application: repository interfaces, `ITenantContext`, property query service (verify: build)
- [ ] 2.4 Json provider: `JsonDataStore`, repositories scoped by agency (verify: Json contract tests pass — AGY-001, PRP-001)
- [ ] 2.5 Postgres provider: `TenancyDbContext` with agency query filters, initial migration, `PostgresSeeder` (verify: Postgres contract tests pass under Docker — DAT-002; unit test that every agency-owned entity is filtered)
- [ ] 2.6 Provider selection with fail-fast on an unknown value (verify: Reqnroll DAT-001)

## 3. API
- [ ] 3.1 Auth: Entra and Dev modes, Production guard, `AgencyUser` policy (verify: Reqnroll IAM-001, IAM-002, IAM-004)
- [ ] 3.2 Endpoints: `/health`, `/api/me`, `/api/properties`, `/api/properties/{id}` with ProblemDetails (verify: Reqnroll SYS-001, IAM-003, PRP-001..003, AGY-001..002)
- [x] 3.3 OpenAPI generated on demand and Scalar UI in Development (verify: `openapi/openapi.json` produced with `-p:OpenApiGenerateDocumentsOnBuild=true`)

## 4. Web
- [x] 4.1 Vite React TS app with MUI, Router, TanStack Query, oxlint (verify: `npm run build`, `npm run lint`)
- [x] 4.2 Orval client generated from the committed OpenAPI file (verify: `npx orval` produces `src/api/generated`)
- [x] 4.3 Auth providers: Dev role picker and MSAL (verify: Vitest test for the Dev provider headers)
- [x] 4.4 Header showing current user and agency, and a properties page (verify: Vitest render test)
- [ ] 4.5 Playwright smoke test: pick Agent role, see 4 properties (verify: `npx playwright test`)

## 5. Delivery
- [ ] 5.1 Dockerfiles (api, web) and `docker-compose.yml` with postgres (verify: `docker compose config` and `docker compose up --build`)
- [x] 5.2 Bicep + `azure.yaml` for Azure (verify: `az bicep build --file infra/main.bicep`)
- [ ] 5.3 GitHub Actions CI: build, tests, OpenSpec validate, traceability, web, e2e (verify: first green CI run)
- [x] 5.4 `scripts/check-traceability.mjs` (verify: passes with all Phase 0 IDs tagged)
- [x] 5.5 `scripts/entra/register-apps.ps1` and README setup docs (verify: script reviewed; not executed)

## Workflow follow-up
- Once all tasks are ticked, archive with `/openspec-archive-change phase-0-foundations`, which creates `openspec/specs/*`.
