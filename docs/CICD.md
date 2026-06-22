# CI/CD — GitHub Actions

Workflows:

- [`.github/workflows/ci.yml`](../.github/workflows/ci.yml) — PRs and feature branches: `dotnet test` + Docker build (no deploy)
- [`.github/workflows/build-and-deploy-tvs.yml`](../.github/workflows/build-and-deploy-tvs.yml) — `main` / `dev`: migrate DB (via tvs-sqlscript, gated), then build, push ACR, deploy Container Apps + Functions, then post-deploy regression + Leave E2E

## Database migrations (tvs-sqlscript)

| Repo branch | GitHub Environment | What runs |
|-------------|-------------------|-----------|
| `dev` | `saas-dev` | EF Core `deploy` (all modules incl. `human_resource`) |
| `main` | `saas-prod` | EF Core `deploy` (all modules incl. `human_resource`) |

Schema lives in [tvs-sqlscript](https://github.com/deladetech1/tvs-sqlscript). **This repo never runs migrations itself** — the deploy pipeline *calls* the central migrator's reusable workflow (`tvs-sqlscript/.github/workflows/migrate.yml`, module `human_resource`) **before** rolling out the new image, and the rollout is **gated on that migration succeeding**. The migrator (`tvs_migrator`) owns the schema; the app connects with a per-role CRUD login.

When both repos change: just push ZelosHR — the deploy migrates `tvs-sqlscript@main` first, so merge tvs-sqlscript before deploying ZelosHR. Manual DB commands (rollback, enterprise, `migrations-list`): tvs-sqlscript workflow **Database (EF Core dispatch)**.

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
| `TROVESUITE_SECRET_KEY` | Optional — same HS256 value as Core Platform `SECRET_KEY` (≥ 32 chars). When set, deploy workflow syncs **`SECRET_KEY`** on the ZelosHR Container App. Also used to **mint JWTs** for Leave E2E in CI. |

### Leave E2E (Hurl)

After a successful **`dev`** deploy, CI can run `tests/e2e/leave/*.hurl` and upload an **HTML report** (full curl request/response per step). Configure these **repository secrets** (values from dev Trove session / `generate-live-session.sh --mint`):

| Secret | Purpose |
|--------|---------|
| `ZELOSHR_E2E_USER_ID` | Platform user id (`cp_users.id`) for JWT `user_id` |
| `ZELOSHR_E2E_TENANT_ID` | JWT `tenant_id` (must match org/bus/loc) |
| `ZELOSHR_E2E_ORG_ID` | Trove `org-id` header |
| `ZELOSHR_E2E_BUS_ID` | Trove `bus-id` header |
| `ZELOSHR_E2E_LOC_ID` | Trove `loc-id` header |
| `ZELOSHR_E2E_EMAIL` | Optional — JWT claim (default `ci-e2e@deladetech.com`) |
| `ZELOSHR_E2E_FULLNAME` | Optional — JWT claim (default `CI E2E`) |

If any required secret is missing, the **Leave E2E** job is skipped with a warning (deploy stays green).

**Local (same scenarios + report):**

```bash
./scripts/local-dev/test-leave-e2e.sh --open
```

**CI artifact:** `leave-e2e-report` → `html/index.html`, `junit.xml`.

See [tests/e2e/README.md](../tests/e2e/README.md).

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
| **CI** (`build-and-deploy-tvs.yml`) | `./scripts/ci/regression-test.sh` + optional **Leave E2E** (`./scripts/ci/leave-e2e.sh`, artifact `leave-e2e-report`) |
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
