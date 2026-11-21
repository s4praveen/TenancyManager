# Deploying TenancyManager to Azure: a first-timer's guide

This guide takes you from "I have an Azure account" to "TenancyManager is running in Azure and I can
sign in". It assumes no Azure experience. Run the commands in **PowerShell** from the repository root
(`C:\Development\TenancyManager`) unless a step says otherwise.

Expect about **45–60 minutes** the first time, most of it waiting for Azure.

---

## 0. Ideas you need first

| Term | What it means here |
|---|---|
| **Tenant** (Microsoft Entra ID directory) | Your organisation's identity directory: the list of users who can sign in. Your Azure account came with one, usually called *Default Directory*. TenancyManager lets **users in this tenant** sign in. |
| **Subscription** | The billing container. Everything you create is charged to it. Yours is *Azure subscription 1*. |
| **Resource group** | A folder for related resources. We create `rg-tenancymanager-dev`. Deleting the folder deletes everything inside it. |
| **Region** | The data centre. We use **UK South** (London) so UK tenant data stays in the UK. |
| **App registration** | How you tell Entra ID "this application exists". It's needed so users can sign in and the web app can call the API. |
| **azd** (Azure Developer CLI) | A tool that reads `azure.yaml` and `infra/*.bicep`, creates all the resources, then builds and uploads the code. |
| **Bicep** | The files in `infra/` that describe the Azure resources as code. |

### What gets created

```
                    Users' browsers
                          │
        ┌─────────────────┴──────────────────┐
        ▼                                    ▼
 Static Web App (React SPA)        Microsoft Entra ID (sign-in)
        │  HTTPS + access token
        ▼
 Container App  ── API (.NET 10) ──► PostgreSQL Flexible Server
        │                               ▲
        │ managed identity              │ connection string
        ├──► Key Vault ─────────────────┘ (stored as a secret)
        ├──► Container Registry (holds the API image)
        └──► Log Analytics / Application Insights (logs)
```

| Resource | Purpose | Approx. cost (dev size) |
|---|---|---|
| Container App (API) | Runs the API, scales to zero when idle | Usually within the free monthly grant |
| Static Web App, Free tier | Hosts the React site | Free |
| PostgreSQL Flexible Server, B1ms | Database | The main cost: roughly £10–15/month |
| Container Registry, Basic | Stores the API image | About £4/month |
| Key Vault | Stores the database password | Pennies |
| Log Analytics + App Insights | Logs and monitoring | Usually pennies at dev volumes |

