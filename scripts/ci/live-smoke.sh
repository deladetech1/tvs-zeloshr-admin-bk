#!/usr/bin/env bash
# Live smoke against deployed dev (or ZELOSHR_API_BASE).
# Reads Trove headers from live-session.env when run locally.
set -uo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
ENV_FILE="${ROOT}/scripts/local-dev/live-session.env"
JWT_FILE="${ROOT}/scripts/local-dev/.jwt-secret.local"
SAVED_PATH="${PATH:-/usr/bin:/bin:/usr/local/bin:/opt/anaconda3/bin}"

if [[ -f "${ENV_FILE}" && -z "${TROVE_BEARER_TOKEN:-}" ]]; then
  # shellcheck source=/dev/null
  set -a && source "${ENV_FILE}" && set +a
  [[ -f "${JWT_FILE}" ]] && set -a && source "${JWT_FILE}" && set +a
  export PATH="${SAVED_PATH}"
  export LIVE_SESSION_JWT_FILE="${JWT_FILE}"
  # shellcheck source=scripts/local-dev/live-session-auth.sh
  source "${ROOT}/scripts/local-dev/live-session-auth.sh"
  ensure_live_session_auth || exit 1
fi

# shellcheck source=scripts/ci/api-response-format.sh
source "${ROOT}/scripts/ci/api-response-format.sh"

BASE="${ZELOSHR_API_BASE:-https://zeloshr.app.backend.dev.trovesuite.com}"
TOKEN="${TROVE_BEARER_TOKEN:-}"
ORG="${TROVE_ORG_ID:-}"
BUS="${TROVE_BUS_ID:-}"
LOC="${TROVE_LOC_ID:-}"
APP_ID="${TROVE_APP_ID:-app-hr}"

missing=()
[[ -z "$TOKEN" ]] && missing+=("TROVE_BEARER_TOKEN (live-session.env)")
[[ -z "$ORG" ]] && missing+=("TROVE_ORG_ID")
[[ -z "$BUS" ]] && missing+=("TROVE_BUS_ID")
[[ -z "$LOC" ]] && missing+=("TROVE_LOC_ID")

