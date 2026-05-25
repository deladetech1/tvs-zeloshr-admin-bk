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
| `App:DatabaseUrl` | `App__DatabaseUrl` | Preferred: full URL with `sslmode=require` |
| `App:DbHost` … `DbPassword` | `App__DbHost`, etc. | Alternative to DatabaseUrl |
| `Trovesuite:Database:*` | `Trovesuite__Database__*` | Same DB as App (auth on `core_platform`) |
| `Trovesuite:Jwt:SecretKey` | `Trovesuite__Jwt__SecretKey` | Match Core Platform issuer (≥ 32 chars) |
| `App:SecretKey` | `App__SecretKey` | Same as Jwt secret |
| `AzureStorage:ConnectionString` | `AzureStorage__ConnectionString` | Employee docs / profile photos |
| `App:CorsOrigins` | `App__CorsOrigins` | Admin UI origin(s), comma-separated |
| `TrovesuiteIntegration:RequireAuthentication` | `TrovesuiteIntegration__RequireAuthentication` | `true` (in Production json) |

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

App__DatabaseUrl=postgresql://zeloshr_api%40myserver:SECRET@myserver.postgres.database.azure.com:5432/trovesuite?sslmode=require

Trovesuite__Database__Host=myserver.postgres.database.azure.com
Trovesuite__Database__Port=5432
Trovesuite__Database__Database=trovesuite
Trovesuite__Database__Username=zeloshr_api@myserver
Trovesuite__Database__Password=SECRET

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

### `Format of the initialization string does not conform to specification starting at index 0`

The Container App is running with **empty** database settings from `appsettings.Production.json` and no overrides. Set at least one of:

- **`App__DatabaseUrl`** — full `postgresql://user:password@host:5432/dbname?sslmode=require` (preferred), or
- **`App__DbHost`**, **`App__DbName`**, **`App__DbUser`**, **`App__DbPassword`** (and optionally **`App__DbPort`**)

Also set **`Trovesuite__Database__Host`**, **`Trovesuite__Database__Database`**, **`Trovesuite__Database__Username`**, **`Trovesuite__Database__Password`** (same server as `App`; auth reads `core_platform`).

In Azure Portal: Container App → **Containers** → your container → **Environment variables** (or **Secrets** referenced by env vars). Redeploy is not required after env changes; the revision restarts automatically.

## Verify

```bash
curl -s -o /dev/null -w "%{http_code}\n" https://<fqdn>/api/v1/navigation \
  -H "app-id: app-hr" \
  -H "authorization: Bearer <token>" \
  -H "org-id: <org>" -H "bus-id: <bus>" -H "loc-id: <loc>"
```

See [CICD.md](CICD.md) for deploy workflow and [TROVESUITE.md](TROVESUITE.md) for auth.
