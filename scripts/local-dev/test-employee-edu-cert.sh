#!/usr/bin/env bash
# GET → PUT (sync collapse) → PUT (update with ids) → PUT (repeat) → GET
# Monitors education/certification row counts for duplicate-on-resave regressions.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
ENV_FILE="${ROOT}/scripts/local-dev/live-session.env"
JWT_FILE="${ROOT}/scripts/local-dev/.jwt-secret.local"
SAVED_PATH="${PATH:-/usr/bin:/bin:/usr/local/bin:/opt/homebrew/bin:/opt/homebrew/opt/libpq/bin}"

if [[ ! -f "${ENV_FILE}" ]]; then
  echo "Missing ${ENV_FILE}. cp scripts/local-dev/live-session.example.env" >&2
  exit 1
fi

# shellcheck source=/dev/null
set -a && source "${ENV_FILE}" && set +a
[[ -f "${JWT_FILE}" ]] && set -a && source "${JWT_FILE}" && set +a
export PATH="${SAVED_PATH}"

# shellcheck source=/dev/null
source "${ROOT}/scripts/local-dev/live-session-auth.sh"
ensure_live_session_auth || {
  echo "Refresh token: ./scripts/local-dev/generate-live-session.sh 'eyJhbG...'" >&2
  exit 1
}

BASE="${ZELOSHR_API_BASE:-https://zeloshr.app.backend.dev.trovesuite.com}"
EMP_ID="${EMPLOYEE_ID:-3db13bc3-19dc-4cc2-8a5f-7e6a0efaccc2}"
TOKEN="${TROVE_BEARER_TOKEN}"

curl_base=(
  -sS
  -H "accept: application/json"
  -H "content-type: application/json"
  -H "app-id: ${TROVE_APP_ID:-app-hr}"
  -H "authorization: Bearer ${TOKEN}"
  -H "bus-id: ${TROVE_BUS_ID}"
  -H "loc-id: ${TROVE_LOC_ID}"
  -H "org-id: ${TROVE_ORG_ID}"
)

api_get() {
  local path="$1"
  local raw code body
  raw="$(/usr/bin/curl "${curl_base[@]}" -w "\n__HTTP__%{http_code}" "${BASE}${path}")"
  code="${raw##*__HTTP__}"
  body="${raw%__HTTP__*}"
  LAST_CODE="$code"
  LAST_JSON="$body"
  echo "$code"
}

api_put() {
  local path="$1"
  local json="$2"
  local raw code body
  raw="$(/usr/bin/curl "${curl_base[@]}" -X PUT -d "${json}" -w "\n__HTTP__%{http_code}" "${BASE}${path}")"
  code="${raw##*__HTTP__}"
  body="${raw%__HTTP__*}"
  LAST_CODE="$code"
  LAST_JSON="$body"
  echo "$code"
}

print_counts() {
  local label="$1"
  LABEL="$label" JSON_INPUT="$LAST_JSON" python3 - <<'PY'
import json, os
label = os.environ["LABEL"]
d = json.loads(os.environ["JSON_INPUT"])
if d.get("success") is False or d.get("status_code", 200) >= 400:
    print(f"{label}: API error {d.get('status_code')} — {d.get('detail', d.get('error', ''))[:120]}")
    raise SystemExit(0)
data = d.get("data") or {}
edu = data.get("education") or []
cert = data.get("certifications") or []
print(f"{label}: education={len(edu)} certifications={len(cert)}")
for i, e in enumerate(edu[:5]):
    print(f"  edu[{i}] id={e.get('id')} degree={e.get('degree')!r} institution={e.get('institution')!r}")
if len(edu) > 5:
    print(f"  ... +{len(edu)-5} more education rows")
for i, c in enumerate(cert[:5]):
    print(f"  cert[{i}] id={c.get('id')} name={c.get('name')!r}")
if len(cert) > 5:
    print(f"  ... +{len(cert)-5} more certification rows")
PY
}

fail=0
LAST_CODE=""
LAST_JSON=""
echo "ZelosHR: ${BASE}"
echo "Employee: ${EMP_ID}"
echo ""

echo "=== 1. GET baseline ==="
api_get "/api/v1/employees/get?employee_id=${EMP_ID}"
code="${LAST_CODE}"
echo "HTTP ${code}"
[[ "$code" =~ ^2 ]] || fail=1
print_counts "baseline"

EDU_ID="$(JSON_INPUT="$LAST_JSON" python3 -c "import json,os; d=json.loads(os.environ['JSON_INPUT']); print((d.get('data',{}).get('education') or [{}])[0].get('id',''))")"
CERT_ID="$(JSON_INPUT="$LAST_JSON" python3 -c "import json,os; d=json.loads(os.environ['JSON_INPUT']); print((d.get('data',{}).get('certifications') or [{}])[0].get('id',''))")"

if [[ -z "$EDU_ID" || -z "$CERT_ID" ]]; then
  echo "No education/certification ids on GET — cannot continue upsert test." >&2
  exit 1
fi

