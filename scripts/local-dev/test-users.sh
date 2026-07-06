#!/usr/bin/env bash
# Smoke-test platform users list (deployed or local API).
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
ENV_FILE="${ROOT}/scripts/local-dev/live-session.env"

if [[ ! -f "${ENV_FILE}" ]]; then
  echo "Missing ${ENV_FILE}. See scripts/local-dev/README.md" >&2
  exit 1
fi

# shellcheck source=/dev/null
set -a && source "${ENV_FILE}" && set +a

BASE="${ZELOSHR_API_BASE:-https://zeloshr.app.backend.dev.trovesuite.com}"

if [[ -z "${TROVE_BEARER_TOKEN:-}" ]]; then
  echo "TROVE_BEARER_TOKEN is empty — log in on dev and paste a fresh JWT." >&2
  exit 1
fi

curl_headers=(
  -H "accept: application/json"
  -H "app-id: ${TROVE_APP_ID:-app-zeloshr}"
  -H "authorization: Bearer ${TROVE_BEARER_TOKEN}"
  -H "bus-id: ${TROVE_BUS_ID}"
  -H "loc-id: ${TROVE_LOC_ID}"
  -H "org-id: ${TROVE_ORG_ID}"
)

paths=(
  "/api/v1/users/get-users?page=1&size=10"
  "/api/v1/users/get-users?page=1&size=5&is_active=true&delete_status=NOT_DELETED"
  "/api/v1/users/get-users?page=1&size=5&fullname=demo&use_or=false"
)

for path in "${paths[@]}"; do
  echo "GET ${BASE}${path}"
  code="$(curl -sS -o /tmp/users-smoke.json -w "%{http_code}" "${curl_headers[@]}" "${BASE}${path}")"
  echo "  -> ${code}"
  if [[ "${code}" != "200" ]]; then
    cat /tmp/users-smoke.json >&2
    exit 1
  fi
done

echo "Users list smoke OK"
