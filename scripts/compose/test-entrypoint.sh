#!/usr/bin/env bash
# Restore Trovesuite.Package and run ZelosHR unit tests (no database required).
set -euo pipefail

cd /src

PKG="${PACKAGES_TOKEN:-}"
if [[ -z "$PKG" ]]; then
  echo "ERROR: PACKAGES_TOKEN is not set (add to app/.env — org secret name for Trovesuite.Package)." >&2
  exit 1
fi

dotnet nuget update source github-deladetech1 \
  --username x-access-token \
  --password "${PKG}" \
  --store-password-in-clear-text \
  --configfile nuget.config

echo "==> dotnet test (Release)"
dotnet test tests/ZelosHR.Api.Tests/ZelosHR.Api.Tests.csproj -c Release --nologo -v minimal
