#!/usr/bin/env bash
# Mint Trove JWT + export session env for CI Leave E2E (no live-session.env file).
#
# Required: TROVESUITE_SECRET_KEY
# Session (secrets or vars; see docs/CICD.md):
#   ZELOSHR_E2E_USER_ID, ZELOSHR_E2E_TENANT_ID, ZELOSHR_E2E_ORG_ID, ZELOSHR_E2E_BUS_ID, ZELOSHR_E2E_LOC_ID
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
# shellcheck source=scripts/ci/api-response-format.sh
source "${ROOT}/scripts/ci/api-response-format.sh"

BASE="${ZELOSHR_API_BASE:-https://zeloshr.app.backend.dev.trovesuite.com}"
SECRET="${TROVESUITE_SECRET_KEY:-${TROVESUITE_JWT_SECRET:-}}"

USER_ID="${ZELOSHR_E2E_USER_ID:-${TROVE_USER_ID:-}}"
TENANT_ID="${ZELOSHR_E2E_TENANT_ID:-${TROVE_TENANT_ID:-}}"
ORG_ID="${ZELOSHR_E2E_ORG_ID:-${TROVE_ORG_ID:-}}"
BUS_ID="${ZELOSHR_E2E_BUS_ID:-${TROVE_BUS_ID:-}}"
LOC_ID="${ZELOSHR_E2E_LOC_ID:-${TROVE_LOC_ID:-}}"
EMAIL="${ZELOSHR_E2E_EMAIL:-ci-e2e@deladetech.com}"
FULLNAME="${ZELOSHR_E2E_FULLNAME:-CI E2E}"

missing=()
[[ -z "${SECRET}" ]] && missing+=("TROVESUITE_SECRET_KEY")
[[ -z "${USER_ID}" ]] && missing+=("ZELOSHR_E2E_USER_ID")
[[ -z "${TENANT_ID}" ]] && missing+=("ZELOSHR_E2E_TENANT_ID")
[[ -z "${ORG_ID}" ]] && missing+=("ZELOSHR_E2E_ORG_ID")
[[ -z "${BUS_ID}" ]] && missing+=("ZELOSHR_E2E_BUS_ID")
[[ -z "${LOC_ID}" ]] && missing+=("ZELOSHR_E2E_LOC_ID")

if ((${#missing[@]} > 0)); then
  echo "E2E session mint missing: ${missing[*]}" >&2
  echo "Configure GitHub secrets/vars — see docs/CICD.md#leave-e2e-hurl" >&2
  exit 1
fi

python3 - <<'PY' >/dev/null 2>&1 || python3 -m pip install --user 'PyJWT>=2.8.0'
import jwt  # noqa: F401
PY

export USER_ID TENANT_ID EMAIL FULLNAME SECRET
TOKEN="$(
  python3 - <<'PY'
import os, time, jwt

secret = os.environ["SECRET"]
now = int(time.time())
claims = {
    "user_id": os.environ["USER_ID"],
    "tenant_id": os.environ["TENANT_ID"],
    "email": os.environ["EMAIL"],
    "fullname": os.environ["FULLNAME"],
    "iat": now,
    "exp": now + 7200,
}
print(jwt.encode(claims, secret, algorithm="HS256"))
PY
)"

if [[ -z "${SKIP_E2E_HEALTH_WAIT:-}" ]]; then
  wait_for_api_health "${BASE}" 18 10
fi

code="$(
  curl -sS -o /dev/null -w "%{http_code}" \
    -H "app-id: ${TROVE_APP_ID:-app-zeloshr}" \
    -H "authorization: Bearer ${TOKEN}" \
    -H "org-id: ${ORG_ID}" \
    -H "bus-id: ${BUS_ID}" \
    -H "loc-id: ${LOC_ID}" \
    "${BASE}/api/v1/employees/list?page=1&size=1"
)"

if [[ ! "${code}" =~ ^2 ]]; then
  echo "Minted E2E JWT rejected (HTTP ${code}) on GET /api/v1/employees/list." >&2
  echo "Check ZELOSHR_E2E_* ids and TROVESUITE_SECRET_KEY match dev Container App." >&2
  exit 1
fi

export TROVE_BEARER_TOKEN="${TOKEN}"
export TROVE_ORG_ID="${ORG_ID}"
export TROVE_BUS_ID="${BUS_ID}"
export TROVE_LOC_ID="${LOC_ID}"
export TROVE_TENANT_ID="${TENANT_ID}"
export TROVE_USER_ID="${USER_ID}"
export TROVE_APP_ID="${TROVE_APP_ID:-app-zeloshr}"
export ZELOSHR_API_BASE="${BASE}"

echo "E2E session ready (user=${USER_ID}, tenant=${TENANT_ID}, org=${ORG_ID})"