> Prices change. Check the [Azure pricing calculator](https://azure.microsoft.com/pricing/calculator/).
> When you've finished experimenting, delete everything with `azd down` (step 10) and the charges stop.

> The Static Web App is created in **West Europe**, because Static Web Apps isn't offered in UK regions.
> It serves only the website's files, never tenant data. Everything that holds data stays in UK South.

---

## 1. Install the tools (one-off)

You already have the **Azure CLI** (`az`), **Git**, **.NET 10** and **Node.js**. You need two more:

```powershell
winget install Microsoft.Azd          # Azure Developer CLI
winget install Microsoft.PowerShell   # PowerShell 7 ("pwsh"), used by a deployment hook
```

**Close and reopen your terminal** (or VS Code) so the new commands are found, then check:

```powershell
azd version
pwsh -v
az version
```

Docker is **not** needed: the API image is built inside Azure Container Registry.

---

## 2. Sign in

`az` and `azd` keep separate sign-ins, so sign in to both:

```powershell
az login
azd auth login
```

Each opens a browser window. Use the account that owns your Azure subscription.

Now find your **tenant ID** and **subscription ID**. You'll need them later:

```powershell
az account show --query "{subscription:name, subscriptionId:id, tenantId:tenantId}" -o table
```

If you have more than one subscription, choose the one to use:

```powershell
az account set --subscription "<subscription name or id>"
```

---

## 3. Prepare the subscription (one-off)

Azure has to be told which services your subscription will use, which is called "registering resource
providers". New subscriptions often have only a few registered. Run:

```powershell
"Microsoft.App","Microsoft.ContainerRegistry","Microsoft.DBforPostgreSQL","Microsoft.KeyVault",
"Microsoft.OperationalInsights","Microsoft.Insights","Microsoft.Web","Microsoft.ManagedIdentity" |
  ForEach-Object { az provider register --namespace $_ }
```

Registration takes a few minutes. Check progress with:

```powershell
az provider list --query "[?contains('Microsoft.App Microsoft.DBforPostgreSQL Microsoft.Web', namespace)].{ns:namespace, state:registrationState}" -o table
```

Continue when they all say `Registered`.

---

## 4. Create the Entra ID app registrations (one-off)

TenancyManager needs two app registrations:

- **TenancyManager-API** defines the API's permission (`access_as_user`) and the **roles**
  `AgencyAdmin`, `Agent`, `Accounts` and `ReadOnly`. The API only lets in users who hold a role.
- **TenancyManager-Web** is the React app. Users sign in through it, and it asks for an access token for the API.

The repository has a script that creates both. Replace `<tenant-id>` with the value from step 2:

```powershell
pwsh ./scripts/entra/register-apps.ps1 -TenantId <tenant-id> -AssignMeRole AgencyAdmin -GrantAdminConsent
```

What the switches do:
- `-AssignMeRole AgencyAdmin` gives **you** a role, so you can sign in after deployment. Without a role
  the API answers *403 Forbidden* and the site shows "Could not load properties".
- `-GrantAdminConsent` approves the web app's request to call the API on behalf of everyone in the
  tenant, so users don't see a consent prompt. You can do this because you're the tenant's administrator.

At the end the script prints three values. **Keep them**:

```
ENTRA_TENANT_ID     = ...
ENTRA_API_CLIENT_ID = ...
ENTRA_WEB_CLIENT_ID = ...
```

To see what was created: [Azure portal](https://portal.azure.com) → search **"App registrations"** → *All applications*.

---

## 5. Create the azd environment

An **azd environment** is a named set of settings for one deployment (`dev`, `test`, `prod`, ...). It's
stored in the `.azure/` folder, which is git-ignored because it holds values specific to you.

```powershell
azd env new dev
```

If it asks, choose your subscription and the **uksouth** location. Then set the values it needs:

```powershell
azd env set AZURE_LOCATION uksouth
azd env set AZURE_SUBSCRIPTION_ID <subscription-id>

azd env set ENTRA_TENANT_ID     <value from step 4>
azd env set ENTRA_API_CLIENT_ID <value from step 4>
azd env set ENTRA_WEB_CLIENT_ID <value from step 4>
```

Choose a **database admin password**: at least 12 characters with upper case, lower case, a number and a
symbol. You won't type it anywhere else, because it's stored in Key Vault and the API reads it from there.

```powershell
azd env set POSTGRES_ADMIN_PASSWORD "<a-strong-password>"
```

Check everything is set (the values are shown, so don't share this output):

```powershell
azd env get-values
```

---

## 6. Create the Azure resources (`azd provision`)

```powershell
azd provision
```

What happens:
1. azd compiles `infra/main.bicep` and sends it to Azure.
2. Azure creates the resource group and everything in it. **This takes 10–15 minutes.** PostgreSQL is the slowest.
3. azd saves the outputs (`API_URL`, `WEB_URL`, ...) into your environment.

While it runs, you can watch progress in the portal: **Resource groups → rg-tenancymanager-dev → Deployments**.

---

## 7. Build and upload the code (`azd deploy`)

```powershell
azd deploy
```

What happens:
- **api**: azd sends the source code to Container Registry, which builds the Docker image
  (`src/api/Dockerfile`). azd then points the Container App at the new image. On start-up the API
  applies the database migrations and loads the fictional seed data (Northgate Lettings and Southbank Homes).
- **web**: a hook (`scripts/azd/write-web-env.ps1`) writes the site's settings (API address, Entra IDs)
  into `src/web/.env.production.local`. azd then builds the React app and uploads it to the Static Web App.

> Next time you can do steps 6 and 7 in one go with `azd up`.

When it finishes, azd prints the endpoints. You can also get them with:

```powershell
azd env get-value WEB_URL
azd env get-value API_URL
```

---

## 8. Allow sign-in from the new website address

Entra ID only sends users back to addresses it knows ("redirect URIs"). So far it only knows
`http://localhost:5173`. Add the Static Web App address by re-running the script with both URLs:

```powershell
$web = azd env get-value WEB_URL
pwsh ./scripts/entra/register-apps.ps1 -TenantId <tenant-id> -SpaRedirectUris http://localhost:5173, $web
```

---

## 9. Test it

1. **API health check.** This should return `{"status":"Healthy","dataProvider":"Postgres"}`:
   ```powershell
   Invoke-RestMethod "$(azd env get-value API_URL)/health"
   ```
   The first call can take 10–20 seconds, because the API scales to zero when idle and has to wake up.
2. **Without signing in**, `/api/properties` should be refused with 401. That's correct.
3. Open the **WEB_URL** in your browser → **Sign in with Microsoft** → use your account.
4. You should see **Northgate Lettings** in the header and **4 properties** in the table.

### Letting other people in

1. Portal → **Microsoft Entra ID → Users → New user → Create new user**. You can also invite an external email as a guest.
2. Portal → **Enterprise applications → TenancyManager-API → Users and groups → Add user/group**.
3. Pick the user and a **role** (`Agent`, `Accounts`, `ReadOnly` or `AgencyAdmin`) → **Assign**.

---

## 10. Updating and cleaning up

| I want to... | Command |
|---|---|
| Deploy new code after changes | `azd deploy` (or `azd deploy api` / `azd deploy web`) |
| Apply infrastructure changes | `azd provision` |
| See API logs live | `az containerapp logs show -g rg-tenancymanager-dev -n (az containerapp list -g rg-tenancymanager-dev --query "[0].name" -o tsv) --follow` |
| Open the resources in the portal | `azd show` |
| **Delete everything and stop all charges** | `azd down --purge` |

`--purge` also permanently removes the soft-deleted Key Vault, so you can recreate the environment
later. For a `prod` environment purge protection is on, so its Key Vault can't be purged.

---

## Troubleshooting

| Symptom | Likely cause → fix |
|---|---|
| `The subscription is not registered to use namespace 'Microsoft.X'` | Step 3 isn't finished. Register the provider and wait for `Registered`. |
| `LocationIsOfferRestricted` / `The location is restricted` for PostgreSQL | Some subscription types (free trials, for example) can't create PostgreSQL in some regions. Try `azd env set AZURE_LOCATION ukwest` and run `azd provision` again. |
| Provision fails on the Container App with a **Key Vault secret** or **registry** access error | Newly granted permissions can take a few minutes to take effect. Wait 2–3 minutes and run `azd provision` again. |
| `pwsh is not recognized` during `azd deploy` | PowerShell 7 isn't installed or the terminal wasn't reopened. See step 1. |
| Sign-in error **AADSTS50011** (redirect URI mismatch) | Step 8 wasn't done, or the URL differs. Re-run it with the exact `WEB_URL`. |
| Sign-in works but the page says **"Could not load properties"** | Your user has no role (the API returns 403). Assign one (step 9, "Letting other people in"). |
| Browser console shows a **CORS** error | The API only accepts calls from the Static Web App address. Run `azd provision` again so the API picks up the current `WEB_URL`. |
| `/health` times out on the first call | The API is waking up from zero. Wait 20 seconds and retry. |
| Anything else | `az containerapp logs show ... --follow` (see step 10) usually shows the real error. |

---

## Later: deploying from GitHub instead of your PC

[.github/workflows/deploy.yml](../.github/workflows/deploy.yml) runs the same `azd provision` + `azd deploy`
from GitHub Actions. Its sign-in uses OIDC: GitHub proves its identity to Azure, and no passwords are
stored. The easiest way to set this up is:

```powershell
azd pipeline config
```

It creates an identity in Azure, connects it to your GitHub repository, and stores the non-secret
settings as GitHub variables. You then add `POSTGRES_ADMIN_PASSWORD` as a GitHub **secret** and the
three `ENTRA_*` values as **variables** under *Settings → Environments → dev*, and run the
*Deploy to Azure* workflow from the Actions tab.
