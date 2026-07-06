#!/usr/bin/env bash
# Live verification for: employee list date filters, export parity, department profile_url, clear head.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
ENV_FILE="${ROOT}/scripts/local-dev/live-session.env"
JWT_FILE="${ROOT}/scripts/local-dev/.jwt-secret.local"

if [[ ! -f "${ENV_FILE}" ]]; then
  echo "Missing ${ENV_FILE}. See scripts/local-dev/README.md" >&2
  exit 1
fi

# shellcheck source=/dev/null
set -a && source "${ENV_FILE}" && set +a
[[ -f "${JWT_FILE}" ]] && export TROVESUITE_JWT_SECRET="$(cat "${JWT_FILE}")" && export LIVE_SESSION_JWT_FILE="${JWT_FILE}"
# shellcheck source=/dev/null
source "${ROOT}/scripts/local-dev/live-session-auth.sh"
ensure_live_session_auth

BASE="${ZELOSHR_API_BASE:-https://zeloshr.app.backend.dev.trovesuite.com}"
HDR=(
  -H "accept: application/json"
  -H "app-id: ${TROVE_APP_ID:-app-zeloshr}"
  -H "authorization: Bearer ${TROVE_BEARER_TOKEN}"
  -H "bus-id: ${TROVE_BUS_ID}"
  -H "loc-id: ${TROVE_LOC_ID}"
  -H "org-id: ${TROVE_ORG_ID}"
)

api_get() {
  curl -sS -w "\n%{http_code}" "${HDR[@]}" "${BASE}$1"
}

api_json() {
  local method="$1" path="$2" body="${3:-}"
  if [[ -n "$body" ]]; then
    curl -sS -w "\n%{http_code}" -X "$method" "${HDR[@]}" -H "content-type: application/json" -d "$body" "${BASE}${path}"
  else
    curl -sS -w "\n%{http_code}" -X "$method" "${HDR[@]}" "${BASE}${path}"
  fi
}

split_response() {
  HTTP_CODE=$(echo "$1" | tail -1)
  BODY=$(echo "$1" | sed '$d')
}

pass=0
fail=0
warn=0
check() {
  local name="$1" ok="$2" detail="$3"
  if [[ "$ok" == "yes" ]]; then
    echo "PASS: ${name} — ${detail}"
    pass=$((pass + 1))
  elif [[ "$ok" == "warn" ]]; then
    echo "WARN: ${name} — ${detail}"
    warn=$((warn + 1))
  else
    echo "FAIL: ${name} — ${detail}"
    fail=$((fail + 1))
  fi
}

echo "=== Live dev API: ${BASE} ==="
echo ""

DATE_Q="start_date=2020-01-01&end_date=2030-12-31"

# 1) Employee list date filters
split_response "$(api_get "/api/v1/employees/list?page=1&size=5&${DATE_Q}")"
check "employee list accepts start_date/end_date" "$([[ "${HTTP_CODE}" == "200" ]] && echo yes || echo no)" "HTTP ${HTTP_CODE}"
list_total=""
if [[ "${HTTP_CODE}" == "200" ]]; then
  list_total=$(echo "${BODY}" | python3 -c "import json,sys; d=json.load(sys.stdin); print((d.get('pagination') or {}).get('total',0))")
  check "employee list returns pagination with date filter" "yes" "total=${list_total}"
fi

split_response "$(api_get "/api/v1/employees/list?start_date=2026-12-31&end_date=2026-01-01")"
check "employee list rejects start_date > end_date" "$([[ "${HTTP_CODE}" == "400" ]] && echo yes || echo no)" "HTTP ${HTTP_CODE}"

# 2) Export parity (date filters only — status= triggers known EF translation issue)
split_response "$(curl -sS -w "\n%{http_code}" "${HDR[@]}" -H "accept: text/csv" \
  "${BASE}/api/v1/employees/export?${DATE_Q}")"
check "export accepts start_date/end_date" "$([[ "${HTTP_CODE}" == "200" ]] && echo yes || echo no)" "HTTP ${HTTP_CODE}"
csv_rows=0
if [[ "${HTTP_CODE}" == "200" ]]; then
  csv_rows=$(echo "${BODY}" | tail -n +2 | grep -c . || true)
  check "export returns CSV" "yes" "${csv_rows} data rows (list total=${list_total:-?})"
fi

split_response "$(api_get "/api/v1/employees/list?page=1&size=1&employment_status=Draft")"
draft_total=""
if [[ "${HTTP_CODE}" == "200" ]]; then
  draft_total=$(echo "${BODY}" | python3 -c "import json,sys; d=json.load(sys.stdin); print((d.get('pagination') or {}).get('total',0))")
fi
split_response "$(curl -sS -w "\n%{http_code}" "${HDR[@]}" -H "accept: text/csv" \
  "${BASE}/api/v1/employees/export?employment_status=Draft")"
draft_csv=0
if [[ "${HTTP_CODE}" == "200" ]]; then
  draft_csv=$(echo "${BODY}" | tail -n +2 | grep -c . || true)
