#!/usr/bin/env bash
# Full live smoke: GET reads + POST/PUT/DELETE for Swagger-shipped modules.
# Creates temporary rows on dev and cleans up (archive/delete) at the end of each section.
set -uo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
ENV_FILE="${ROOT}/scripts/local-dev/live-session.env"
JWT_FILE="${ROOT}/scripts/local-dev/.jwt-secret.local"
SAVED_PATH="${PATH:-/usr/bin:/bin:/usr/local/bin:/opt/anaconda3/bin}"
# shellcheck source=scripts/ci/api-response-format.sh
source "${ROOT}/scripts/ci/api-response-format.sh"

if [[ ! -f "${ENV_FILE}" ]]; then
  echo "Missing ${ENV_FILE}" >&2
  exit 1
fi

# shellcheck source=/dev/null
set -a && source "${ENV_FILE}" && set +a
[[ -f "${JWT_FILE}" ]] && set -a && source "${JWT_FILE}" && set +a
export PATH="${SAVED_PATH}"
export LIVE_SESSION_JWT_FILE="${JWT_FILE}"

# shellcheck source=scripts/local-dev/live-session-auth.sh
source "${ROOT}/scripts/local-dev/live-session-auth.sh"

BASE="${ZELOSHR_API_BASE:-https://zeloshr.app.backend.dev.trovesuite.com}"
TS="$(date +%s)"
TAG="live-${TS}"
DUE_DATE="$(python3 -c "from datetime import date, timedelta; print((date.today()+timedelta(days=30)).isoformat())")"

ensure_live_session_auth || exit 1
TOKEN="${TROVE_BEARER_TOKEN}"

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

# Print document registry fields from GET /file/list (or PUT response data object).
print_file_document() {
  local json="$1"
  FILE_DOC_JSON="$json" python3 - <<'PY'
import json, os, sys

raw = os.environ.get("FILE_DOC_JSON", "")
try:
    body = json.loads(raw)
except json.JSONDecodeError:
    sys.exit(0)

data = body.get("data")
items = data if isinstance(data, list) else ([data] if isinstance(data, dict) else [])
for item in items:
    if not isinstance(item, dict):
        continue
    doc_id = item.get("id") or item.get("document_id") or ""
    if doc_id:
        print(f"  document_id:   {doc_id}")
    if item.get("file_name"):
        print(f"  file_name:     {item['file_name']}")
    if item.get("description") is not None:
        print(f"  description:   {item['description']}")
    url = item.get("presigned_url") or ""
    if url:
        preview = url if len(url) <= 160 else url[:160] + "…"
        print(f"  presigned_url: {preview}")
PY
}

record() {
  local mark method path
  mark="$1"
  method="$2"
  path="$3"
  local summary
  summary="$(format_api_response "$LAST_JSON")"
  printf "[%s] HTTP %s %s %s\n" "$mark" "$LAST_CODE" "$method" "$path"
  if [[ -n "$summary" ]]; then
    printf "  %s\n" "$summary"
  fi
  printf "\n"
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
  "/api/v1/org-structure/departments/list?page=1&size=5&sort_by=name&sort_order=asc&include_archived=false"
  "/api/v1/org-structure/branches/list?page=1&size=5&include_archived=false"
  "/api/v1/employees/statistics"
  "/api/v1/employees/directory/summary"
  "/api/v1/employees/list?page=1&size=5"
  "/api/v1/employees/import/search?query=a"
  "/api/v1/lifecycle-events/statistics"
  "/api/v1/lifecycle-events/list?page=1&size=5"
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

BRANCH_BODY="$(printf '{"name":"Branch %s","address":"Greater Accra, 4th Avenue 128B","country":"Ghana","description":null}' "$TAG")"
if api_post "/api/v1/org-structure/branches/add" "$BRANCH_BODY"; then
  BRANCH_ID="$(json_path "$LAST_JSON" "data.branch_id")"
  api_put "/api/v1/org-structure/branches/update?branch_id=${BRANCH_ID}" \
    "$(printf '{"name":"Branch %s HQ","address":"1 Canada Square, Canary Wharf","country":"United Kingdom"}' "$TAG")" || true
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
    "phone": "+233201234567",
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

