# TenancyManager

Tenancy and landlord management for UK letting agents, covering England, Wales and Scotland.
React + TypeScript front end, .NET 10 REST API, and PostgreSQL (or a JSON stub). Built spec-first with
OpenSpec and BDD (Reqnroll + Playwright). Hosted on Azure.

Conventions and workflow for contributors (and Claude Code) are in [CLAUDE.md](CLAUDE.md).
Behaviour specs are in [openspec/](openspec/). The current change is
[phase-0-foundations](openspec/changes/phase-0-foundations/proposal.md).

## Quick start (no database, no Entra)

Prerequisites: .NET SDK 10, Node 22+.

```bash
dotnet run --project src/api/TenancyManager.Api        # API on http://localhost:5080 (docs: /scalar)
cd src/web && npm install && npm run dev               # web on http://localhost:5173
```

Sign in with the Dev role picker. Data comes from [seed/](seed/) (fictional agencies "Northgate Lettings"
and "Southbank Homes").

## Running against PostgreSQL

```bash
docker compose up -d postgres
DataProvider__Type=Postgres dotnet run --project src/api/TenancyManager.Api
```

The API applies migrations and seeds an empty database from the same `seed/` files at start-up.
`docker compose up --build` runs the whole stack (Postgres + API + web) on http://localhost:8080.

## Tests

| What | Command |
|---|---|
| Unit, acceptance (Reqnroll), provider contract tests | `dotnet test` (CI: [backend.yml](.github/workflows/backend.yml)) |
| Web unit tests, lint, types | `cd src/web && npm test && npm run lint && npm run typecheck` (CI: [frontend.yml](.github/workflows/frontend.yml)) |
| End-to-end (starts API + web; CI: [quality.yml](.github/workflows/quality.yml)) | `cd tests/e2e && npm ci && npx playwright install chromium && npx playwright test` |
| Spec validation | `npx @fission-ai/openspec@2.1.1 validate --all --strict` |
| Requirement → test traceability | `node scripts/check-traceability.mjs --verbose` |

Postgres contract tests use Testcontainers. Without Docker they are skipped locally, but CI requires them.

> **Windows + Smart App Control:** if `dotnet test` or `dotnet run` fails with *"An Application Control
> policy has blocked this file"*, Windows Smart App Control is blocking the unsigned assemblies you
> built locally. Use the dev container ([.devcontainer/](.devcontainer/)) or Codespaces, or turn off
> Smart App Control (Windows Security → App & browser control). That is your decision to make.

## Entra ID (single tenant)

1. Run [scripts/entra/register-apps.ps1](scripts/entra/register-apps.ps1) `-TenantId <your-tenant-id> -AssignMeRole AgencyAdmin`.
2. Put the printed values in API user-secrets and `src/web/.env.local` (see [.env.example](src/web/.env.example)).
3. Run the API with `Auth__Mode=Entra` and the web with `VITE_AUTH_MODE=entra`.

Agency roles: `AgencyAdmin`, `Agent`, `Accounts`, `ReadOnly`. Assign them under Enterprise applications → TenancyManager-API → Users and groups.

## Azure

First time? Follow the step-by-step guide in [docs/azure-deployment.md](docs/azure-deployment.md).

```powershell
azd env new dev
azd env set AZURE_LOCATION uksouth
azd env set ENTRA_TENANT_ID <id>; azd env set ENTRA_API_CLIENT_ID <id>; azd env set ENTRA_WEB_CLIENT_ID <id>
azd env set POSTGRES_ADMIN_PASSWORD <strong-password>
azd up                                                  # defaults to UK South
```

This provisions Container Apps (API), Static Web Apps (web), PostgreSQL Flexible Server, Key Vault,
Container Registry, Log Analytics and App Insights ([infra/](infra/)). The API image is built inside Azure
Container Registry, so local Docker isn't needed. [.github/workflows/deploy.yml](.github/workflows/deploy.yml)
does the same from GitHub using OIDC.
