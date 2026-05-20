# Trovesuite.Package integration

ZelosHR uses [Trovesuite.Package](https://github.com/deladetech1/tvs-package-dotnet/pkgs/nuget/Trovesuite.Package) from GitHub Packages for:

- JWT auth + multi-tenant authorization (`core_platform` schema)
- Email notifications (MailKit)
- Azure Blob Storage (Managed Identity)

## Prerequisites

1. **.NET 10 SDK** (package targets `net10.0`)
2. **GitHub Packages read token** with `read:packages` scope
3. **`core_platform` schema** in PostgreSQL — apply via [tvs-sqlscript](https://github.com/deladetech1/tvs-sqlscript) (the NuGet package does not run migrations)

## Authenticate NuGet (local)

```bash
export GITHUB_PACKAGES_TOKEN=ghp_xxxxxxxx
dotnet nuget update source github-deladetech1 \
  --username deladetech1 \
  --password "$GITHUB_PACKAGES_TOKEN" \
  --store-password-in-clear-text \
  --configfile nuget.config
```

(`github-deladetech1` is already listed in the repo root `nuget.config`; only credentials need updating.)

## Docker Compose (local API + Postgres)

The `api` service must reach Postgres for **both** ZelosHR queries and Trovesuite auth:

| Env override | Purpose |
|--------------|---------|
| `App__DbHost=db` | ZelosHR `DatabaseManager` |
| `Trovesuite__Database__Host=db` | `IAuthService` / platform tables in `core_platform` |
| `Trovesuite__Jwt__SecretKey` | Must be **≥ 32 characters** for HS256 (IdentityModel v8) |

Demo HR data: deploy `tvs-sqlscript` with `TVS_SEED_ZELOSHR_DEMO=1`, then call APIs with `X-Tenant-Id: demo-tenant` and `X-Org-Id: demo-org`.

JWT mode (optional): `TROVESUITE_REQUIRE_AUTH=true docker compose up -d api` — send `Authorization: Bearer <token>` (claims `user_id`, `tenant_id`).

### How to get a Bearer token for testing

**Option A — Demo mode (no JWT)**  
Leave `TrovesuiteIntegration__RequireAuthentication=false` (default). Use headers only:

```http
X-Tenant-Id: demo-tenant
X-Org-Id: demo-org
```

Swagger: skip **Authorize**; tenant headers are pre-filled on `/api/v1/*` routes.

**Option B — Local dev JWT (fastest for JWT testing)**  
The API validates HS256 tokens signed with `Trovesuite__Jwt__SecretKey` (must be **≥ 32 characters**). Generate one from the repo root (uses Docker if `dotnet` is not installed):

```bash
./scripts/gen-trovesuite-jwt.sh

# Optional: secret, user_id, tenant_id
./scripts/gen-trovesuite-jwt.sh "change-me-in-production-use-32-chars-min" \
  "u1000001-0000-4000-8000-000000000001" "demo-tenant"
```

With .NET 10 SDK installed locally you can instead run:

```bash
dotnet run --project scripts/gen-trovesuite-jwt/GenJwt.csproj -c Release -- \
  "change-me-in-production-use-32-chars-min"
```

Copy the printed token, then:

```bash
export TOKEN="<paste>"
curl -s -H "Authorization: Bearer $TOKEN" \
  -H "X-Tenant-Id: demo-tenant" -H "X-Org-Id: demo-org" \
  http://localhost:8000/api/v1/employees | jq .
```

In Swagger → **Authorize** → `Bearer <token>` (include the word `Bearer` only in the value field if the UI adds it automatically; otherwise paste `Bearer eyJ...`).

**Option C — Verify an existing token**  
If you already have a JWT from Core Platform login:

```bash
curl -s -X POST http://localhost:8000/api/v1/platform/auth/verify \
  -H "Authorization: Bearer $TOKEN" | jq .
```

`POST /api/v1/platform/auth/authorize` checks user/tenant IDs in `core_platform` but does **not** issue a JWT — use Core Platform login for production tokens.

Platform auth also requires `core_platform.cp_login_settings` for the user (see `AuthorizeAsync` in Trovesuite.Package).

## Docker build

Put the token in **`app/.env`** (same file as DB settings):

```bash
GITHUB_PACKAGES_TOKEN=ghp_xxxxxxxx
```

Compose only substitutes `${GITHUB_PACKAGES_TOKEN}` from env files you pass in. Point it at `app/.env`:

```bash
COMPOSE_ENV_FILES=app/.env docker compose build api
COMPOSE_ENV_FILES=app/.env docker compose up -d
```

Or export once in your shell: `export GITHUB_PACKAGES_TOKEN=ghp_xxx` then `docker compose up -d --build`.

## Configuration

| Section | Purpose |
|---------|---------|
| `Trovesuite:Database` | Postgres for `core_platform` (auth queries) |
| `Trovesuite:Jwt` | JWT secret (must match token issuer) |
| `Trovesuite:Mail` | SMTP fallback for emails |
| `Trovesuite:AzureStorage` | Blob storage account |
| `TrovesuiteIntegration:RequireAuthentication` | `false` = demo headers; `true` = Bearer JWT required |

## Platform API (Swagger)

| Endpoint | Description |
|----------|-------------|
| `POST /api/v1/platform/auth/verify` | Validate JWT, return roles/permissions |
| `POST /api/v1/platform/auth/authorize` | Authorize by user/tenant IDs |
| `GET /api/v1/platform/auth/context` | Current tenant/user from middleware |
| `POST /api/v1/platform/notifications/email` | Send email |
| `POST /api/v1/platform/storage/file-url` | SAS URL for blob |

## Demo mode (default)

`TrovesuiteIntegration:RequireAuthentication` is `false`. HR APIs use:

- `X-Tenant-Id: demo-tenant`
- `X-Org-Id: demo-org`

## Production auth

Set `TrovesuiteIntegration:RequireAuthentication` to `true`. Send:

```http
Authorization: Bearer <jwt-from-core-platform>
```

Middleware calls `IAuthService.AuthorizeUserFromTokenAsync` and sets tenant/org/user on the request.