echo ""
echo "=== 2. PUT sync — collapse to one education + one certification ==="
SYNC_BODY="$(cat <<EOF
{
  "sync_education": true,
  "sync_certifications": true,
  "education": [{
    "id": "${EDU_ID}",
    "institution": "University of Ghana",
    "degree": "BSc Computer Science",
    "field_of_study": "Computer Science",
    "is_current": false
  }],
  "certifications": [{
    "id": "${CERT_ID}",
    "name": "Masters in react fundamentals",
    "issuing_body": "Udemy"
  }]
}
EOF
)"
api_put "/api/v1/employees/update?employee_id=${EMP_ID}" "$SYNC_BODY"
code="${LAST_CODE}"
echo "HTTP ${code}"
[[ "$code" =~ ^2 ]] || fail=1
print_counts "after sync"

echo ""
echo "=== 3. PUT update with ids (degree/name change) ==="
UPDATE_BODY="$(cat <<EOF
{
  "education": [{
    "id": "${EDU_ID}",
    "institution": "University of Ghana",
    "degree": "MSc Computer Science",
    "field_of_study": "Computer Science",
    "is_current": false
  }],
  "certifications": [{
    "id": "${CERT_ID}",
    "name": "AWS Solutions Architect Professional",
    "issuing_body": "Amazon Web Services"
  }]
}
EOF
)"
api_put "/api/v1/employees/update?employee_id=${EMP_ID}" "$UPDATE_BODY"
code="${LAST_CODE}"
echo "HTTP ${code}"
[[ "$code" =~ ^2 ]] || fail=1
print_counts "after update"

echo ""
echo "=== 4. PUT repeat same payload (must NOT duplicate rows) ==="
api_put "/api/v1/employees/update?employee_id=${EMP_ID}" "$UPDATE_BODY"
code="${LAST_CODE}"
echo "HTTP ${code}"
[[ "$code" =~ ^2 ]] || fail=1
print_counts "after repeat"

echo ""
echo "=== 4b. PUT add row with client UUID (edit flow — new education + cert section) ==="
CLIENT_EDU_ID="aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"
CLIENT_CERT_ID="bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"
ADD_CLIENT_BODY="$(cat <<EOF
{
  "education": [
    {
      "id": "${EDU_ID}",
      "institution": "University of Ghana",
      "degree": "MSc Computer Science",
      "field_of_study": "Computer Science",
      "is_current": false
    },
    {
      "id": "${CLIENT_EDU_ID}",
      "institution": "KNUST",
      "degree": "PhD Informatics",
      "is_current": true
    }
  ],
  "certifications": [
    {
      "id": "${CERT_ID}",
      "name": "AWS Solutions Architect Professional",
      "issuing_body": "Amazon Web Services"
    },
    {
      "id": "${CLIENT_CERT_ID}",
      "name": "Scrum Master",
      "issuing_body": "Scrum Alliance"
    }
  ]
}
EOF
)"
api_put "/api/v1/employees/update?employee_id=${EMP_ID}" "$ADD_CLIENT_BODY"
code="${LAST_CODE}"
echo "HTTP ${code}"
[[ "$code" =~ ^2 ]] || fail=1
print_counts "after client-uuid add"

EDU_COUNT="$(JSON_INPUT="$LAST_JSON" python3 -c "import json,os; print(len(json.loads(os.environ['JSON_INPUT']).get('data',{}).get('education') or []))")"
CERT_COUNT="$(JSON_INPUT="$LAST_JSON" python3 -c "import json,os; print(len(json.loads(os.environ['JSON_INPUT']).get('data',{}).get('certifications') or []))")"
EDU_DEGREE="$(JSON_INPUT="$LAST_JSON" python3 -c "import json,os; e=(json.loads(os.environ['JSON_INPUT']).get('data',{}).get('education') or [{}])[0]; print(e.get('degree',''))")"
CERT_NAME="$(JSON_INPUT="$LAST_JSON" python3 -c "import json,os; c=(json.loads(os.environ['JSON_INPUT']).get('data',{}).get('certifications') or [{}])[0]; print(c.get('name',''))")"

echo ""
echo "=== 5. GET verify ==="
api_get "/api/v1/employees/get?employee_id=${EMP_ID}"
code="${LAST_CODE}"
echo "HTTP ${code}"
[[ "$code" =~ ^2 ]] || fail=1
print_counts "final GET"

echo ""
echo "=== Summary ==="
if [[ "$EDU_COUNT" == "2" && "$CERT_COUNT" == "2" ]]; then
  echo "PASS — edit + new sections: 2 education, 2 certifications (client UUID rows added)."
elif [[ "$EDU_COUNT" == "1" && "$CERT_COUNT" == "1" && "$EDU_DEGREE" == "MSc Computer Science" && "$CERT_NAME" == "AWS Solutions Architect Professional" ]]; then
  echo "PASS — id upsert + repeat save: 1 education, 1 certification, values updated, no duplicates."
else
  echo "FAIL — expected 2/2 after client-uuid add or 1/1 after repeat; got edu=${EDU_COUNT} cert=${CERT_COUNT} degree=${EDU_DEGREE} cert_name=${CERT_NAME}"
  fail=1
fi

exit "$fail"
