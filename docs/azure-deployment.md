# Deploying TenancyManager to Azure: a first-timer's guide

This guide takes you from "I have an Azure account" to "every push to `main` deploys TenancyManager to
Azure". It assumes no Azure experience. Run the commands in **PowerShell** from the repository root
(`C:\Development\TenancyManager`) unless a step says otherwise.

Expect about **45 minutes** the first time. After that, deployments happen on their own.

---

## 0. Ideas you need first

| Term | What it means here |
|---|---|
| **Tenant** (Microsoft Entra ID directory) | Your organisation's identity directory: the list of users who can sign in. Your Azure account came with one, usually called *Default Directory*. |
| **Subscription** | The billing container. Everything you create is charged to it. |
| **Resource group** | A folder for related resources. The deployment creates `rg-tenancymanager-dev`. Deleting the folder deletes everything inside it. |
| **Region** | The data centre. We use **UK South** (London). |
| **App registration** | How you tell Entra ID "this application exists". We create three: the API, the web app, and one that GitHub uses to deploy. |
| **OIDC / federated credential** | Lets GitHub Actions sign in to Azure **without a stored password**. Azure is told: "trust tokens from GitHub for repo *s4praveen/TenancyManager*, environment *dev*". |
| **GitHub Container Registry (ghcr.io)** | Where GitHub Actions stores the API's Docker image. Free for public packages. Azure downloads the image from there. |
| **Bicep** | The files in `infra/` that describe the Azure resources as code. |

### How a deployment works

```
 git push main
      │
      ▼
 GitHub Actions: .github/workflows/deploy.yml
  1. image  ─ docker build ─► ghcr.io/s4praveen/tenancymanager-api:<commit>
  2. infra  ─ sign in to Azure (OIDC) ─► az deployment sub create (infra/*.bicep)
             └─ creates/updates the resources and points the Container App at the new image
  3. web    ─ npm run build (with the API URL + Entra ids) ─► upload to the Static Web App
```

### What gets created (the PoC setup)

| Resource | Purpose | Approx. cost |
|---|---|---|
| Container App (API) | Runs the API. It scales to zero when idle. | Usually within the free monthly grant |
| Static Web App, Free tier | Hosts the React site | Free |
| Log Analytics + App Insights | Logs and monitoring | Pennies at PoC volumes (the first 5 GB/month is free) |
| Managed identity | The API's own Azure identity | Free |

**Expect roughly £0–1/month.** The API serves the fictional seed data from JSON files baked into its
image (`DataProvider=Json`), so no database is needed. That's fine while the app only reads data. Anything a
user changed would be lost on restart, but nothing can be changed yet.

