#!/usr/bin/env bash
# Live test: id-card-types + identity.identifications[] on employee add/update/get.
# Verifies deployed dev matches Swagger shape (no flat id_type on identity).
#
#   ./scripts/local-dev/test-employee-identifications.sh
#   CLEANUP=0 ./scripts/local-dev/test-employee-identifications.sh   # leave draft employee
#
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
ENV_FILE="${ROOT}/scripts/local-dev/live-session.env"
JWT_FILE="${ROOT}/scripts/local-dev/.jwt-secret.local"
SAVED_PATH="${PATH:-/usr/bin:/bin:/usr/local/bin:/opt/homebrew/bin}"

if [[ ! -f "${ENV_FILE}" ]]; then
  echo "Missing ${ENV_FILE}. Run:" >&2
  echo "  cp scripts/local-dev/live-session.example.env scripts/local-dev/live-session.env" >&2
  echo "  ./scripts/local-dev/generate-live-session.sh 'eyJhbG...'" >&2
  exit 1
fi

# shellcheck source=/dev/null
set -a && source "${ENV_FILE}" && set +a
[[ -f "${JWT_FILE}" ]] && set -a && source "${JWT_FILE}" && set +a
export PATH="${SAVED_PATH}"
export LIVE_SESSION_JWT_FILE="${JWT_FILE}"

# shellcheck source=scripts/local-dev/live-session-auth.sh
source "${ROOT}/scripts/local-dev/live-session-auth.sh"
ensure_live_session_auth || exit 1

BASE="${ZELOSHR_API_BASE:-https://zhr-admin.dev.backend.trovesuite.com}"
TOKEN="${TROVE_BEARER_TOKEN}"
TS="$(date +%s)"
TAG="id-live-${TS}"
CLEANUP="${CLEANUP:-1}"

curl_base=(
  -sS
  -H "accept: application/json"
  -H "content-type: application/json"
  -H "app-id: ${TROVE_APP_ID:-app-zeloshr}"
  -H "authorization: Bearer ${TOKEN}"
  -H "bus-id: ${TROVE_BUS_ID}"
  -H "loc-id: ${TROVE_LOC_ID}"
  -H "org-id: ${TROVE_ORG_ID}"
)

LAST_CODE=""
LAST_JSON=""
FAIL=0

api() {
  local method="$1"
  local path="$2"
  local body="${3:-}"
  local raw
  if [[ -n "$body" ]]; then
    raw="$(curl "${curl_base[@]}" -X "$method" -d "$body" \
      -w "\n__HTTP__%{http_code}" "${BASE}${path}")"
  else
    raw="$(curl "${curl_base[@]}" -X "$method" \
      -w "\n__HTTP__%{http_code}" "${BASE}${path}")"
  fi
  LAST_CODE="${raw##*__HTTP__}"
  LAST_JSON="${raw%$'\n'__HTTP__*}"
}

json_path() {
  JSON_INPUT="$LAST_JSON" JSON_PATH="$1" python3 - <<'PY'
import json, os, sys
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
    sys.exit(1)
print(cur)
PY
}

step() {
  local label="$1"
  printf "\n=== %s ===\n" "$label"
}

assert_http_2xx() {
  local label="$1"
  if [[ "$LAST_CODE" =~ ^2 ]]; then
    echo "OK HTTP ${LAST_CODE} — ${label}"
    return 0
  fi
  echo "FAIL HTTP ${LAST_CODE} — ${label}" >&2
  echo "$LAST_JSON" | python3 -m json.tool 2>/dev/null || echo "$LAST_JSON"
  FAIL=1
  return 1
}

