#!/usr/bin/env bash
# Full live smoke: GET reads + POST/PUT/DELETE for Swagger-shipped modules.
# Creates temporary rows on dev and cleans up (archive/delete) at the end of each section.
set -uo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
ENV_FILE="${ROOT}/scripts/local-dev/live-session.env"
JWT_FILE="${ROOT}/scripts/local-dev/.jwt-secret.local"
SAVED_PATH="${PATH:-/usr/bin:/bin:/usr/local/bin:/opt/anaconda3/bin}"

if [[ ! -f "${ENV_FILE}" ]]; then
  echo "Missing ${ENV_FILE}" >&2
  exit 1
fi

# shellcheck source=/dev/null
set -a && source "${ENV_FILE}" && set +a
[[ -f "${JWT_FILE}" ]] && set -a && source "${JWT_FILE}" && set +a
export PATH="${SAVED_PATH}"

BASE="${ZELOSHR_API_BASE:-https://zeloshr.app.backend.dev.trovesuite.com}"
TS="$(date +%s)"
TAG="live-${TS}"

if [[ -z "${TROVE_BEARER_TOKEN:-}" ]]; then
  echo "TROVE_BEARER_TOKEN is empty" >&2
  exit 1
fi

TOKEN="${TROVE_BEARER_TOKEN}"
if [[ -f "${JWT_FILE}" && -n "${TROVESUITE_JWT_SECRET:-}" ]]; then
  TOKEN="$(python3 - <<'PY'
import os, json, base64, time
import jwt

old = os.environ["TROVE_BEARER_TOKEN"]
part = old.split(".")[1]
payload = json.loads(base64.urlsafe_b64decode(part + "=" * (-len(part) % 4)))
secret = os.environ["TROVESUITE_JWT_SECRET"]
claims = {k: v for k, v in payload.items() if k not in ("exp", "iat", "nbf")}
now = int(time.time())
claims["iat"] = now
claims["exp"] = now + 7200
print(jwt.encode(claims, secret, algorithm="HS256"))
PY
)"
  echo "Using refreshed JWT (minted from .jwt-secret.local)"
else
  echo "Using TROVE_BEARER_TOKEN from live-session.env (refresh if expired)"
fi

FAIL=0
LAST_JSON=""
LAST_CODE=""

curl_base=(
  -sS
  -H "app-id: ${TROVE_APP_ID:-app-hr}"
  -H "authorization: Bearer ${TOKEN}"
  -H "bus-id: ${TROVE_BUS_ID}"
  -H "loc-id: ${TROVE_LOC_ID}"
  -H "org-id: ${TROVE_ORG_ID}"
)

json_path() {
  local json="$1"
  local path="$2"
  JSON_INPUT="$json" JSON_PATH="$path" python3 - <<'PY'
import json, os
data = json.loads(os.environ["JSON_INPUT"])
cur = data
for part in os.environ["JSON_PATH"].split("."):
    if cur is None:
        break
    if isinstance(cur, dict):
        cur = cur.get(part)
    elif isinstance(cur, list) and part.isdigit():
        cur = cur[int(part)]
    else:
        cur = None
if cur is None:
    raise SystemExit(1)
print(cur)
PY
}

record() {
  local mark method path
  mark="$1"
  method="$2"
  path="$3"
  local preview
  preview="$(echo "$LAST_JSON" | head -c 700)"
  printf "[%s] HTTP %s %s %s\n%s\n\n" "$mark" "$LAST_CODE" "$method" "$path" "$preview"
  [[ "$mark" == "FAIL" ]] && FAIL=$((FAIL + 1))
  [[ "$mark" == "SKIP" ]] && true
}

record_skip() {
  local method="$1"
  local path="$2"
  local reason="$3"
  printf "[SKIP] %s %s\n  %s\n\n" "$method" "$path" "$reason"
}

api() {
  local method="$1"
  local path="$2"
  local body="${3:-}"
  local accept="${4:-application/json}"

  local raw
  if [[ -n "$body" ]]; then
    raw="$(curl "${curl_base[@]}" -X "$method" -H "accept: ${accept}" \
      -H "content-type: application/json" -d "$body" \
      -w "\n__HTTP__%{http_code}" "${BASE}${path}")"
  else
    raw="$(curl "${curl_base[@]}" -X "$method" -H "accept: ${accept}" \
      -w "\n__HTTP__%{http_code}" "${BASE}${path}")"
  fi
  LAST_CODE="${raw##*__HTTP__}"
  LAST_JSON="${raw%$'\n'__HTTP__*}"

  if [[ "$LAST_CODE" =~ ^2 ]]; then
    record OK "$method" "$path"
    return 0
  fi
  record FAIL "$method" "$path"
  return 1
}