  echo "=== Employee education + certifications (id upsert) ==="
  EDU_CERT_ADD="$(cat <<EOF
{
  "education": [{
    "institution": "University of Ghana",
    "degree": "BSc Computer Science",
    "field_of_study": "Computer Science"
  }],
  "certifications": [{
    "name": "AWS Solutions Architect",
    "issuing_body": "Amazon Web Services"
  }]
}
EOF
)"
  if api_put "/api/v1/employees/update?employee_id=${EMP_ID}" "$EDU_CERT_ADD"; then
    if api_get "/api/v1/employees/get?employee_id=${EMP_ID}"; then
      EDU_ID="$(json_path "$LAST_JSON" "data.education.0.id" 2>/dev/null || true)"
      CERT_ID="$(json_path "$LAST_JSON" "data.certifications.0.id" 2>/dev/null || true)"
      if [[ -n "$EDU_ID" && -n "$CERT_ID" ]]; then
        EDU_CERT_UPDATE="$(cat <<EOF
{
  "education": [{
    "id": "${EDU_ID}",
    "institution": "University of Ghana",
    "degree": "MSc Computer Science",
    "field_of_study": "Computer Science"
  }],
  "certifications": [{
    "id": "${CERT_ID}",
    "name": "AWS Solutions Architect Professional",
    "issuing_body": "Amazon Web Services"
  }]
}
EOF
)"
        api_put "/api/v1/employees/update?employee_id=${EMP_ID}" "$EDU_CERT_UPDATE" || true
        if api_get "/api/v1/employees/get?employee_id=${EMP_ID}"; then
          EDU_DEGREE="$(json_path "$LAST_JSON" "data.education.0.degree" 2>/dev/null || true)"
          CERT_NAME="$(json_path "$LAST_JSON" "data.certifications.0.name" 2>/dev/null || true)"
          EDU_COUNT="$(JSON_INPUT="$LAST_JSON" python3 - <<'PY'
import json, os
data = json.loads(os.environ["JSON_INPUT"])
print(len(data.get("data", {}).get("education") or []))
PY
)"
          CERT_COUNT="$(JSON_INPUT="$LAST_JSON" python3 - <<'PY'
import json, os
data = json.loads(os.environ["JSON_INPUT"])
print(len(data.get("data", {}).get("certifications") or []))
PY
)"
          if [[ "$EDU_DEGREE" == "MSc Computer Science" && "$CERT_NAME" == "AWS Solutions Architect Professional" && "$EDU_COUNT" == "1" && "$CERT_COUNT" == "1" ]]; then
            record OK PUT "/api/v1/employees/update (education+cert id upsert)"
          else
            LAST_CODE="409"
            record FAIL PUT "/api/v1/employees/update (education+cert id upsert — duplicate or wrong values)"
          fi
          api_put "/api/v1/employees/update?employee_id=${EMP_ID}" "$EDU_CERT_UPDATE" || true
          if api_get "/api/v1/employees/get?employee_id=${EMP_ID}"; then
            EDU_COUNT2="$(JSON_INPUT="$LAST_JSON" python3 - <<'PY'
import json, os
data = json.loads(os.environ["JSON_INPUT"])
print(len(data.get("data", {}).get("education") or []))
PY
)"
            CERT_COUNT2="$(JSON_INPUT="$LAST_JSON" python3 - <<'PY'
import json, os
data = json.loads(os.environ["JSON_INPUT"])
print(len(data.get("data", {}).get("certifications") or []))
PY
)"
            if [[ "$EDU_COUNT2" == "1" && "$CERT_COUNT2" == "1" ]]; then
              record OK PUT "/api/v1/employees/update (re-save with ids — no duplicate)"
            else
              LAST_CODE="409"
              record FAIL PUT "/api/v1/employees/update (re-save with ids — expected 1 row each)"
            fi
          fi
        fi
      else
        record_skip PUT "/api/v1/employees/update (education+cert)" "Missing education or certification id on GET."
      fi
    fi
  fi
fi

echo "=== Lifecycle events (POST / GET / PUT / DELETE) ==="
LC_ID=""
if [[ -n "$EMP_ID" ]]; then
  LC_BODY="$(cat <<EOF
{
  "employee_id": "${EMP_ID}",
  "event_type": "Probation review ${TAG}",
  "due_date": "${DUE_DATE}",
  "status": "Pending",
  "urgency": "Upcoming"
}
EOF
)"
  if api_post "/api/v1/lifecycle-events/add" "$LC_BODY"; then
    LC_ID="$(json_path "$LAST_JSON" "data.lifecycle_event_id" 2>/dev/null || true)"
    [[ -n "$LC_ID" ]] && api_get "/api/v1/lifecycle-events/get?lifecycle_event_id=${LC_ID}" || true
    [[ -n "$LC_ID" ]] && api_put "/api/v1/lifecycle-events/update?lifecycle_event_id=${LC_ID}" \
      '{"status":"Awaiting Manager","urgency":"Critical"}' || true
  fi
