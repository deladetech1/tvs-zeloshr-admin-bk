#!/usr/bin/env bash
# Smoke-test audit-log read endpoints (deployed or local API).
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
  -H "app-id: ${TROVE_APP_ID:-app-hr}"
  -H "authorization: Bearer ${TROVE_BEARER_TOKEN}"
  -H "bus-id: ${TROVE_BUS_ID}"
  -H "loc-id: ${TROVE_LOC_ID}"
  -H "org-id: ${TROVE_ORG_ID}"
)

paths=(
  "/api/v1/audit-logs/statistics"
  "/api/v1/audit-logs/list?page=1&size=5"
  "/api/v1/audit-logs/list?page=1&size=5&severity=Medium&actor=all&action=all"
  "/api/v1/audit-logs/list?page=1&size=5&start_date=2026-01-01&end_date=2026-12-31"
  "/api/v1/audit-logs/export?severity=all&actor=all&action=all"
  "/api/v1/audit-logs/purge/preview?retention_window=90"
  "/api/v1/audit-logs/purge/preview?retention_window=180"
)

for path in "${paths[@]}"; do
  echo "GET ${BASE}${path}"
  code="$(curl -sS -o /tmp/audit-log-smoke.json -w "%{http_code}" "${curl_headers[@]}" "${BASE}${path}")"
  echo "  -> ${code}"
  if [[ "${code}" != "200" ]]; then
    cat /tmp/audit-log-smoke.json >&2
    exit 1
  fi
done

echo "Audit log smoke OK"
