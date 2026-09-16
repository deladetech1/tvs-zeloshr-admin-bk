#!/usr/bin/env bash
# Restore Trovesuite.Package and run ZelosHR unit tests (no database required).
set -euo pipefail

cd /src

PKG="${PACKAGES_TOKEN:-}"
if [[ -z "$PKG" ]]; then
  echo "ERROR: PACKAGES_TOKEN is not set (add to app/.env — org secret name for Trovesuite.Package)." >&2
  exit 1
fi

# GitHub Packages NuGet: username may be any non-empty string for a PAT; for GITHUB_TOKEN
# (ghs_*) GitHub recommends the actor / a placeholder such as x-access-token.
NUGET_USER="deladetech1"
if [[ "$PKG" == ghs_* ]] || [[ "$PKG" == gho_* ]]; then
  NUGET_USER="x-access-token"
fi

dotnet nuget update source github-deladetech1 \
  --username "${NUGET_USER}" \
  --password "${PKG}" \
  --store-password-in-clear-text \
  --configfile nuget.config

echo "==> NuGet source github-deladetech1 ready (token_len=${#PKG}, user=${NUGET_USER})"
echo "==> dotnet test (Release)"
dotnet test tests/ZelosHR.Api.Tests/ZelosHR.Api.Tests.csproj -c Release --nologo -v minimal
