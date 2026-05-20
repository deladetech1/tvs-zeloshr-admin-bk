#!/usr/bin/env bash
# Generate a Trovesuite-compatible JWT (no local .NET SDK required).
# Usage: ./scripts/gen-trovesuite-jwt.sh [secret] [user_id] [tenant_id]
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
SECRET="${1:-change-me-in-production-use-32-chars-min}"
USER_ID="${2:-u1000001-0000-4000-8000-000000000001}"
TENANT_ID="${3:-demo-tenant}"

if command -v dotnet >/dev/null 2>&1; then
  exec dotnet run --project "$ROOT/scripts/gen-trovesuite-jwt/GenJwt.csproj" -c Release -- \
    "$SECRET" "$USER_ID" "$TENANT_ID"
fi

exec docker run --rm \
  -v "$ROOT/scripts/gen-trovesuite-jwt:/src" \
  -w /src \
  mcr.microsoft.com/dotnet/sdk:10.0 \
  dotnet run -c Release -- "$SECRET" "$USER_ID" "$TENANT_ID"
