# CI/CD — GitHub Actions

Workflows:

- [`.github/workflows/ci.yml`](../.github/workflows/ci.yml) — PRs and feature branches: `dotnet test` + Docker build (no deploy)
- [`.github/workflows/build-and-deploy.yml`](../.github/workflows/build-and-deploy.yml) — `main` / `dev`: build, push ACR, deploy Container Apps + Functions

| Branch | Environment | Container App | Function App | ACR image |
|--------|-------------|---------------|--------------|-----------|
| `dev` | dev | `trovesuite-dev-zeloshr-ca` | `trovesuite-dev-zeloshr-func` | `{DEV_ACR}.azurecr.io/zeloshr:{run}` |
| `main` | prod | `trovesuite-prod-zeloshr-ca` | `trovesuite-prod-zeloshr-func` | `{PROD_ACR}.azurecr.io/zeloshr:{run}` |

Create these Azure resources (or rename the workflow outputs to match your naming) before the first deploy.

## Repository secrets

| Secret | Used for |
|--------|----------|
| `TROVESUITE_AZURE_CLIENT_ID` | OIDC federated login |
| `AZURE_TENANT_ID` | Azure AD tenant |
| `TROVESUITE_DEV_AZURE_SUBSCRIPTION_ID` | `dev` branch deploys |
| `TROVESUITE_PROD_AZURE_SUBSCRIPTION_ID` | `main` branch deploys |
| `PACKAGES_TOKEN` | Docker build / CI — restore **Trovesuite.Package** (`read:packages` PAT). Required for PR workflow (`ci.yml`) and deploy build. |

## Repository variables

| Variable | Example |
|----------|---------|
| `DEV_CONTAINER_REGISTRY_NAME` | `trovesuitedevacr` |
| `PROD_CONTAINER_REGISTRY_NAME` | `trovesuiteprodacr` |
| `DEV_RESOURCE_GROUP` | `rg-trovesuite-dev` |
| `PROD_RESOURCE_GROUP` | `rg-trovesuite-prod` |

Same names as Core Platform if both backends share one Trovesuite subscription.

## Path filters

- **API image** rebuilds on `app/**`, `nuget.config`, or workflow changes.
- **Functions** deploy only on `func/**` changes (workflow edits alone do **not** trigger Functions).
- **`workflow_dispatch`**: choose **Deploy Container App** and/or **Deploy Functions** (Functions default off).
- If the Function App does not exist in Azure (`trovesuite-dev-zeloshr-func` / `trovesuite-prod-zeloshr-func`), the workflow **skips** Functions deploy with a warning instead of failing.

## Local parity with CI

```bash
cp app/.env.example app/.env   # set PACKAGES_TOKEN (local Docker; same name as org secret) and/or App__DatabaseUrl for shared dev
./scripts/compose.sh ci        # test (compose) + docker build
```

See [LOCAL_DEV.md](LOCAL_DEV.md) for migrate, dev stack, and reset.

## Production runtime config

After the image is deployed, configure the Container App environment variables (secrets + non-secrets). Full list: [PRODUCTION_CONFIG.md](PRODUCTION_CONFIG.md). Base JSON: [`app/appsettings.Production.json`](../app/appsettings.Production.json).

Functions publish (same as CI):

```bash
dotnet publish func/ZelosHR.Functions.csproj -c Release -o ./func-publish
```