if ((${#missing[@]} > 0)); then
  echo "Live smoke missing: ${missing[*]}" >&2
  echo "  cp scripts/local-dev/live-session.example.env scripts/local-dev/live-session.env" >&2
  echo "  ./scripts/local-dev/refresh-live-session.sh 'eyJhbG...'" >&2
  exit 1
fi

if [[ -z "${SKIP_LIVE_WAIT:-}" ]]; then
  wait_for_api_health "${BASE}" 18 10
fi

TS="$(date +%s)"
TAG="ci-${TS}"
FAIL=0

curl_base=(
  -sS
  -H "app-id: ${APP_ID}"
  -H "authorization: Bearer ${TOKEN}"
  -H "bus-id: ${BUS}"
  -H "loc-id: ${LOC}"
  -H "org-id: ${ORG}"
)

check() {
  local label="$1"
  local method="$2"
  local path="$3"
  local body="${4:-}"
  local raw code json

  if [[ -n "$body" ]]; then
    raw="$(curl "${curl_base[@]}" -X "$method" -H "content-type: application/json" -d "$body" \
      -w "\n__HTTP__%{http_code}" "${BASE}${path}")"
  else
    raw="$(curl "${curl_base[@]}" -X "$method" -w "\n__HTTP__%{http_code}" "${BASE}${path}")"
  fi

  code="${raw##*__HTTP__}"
  json="${raw%$'\n'__HTTP__*}"

  if [[ "$code" =~ ^2 ]]; then
    echo "[OK]   HTTP ${code} ${method} ${path}"
    return 0
  fi

  echo "[FAIL] HTTP ${code} ${method} ${path}"
  format_api_response "$json" | sed 's/^/        /'
  FAIL=$((FAIL + 1))
  return 1
}

json_path() {
  local json="$1"
  local path="$2"
  JSON_INPUT="$json" JSON_PATH="$path" python3 - <<'PY'
import json, os, sys
data = json.loads(os.environ["JSON_INPUT"])
cur = data
for part in os.environ["JSON_PATH"].split("."):
    if cur is None:
        break
    if isinstance(cur, dict):
        cur = cur.get(part)
    else:
        cur = None
if cur is None:
    sys.exit(1)
print(cur)
PY
}

echo "Live smoke → ${BASE} (tag ${TAG})"

check "health" GET "/api/v1/health" || true
check "navigation" GET "/api/v1/navigation" || true
check "org stats" GET "/api/v1/org-structure/statistics" || true
check "employees stats" GET "/api/v1/employees/statistics" || true

BRANCH_BODY="$(printf '{"name":"CI Branch %s","city":"Accra","region":"Greater Accra","country_code":"GH"}' "$TAG")"
BRANCH_ID=""
branch_raw="$(curl "${curl_base[@]}" -X POST -H "content-type: application/json" -d "$BRANCH_BODY" \
  -w "\n__HTTP__%{http_code}" "${BASE}/api/v1/org-structure/branches/add")"
branch_code="${branch_raw##*__HTTP__}"
branch_json="${branch_raw%$'\n'__HTTP__*}"
if [[ "$branch_code" =~ ^2 ]]; then
  echo "[OK]   HTTP ${branch_code} POST /api/v1/org-structure/branches/add"
  BRANCH_ID="$(json_path "$branch_json" "data.branch_id" 2>/dev/null || true)"
else
  echo "[FAIL] HTTP ${branch_code} POST /api/v1/org-structure/branches/add"
  format_api_response "$branch_json" | sed 's/^/        /'
  FAIL=$((FAIL + 1))
fi

EMP_BODY="$(cat <<EOF
{
  "status": "draft",
  "identity": {
    "full_name": "CI Live ${TAG}",
    "phone": "+233200000001"
  }
}
EOF
)"
EMP_ID=""
emp_raw="$(curl "${curl_base[@]}" -X POST -H "content-type: application/json" -d "$EMP_BODY" \
  -w "\n__HTTP__%{http_code}" "${BASE}/api/v1/employees/add")"
emp_code="${emp_raw##*__HTTP__}"
emp_json="${emp_raw%$'\n'__HTTP__*}"
if [[ "$emp_code" =~ ^2 ]]; then
  echo "[OK]   HTTP ${emp_code} POST /api/v1/employees/add"
  EMP_ID="$(json_path "$emp_json" "data.id" 2>/dev/null || true)"
else
  echo "[FAIL] HTTP ${emp_code} POST /api/v1/employees/add"
  format_api_response "$emp_json" | sed 's/^/        /'
  FAIL=$((FAIL + 1))
fi

# Work arrangement rule: remote + branch must fail with a clear field error.
if [[ -n "$BRANCH_ID" && -n "$EMP_ID" ]]; then
  bad_body="$(printf '{"employment":{"work_arrangement":"remote","branch_id":"%s"}}' "$BRANCH_ID")"
  bad_raw="$(curl "${curl_base[@]}" -X PUT -H "content-type: application/json" -d "$bad_body" \
    -w "\n__HTTP__%{http_code}" "${BASE}/api/v1/employees/update?employee_id=${EMP_ID}")"
  bad_code="${bad_raw##*__HTTP__}"
  bad_json="${bad_raw%$'\n'__HTTP__*}"
  if [[ "$bad_code" == "400" ]]; then
    echo "[OK]   HTTP 400 PUT /api/v1/employees/update (remote+branch rejected)"
    format_api_response "$bad_json" | sed 's/^/        /'
  else
    echo "[FAIL] expected HTTP 400 for remote+branch, got ${bad_code}"
    format_api_response "$bad_json" | sed 's/^/        /'
    FAIL=$((FAIL + 1))
  fi
fi

echo "=== Cleanup ==="
[[ -n "$EMP_ID" ]] && check "cleanup employee" DELETE "/api/v1/employees/delete?employee_id=${EMP_ID}" || true
[[ -n "$BRANCH_ID" ]] && check "cleanup branch" DELETE "/api/v1/org-structure/branches/delete?branch_id=${BRANCH_ID}" || true

echo "Live smoke failures: ${FAIL}"
exit "$FAIL"
