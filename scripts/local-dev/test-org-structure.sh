#!/usr/bin/env bash
# Smoke-test org-structure endpoints (deployed or local API).
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
  "/api/v1/org-structure/statistics"
  "/api/v1/org-structure/chart"
  "/api/v1/org-structure/departments/list?page=1&size=5&sort_by=name&sort_order=asc&include_archived=false"
  "/api/v1/org-structure/branches/list?page=1&size=5&include_archived=false"
)

echo "ZelosHR base: ${BASE}"
echo ""

for path in "${paths[@]}"; do
  body=$(curl -sS -w "\n%{http_code}" -H "accept: application/json" "${curl_headers[@]}" "${BASE}${path}")
  code=$(echo "$body" | tail -1)
  json=$(echo "$body" | sed '$d' | head -c 400)
  printf "GET %s\n  HTTP %s\n  %s\n\n" "$path" "$code" "$json"
done

branch_body='{"name":"Sprint Test Branch","address":"Westlands Business Park","country":"Kenya","description":null}'
create=$(curl -sS -w "\n%{http_code}" -X POST "${curl_headers[@]}" \
  -H "content-type: application/json" \
  -d "${branch_body}" \
  "${BASE}/api/v1/org-structure/branches/add")
create_code=$(echo "$create" | tail -1)
create_json=$(echo "$create" | sed '$d' | head -c 500)
printf "POST /api/v1/org-structure/branches/add\n  HTTP %s\n  %s\n\n" "$create_code" "$create_json"

branch_id=$(echo "$create_json" | python3 -c "import sys,json; d=json.load(sys.stdin); print((d.get('data') or {}).get('branch_id',''))" 2>/dev/null || true)
if [[ -n "${branch_id}" ]]; then
  update_body='{"address":"1 Canada Square, Canary Wharf","country":"United Kingdom"}'
  update=$(curl -sS -w "\n%{http_code}" -X PUT "${curl_headers[@]}" \
    -H "content-type: application/json" \
    -d "${update_body}" \
    "${BASE}/api/v1/org-structure/branches/update?branch_id=${branch_id}")
  update_code=$(echo "$update" | tail -1)
  update_json=$(echo "$update" | sed '$d' | head -c 500)
  printf "PUT /api/v1/org-structure/branches/update?branch_id=%s\n  HTTP %s\n  %s\n\n" "$branch_id" "$update_code" "$update_json"

  delete=$(curl -sS -w "\n%{http_code}" -X DELETE "${curl_headers[@]}" \
    "${BASE}/api/v1/org-structure/branches/delete?branch_id=${branch_id}")
  delete_code=$(echo "$delete" | tail -1)
  delete_json=$(echo "$delete" | sed '$d' | head -c 500)
  printf "DELETE /api/v1/org-structure/branches/delete?branch_id=%s\n  HTTP %s\n  %s\n\n" "$branch_id" "$delete_code" "$delete_json"
fi
