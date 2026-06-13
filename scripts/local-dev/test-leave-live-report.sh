#!/usr/bin/env bash
# Exercise every Leave API endpoint against live dev and write an HTML report
# with full request/response bodies (token redacted in report).
#
#   ./scripts/local-dev/test-leave-live-report.sh
#   ./scripts/local-dev/test-leave-live-report.sh --open
#
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
ENV_FILE="${ROOT}/scripts/local-dev/live-session.env"
REPORT_LATEST="${ROOT}/reports/e2e/leave/live-report/latest/index.html"

OPEN=0
while [[ $# -gt 0 ]]; do
  case "$1" in
    --open) OPEN=1; shift ;;
    -h|--help)
      sed -n '2,8p' "$0"
      exit 0
      ;;
    *) echo "Unknown option: $1" >&2; exit 1 ;;
  esac
done

if [[ ! -f "${ENV_FILE}" ]]; then
  echo "Missing ${ENV_FILE}. See scripts/local-dev/README.md" >&2
  exit 1
fi

# shellcheck source=/dev/null
set -a && source "${ENV_FILE}" && set +a

python3 "${ROOT}/scripts/local-dev/leave-live-report.py"
EXIT=$?

if (( OPEN )) && [[ -f "${REPORT_LATEST}" ]]; then
  if command -v open >/dev/null 2>&1; then
    open "${REPORT_LATEST}"
  elif command -v xdg-open >/dev/null 2>&1; then
    xdg-open "${REPORT_LATEST}"
  fi
fi

exit "${EXIT}"