api_get() { api GET "$1" "" "${2:-application/json}"; }
api_post() { api POST "$1" "$2"; }
api_put() { api PUT "$1" "$2"; }
api_delete() { api DELETE "$1" ""; }

echo "ZelosHR base: ${BASE}"
echo "Run tag: ${TAG}"
echo ""

echo "=== GET reads ==="
read_paths=(
  "/api/v1/health"
  "/api/v1/navigation"
  "/api/v1/currencies/list"
  "/api/v1/custom-fields/statistics"
  "/api/v1/custom-fields/entity-types"
  "/api/v1/custom-fields/sections?entity_type=employee"
  "/api/v1/custom-fields/schema?entity_type=employee"
  "/api/v1/custom-fields/list?page=1&size=5"
  "/api/v1/custom-fields/audit-logs?page=1&size=5"
  "/api/v1/org-structure/statistics"
  "/api/v1/org-structure/chart"
  "/api/v1/org-structure/departments?page=1&size=5&sort_by=name&sort_order=asc&include_archived=false"
  "/api/v1/org-structure/branches?page=1&size=5&include_archived=false"
  "/api/v1/employees/statistics"
  "/api/v1/employees/directory/summary"
  "/api/v1/employees/list?page=1&size=5"
  "/api/v1/employees/import/search?query=a"
)
for path in "${read_paths[@]}"; do
  api_get "$path" || true
done
api GET "/api/v1/employees/bulk/template" "" "text/csv,application/json" || true

CURRENCY_ID=""
if api_get "/api/v1/currencies/list"; then
  CURRENCY_ID="$(json_path "$LAST_JSON" "data.0.id" 2>/dev/null || true)"
  if [[ -n "$CURRENCY_ID" ]]; then
    api_get "/api/v1/currencies/get?currency_id=${CURRENCY_ID}" || true
  fi
fi

echo "=== Org structure (POST / PUT / DELETE) ==="
DEPT_ID=""
CHILD_DEPT_ID=""
BRANCH_ID=""

DEPT_BODY="$(printf '{"name":"Engineering %s"}' "$TAG")"
if api_post "/api/v1/org-structure/departments/add" "$DEPT_BODY"; then
  DEPT_ID="$(json_path "$LAST_JSON" "data.department_id")"
  api_put "/api/v1/org-structure/departments/update?department_id=${DEPT_ID}" \
    "$(printf '{"name":"Engineering %s (updated)"}' "$TAG")" || true

  CHILD_BODY="$(printf '{"name":"Platform %s","parent_department_id":"%s"}' "$TAG" "$DEPT_ID")"
  if api_post "/api/v1/org-structure/departments/add" "$CHILD_BODY"; then
    CHILD_DEPT_ID="$(json_path "$LAST_JSON" "data.department_id")"
    api_get "/api/v1/org-structure/chart" || true
  fi
fi

BRANCH_BODY="$(printf '{"name":"Branch %s"}' "$TAG")"
if api_post "/api/v1/org-structure/branches/add" "$BRANCH_BODY"; then
  BRANCH_ID="$(json_path "$LAST_JSON" "data.branch_id")"
  api_put "/api/v1/org-structure/branches/update?branch_id=${BRANCH_ID}" \
    "$(printf '{"name":"Branch %s HQ"}' "$TAG")" || true
fi

echo "=== Custom fields (POST / GET / PUT / DELETE) ==="
FIELD_KEY="live_test_${TS}"
CF_BODY="$(cat <<EOF
{
  "entity_type": "employee",
  "field_key": "${FIELD_KEY}",
  "label": "Live test field ${TAG}",
  "field_type": "text",
  "is_required": false,
  "is_sensitive": false,
  "is_filterable": false,
  "is_searchable": false,
  "display_order": 0,
  "section_name": "employee-directory-identity",
  "section_order": 0,
  "is_active": true
}
EOF
)"
CF_ID=""
if api_post "/api/v1/custom-fields/add" "$CF_BODY"; then
  CF_ID="$(json_path "$LAST_JSON" "data.id")"
  api_get "/api/v1/custom-fields/get?custom_field_id=${CF_ID}" || true
  api_put "/api/v1/custom-fields/update?custom_field_id=${CF_ID}" \
    '{"label":"Live test field (updated)","is_required":true}' || true
  REORDER_BODY="$(printf '{"items":[{"id":"%s","display_order":1,"section_order":0}]}' "$CF_ID")"
  api_put "/api/v1/custom-fields/reorder" "$REORDER_BODY" || true