else
  record_skip POST "/api/v1/lifecycle-events/add" "No employee_id from prior step."
fi

echo "=== File management (POST multipart / GET / PUT / DELETE) ==="
TMPFILE="$(mktemp)"
echo "Live test upload ${TAG}" >"$TMPFILE"
DOC_ID=""

upload_raw="$(curl "${curl_base[@]}" -X POST -H "accept: application/json" \
  -F "files=@${TMPFILE};type=text/plain" \
  -w "\n__HTTP__%{http_code}" \
  "${BASE}/api/v1/file/post/multiple?descriptions=live-test")"
rm -f "$TMPFILE"
LAST_CODE="${upload_raw##*__HTTP__}"
LAST_JSON="${upload_raw%$'\n'__HTTP__*}"
if [[ "$LAST_CODE" =~ ^2 ]]; then
  record OK POST "/api/v1/file/post/multiple (auto blob path)"
  DOC_ID="$(json_path "$LAST_JSON" "data.0.id" 2>/dev/null || true)"
  if [[ -n "$DOC_ID" ]]; then
    echo "  uploaded document_id: ${DOC_ID}"
    if api_get "/api/v1/file/list?document_ids=${DOC_ID}"; then
      print_file_document "$LAST_JSON"
    fi
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
      echo "  after PUT (presigned_url in response):"
      print_file_document "$LAST_JSON"
      if api_get "/api/v1/file/list?document_ids=${DOC_ID}"; then
        echo "  after PUT (GET /file/list):"
        print_file_document "$LAST_JSON"
      fi
    else
      record FAIL PUT "/api/v1/file/put?document_id=${DOC_ID}"
    fi
    if [[ -n "$DOC_ID" && -n "$EMP_ID" ]]; then
      echo "=== Employee documents (attach + read documents[]) ==="
      api_put "/api/v1/employees/update?employee_id=${EMP_ID}" \
        "$(printf '{"document_ids":["%s"]}' "$DOC_ID")" || true
      if api_get "/api/v1/employees/get?employee_id=${EMP_ID}"; then
        DOC_COUNT="$(json_path "$LAST_JSON" "data.documents.0.doc_id" 2>/dev/null || true)"
        if [[ -n "$DOC_COUNT" ]]; then
          echo "  employee documents[0].doc_id: ${DOC_COUNT}"
        else
          LEGACY_ID="$(json_path "$LAST_JSON" "data.documents.0.id" 2>/dev/null || true)"
          [[ -n "$LEGACY_ID" ]] && echo "  employee documents[0].id (legacy): ${LEGACY_ID}"
        fi
      fi
    fi
  fi
else
  if echo "$LAST_JSON" | grep -q "DefaultAzureCredential"; then
    record_skip POST "/api/v1/file/post/multiple" "Azure Storage credential not available on dev CA."
  else
    record FAIL POST "/api/v1/file/post/multiple"
  fi
  echo ""
fi

echo "=== Cleanup (DELETE / archive) ==="
[[ -n "$LC_ID" ]] && api_delete "/api/v1/lifecycle-events/delete?lifecycle_event_id=${LC_ID}" || true
[[ -n "$DOC_ID" ]] && api_delete "/api/v1/file/delete?document_id=${DOC_ID}" || true
[[ -n "$EMP_ID" ]] && api_delete "/api/v1/employees/delete?employee_id=${EMP_ID}" || true
[[ -n "$CF_ID" ]] && api_delete "/api/v1/custom-fields/delete?custom_field_id=${CF_ID}" || true
[[ -n "$CHILD_DEPT_ID" ]] && api_delete "/api/v1/org-structure/departments/delete?department_id=${CHILD_DEPT_ID}" || true
[[ -n "$DEPT_ID" ]] && api_delete "/api/v1/org-structure/departments/delete?department_id=${DEPT_ID}" || true
[[ -n "$BRANCH_ID" ]] && api_delete "/api/v1/org-structure/branches/delete?branch_id=${BRANCH_ID}" || true

echo "Total failures: ${FAIL}"
exit "$FAIL"
