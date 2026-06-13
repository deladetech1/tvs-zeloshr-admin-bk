#!/usr/bin/env bash
# CI entrypoint: mint dev Trove session + run Leave Hurl E2E with HTML/JUnit reports.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
chmod +x "${ROOT}/scripts/ci/mint-e2e-session.sh" "${ROOT}/scripts/local-dev/test-leave-e2e.sh"

# shellcheck source=scripts/ci/mint-e2e-session.sh
source "${ROOT}/scripts/ci/mint-e2e-session.sh"

export ZELOSHR_E2E_CI=1
exec "${ROOT}/scripts/local-dev/test-leave-e2e.sh" "$@"
