#!/usr/bin/env bash
# Smoke-test custom-fields endpoints (deployed or local API).
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
ENV_FILE="${ROOT}/scripts/local-dev/live-session.env"
DB_ENV="${ROOT}/scripts/local-dev/live-db.env"

if [[ ! -f "${ENV_FILE}" ]]; then
  echo "Missing ${ENV_FILE}. See scripts/local-dev/README.md" >&2
  exit 1
fi

# shellcheck source=/dev/null
set -a && source "${ENV_FILE}" && set +a
[[ -f "${DB_ENV}" ]] && set -a && source "${DB_ENV}" && set +a

BASE="${ZELOSHR_API_BASE:-https://zeloshr.app.backend.dev.trovesuite.com}"

if [[ -z "${TROVE_BEARER_TOKEN:-}" ]]; then
  echo "TROVE_BEARER_TOKEN is empty — log in on dev and paste a fresh JWT." >&2
  exit 1
fi

curl_headers=(
  -H "accept: application/json"
  -H "app-id: ${TROVE_APP_ID:-app-hr}"
  -H "authorization: Bearer ${TROVE_BEARER_TOKEN}"
  -H "bus-id: ${TROVE_BUS_ID}"
  -H "loc-id: ${TROVE_LOC_ID}"
  -H "org-id: ${TROVE_ORG_ID}"
)

paths=(
  "/api/v1/custom-fields/statistics"
  "/api/v1/custom-fields/entity-types"
  "/api/v1/custom-fields/sections?entity_type=employee"
  "/api/v1/custom-fields/schema?entity_type=employee"
  "/api/v1/custom-fields/list?page=1&size=5"
  "/api/v1/custom-fields/audit-logs?page=1&size=5"
)

echo "ZelosHR base: ${BASE}"
echo ""

for path in "${paths[@]}"; do
  body=$(curl -sS -w "\n%{http_code}" "${curl_headers[@]}" "${BASE}${path}")
  code=$(echo "$body" | tail -1)
  json=$(echo "$body" | sed '$d' | head -c 320)
  printf "GET %s\n  HTTP %s\n  %s\n\n" "$path" "$code" "$json"
done