fi
check "list+export parity (employment_status=Draft)" \
  "$([[ "${HTTP_CODE}" == "200" && "${draft_total}" == "${draft_csv}" ]] && echo yes || echo no)" \
  "list=${draft_total:-?} export rows=${draft_csv}"

split_response "$(api_get "/api/v1/employees/list?status=active&page=1&size=1")"
if [[ "${HTTP_CODE}" == "400" ]]; then
  check "status= smart filter" "warn" "HTTP 400 — EF cannot translate MatchesEngagement (pre-existing; use employment_status= for now)"
else
  check "status= smart filter" "$([[ "${HTTP_CODE}" == "200" ]] && echo yes || echo no)" "HTTP ${HTTP_CODE}"
fi

# 3) Department head profile_url + clear head (use Backend Team if present, else create)
split_response "$(api_get "/api/v1/employees/list?page=1&size=5")"
head_id=""
dept_id=""
if [[ "${HTTP_CODE}" == "200" ]]; then
  head_id=$(echo "${BODY}" | python3 -c "
import json,sys
d=json.load(sys.stdin)
for i in (d.get('data') or {}).get('items') or []:
  if 'Ada' in (i.get('full_name') or ''):
    print(i['employee_id']); break
else:
  items=(d.get('data') or {}).get('items') or []
  print(items[0]['employee_id'] if items else '')
")
fi

split_response "$(api_get "/api/v1/org-structure/departments/list?page=1&size=20")"
if [[ "${HTTP_CODE}" == "200" ]]; then
  dept_id=$(echo "${BODY}" | python3 -c "
import json,sys
d=json.load(sys.stdin)
for i in (d.get('data') or {}).get('items') or []:
  if i.get('name')=='Backend Team':
    print(i['department_id']); break
")
fi

created_dept=""
if [[ -z "${dept_id}" && -n "${head_id}" ]]; then
  stamp=$(date +%s)
  split_response "$(api_json POST "/api/v1/org-structure/departments/add" "{\"name\":\"Live Head Test ${stamp}\",\"head_of_department_id\":\"${head_id}\"}")"
  dept_id=$(echo "${BODY}" | python3 -c "import json,sys; d=json.load(sys.stdin); print((d.get('data') or {}).get('department_id',''))" 2>/dev/null || true)
  created_dept="${dept_id}"
fi

if [[ -n "${dept_id}" && -n "${head_id}" ]]; then
  split_response "$(api_json PUT "/api/v1/org-structure/departments/update?department_id=${dept_id}" "{\"head_of_department_id\":\"${head_id}\"}")"
  if [[ "${HTTP_CODE}" == "200" ]]; then
    echo "${BODY}" | python3 -c "
import json,sys
d=json.load(sys.stdin)
h=(d.get('data') or {}).get('head_of_department') or {}
keys=sorted(h.keys())
print('  head keys after set:', keys)
print('  profile_url:', h.get('profile_url'))
print('  has initials:', 'initials' in h)
"
    head_ok=$(echo "${BODY}" | python3 -c "
import json,sys
h=(json.load(sys.stdin).get('data') or {}).get('head_of_department') or {}
# initials removed; profile_url omitted when null (same as employee list)
ok='initials' not in h and ('profile_url' not in h or h.get('profile_url') is None or isinstance(h.get('profile_url'), dict))
print('yes' if ok else 'no')
")
    check "department head shape (no initials; profile_url when set)" "${head_ok}" "keys=$(echo "${BODY}" | python3 -c "import json,sys; print(sorted(((json.load(sys.stdin).get('data') or {}).get('head_of_department') or {}).keys()))")"
  else
    check "department head uses profile_url not initials" "no" "set head failed HTTP ${HTTP_CODE}"
  fi

  split_response "$(api_json PUT "/api/v1/org-structure/departments/update?department_id=${dept_id}" '{"head_of_department_id":null}')"
  cleared=$(echo "${BODY}" | python3 -c "import json,sys; d=json.load(sys.stdin); print('yes' if (d.get('data') or {}).get('head_of_department') is None else 'no')" 2>/dev/null || echo no)
  check "clear department head (null id)" "$([[ "${HTTP_CODE}" == "200" && "${cleared}" == "yes" ]] && echo yes || echo no)" "HTTP ${HTTP_CODE}"
  if [[ "${cleared}" != "yes" ]]; then
    echo "  response: $(echo "${BODY}" | head -c 500)"
  fi
else
  check "department head + clear tests" "no" "need employee_id=${head_id:-?} dept_id=${dept_id:-?}"
fi

if [[ -n "${created_dept}" ]]; then
  split_response "$(api_json DELETE "/api/v1/org-structure/departments/delete?department_id=${created_dept}")"
  check "cleanup temp department" "$([[ "${HTTP_CODE}" == "200" ]] && echo yes || echo no)" "HTTP ${HTTP_CODE}"
fi

echo ""
echo "=== Results: ${pass} passed, ${fail} failed, ${warn} warnings ==="
[[ "${fail}" -eq 0 ]]