When you need a real database (Phase 1 adds editing), set `USE_POSTGRES=true`: see
[Turning on PostgreSQL](#later-turning-on-postgresql). It adds PostgreSQL (~£13–15/month) and Key Vault.

> The Static Web App is created in **West Europe**, because Static Web Apps isn't offered in UK regions.
> It serves only the website's files, never tenant data.

---

## 1. Install and sign in

You already have the **Azure CLI** (`az`), **Git**, **.NET 10** and **Node.js**. Nothing else is needed
on your PC. Docker isn't needed either, because GitHub builds the image.

```powershell
az login
az account show --query "{subscription:name, subscriptionId:id, tenantId:tenantId}" -o table
```

Keep the **subscriptionId** and **tenantId**. If you have several subscriptions, pick one with
`az account set --subscription "<name or id>"`.

> If `az` says *"The refresh token has expired due to inactivity"*, just run `az login` again.

### Set a budget alert (2 minutes, recommended)

Portal ([portal.azure.com](https://portal.azure.com)) → search **Cost Management** → **Budgets** → **Add**.
Use scope *your subscription*, an amount of £10/month, and an alert at 80% to your email. Azure then emails you
before anything costs more than expected.

---

## 2. Prepare the subscription (one-off)

Azure has to be told which services your subscription will use, which is called "registering resource
providers":

```powershell
"Microsoft.App","Microsoft.OperationalInsights","Microsoft.Insights","Microsoft.Web",
"Microsoft.ManagedIdentity","Microsoft.KeyVault","Microsoft.DBforPostgreSQL" |
  ForEach-Object { az provider register --namespace $_ }
```

It takes a few minutes. Check with:

```powershell
az provider show -n Microsoft.App --query registrationState -o tsv
```

Continue when it says `Registered`.

---

## 3. Create the sign-in app registrations (one-off)

TenancyManager needs two app registrations:

- **TenancyManager-API** defines the API's permission (`access_as_user`) and the **roles**
  `AgencyAdmin`, `Agent`, `Accounts` and `ReadOnly`. The API only lets in users who hold a role.
- **TenancyManager-Web** is the React app. Users sign in through it, and it asks for a token for the API.

```powershell
powershell -File ./scripts/entra/register-apps.ps1 -TenantId <tenant-id> -AssignMeRole AgencyAdmin -GrantAdminConsent
```

What the switches do:
- `-AssignMeRole AgencyAdmin` gives **you** a role, so you can sign in after deployment.
- `-GrantAdminConsent` approves the web app's access to the API for everyone in the tenant, so users
  don't see a consent prompt.

It prints `ENTRA_TENANT_ID`, `ENTRA_API_CLIENT_ID` and `ENTRA_WEB_CLIENT_ID`. **Keep them** for step 5.

---

## 4. Let GitHub deploy to Azure (one-off)

This creates a third app registration, **TenancyManager-GitHub-Deploy**. It trusts GitHub's
`dev` environment for this repository, and it may create resources in your subscription (the *Contributor* role).

```powershell
powershell -File ./scripts/azure/setup-github-deploy.ps1 -SubscriptionId <subscription-id> -Repo s4praveen/TenancyManager
```

It prints `AZURE_CLIENT_ID`, `AZURE_TENANT_ID` and `AZURE_SUBSCRIPTION_ID`. **Keep them**.

---

## 5. Configure GitHub (one-off)

### 5a. Create the `dev` environment
GitHub → your repository → **Settings → Environments → New environment** → name it `dev` → **Configure environment**.
(Later you can add *Required reviewers* here, so a deployment waits for your approval.)

### 5b. Add the six variables
**Settings → Secrets and variables → Actions → Variables tab → New repository variable**, once for each:

| Name | Value from |
|---|---|
| `AZURE_CLIENT_ID` | step 4 |
| `AZURE_TENANT_ID` | step 4 |
| `AZURE_SUBSCRIPTION_ID` | step 4 |
| `ENTRA_TENANT_ID` | step 3 |
| `ENTRA_API_CLIENT_ID` | step 3 |
| `ENTRA_WEB_CLIENT_ID` | step 3 |

They are **variables**, not secrets, because none of them is a password. They're ids, and Azure only accepts
them alongside GitHub's own signed token. Optional: `AZURE_LOCATION` (default `uksouth`) and `USE_POSTGRES`
(default `false`).

> Until `AZURE_CLIENT_ID` exists, the workflow only builds the image and skips the Azure jobs. That's why
> it didn't fail before you set this up.

---

## 6. First deployment

1. Push your code to `main`, or go to **Actions → Deploy (CD) → Run workflow → dev → Run workflow**.
2. Open the run. You'll see three jobs: **Build + push API image**, **Deploy Azure resources + API**, and
   **Build + deploy web**. The first run takes about 10 minutes.

### The first run will probably stop at "Image is pullable by Azure"; that's expected

GitHub makes a new container package **private** the first time it's pushed, so Azure can't download it yet.
Make it public, once:

1. GitHub → your profile picture → **Your profile → Packages** (or the repo's right-hand side bar → *Packages*)
   → **tenancymanager-api**.
2. **Package settings** (bottom right) → **Danger Zone → Change visibility → Public** → type the name to confirm.
3. Back in the failed run → **Re-run failed jobs**.

Making it public is reasonable here because the repository itself is public, the image holds no
secrets, and its data is fictional. If you'd rather keep it private, create a **classic** personal access
token with only `read:packages` and add it as the repository **secret** `GHCR_PULL_TOKEN`. Azure then
signs in to ghcr.io with it. Classic tokens expire, so you'd need to renew it.

When it succeeds, the run's **Summary** page shows the **Web** and **API** addresses.

---

## 7. Allow sign-in from the new website address (one-off)

Entra ID only sends users back to addresses it knows ("redirect URIs"). Add the Web address from the run
summary:

```powershell
powershell -File ./scripts/entra/register-apps.ps1 -TenantId <tenant-id> -SpaRedirectUris http://localhost:5173, https://<name>.azurestaticapps.net
```

---

## 8. Test it

1. Open `https://<api-address>/health` in a browser. It should return `{"status":"Healthy",...}`. The workflow already checked this.
   The first call after a quiet spell takes 10–20 seconds, because the API wakes up from zero.
2. Open `https://<api-address>/api/properties`. It should return **401**. That's correct, because you aren't signed in.
3. Open the **Web** address → **Sign in with Microsoft** → use your account.
4. You should see **Northgate Lettings** in the header and **4 properties** in the table.

### Letting other people in
1. Portal → **Microsoft Entra ID → Users → New user** (or invite a guest).
2. Portal → **Enterprise applications → TenancyManager-API → Users and groups → Add user/group** → choose a
   **role** → **Assign**.

---

## 9. Day to day

| I want to... | Do this |
|---|---|
| Deploy a change | Push to `main`. Changes under `src/`, `seed/` or `infra/` trigger **Deploy (CD)**. |
| Deploy by hand | Actions → **Deploy (CD)** → **Run workflow** |
| See the API's logs | `az containerapp logs show -g rg-tenancymanager-dev -n (az containerapp list -g rg-tenancymanager-dev --query "[0].name" -o tsv) --type console --follow` |
| See what's deployed | Portal → **Resource groups → rg-tenancymanager-dev** |
| **Delete everything** | `az group delete -n rg-tenancymanager-dev --yes` (the next deployment recreates it) |
| Tidy old images | GitHub → Packages → tenancymanager-api → *Manage versions* |

Every deployment uses an image tagged with the exact commit id, so you can always tell what's running:
Portal → the Container App → **Containers**.

---

## Later: turning on PostgreSQL

1. Re-run step 4 with `-AllowRoleAssignments`. The database's Key Vault grants the API access to its secret,
   which needs that extra permission.
2. Add the repository variable `USE_POSTGRES` = `true`.
3. Add the repository **secret** `POSTGRES_ADMIN_PASSWORD`: 12+ characters with upper and lower case, a
   number and a symbol. It's stored in Key Vault, and you won't need to type it again.
4. Run **Deploy (CD)**. PostgreSQL takes 10–15 minutes to create the first time. On start-up the API creates the
   tables and loads the same seed data.

Setting `USE_POSTGRES` back to `false` points the API back at the JSON data, but it **doesn't delete** the
database server. Delete it in the portal to stop its charges.

---

## Troubleshooting

| Symptom | Likely cause → fix |
|---|---|
| **Image is pullable by Azure** fails | The package is private. See step 6. |
| `AADSTS700213` / `AADSTS70021: No matching federated identity record` | The federated credential doesn't match the subject GitHub presented. The error message shows that subject, for example `repo:owner@123/Repo@456:environment:dev`, which is GitHub's newer *immutable id* format. Add it exactly: App registrations → TenancyManager-GitHub-Deploy → Certificates & secrets → Federated credentials → Add → **Other issuer**, issuer `https://token.actions.githubusercontent.com`, then paste the subject. (Re-running the step-4 script also adds it.) |
| `AuthorizationFailed` during **az deployment** | Step 4's role assignment hasn't taken effect yet (wait 5 minutes and re-run), or it was run on a different subscription. |
| `The subscription is not registered to use namespace 'Microsoft.X'` | Step 2 isn't finished. |
| `RoleAssignmentUpdateNotPermitted` / `does not have authorization to perform action 'Microsoft.Authorization/roleAssignments/write'` | You turned on Postgres without `-AllowRoleAssignments` (see *Later*). |
| Deploy jobs show as **skipped** | `AZURE_CLIENT_ID` isn't a *repository* variable (step 5b). An *environment* variable isn't visible to the check that decides whether to run. |
| Sign-in error **AADSTS50011** (redirect URI mismatch) | Step 7 wasn't done, or the URL differs. |
| Page says **"Could not load properties"** | Your user has no role (403). See *Letting other people in*. |
| Browser console shows a **CORS** error | The API only accepts calls from the Static Web App address. Re-run **Deploy (CD)**. |
| **API health** step fails | Run the log command in step 9. The real error is usually in the first lines. |

---

## Deploying from your PC (optional)

The workflow is just `az deployment sub create`, so you can run it yourself once an image exists in ghcr.io:

```powershell
$env:ENVIRONMENT_NAME = 'dev'
$env:ENTRA_TENANT_ID = '<id>'; $env:ENTRA_API_CLIENT_ID = '<id>'
$env:API_IMAGE = 'ghcr.io/s4praveen/tenancymanager-api:<commit-sha>'
az deployment sub create --location uksouth --parameters infra/main.bicepparam
```
