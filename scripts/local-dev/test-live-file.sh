#!/usr/bin/env bash
# Live file upload → GET presigned URL → optional DELETE.
# Requires scripts/local-dev/live-session.env (see live-session.example.env).
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
KEEP="${KEEP:-0}"
TS="$(date +%s)"
TAG="live-file-${TS}"

ensure_live_session_auth || exit 1
TOKEN="${TROVE_BEARER_TOKEN}"

curl_base=(
  -sS
  -H "app-id: ${TROVE_APP_ID:-app-hr}"
  -H "authorization: Bearer ${TOKEN}"
  -H "bus-id: ${TROVE_BUS_ID}"
  -H "loc-id: ${TROVE_LOC_ID}"
  -H "org-id: ${TROVE_ORG_ID}"
)

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
    if item.get("id"):
        print(f"  document_id:   {item['id']}")
    if item.get("file_name"):
        print(f"  file_name:     {item['file_name']}")
    if item.get("description") is not None:
        print(f"  description:   {item['description']}")
    url = item.get("presigned_url") or ""
    if url:
        print(f"  presigned_url: {url}")
PY
}

TENANT="${TROVE_TENANT_ID:-tenant}"
ORG="${TROVE_ORG_ID:-org}"
BUS="${TROVE_BUS_ID:-bus}"
TMPFILE="$(mktemp)"
echo "Live file test ${TAG}" >"$TMPFILE"
DESCRIPTION="${DESCRIPTION:-live-file-test}"

echo "Base: ${BASE}"
echo "Container: zeloshr (server config)"
echo "Path: auto → {tenant}/{org}/{bus}/employees/documents/{unique}-{filename}"
echo "=== POST /api/v1/file/post/multiple ==="
upload_raw="$(curl "${curl_base[@]}" -X POST -H "accept: application/json" \
  -F "files=@${TMPFILE};type=text/plain;filename=${TAG}.txt" \
  -w "\n__HTTP__%{http_code}" \
  "${BASE}/api/v1/file/post/multiple?descriptions=${DESCRIPTION}")"
rm -f "$TMPFILE"

UP_CODE="${upload_raw##*__HTTP__}"
UP_JSON="${upload_raw%$'\n'__HTTP__*}"
echo "HTTP ${UP_CODE}"
if [[ ! "$UP_CODE" =~ ^2 ]]; then
  format_api_response "$UP_JSON" | sed 's/^/  /'
  exit 1
fi

DOC_ID="$(JSON_INPUT="$UP_JSON" JSON_PATH="data.0.id" python3 - <<'PY'
import json, os, sys
data = json.loads(os.environ["JSON_INPUT"])
cur = data
for part in os.environ["JSON_PATH"].split("."):
    cur = cur.get(part) if isinstance(cur, dict) else (cur[int(part)] if isinstance(cur, list) and part.isdigit() else None)
if cur is None: sys.exit(1)
print(cur)
PY
)" || true

if [[ -z "$DOC_ID" ]]; then
  echo "Upload OK but no document id in response" >&2
  exit 1
fi
echo "  document_id: ${DOC_ID}"

echo ""
echo "=== GET /api/v1/file/list?document_ids=${DOC_ID} ==="
list_raw="$(curl "${curl_base[@]}" -w "\n__HTTP__%{http_code}" \
  "${BASE}/api/v1/file/list?document_ids=${DOC_ID}")"
LIST_CODE="${list_raw##*__HTTP__}"
LIST_JSON="${list_raw%$'\n'__HTTP__*}"
echo "HTTP ${LIST_CODE}"
if [[ ! "$LIST_CODE" =~ ^2 ]]; then
  format_api_response "$LIST_JSON" | sed 's/^/  /'
  exit 1
fi
print_file_document "$LIST_JSON"

if [[ "$KEEP" != "1" ]]; then
  echo ""
  echo "=== DELETE /api/v1/file/delete?document_id=${DOC_ID} ==="
  del_raw="$(curl "${curl_base[@]}" -X DELETE -w "\n__HTTP__%{http_code}" \
    "${BASE}/api/v1/file/delete?document_id=${DOC_ID}")"
  DEL_CODE="${del_raw##*__HTTP__}"
  DEL_JSON="${del_raw%$'\n'__HTTP__*}"
  echo "HTTP ${DEL_CODE}"
  format_api_response "$DEL_JSON" | sed 's/^/  /'
else
  echo ""
  echo "KEEP=1 — skipped delete (document_id=${DOC_ID})"
fi

echo ""
echo "File live test: OK"
