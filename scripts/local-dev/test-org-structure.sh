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
  -H "app-id: ${TROVE_APP_ID:-app-zeloshr}"
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

chart_blank_names=0

echo "ZelosHR base: ${BASE}"
echo ""

for path in "${paths[@]}"; do
  body=$(curl -sS -w "\n%{http_code}" -H "accept: application/json" "${curl_headers[@]}" "${BASE}${path}")
  code=$(echo "$body" | tail -1)
  json=$(echo "$body" | sed '$d')
  printf "GET %s\n  HTTP %s\n" "$path" "$code"

  if [[ "$path" == "/api/v1/org-structure/chart" && "$code" == "200" ]]; then
    chart_blank_names=$(echo "$json" | python3 -c "
import json, sys
data = json.load(sys.stdin).get('data') or {}
roots = data.get('roots') or []
blank = []

def walk(nodes):
    for n in nodes:
        name = (n.get('full_name') or '').strip()
        if not name:
            blank.append(n.get('id'))
        walk(n.get('children') or [])

walk(roots)
print(len(blank))
if blank:
    print('BLANK_IDS:' + ','.join(blank), file=sys.stderr)
" 2>&1)
    printf "  chart nodes with blank full_name: %s\n" "$chart_blank_names"
    if [[ "${chart_blank_names}" != "0" ]]; then
      echo "$json" | head -c 800
      echo ""
      exit 1
    fi
  else
    echo "  $(echo "$json" | head -c 400)"
  fi
  echo ""
done

dept_body='{"name":"Sprint Test Department","parent_department_id":null,"head_of_department_id":null,"description":"Schema smoke test"}'
dept_create=$(curl -sS -w "\n%{http_code}" -X POST "${curl_headers[@]}" \
  -H "content-type: application/json" \
  -d "${dept_body}" \
  "${BASE}/api/v1/org-structure/departments/add")
dept_code=$(echo "$dept_create" | tail -1)
dept_json=$(echo "$dept_create" | sed '$d' | head -c 500)
printf "POST /api/v1/org-structure/departments/add\n  HTTP %s\n  %s\n\n" "$dept_code" "$dept_json"

dept_id=$(echo "$dept_json" | python3 -c "import sys,json; d=json.load(sys.stdin); print((d.get('data') or {}).get('department_id',''))" 2>/dev/null || true)
if [[ -n "${dept_id}" ]]; then
  delete_dept=$(curl -sS -w "\n%{http_code}" -X DELETE "${curl_headers[@]}" \
    "${BASE}/api/v1/org-structure/departments/delete?department_id=${dept_id}")
  delete_dept_code=$(echo "$delete_dept" | tail -1)
  delete_dept_json=$(echo "$delete_dept" | sed '$d' | head -c 500)
  printf "DELETE /api/v1/org-structure/departments/delete?department_id=%s\n  HTTP %s\n  %s\n\n" "$dept_id" "$delete_dept_code" "$delete_dept_json"
fi

emp_list=$(curl -sS -w "\n%{http_code}" "${curl_headers[@]}" \
  "${BASE}/api/v1/employees/list?page=1&size=3&sort_by=last_name&sort_order=asc")
emp_code=$(echo "$emp_list" | tail -1)
emp_json=$(echo "$emp_list" | sed '$d' | head -c 500)
printf "GET /api/v1/employees/list\n  HTTP %s\n  %s\n\n" "$emp_code" "$emp_json"

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
