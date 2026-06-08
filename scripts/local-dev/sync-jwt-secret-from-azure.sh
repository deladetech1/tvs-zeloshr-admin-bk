#!/usr/bin/env bash
# Pull Trovesuite JWT signing key from Azure Key Vault into .jwt-secret.local (gitignored).
# Requires: az login + Key Vault Secrets User on trovesuite-dev-kv (secret: secret-key).
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
OUT="${ROOT}/scripts/local-dev/.jwt-secret.local"
VAULT="${AZURE_KEY_VAULT:-trovesuite-dev-kv}"
SECRET_NAME="${AZURE_JWT_SECRET_NAME:-secret-key}"

if ! command -v az >/dev/null 2>&1; then
  echo "az CLI required. Install Azure CLI and run: az login" >&2
  exit 1
fi

echo "Fetching Key Vault secret ${SECRET_NAME} from ${VAULT}..."
VALUE="$(az keyvault secret show --vault-name "$VAULT" --name "$SECRET_NAME" --query value -o tsv 2>&1)" || {
  cat >&2 <<EOF
Could not read JWT secret from Key Vault.

  vault:  ${VAULT}
  secret: ${SECRET_NAME}

Ask your Azure admin for Key Vault Secrets User on the vault, then retry:

  az login
  ./scripts/local-dev/sync-jwt-secret-from-azure.sh

Until then, paste a fresh Bearer token from the browser after login:

  ./scripts/local-dev/refresh-live-session.sh 'eyJhbG...'

EOF
  echo "$VALUE" >&2
  exit 1
}

if [[ ${#VALUE} -lt 32 ]]; then
  echo "JWT secret from Key Vault is too short (${#VALUE} chars)." >&2
  exit 1
fi

umask 077
printf 'TROVESUITE_JWT_SECRET=%s\n' "$VALUE" >"$OUT"
echo "Wrote ${OUT} (${#VALUE} chars)"
echo "Next: ./scripts/local-dev/generate-live-session.sh --mint"
