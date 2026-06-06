#!/usr/bin/env bash
# Run after a dev deploy (local): regression + live smoke against deployed API.
#
#   ./scripts/local-dev/post-deploy-verify.sh           # quick live smoke
#   ./scripts/local-dev/post-deploy-verify.sh --full    # full test-live-all.sh
#   ./scripts/local-dev/post-deploy-verify.sh --regression-only
#   ./scripts/local-dev/post-deploy-verify.sh --live-only
#
# Auth: auto-mints JWT from .jwt-secret.local when possible, else uses live-session.env.
#       Paste a fresh token with refresh-live-session.sh when mint fails.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
ENV_FILE="${ROOT}/scripts/local-dev/live-session.env"
JWT_FILE="${ROOT}/scripts/local-dev/.jwt-secret.local"
SAVED_PATH="${PATH:-/usr/bin:/bin:/usr/local/bin:/opt/anaconda3/bin}"

RUN_REGRESSION=1
RUN_LIVE=1
LIVE_MODE="quick"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --full) LIVE_MODE="full"; shift ;;
    --regression-only) RUN_LIVE=0; shift ;;
    --live-only) RUN_REGRESSION=0; shift ;;
    -h|--help)
      sed -n '2,12p' "$0"
      exit 0
      ;;
    *)
      echo "Unknown option: $1" >&2
      exit 1
      ;;
  esac
done

if [[ ! -f "${ENV_FILE}" ]]; then
  echo "Missing ${ENV_FILE} — cp scripts/local-dev/live-session.example.env first" >&2
  exit 1
fi

# shellcheck source=/dev/null
set -a && source "${ENV_FILE}" && set +a
[[ -f "${JWT_FILE}" ]] && set -a && source "${JWT_FILE}" && set +a
export PATH="${SAVED_PATH}"
export LIVE_SESSION_JWT_FILE="${JWT_FILE}"

# shellcheck source=scripts/local-dev/live-session-auth.sh
source "${ROOT}/scripts/local-dev/live-session-auth.sh"

if (( RUN_REGRESSION )); then
  echo "=== Regression (Docker Compose tests) ==="
  if [[ -z "${PACKAGES_TOKEN:-}" && -f "${ROOT}/app/.env" ]]; then
    # shellcheck source=/dev/null
    set -a && source "${ROOT}/app/.env" && set +a
  fi
  if [[ -z "${PACKAGES_TOKEN:-}" ]]; then
    echo "PACKAGES_TOKEN missing — set in app/.env for regression tests" >&2
    exit 1
  fi
  chmod +x "${ROOT}/scripts/ci/regression-test.sh" "${ROOT}/scripts/compose.sh" "${ROOT}/scripts/compose/"*.sh
  export PACKAGES_TOKEN
  "${ROOT}/scripts/ci/regression-test.sh"
  echo ""
fi

if (( RUN_LIVE )); then
  echo "=== Live smoke (deployed dev API) ==="
  ensure_live_session_auth
  chmod +x "${ROOT}/scripts/ci/live-smoke.sh" "${ROOT}/scripts/ci/api-response-format.sh"
  if [[ "${LIVE_MODE}" == "full" ]]; then
    chmod +x "${ROOT}/scripts/local-dev/test-live-all.sh"
    "${ROOT}/scripts/local-dev/test-live-all.sh"
  else
    SKIP_LIVE_WAIT="${SKIP_LIVE_WAIT:-}" "${ROOT}/scripts/ci/live-smoke.sh"
  fi
fi

echo "Post-deploy verification complete."
