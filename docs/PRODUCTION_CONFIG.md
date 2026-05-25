# Production configuration — ZelosHR API

The API runs as **`trovesuite-prod-zeloshr-ca`** (or dev equivalent) with `ASPNETCORE_ENVIRONMENT=Production`, which loads [`app/appsettings.Production.json`](../app/appsettings.Production.json).

**Never commit secrets.** Set them as Container App **secrets** or environment variables (Azure Portal, Bicep, or `az containerapp update`).

## Prerequisites (database)

1. Deploy **tvs-sqlscript** to the production PostgreSQL database (`core_platform` → `human_resource` / `zeloshr` schemas, EF RBAC seeds).
2. Use the **same database** for:
   - `App` / EF (`zeloshr.*`, `human_resource.hr_employees`)
   - `Trovesuite:Database` (auth queries on `core_platform.*`)
3. Do **not** set `App__RunDatabaseMigrations=true` in production (schema comes from tvs-sqlscript only).

## Required secrets (Container App)

| Setting | Env var (preferred) | Notes |
|---------|---------------------|--------|
| Postgres URL | `App__DatabaseUrl` | Full Npgsql URL, e.g. `postgresql://USER:PASS@HOST:5432/DB?sslmode=require` |
| Same DB for Trovesuite auth | `Trovesuite__Database__Host`, `Trovesuite__Database__Port`, `Trovesuite__Database__Database`, `Trovesuite__Database__Username`, `Trovesuite__Database__Password` | Required if not using only `App__DatabaseUrl` for Trovesuite.Package |
| JWT signing key | `Trovesuite__Jwt__SecretKey` | **Must match** Core Platform / token issuer (≥ 32 chars) |
| Legacy JWT (if used) | `App__SecretKey` | Set to **same value** as `Trovesuite__Jwt__SecretKey` if anything reads `App:SecretKey` |
| Blob storage | `AzureStorage__ConnectionString` | Required for profile photos / employee documents (not local disk) |
| SMTP (optional) | `Trovesuite__Mail__SenderEmail`, `Trovesuite__Mail__SenderPassword` | Email notifications |

### Example: Azure Database for PostgreSQL

```bash
App__DatabaseUrl=postgresql://zeloshr_api%40myserver:SECRET@myserver.postgres.database.azure.com:5432/trovesuite?sslmode=require

Trovesuite__Database__Host=myserver.postgres.database.azure.com
Trovesuite__Database__Port=5432
Trovesuite__Database__Database=trovesuite
Trovesuite__Database__Username=zeloshr_api@myserver
Trovesuite__Database__Password=SECRET

Trovesuite__Jwt__SecretKey=<same-as-core-platform-jwt-secret>
App__SecretKey=<same-as-core-platform-jwt-secret>

AzureStorage__ConnectionString=DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net
```

## Required non-secret settings

| Setting | Env var | Production value |
|---------|---------|------------------|
| Environment | `ASPNETCORE_ENVIRONMENT` | `Production` |
| Auth enforcement | `TrovesuiteIntegration__RequireAuthentication` | `true` |
| Trove headers | `TrovesuiteIntegration__RequireStandardHeaders` | `true` |
| Platform validation | `TrovesuiteIntegration__ValidatePlatformContext` | `true` |
| HR app id | `App__AppId` | `app-hr` (must match `app-id` header) |
| CORS | `App__CorsOrigins` | Your admin UI origin(s), comma-separated |
| Public URL | `App__AppUrl` | e.g. `https://api.zeloshr.com` or Container App FQDN |
| Migrations off | `App__RunDatabaseMigrations` | `false` (default in Production json) |

## API contract (every `/api/v1/*` call)

Clients must send Trove standard headers (see [SWAGGER.md](SWAGGER.md)):

| Header | Value |
|--------|--------|
| `app-id` | `app-hr` |
| `authorization` | `Bearer <JWT>` with claims `tenant_id`, `user_id` |
| `org-id` | Valid `cp_organizations.id` for tenant |
| `bus-id` | Valid `cp_businesses.id` |
| `loc-id` | Valid `cp_locations.id` + user `cp_user_locations` |

JWT must be issued by Core Platform with the same `Trovesuite__Jwt__SecretKey`.

## RBAC

Permissions are seeded in production DB via tvs-sqlscript (`permission-zeloshr-*`, `role-subscribed-app-hr-admin`). Users need roles assigned in `core_platform.cp_assign_roles` with matching permissions.

## Optional

| Setting | Env var | Purpose |
|---------|---------|---------|
| Azure storage account name | `Trovesuite__AzureStorage__AccountName` | Trovesuite.Package SAS URLs |
| Mail | `Trovesuite__Mail__SmtpHost`, `SmtpPort`, `UseSsl` | Defaults in appsettings.Production.json |
| Log level | `Logging__LogLevel__Default` | `Information` or `Warning` |

## Verify after deploy

```bash
# Health of DB pool (if you expose a health route)
curl -s -o /dev/null -w "%{http_code}\n" https://<container-app-fqdn>/api/v1/navigation \
  -H "app-id: app-hr" \
  -H "authorization: Bearer <prod-jwt>" \
  -H "org-id: <org>" -H "bus-id: <bus>" -H "loc-id: <loc>"
```

Expect **200** when JWT, headers, and DB/RBAC are correct; **401** / **403** when auth or permissions fail.

## `az containerapp` env template (prod)

Replace placeholders; store secrets in Container App secret refs.

```bash
az containerapp update \
  --name trovesuite-prod-zeloshr-ca \
  --resource-group "<PROD_RESOURCE_GROUP>" \
  --set-env-vars \
    ASPNETCORE_ENVIRONMENT=Production \
    TrovesuiteIntegration__RequireAuthentication=true \
    TrovesuiteIntegration__RequireStandardHeaders=true \
    TrovesuiteIntegration__ValidatePlatformContext=true \
    App__AppId=app-hr \
    App__RunDatabaseMigrations=false \
    App__CorsOrigins="https://<your-admin-ui>" \
    App__AppUrl="https://<container-app-fqdn>" \
  --replace-env-vars
# Add secrets separately: App__DatabaseUrl, Trovesuite__Jwt__SecretKey, AzureStorage__ConnectionString, etc.
```

See also [CICD.md](CICD.md) for deploy workflow and [TROVESUITE.md](TROVESUITE.md) for auth details.
