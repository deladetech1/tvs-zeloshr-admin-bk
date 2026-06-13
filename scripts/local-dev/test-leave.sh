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
  "/api/v1/leave/dashboard"
  "/api/v1/leave/requests/list?page=1&size=5"
  "/api/v1/leave/requests/list?approval_stage=pending_final&page=1&size=5"
  "/api/v1/leave/balances/list"
  "/api/v1/leave/types/list?active_only=true"
  "/api/v1/leave/holidays/list?page=1&size=10"
  "/api/v1/leave/summary"
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

emp_body=$(curl -sS -w "\n%{http_code}" "${curl_headers[@]}" "${BASE}/api/v1/employees/list?page=1&size=1")
emp_code=$(echo "$emp_body" | tail -1)
emp_json=$(echo "$emp_body" | sed '$d')
employee_id=$(echo "$emp_json" | python3 -c "import json,sys; d=json.load(sys.stdin); print(d.get('data',{}).get('items',[{}])[0].get('employee_id',''))" 2>/dev/null || true)

if [[ -n "${employee_id}" ]]; then
  my_paths=(
    "/api/v1/leave/my/summary?employee_id=${employee_id}"
    "/api/v1/leave/my/balances/list?employee_id=${employee_id}"
    "/api/v1/leave/my/requests/list?employee_id=${employee_id}&page=1&size=5"
  )
  for path in "${my_paths[@]}"; do
    body=$(curl -sS -w "\n%{http_code}" "${curl_headers[@]}" "${BASE}${path}")
    code=$(echo "$body" | tail -1)
    json=$(echo "$body" | sed '$d')
    printf "GET %s\n  HTTP %s\n" "$path" "$code"
    echo "  $(echo "$json" | head -c 500)"
    echo ""
  done
else
  echo "Skipping My Leave routes — no employee_id from GET /employees/list (HTTP ${emp_code})"
  echo ""
fi

echo "Leave smoke checks completed."
