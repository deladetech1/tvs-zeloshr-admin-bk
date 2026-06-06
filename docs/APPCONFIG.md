# Application configuration (appsettings)

All runtime settings are defined in **`app/appsettings*.json`**. ASP.NET Core loads:

| File | When |
|------|------|
| `appsettings.json` | Always (full template) |
| `appsettings.Development.json` | `ASPNETCORE_ENVIRONMENT=Development` (local `dotnet run`) |
| `appsettings.Docker.json` | `ASPNETCORE_ENVIRONMENT=Docker` (`./scripts/compose.sh dev`) |
| `appsettings.Production.json` | `ASPNETCORE_ENVIRONMENT=Production` (Azure Container App) |

Environment variables and Container App secrets **override** the same keys using `__` (e.g. `App__ConnectionString`, `Trovesuite__Jwt__SecretKey`).

## App

| Key | Purpose |
|-----|---------|
| `ConnectionString` | PostgreSQL URI for ZelosHR (`postgresql://user:pass@host:5432/db`) |
| `AppName`, `AppVersion`, `Environment`, `Debug` | Metadata |
| `LogLevel`, `LogDir` | Logging |
| `SecretKey`, `Algorithm`, `AccessTokenExpireMinutes` | Legacy JWT helpers / Swagger dev token fallback |
| `CorsOrigins` | Comma-separated allowed origins (include `http://localhost:3003` for local frontends). When empty, the API falls back to localhost ports 3000/3003/8080 for local UI dev. |
| `AppUrl`, `AppId` | Public URL; must use `app-hr` for Trove `app-id` header |
| `RunDatabaseMigrations` | Must stay `false` in prod (schema from tvs-sqlscript) |
| `CorePlatformUsersTable`, `CorePlatformMembersTable` | Optional table overrides |
| `ActivityLogsTable`, `EmployeesTable` | Optional table overrides |

## Trovesuite (NuGet package — optional overrides)

The package expects a `Trovesuite` section in configuration. **Database host/user/password come from `App:ConnectionString` automatically** — you do not configure `Trovesuite:Database` yourself.

| Key | Purpose |
|-----|---------|
| `Trovesuite:Jwt:SecretKey` | HS256 secret (≥ 32 chars; match Core Platform) |
| `Trovesuite:Jwt:Algorithm` | `HS256` |
| `Trovesuite:Jwt:AccessTokenExpireMinutes` | Token lifetime hint |
| `Trovesuite:Mail:SenderEmail` | SMTP sender |
| `Trovesuite:Mail:SenderPassword` | SMTP password |
| `Trovesuite:Mail:SmtpHost`, `SmtpPort`, `UseSsl` | SMTP transport |
| `Trovesuite:AzureStorage:AccountName` | Blob account (managed identity / SAS) |
| `Trovesuite:AzureStorage:ConnectionString` | Optional blob connection string |
| `Trovesuite:App:Name`, `Environment`, `AppUrl` | Package app metadata |

## AzureStorage (ZelosHR file uploads)

| Key | Purpose |
|-----|---------|
| `ConnectionString` | If set, uses Azure Blob; else local dev file storage |
| `DocumentsContainer` | Blob container for employee files (**default: `zeloshr`**) |
| `ProfilePhotosContainer` | Legacy key; profile photos use `DocumentsContainer` via file registry |

**Blob layout** (inside `zeloshr` container):

```
{tenant_id}/{org_id}/{bus_id}/employees/documents/{unique}-{filename}   ← file API (auto path)
{tenant_id}/{org_id}/{bus_id}/employees/profile/{employee_id}.{ext}     ← profile photo
{tenant_id}/{org_id}/{bus_id}/employees/documents/wizard/{employee_id}/… ← wizard upload
```

## TrovesuiteIntegration (ZelosHR middleware)

| Key | Purpose |
|-----|---------|
| `RequireAuthentication` | `true` = full Trovesuite DB auth; `false` = read JWT claims only |
| `RequireStandardHeaders` | Require Trove headers on `/api/v1/*` |
| `ValidatePlatformContext` | Validate org/bus/loc against `core_platform` |
| `HrAdminPermission` | Optional permission id for admin-only routes |

## LocalDevelopment

Used for Swagger dev bootstrap and dev fallbacks only. Leave empty in production.

## Kestrel

| Key | Purpose |
|-----|---------|
| `Endpoints:Http:Url` | Listen URL (`http://0.0.0.0:8000`) |

## Production secrets (fill in Azure or override env)

Set these on the Container App (same names as appsettings, `__` separator):

- `App__ConnectionString` (PostgreSQL URI)
- `Trovesuite__Jwt__SecretKey` and `App__SecretKey` (same value), **or** `SECRET_KEY` (same env name as Core Platform; mapped automatically)
- `AzureStorage__ConnectionString`
- `Trovesuite__Mail__SenderEmail`, `Trovesuite__Mail__SenderPassword`
- `App__CorsOrigins`

See [PRODUCTION_CONFIG.md](PRODUCTION_CONFIG.md) for deploy checklist.
