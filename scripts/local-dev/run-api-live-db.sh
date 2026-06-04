#!/usr/bin/env bash
# Run ZelosHR API locally against dev Postgres (same DB as Container App).
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
DB_ENV="${ROOT}/scripts/local-dev/live-db.env"
JWT_FILE="${ROOT}/scripts/local-dev/.jwt-secret.local"

if [[ ! -f "$DB_ENV" ]]; then
  echo "Missing ${DB_ENV}" >&2
  echo "  ./scripts/local-dev/pull-azure-dev-env.sh" >&2
  echo "  # or: cp scripts/local-dev/live-db.example.env scripts/local-dev/live-db.env" >&2
  exit 1
fi

# shellcheck source=/dev/null
set -a && source "$DB_ENV" && set +a

if [[ -f "$JWT_FILE" ]]; then
  # shellcheck source=/dev/null
  set -a && source "$JWT_FILE" && set +a
fi

if [[ -z "${APP__CONNECTION_STRING:-}" ]]; then
  echo "APP__CONNECTION_STRING is empty in live-db.env" >&2
  exit 1
fi

JWT="${TROVESUITE__JWT__SECRET_KEY:-${TROVESUITE_JWT_SECRET:-}}"
if [[ -z "$JWT" ]]; then
  echo "JWT secret missing. Set TROVESUITE__JWT__SECRET_KEY in live-db.env or .jwt-secret.local" >&2
  exit 1
fi

export App__ConnectionString="$APP__CONNECTION_STRING"
export Trovesuite__Jwt__SecretKey="$JWT"
export App__SecretKey="$JWT"
export SECRET_KEY="$JWT"
export App__RunDatabaseMigrations=false
export TrovesuiteIntegration__RequireAuthentication=true
export TrovesuiteIntegration__RequireStandardHeaders=true
export TrovesuiteIntegration__ValidatePlatformContext=true
export ASPNETCORE_ENVIRONMENT=Development

echo "Local API → dev Postgres (no embedded migrations)"
echo "  Swagger: http://localhost:${API_PORT:-8000}/swagger"
echo "  Use Trove headers + Bearer from scripts/local-dev/live-session.env"
echo ""

cd "${ROOT}/app"
if command -v dotnet >/dev/null 2>&1; then
  exec dotnet run --no-launch-profile
fi

echo "dotnet not found; starting via Docker SDK image..." >&2
exec docker run --rm -it \
  -p "${API_PORT:-8000}:8000" \
  -v "${ROOT}:/src" \
  -w /src/app \
  -e App__ConnectionString \
  -e Trovesuite__Jwt__SecretKey \
  -e App__SecretKey \
  -e SECRET_KEY \
  -e App__RunDatabaseMigrations \
  -e TrovesuiteIntegration__RequireAuthentication \
  -e TrovesuiteIntegration__RequireStandardHeaders \
  -e TrovesuiteIntegration__ValidatePlatformContext \
  -e ASPNETCORE_ENVIRONMENT \
  mcr.microsoft.com/dotnet/sdk:10.0 \
  dotnet run --no-launch-profile
