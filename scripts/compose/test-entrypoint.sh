#!/usr/bin/env bash
# Restore Trovesuite.Package and run ZelosHR unit tests (no database required).
set -euo pipefail

cd /src

if [[ -z "${GITHUB_PACKAGES_TOKEN:-}" ]]; then
  echo "ERROR: GITHUB_PACKAGES_TOKEN is not set (add to app/.env)." >&2
  exit 1
fi

dotnet nuget update source github-deladetech1 \
  --username deladetech1 \
  --password "${GITHUB_PACKAGES_TOKEN}" \
  --store-password-in-clear-text \
  --configfile nuget.config

echo "==> dotnet test (Release)"
dotnet test tests/ZelosHR.Api.Tests/ZelosHR.Api.Tests.csproj -c Release --nologo -v minimal