fi

echo "=== Employees (POST draft / GET / PUT / DELETE) ==="
EMP_EMAIL="live-test-${TS}@example.com"
EMP_BODY="$(cat <<EOF
{
  "status": "draft",
  "identity": {
    "full_name": "Live Test ${TAG}",
    "personal_email": "${EMP_EMAIL}"
  }
}
EOF
)"
EMP_ID=""
if api_post "/api/v1/employees/add" "$EMP_BODY"; then
  EMP_ID="$(json_path "$LAST_JSON" "data.id")"
  api_get "/api/v1/employees/get?employee_id=${EMP_ID}" || true
  UPDATE_BODY="$(cat <<EOF
{
  "identity": {
    "full_name": "Live Test ${TAG} Updated",
    "phone": "+233201234567"
  },
  "employment": {
    "job_title": "QA Engineer"
  }
}
EOF
)"
  api_put "/api/v1/employees/update?employee_id=${EMP_ID}" "$UPDATE_BODY" || true
fi

echo "=== File management (POST multipart / GET / PUT / DELETE) ==="
TENANT="${TROVE_TENANT_ID:-tenant}"
ORG="${TROVE_ORG_ID:-org}"
BUS="${TROVE_BUS_ID:-bus}"
TMPFILE="$(mktemp)"
echo "Live test upload ${TAG}" >"$TMPFILE"
BLOB_PATH="${TENANT}/${ORG}/${BUS}/employees/${TAG}.txt"
DOC_ID=""

upload_raw="$(curl "${curl_base[@]}" -X POST -H "accept: application/json" \
  -F "files=@${TMPFILE};type=text/plain" \
  -w "\n__HTTP__%{http_code}" \
  "${BASE}/api/v1/file/post/multiple?blob_paths=${BLOB_PATH}&descriptions=live-test")"
rm -f "$TMPFILE"
LAST_CODE="${upload_raw##*__HTTP__}"
LAST_JSON="${upload_raw%$'\n'__HTTP__*}"
if [[ "$LAST_CODE" =~ ^2 ]]; then
  record OK POST "/api/v1/file/post/multiple?blob_paths=…"
  DOC_ID="$(json_path "$LAST_JSON" "data.0.id" 2>/dev/null || true)"
  if [[ -n "$DOC_ID" ]]; then
    api_get "/api/v1/file/list?document_ids=${DOC_ID}" || true
    TMPFILE2="$(mktemp)"
    echo "Live test replace ${TAG}" >"$TMPFILE2"
    replace_raw="$(curl "${curl_base[@]}" -X PUT -H "accept: application/json" \
      -F "file=@${TMPFILE2};type=text/plain" \
      -w "\n__HTTP__%{http_code}" \
      "${BASE}/api/v1/file/put?document_id=${DOC_ID}&description=live-test-updated")"
    rm -f "$TMPFILE2"
    LAST_CODE="${replace_raw##*__HTTP__}"
    LAST_JSON="${replace_raw%$'\n'__HTTP__*}"
    if [[ "$LAST_CODE" =~ ^2 ]]; then
      record OK PUT "/api/v1/file/put?document_id=${DOC_ID}"
    else
      record FAIL PUT "/api/v1/file/put?document_id=${DOC_ID}"
    fi
  fi
else
  if echo "$LAST_JSON" | grep -q "DefaultAzureCredential"; then
    record_skip POST "/api/v1/file/post/multiple?blob_paths=…" "Azure Storage credential not available on dev CA."
  else
    record FAIL POST "/api/v1/file/post/multiple?blob_paths=…"
  fi
  echo ""
fi

echo "=== Cleanup (DELETE / archive) ==="
[[ -n "$DOC_ID" ]] && api_delete "/api/v1/file/delete?document_id=${DOC_ID}" || true
[[ -n "$EMP_ID" ]] && api_delete "/api/v1/employees/delete?employee_id=${EMP_ID}" || true
[[ -n "$CF_ID" ]] && api_delete "/api/v1/custom-fields/delete?custom_field_id=${CF_ID}" || true
[[ -n "$CHILD_DEPT_ID" ]] && api_delete "/api/v1/org-structure/departments/delete?department_id=${CHILD_DEPT_ID}" || true
[[ -n "$DEPT_ID" ]] && api_delete "/api/v1/org-structure/departments/delete?department_id=${DEPT_ID}" || true
[[ -n "$BRANCH_ID" ]] && api_delete "/api/v1/org-structure/branches/delete?branch_id=${BRANCH_ID}" || true

echo "Total failures: ${FAIL}"
exit "$FAIL"
