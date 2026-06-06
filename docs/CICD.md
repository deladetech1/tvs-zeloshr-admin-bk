# CI/CD — GitHub Actions

Workflows:

- [`.github/workflows/ci.yml`](../.github/workflows/ci.yml) — PRs and feature branches: `dotnet test` + Docker build (no deploy)
- [`.github/workflows/build-and-deploy.yml`](../.github/workflows/build-and-deploy.yml) — `main` / `dev`: build, push ACR, deploy Container Apps + Functions

## Database migrations (tvs-sqlscript)

| Repo branch | GitHub Environment | What runs |
|-------------|-------------------|-----------|
| `dev` | `saas-dev` | EF Core `deploy` (all modules incl. `human_resource`) |
| `main` | `saas-prod` | EF Core `deploy` (all modules incl. `human_resource`) |

Schema lives in [tvs-sqlscript](https://github.com/deladetech1/tvs-sqlscript). **This repo does not migrate Postgres on deploy.**

When both repos change: **merge tvs-sqlscript first**, then ZelosHR. Manual DB commands (rollback, enterprise, `migrations-list`): tvs-sqlscript workflow **Database (EF Core dispatch)**.

| Branch | Environment | Container App | Function App | ACR image |
|--------|-------------|---------------|--------------|-----------|
| `dev` | dev | `trovesuite-dev-zeloshr-ca` | `trovesuite-dev-zeloshr-func` | `{DEV_ACR}.azurecr.io/zeloshr:{run}` |
| `main` | prod | `trovesuite-prod-zeloshr-ca` | `trovesuite-prod-zeloshr-func` | `{PROD_ACR}.azurecr.io/zeloshr:{run}` |

Create these Azure resources before the first deploy (mirror **MyStoreGuard** naming: `trovesuite-dev-mystoreguard-ca` / `trovesuite-dev-mystoreguard-func`).

If a resource is missing, the workflow **still builds and pushes the image to ACR** but **skips** deploy with a warning (job stays green).

## Repository secrets

| Secret | Used for |
|--------|----------|
| `TROVESUITE_AZURE_CLIENT_ID` | OIDC federated login |
| `AZURE_TENANT_ID` | Azure AD tenant |
| `TROVESUITE_DEV_AZURE_SUBSCRIPTION_ID` | `dev` branch deploys |
| `TROVESUITE_PROD_AZURE_SUBSCRIPTION_ID` | `main` branch deploys |
| `PACKAGES_TOKEN` | Docker build / CI — restore **Trovesuite.Package** (`read:packages` PAT). Required for PR workflow (`ci.yml`) and deploy build. |
| `TROVESUITE_SECRET_KEY` | Optional — same HS256 value as Core Platform `SECRET_KEY` (≥ 32 chars). When set, deploy workflow syncs **`SECRET_KEY`** on the ZelosHR Container App. |

## Repository variables

| Variable | Example |
|----------|---------|
| `DEV_CONTAINER_REGISTRY_NAME` | `trovesuitedevacr` |
| `PROD_CONTAINER_REGISTRY_NAME` | `trovesuiteprodacr` |
| `DEV_RESOURCE_GROUP` | `trovesuite-dev-appservers-rg` (dev Container Apps / Functions) |
| `PROD_RESOURCE_GROUP` | prod apps resource group (mirror MyStoreGuard naming) |

Same names as Core Platform if both backends share one Trovesuite subscription.

## Path filters

- **API image** rebuilds on `app/**`, `scripts/ci/**`, `nuget.config`, or workflow changes.
- **Functions** deploy only on `func/**` changes (workflow edits alone do **not** trigger Functions).
- **`workflow_dispatch`**: choose **Deploy Container App** and/or **Deploy Functions** (Functions default off).
- If the Function App does not exist in Azure (`trovesuite-dev-zeloshr-func` / `trovesuite-prod-zeloshr-func`), the workflow **skips** Functions deploy with a warning instead of failing.

## Post-deploy verification

After a successful Container App deploy, CI runs **regression tests only** (Docker Compose — no Trove JWT needed):

| Where | What |
|-------|------|
| **CI** (`build-and-deploy.yml`) | `./scripts/ci/regression-test.sh` |
| **Local** (after deploy) | `./scripts/local-dev/post-deploy-verify.sh` |

Live smoke needs a Trove Bearer token that expires — **not stored in GitHub**. Run locally:

```bash
# Once per session (or when token expires) — paste from browser DevTools:
./scripts/local-dev/refresh-live-session.sh 'eyJhbG...'

# Or auto-mint when .jwt-secret.local matches dev Container App SECRET_KEY:
./scripts/local-dev/refresh-live-session.sh --mint

# After deploy — regression + live smoke (quick)
./scripts/local-dev/post-deploy-verify.sh

# Full live suite (GET + POST + PUT + DELETE with cleanup)
./scripts/local-dev/post-deploy-verify.sh --full
```

## Local parity with CI

```bash
cp app/.env.example app/.env   # set PACKAGES_TOKEN (local Docker; same name as org secret) and/or App__ConnectionString for shared dev
./scripts/compose.sh ci        # test (compose) + docker build
```

See [LOCAL_DEV.md](LOCAL_DEV.md) for migrate, dev stack, and reset.

## Production runtime config

After the image is deployed, set Container App secrets to override empty values in [`app/appsettings.Production.json`](../app/appsettings.Production.json). Full key list: [APPCONFIG.md](APPCONFIG.md), deploy checklist: [PRODUCTION_CONFIG.md](PRODUCTION_CONFIG.md).

Functions publish (same as CI):

```bash
dotnet publish func/ZelosHR.Functions.csproj -c Release -o ./func-publish
```