assert_no_flat_id_fields() {
  local label="$1"
  JSON_INPUT="$LAST_JSON" LABEL="$label" python3 - <<'PY'
import json, os, sys
body = json.loads(os.environ["JSON_INPUT"])
identity = (body.get("data") or {}).get("identity") or {}
flat = [k for k in ("id_type", "id_number", "id_issue_date", "id_expiry_date") if k in identity]
if flat:
    print(f"FAIL — {os.environ['LABEL']}: legacy flat fields still present: {flat}", file=sys.stderr)
    sys.exit(1)
if "identifications" not in identity and not identity:
    print(f"WARN — {os.environ['LABEL']}: empty identity block")
else:
    ids = identity.get("identifications")
    if ids is None:
        print(f"WARN — {os.environ['LABEL']}: identity.identifications missing (API may not be deployed yet)")
    elif not isinstance(ids, list):
        print(f"FAIL — identifications is not an array", file=sys.stderr)
        sys.exit(1)
    else:
        print(f"OK — identity.identifications is array (len={len(ids)})")
        for i, row in enumerate(ids[:3]):
            keys = sorted(row.keys()) if isinstance(row, dict) else []
            print(f"  [{i}] keys: {', '.join(keys)}")
PY
}

step "0. Swagger — employees/add examples include identifications"
SWAGGER_TMP="$(mktemp)"
trap 'rm -f "$SWAGGER_TMP"' EXIT
curl -sS "${curl_base[@]}" "${BASE}/swagger/v1/swagger.json" >"$SWAGGER_TMP"
SWAGGER_CHECK="$(python3 - "$SWAGGER_TMP" <<'PY'
import json, sys
with open(sys.argv[1], encoding="utf-8") as f:
    doc = json.load(f)
paths = doc.get("paths") or {}
add = paths.get("/api/v1/employees/add") or {}
post = add.get("post") or {}
examples = (post.get("requestBody") or {}).get("content") or {}
found_ident = False
legacy_flat = False
for spec in examples.values():
    blobs = []
    if "example" in spec:
        blobs.append(spec["example"])
    for ex in (spec.get("examples") or {}).values():
        val = ex.get("value") if isinstance(ex, dict) else None
        if val:
            blobs.append(val)
    for blob in blobs:
        if not isinstance(blob, dict):
            continue
        ident = blob.get("identity") or {}
        if "identifications" in ident:
            found_ident = True
        if any(k in ident for k in ("id_type", "id_number")):
            legacy_flat = True
if legacy_flat:
    print("FAIL swagger: employees/add still shows flat id_type/id_number")
    sys.exit(0)
if found_ident:
    print("OK swagger: employees/add examples include identity.identifications[]")
else:
    print("WARN swagger: no identifications in add examples (deploy pending?)")
PY
)"
echo "$SWAGGER_CHECK"
[[ "$SWAGGER_CHECK" == FAIL* ]] && FAIL=1

step "1. GET /id-card-types/list"
api GET "/api/v1/id-card-types/list?page=1&size=20"
assert_http_2xx "id-card-types list"
ID_TYPE_ID="$(json_path "data.items.0.id_card_type_id" 2>/dev/null || true)"
ID_TYPE_ID2="$(json_path "data.items.1.id_card_type_id" 2>/dev/null || true)"
if [[ -z "$ID_TYPE_ID" ]]; then
  echo "FAIL — no id card types returned; seed defaults on first list?" >&2
  exit 1
fi
echo "Using id_type_id[0]=${ID_TYPE_ID}"
[[ -n "$ID_TYPE_ID2" ]] && echo "Using id_type_id[1]=${ID_TYPE_ID2}"

step "2. POST /employees/add — draft with identifications[]"
EMP_EMAIL="id-test-${TS}@example.com"
ADD_BODY="$(ID_TYPE_ID="$ID_TYPE_ID" ID_TYPE_ID2="$ID_TYPE_ID2" TS="$TS" TAG="$TAG" EMP_EMAIL="$EMP_EMAIL" python3 - <<'PY'
import json, os
id1 = os.environ["ID_TYPE_ID"]
id2 = os.environ.get("ID_TYPE_ID2") or ""
ts = os.environ["TS"]
tag = os.environ["TAG"]
email = os.environ["EMP_EMAIL"]
idents = [{
    "id_type_id": id1,
    "id_number": f"GHA-LIVE-{ts}",
    "id_issue_date": "2020-01-10",
    "id_expiry_date": "2030-01-10",
}]
if id2:
    idents.append({
        "id_type_id": id2,
        "id_number": f"PASS-{ts}",
        "id_issue_date": "2023-05-01",
        "id_expiry_date": "2033-05-01",
    })
