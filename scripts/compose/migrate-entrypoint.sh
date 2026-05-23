#!/usr/bin/env bash
# Runs tvs-sqlscript deploy against the compose `db` service.
set -euo pipefail

SQLSCRIPT_ROOT="${TVS_SQLSCRIPT_ROOT:-/sqlscript}"
RUNNER_PROJECT="${SQLSCRIPT_ROOT}/src/Trovesuite.Database.Runner"

if [[ ! -f "${SQLSCRIPT_ROOT}/Trovesuite.Database.sln" ]]; then
  echo "ERROR: tvs-sqlscript not found at ${SQLSCRIPT_ROOT}." >&2
  echo "Clone https://github.com/deladetech1/tvs-sqlscript next to ZelosHR or set TVS_SQLSCRIPT_PATH." >&2
  exit 1
fi

DB_HOST="${TVS_DB_HOST:-db}"
DB_PORT="${TVS_DB_PORT:-5432}"
DB_USER="${DB_USER:-user}"
DB_PASSWORD="${DB_PASSWORD:-password}"
DB_NAME="${DB_NAME:-zeloshrdb}"
echo "==> tvs-sqlscript deploy → ${DB_HOST}:${DB_PORT}/${DB_NAME} (schema + reference seeds only)"
cd "${SQLSCRIPT_ROOT}"

dotnet restore Trovesuite.Database.sln --nologo -v q
dotnet build Trovesuite.Database.sln -c Release --no-restore --nologo -v q

# Runner prompts for module selection; "0" = all modules (core_platform → human_resource).
printf '0\n' | dotnet run --project "${RUNNER_PROJECT}" -c Release --no-build -- \
  "${DB_HOST}" "${DB_PORT}" "${DB_USER}" "${DB_PASSWORD}" "${DB_NAME}" deploy

echo "==> Deploy finished."
