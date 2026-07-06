#!/usr/bin/env bash
# POST /api/v1/employees/add — Gary Ntori draft payload (live dev).
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
ENV_FILE="${ROOT}/scripts/local-dev/live-session.env"
JWT_FILE="${ROOT}/scripts/local-dev/.jwt-secret.local"

if [[ ! -f "${ENV_FILE}" ]]; then
  echo "Missing ${ENV_FILE}" >&2
  exit 1
fi

# shellcheck source=/dev/null
set -a && source "${ENV_FILE}" && set +a
[[ -f "${JWT_FILE}" ]] && set -a && source "${JWT_FILE}" && set +a

BASE="${ZELOSHR_API_BASE:-https://zeloshr.app.backend.dev.trovesuite.com}"
TOKEN="${TROVE_BEARER_TOKEN:-}"

if [[ -z "${TOKEN}" ]]; then
  echo "TROVE_BEARER_TOKEN is empty" >&2
  exit 1
fi

if [[ -f "${JWT_FILE}" && -n "${TROVESUITE_JWT_SECRET:-}" ]]; then
  MINTED="$(python3 - <<'PY'
import os, json, base64, time, jwt
old = os.environ["TROVE_BEARER_TOKEN"]
part = old.split(".")[1]
payload = json.loads(base64.urlsafe_b64decode(part + "=" * (-len(part) % 4)))
claims = {k: v for k, v in payload.items() if k not in ("exp", "iat", "nbf")}
now = int(time.time()); claims["iat"]=now; claims["exp"]=now+7200
print(jwt.encode(claims, os.environ["TROVESUITE_JWT_SECRET"], algorithm="HS256"))
PY
)"
  PROBE=$(curl -sS -o /dev/null -w "%{http_code}" \
    -H "app-id: ${TROVE_APP_ID:-app-zeloshr}" \
    -H "authorization: Bearer ${MINTED}" \
    -H "bus-id: ${TROVE_BUS_ID}" -H "loc-id: ${TROVE_LOC_ID}" -H "org-id: ${TROVE_ORG_ID}" \
    "${BASE}/api/v1/health")
  if [[ "${PROBE}" =~ ^2 ]]; then
    TOKEN="${MINTED}"
    echo "Using minted JWT"
  else
    echo "Minted JWT rejected (HTTP ${PROBE}); using live-session.env token"
  fi
fi

PAYLOAD="${ROOT}/scripts/local-dev/fixtures/gary-ntori-add.json"
if [[ ! -f "${PAYLOAD}" ]]; then
  echo "Missing ${PAYLOAD}" >&2
  exit 1
fi

echo "POST ${BASE}/api/v1/employees/add"
echo "--- request ---"
cat "${PAYLOAD}"
echo ""
echo "--- response ---"

curl -sS -w "\nHTTP:%{http_code}\n" \
  -X POST "${BASE}/api/v1/employees/add" \
  -H "accept: application/json" \
  -H "content-type: application/json" \
  -H "app-id: ${TROVE_APP_ID:-app-zeloshr}" \
  -H "authorization: Bearer ${TOKEN}" \
  -H "bus-id: ${TROVE_BUS_ID}" \
  -H "loc-id: ${TROVE_LOC_ID}" \
  -H "org-id: ${TROVE_ORG_ID}" \
  --data-binary "@${PAYLOAD}"