print(json.dumps({
    "identity": {
        "full_name": f"ID Live Test {tag}",
        "phone": "+233201234567",
        "personal_email": email,
        "identifications": idents,
    }
}))
PY
)"
api POST "/api/v1/employees/add" "$ADD_BODY"
assert_http_2xx "create employee with identifications" || {
  if echo "$LAST_JSON" | grep -qi "does not exist\|relation.*identification"; then
    echo "" >&2
    echo "Hint: zhr_employee_identifications table missing — merge tvs-sqlscript dev migration first." >&2
  fi
  if echo "$LAST_JSON" | grep -qi "identifications"; then
    echo "Hint: API may not include identifications yet — merge feat/employee-identifications PR." >&2
  fi
  exit 1
}
EMP_ID="$(json_path "data.id")"
assert_no_flat_id_fields "POST /add response"
ID_ROW_ID="$(json_path "data.identity.identifications.0.id" 2>/dev/null || true)"
ID_COUNT="$(JSON_INPUT="$LAST_JSON" python3 -c "import json,os; print(len(json.loads(os.environ['JSON_INPUT']).get('data',{}).get('identity',{}).get('identifications') or []))")"
echo "employee_id=${EMP_ID} identifications=${ID_COUNT}"

step "3. GET /employees/get — round-trip"
api GET "/api/v1/employees/get?employee_id=${EMP_ID}"
assert_http_2xx "get employee"
assert_no_flat_id_fields "GET response"
JSON_INPUT="$LAST_JSON" python3 - <<'PY'
import json, os, sys
data = json.loads(os.environ["JSON_INPUT"]).get("data") or {}
rows = (data.get("identity") or {}).get("identifications") or []
if not rows:
    print("FAIL — GET returned no identifications", file=sys.stderr)
    sys.exit(1)
row = rows[0]
for key in ("id", "id_type_id", "id_number"):
    if not row.get(key):
        print(f"FAIL — missing {key} on identification row", file=sys.stderr)
        sys.exit(1)
if "id_type_name" not in row:
    print("WARN — id_type_name missing on read (join optional)")
print(f"OK — GET has {len(rows)} identification row(s); first id_type_name={row.get('id_type_name')!r}")
PY

step "4. PUT /employees/update — patch identification by id"
if [[ -n "$ID_ROW_ID" ]]; then
  UPDATE_BODY="$(cat <<EOF
{
  "identity": {
    "identifications": [{
      "id": "${ID_ROW_ID}",
      "id_type_id": "${ID_TYPE_ID}",
      "id_number": "GHA-UPDATED-${TS}",
      "id_issue_date": "2021-02-02",
      "id_expiry_date": "2031-02-02"
    }]
  }
}
EOF
)"
  api PUT "/api/v1/employees/update?employee_id=${EMP_ID}" "$UPDATE_BODY"
  assert_http_2xx "update identification"
  UPDATED_NUM="$(json_path "data.identity.identifications.0.id_number" 2>/dev/null || true)"
  if [[ "$UPDATED_NUM" == "GHA-UPDATED-${TS}" ]]; then
    echo "OK — id_number updated"
  else
    echo "FAIL — expected updated id_number, got ${UPDATED_NUM}" >&2
    FAIL=1
  fi
else
  echo "SKIP — no identification id from create response"
fi

step "5. Cleanup"
if [[ "$CLEANUP" == "1" && -n "${EMP_ID:-}" ]]; then
  api DELETE "/api/v1/employees/delete?employee_id=${EMP_ID}"
  assert_http_2xx "delete test employee" || true
else
  echo "Skipped delete (employee_id=${EMP_ID})"
fi

echo ""
if [[ "$FAIL" -eq 0 ]]; then
  echo "PASS — employee identifications live test"
  exit 0
fi
echo "FAIL — see errors above"
exit 1
