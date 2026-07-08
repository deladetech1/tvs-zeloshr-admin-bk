#!/usr/bin/env bash
# Smoke-test employee endpoints (deployed or local API).
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
  -H "app-id: ${TROVE_APP_ID:-app-zeloshr}"
  -H "authorization: Bearer ${TROVE_BEARER_TOKEN}"
  -H "bus-id: ${TROVE_BUS_ID}"
  -H "loc-id: ${TROVE_LOC_ID}"
  -H "org-id: ${TROVE_ORG_ID}"
)

paths=(
  "/api/v1/employees/statistics"
  "/api/v1/employees/directory/summary"
  "/api/v1/employees/list?page=1&size=5"
  "/api/v1/id-card-types/list?page=1&size=20"
  "/api/v1/employees/import/search?query=a"
  "/api/v1/employees/bulk/template"
  "/api/v1/employees/export"
  "/api/v1/employees/export?start_date=2025-01-01&end_date=2025-12-31"
)

echo "ZelosHR base: ${BASE}"
echo ""

for path in "${paths[@]}"; do
  accept="application/json"
  [[ "$path" == *bulk/template* ]] && accept="text/csv,application/json"
  [[ "$path" == *employees/export* ]] && accept="text/csv,application/json"
  body=$(curl -sS -w "\n%{http_code}" -H "accept: ${accept}" "${curl_headers[@]}" "${BASE}${path}")
  code=$(echo "$body" | tail -1)
  if [[ "$path" == *bulk/template* && "$code" == "200" ]] || [[ "$path" == *employees/export* && "$code" == "200" ]]; then
    bytes=$(echo "$body" | sed '$d' | wc -c | tr -d ' ')
    printf "GET %s\n  HTTP %s\n  (CSV body, %s bytes)\n\n" "$path" "$code" "$bytes"
  else
    json=$(echo "$body" | sed '$d' | head -c 360)
    printf "GET %s\n  HTTP %s\n  %s\n\n" "$path" "$code" "$json"
  fi
done

# Optional: GET /employees/get when EMPLOYEE_ID is set in live-session.env
if [[ -n "${EMPLOYEE_ID:-}" ]]; then
  path="/api/v1/employees/get?employee_id=${EMPLOYEE_ID}"
  body=$(curl -sS -w "\n%{http_code}" "${curl_headers[@]}" "${BASE}${path}")
  code=$(echo "$body" | tail -1)
  json=$(echo "$body" | sed '$d' | head -c 360)
  printf "GET %s\n  HTTP %s\n  %s\n\n" "$path" "$code" "$json"
fi
