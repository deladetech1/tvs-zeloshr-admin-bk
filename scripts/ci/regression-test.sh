#!/usr/bin/env bash
# Regression suite for CI — same as PR workflow (Docker Compose unit/integration tests).
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
cd "${ROOT}"

if [[ -z "${PACKAGES_TOKEN:-}" ]]; then
  echo "::error::PACKAGES_TOKEN is required for regression tests (Trovesuite.Package restore)." >&2
  exit 1
fi

printf 'PACKAGES_TOKEN=%s\n' "${PACKAGES_TOKEN}" >> app/.env

chmod +x scripts/compose.sh scripts/compose/*.sh
./scripts/compose.sh test

echo "Regression tests passed."
