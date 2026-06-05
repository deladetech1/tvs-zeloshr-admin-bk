#!/usr/bin/env bash
# Comprehensive live API smoke test (custom-fields, org-structure, employees).
set -euo pipefail

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

curl_headers=(
  -H "accept: application/json"
  -H "app-id: ${TROVE_APP_ID:-app-hr}"
  -H "authorization: Bearer ${TOKEN}"
  -H "bus-id: ${TROVE_BUS_ID}"
  -H "loc-id: ${TROVE_LOC_ID}"
  -H "org-id: ${TROVE_ORG_ID}"
)

paths=(
  "/api/v1/health"
  "/api/v1/navigation"
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
  "/api/v1/employees/bulk/template"
)

echo "ZelosHR base: ${BASE}"
echo ""

fail=0
for path in "${paths[@]}"; do
  accept="application/json"
  [[ "$path" == *bulk/template* ]] && accept="text/csv,application/json"
  body=$(curl -sS -w "\n%{http_code}" -H "accept: ${accept}" "${curl_headers[@]}" "${BASE}${path}")
  code=$(echo "$body" | tail -1)
  json=$(echo "$body" | sed '$d' | head -c 700)
  if [[ "$code" =~ ^2 ]]; then
    mark="OK"
  else
    mark="FAIL"
    fail=$((fail + 1))
  fi
  printf "[%s] HTTP %s GET %s\n%s\n\n" "$mark" "$code" "$path" "$json"
done

echo "Total failures: ${fail}"
exit "$fail"
