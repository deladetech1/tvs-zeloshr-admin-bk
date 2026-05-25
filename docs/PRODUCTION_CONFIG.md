# Production configuration — ZelosHR API

Production uses **`ASPNETCORE_ENVIRONMENT=Production`**, which loads the complete template in [`app/appsettings.Production.json`](../app/appsettings.Production.json).

Every key the app needs is listed there. **Secrets** are empty strings in JSON; set real values on the Azure Container App (secrets or environment variables) using the same keys with `__` notation.

Full key reference: [APPCONFIG.md](APPCONFIG.md).

## Prerequisites

1. Deploy **tvs-sqlscript** to production PostgreSQL (`core_platform`, `human_resource`, `zeloshr`, EF RBAC seeds).
2. Set `App__RunDatabaseMigrations=false` (default in Production json).
3. Configure Container App env (below).

## Required Container App settings

| appsettings path | Env var override | Notes |
|------------------|------------------|--------|
| `App:ConnectionString` | `App__ConnectionString` | Single PostgreSQL URI for ZelosHR (same DB hosts `core_platform` auth used by Trovesuite.Package) |
| `Trovesuite:Jwt:SecretKey` | `Trovesuite__Jwt__SecretKey` | Match Core Platform issuer (≥ 32 chars) |
| `App:SecretKey` | `App__SecretKey` | Same as Jwt secret |
| `AzureStorage:ConnectionString` | `AzureStorage__ConnectionString` | Employee docs / profile photos |
| `App:CorsOrigins` | `App__CorsOrigins` | Admin UI origin(s), comma-separated |
| `TrovesuiteIntegration:RequireAuthentication` | `TrovesuiteIntegration__RequireAuthentication` | `true` (in Production json) |

You only set **`App__ConnectionString`** on the Container App. Do not set `Trovesuite__Database__*` — that is wired from `App:ConnectionString` for the NuGet package.

## Optional

| appsettings path | Env var |
|------------------|---------|
| `Trovesuite:Mail:SenderEmail` | `Trovesuite__Mail__SenderEmail` |
| `Trovesuite:Mail:SenderPassword` | `Trovesuite__Mail__SenderPassword` |
| `Trovesuite:AzureStorage:AccountName` | `Trovesuite__AzureStorage__AccountName` |
| `App:AppUrl` | `App__AppUrl` |

## Example (Azure Database for PostgreSQL)

```bash
ASPNETCORE_ENVIRONMENT=Production

App__ConnectionString=postgresql://trovesuiteuser:SECRET@trovesuite-shared-sql.postgres.database.azure.com:5432/dev-db?sslmode=require

Trovesuite__Jwt__SecretKey=<shared-with-core-platform>
App__SecretKey=<shared-with-core-platform>

AzureStorage__ConnectionString=DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net

App__CorsOrigins=https://admin.zeloshr.com
App__AppUrl=https://api.zeloshr.com
```

## API contract

| Header | Value |
|--------|--------|
| `app-id` | `app-hr` (`App:AppId`) |
| `authorization` | `Bearer <JWT>` with `tenant_id`, `user_id` |
| `org-id`, `bus-id`, `loc-id` | Valid `core_platform` ids |

## Troubleshooting

### `Database is not configured` or invalid connection string

Set **`App__ConnectionString`** to a full PostgreSQL URI, for example:

`postgresql://user:password@host.postgres.database.azure.com:5432/dbname?sslmode=require`

In Azure Portal: Container App → **Containers** → your container → **Environment variables** (or **Secrets** referenced by env vars). The revision restarts automatically when env changes.

## Verify

```bash
curl -s -o /dev/null -w "%{http_code}\n" https://<fqdn>/api/v1/navigation \
  -H "app-id: app-hr" \
  -H "authorization: Bearer <token>" \
  -H "org-id: <org>" -H "bus-id: <bus>" -H "loc-id: <loc>"
```

See [CICD.md](CICD.md) for deploy workflow and [TROVESUITE.md](TROVESUITE.md) for auth.
