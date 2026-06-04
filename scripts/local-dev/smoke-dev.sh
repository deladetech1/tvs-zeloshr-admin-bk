#!/usr/bin/env bash
# Smoke-test deployed dev APIs using scripts/local-dev/live-session.env (gitignored).
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
ENV_FILE="${ROOT}/scripts/local-dev/live-session.env"

if [[ ! -f "${ENV_FILE}" ]]; then
  echo "Missing ${ENV_FILE}" >&2
  echo "  cp scripts/local-dev/live-session.example.env scripts/local-dev/live-session.env" >&2
  echo "  Then paste your Bearer token from Trove login (DevTools → Network)." >&2
  exit 1
fi

# shellcheck source=/dev/null
set -a && source "${ENV_FILE}" && set +a

DB_ENV="${ROOT}/scripts/local-dev/live-db.env"
if [[ -f "${DB_ENV}" ]]; then
  # shellcheck source=/dev/null
  set -a && source "${DB_ENV}" && set +a
fi

# Deployed API by default; set ZELOSHR_API_BASE=http://localhost:8000 in live-db.env for local API + live DB
ZELOSHR_API_BASE="${ZELOSHR_API_BASE:-https://zeloshr.app.backend.dev.trovesuite.com}"

if [[ -z "${TROVE_BEARER_TOKEN:-}" ]]; then
  echo "TROVE_BEARER_TOKEN is empty in live-session.env" >&2
  exit 1
fi

curl_headers=(
  -H "accept: application/json"
  -H "app-id: ${TROVE_APP_ID:-app-hr}"
  -H "authorization: Bearer ${TROVE_BEARER_TOKEN}"
  -H "bus-id: ${TROVE_BUS_ID}"
  -H "loc-id: ${TROVE_LOC_ID}"
  -H "org-id: ${TROVE_ORG_ID}"
)

case "${1:-smoke}" in
  smoke)
    echo "ZelosHR base: ${ZELOSHR_API_BASE} $( [[ "${ZELOSHR_API_BASE}" == http://localhost* ]] && echo '(local API → live DB)' || echo '(deployed CA)' )"
    for path in /api/v1/health /api/v1/navigation /api/v1/custom-fields/entity-types /api/v1/employees/statistics; do
      code=$(curl -s -o /dev/null -w "%{http_code}" "${curl_headers[@]}" "${ZELOSHR_API_BASE}${path}" || echo "000")
      echo "  ${path} → HTTP ${code}"
    done
    if [[ -n "${CORE_PLATFORM_API_BASE:-}" && -n "${TROVE_USER_ID:-}" ]]; then
      path="/api/v1/users/location-details/${TROVE_USER_ID}"
      code=$(curl -s -o /dev/null -w "%{http_code}" "${curl_headers[@]}" "${CORE_PLATFORM_API_BASE}${path}" || echo "000")
      echo "  [core-platform] ${path} → HTTP ${code}"
    fi
    ;;
  get)
    path="${2:-/api/v1/custom-fields/entity-types}"
    target="${3:-zeloshr}"
    base="${ZELOSHR_API_BASE}"
    [[ "${target}" == "core" ]] && base="${CORE_PLATFORM_API_BASE}"
    curl -sS "${curl_headers[@]}" "${base}${path}" | (command -v jq >/dev/null && jq . || cat)
    ;;
  *)
    echo "Usage: $0 smoke" >&2
    echo "       $0 get </api/v1/...> [zeloshr|core]" >&2
    exit 1
    ;;
esac
