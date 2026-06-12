#!/usr/bin/env bash
# Smoke-test leave endpoints (deployed or local API).
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
ENV_FILE="${ROOT}/scripts/local-dev/live-session.env"

if [[ ! -f "${ENV_FILE}" ]]; then
  echo "Missing ${ENV_FILE}. See scripts/local-dev/README.md" >&2
  exit 1
fi

# shellcheck source=/dev/null
set -a && source "${ENV_FILE}" && set +a

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
  "/api/v1/leave/statistics"
  "/api/v1/leave/requests/list?page=1&size=5"
  "/api/v1/leave/balances/list"
  "/api/v1/leave/types/list?active_only=true"
  "/api/v1/leave/holidays/list?page=1&size=10"
  "/api/v1/leave/my/summary"
  "/api/v1/leave/my/balances/list"
  "/api/v1/leave/my/requests/list?page=1&size=5"
)

echo "ZelosHR base: ${BASE}"
echo ""

for path in "${paths[@]}"; do
  body=$(curl -sS -w "\n%{http_code}" "${curl_headers[@]}" "${BASE}${path}")
  code=$(echo "$body" | tail -1)
  json=$(echo "$body" | sed '$d')
  printf "GET %s\n  HTTP %s\n" "$path" "$code"
  echo "  $(echo "$json" | head -c 500)"
  echo ""
done

echo "Leave smoke checks completed."
