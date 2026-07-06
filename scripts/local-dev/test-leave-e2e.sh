#!/usr/bin/env bash
# Leave module E2E tests via Hurl — HTML + JUnit reports with full request/response bodies.
#
#   ./scripts/local-dev/test-leave-e2e.sh
#   ./scripts/local-dev/test-leave-e2e.sh --open
#   ./scripts/local-dev/test-leave-e2e.sh --local
#
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
ENV_FILE="${ROOT}/scripts/local-dev/live-session.env"
JWT_FILE="${ROOT}/scripts/local-dev/.jwt-secret.local"
HURL_ENV="${ROOT}/tests/e2e/.runtime/hurl.env"
REPORT_ROOT="${ROOT}/reports/e2e/leave"
REPORT_LATEST="${REPORT_ROOT}/latest"
HURL_IMAGE="${HURL_IMAGE:-ghcr.io/orange-opensource/hurl:latest}"
SAVED_PATH="${PATH:-/usr/bin:/bin:/usr/local/bin:/opt/anaconda3/bin}"

CI_MODE=0
if [[ -n "${ZELOSHR_E2E_CI:-}" || "${CI:-}" == "true" || "${GITHUB_ACTIONS:-}" == "true" ]]; then
  CI_MODE=1
fi

OPEN_REPORT=0
USE_LOCAL=0

while [[ $# -gt 0 ]]; do
  case "$1" in
    --open) OPEN_REPORT=1; shift ;;
    --local) USE_LOCAL=1; shift ;;
    -h|--help)
      sed -n '2,8p' "$0"
      exit 0
      ;;
    *)
      echo "Unknown option: $1" >&2
      exit 1
      ;;
  esac
done

if (( CI_MODE == 0 )); then
  if [[ ! -f "${ENV_FILE}" ]]; then
    echo "Missing ${ENV_FILE}. See scripts/local-dev/README.md" >&2
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
else
  if [[ -z "${TROVE_BEARER_TOKEN:-}" ]]; then
    echo "CI mode requires TROVE_BEARER_TOKEN (run scripts/ci/mint-e2e-session.sh first)." >&2
    exit 1
  fi
fi

BASE="${ZELOSHR_API_BASE:-https://zeloshr.app.backend.dev.trovesuite.com}"
if (( USE_LOCAL )); then
  BASE="http://localhost:8000"
fi

if [[ -z "${TROVE_BEARER_TOKEN:-}" ]]; then
  echo "TROVE_BEARER_TOKEN is empty." >&2
  exit 1
fi

RUN_ID="$(date +%Y%m%d-%H%M%S)"
RUN_DIR="${REPORT_ROOT}/${RUN_ID}"
mkdir -p "${RUN_DIR}" "${REPORT_LATEST}" "${ROOT}/tests/e2e/.runtime"

read -r START_DATE END_DATE DELETE_START DELETE_END <<<"$(python3 - <<'PY'
from datetime import date, timedelta
today = date.today()
start = today + timedelta(days=60)
end = start + timedelta(days=4)
delete_start = today + timedelta(days=90)
delete_end = delete_start
print(start.isoformat(), end.isoformat(), delete_start.isoformat(), delete_end.isoformat())
PY
)"

cat >"${HURL_ENV}" <<EOF
base=${BASE}
app_id=${TROVE_APP_ID:-app-zeloshr}
token=${TROVE_BEARER_TOKEN}
bus_id=${TROVE_BUS_ID}
loc_id=${TROVE_LOC_ID}
org_id=${TROVE_ORG_ID}
run_tag=e2e-${RUN_ID}
start_date=${START_DATE}
end_date=${END_DATE}
delete_start_date=${DELETE_START}
delete_end_date=${DELETE_END}
days_requested=5
employee_id=${EMPLOYEE_ID:-}
leave_type_id=${LEAVE_TYPE_ID:-}
EOF

run_hurl() {
  if command -v hurl >/dev/null 2>&1; then
    hurl "$@"
    return
  fi

  if ! command -v docker >/dev/null 2>&1; then
    echo "Install hurl (brew install hurl) or Docker to run E2E tests." >&2
    exit 1
  fi

  # Paths must be relative to /work inside the container (not host absolutes).
  local -a docker_args=()
  local arg
  for arg in "$@"; do
    case "${arg}" in
      "${HURL_ENV}")
        docker_args+=("tests/e2e/.runtime/hurl.env")
        ;;
      "${RUN_DIR}/html")
        docker_args+=("reports/e2e/leave/${RUN_ID}/html")
        ;;
      "${RUN_DIR}/junit.xml")
        docker_args+=("reports/e2e/leave/${RUN_ID}/junit.xml")
        ;;
      *)
        docker_args+=("${arg}")
        ;;
    esac
  done

  docker run --rm \
    -v "${ROOT}:/work" \
    -w /work \
    "${HURL_IMAGE}" \
    "${docker_args[@]}"
}

echo "Leave E2E (Hurl)"
echo "  API base:  ${BASE}"
echo "  Reports:   ${RUN_DIR}"
echo "  Latest:    ${REPORT_LATEST}"
echo ""

HURL_ARGS=(
  --test
  --jobs 1
  --very-verbose
  --variables-file "${HURL_ENV}"
  --report-html "${RUN_DIR}/html"
  --report-junit "${RUN_DIR}/junit.xml"
  tests/e2e/leave/*.hurl
)

set +e
run_hurl "${HURL_ARGS[@]}"
EXIT=$?
set -e

cp -f "${RUN_DIR}/junit.xml" "${REPORT_LATEST}/junit.xml" 2>/dev/null || true
rm -rf "${REPORT_LATEST}/html" 2>/dev/null || true
cp -R "${RUN_DIR}/html" "${REPORT_LATEST}/html" 2>/dev/null || true

cat >"${REPORT_LATEST}/summary.txt" <<EOF
run_id=${RUN_ID}
base=${BASE}
exit_code=${EXIT}
html=${REPORT_LATEST}/html/index.html
junit=${REPORT_LATEST}/junit.xml
EOF

echo ""
if [[ ${EXIT} -eq 0 ]]; then
  echo "Leave E2E passed. Open report:"
else
  echo "Leave E2E failed (exit ${EXIT}). See report:"
fi
echo "  file://${REPORT_LATEST}/html/index.html"

if (( OPEN_REPORT )) && [[ -f "${REPORT_LATEST}/html/index.html" ]]; then
  if command -v open >/dev/null 2>&1; then
    open "${REPORT_LATEST}/html/index.html"
  elif command -v xdg-open >/dev/null 2>&1; then
    xdg-open "${REPORT_LATEST}/html/index.html"
  fi
fi

exit "${EXIT}"
