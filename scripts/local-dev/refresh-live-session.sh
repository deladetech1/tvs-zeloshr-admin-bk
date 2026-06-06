#!/usr/bin/env bash
# Validate a Trove Bearer token against dev ZelosHR and refresh live-session.env.
#
# Usage (after copying token from browser DevTools):
#   ./scripts/local-dev/refresh-live-session.sh 'eyJhbG...'
#
# Or paste org/bus/loc from the same Network request:
#   TROVE_ORG_ID=org_... TROVE_BUS_ID=bus_... TROVE_LOC_ID=loc_... \
#     ./scripts/local-dev/refresh-live-session.sh 'eyJhbG...'
#
# Auto-mint (only works when .jwt-secret.local matches the Container App secret):
#   ./scripts/local-dev/refresh-live-session.sh --mint
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
ENV_FILE="${ROOT}/scripts/local-dev/live-session.env"
JWT_FILE="${ROOT}/scripts/local-dev/.jwt-secret.local"
SAVED_PATH="${PATH:-/usr/bin:/bin:/usr/local/bin:/opt/anaconda3/bin}"

if [[ ! -f "${ENV_FILE}" ]]; then
  echo "Missing ${ENV_FILE} — cp scripts/local-dev/live-session.example.env first" >&2
  exit 1
fi

# shellcheck source=/dev/null
set -a && source "${ENV_FILE}" && set +a
[[ -f "${JWT_FILE}" ]] && set -a && source "${JWT_FILE}" && set +a
export PATH="${SAVED_PATH}"

BASE="${ZELOSHR_API_BASE:-https://zeloshr.app.backend.dev.trovesuite.com}"
MODE="${1:-}"

mint_token() {
  python3 - <<'PY'
import os, json, base64, time
import jwt

old = os.environ["TROVE_BEARER_TOKEN"]
part = old.split(".")[1]
payload = json.loads(base64.urlsafe_b64decode(part + "=" * (-len(part) % 4)))
secret = os.environ.get("TROVESUITE_JWT_SECRET", "")
if not secret:
    raise SystemExit("TROVESUITE_JWT_SECRET missing in .jwt-secret.local")
claims = {k: v for k, v in payload.items() if k not in ("exp", "iat", "nbf")}
now = int(time.time())
claims["iat"] = now
claims["exp"] = now + 7200
print(jwt.encode(claims, secret, algorithm="HS256"))
PY
}

decode_claims() {
  local token="$1"
  TOKEN="$token" python3 - <<'PY'
import json, base64, os
from datetime import datetime, timezone

token = os.environ["TOKEN"].strip()
if token.lower().startswith("bearer "):
    token = token[7:].strip()
part = token.split(".")[1]
payload = json.loads(base64.urlsafe_b64decode(part + "=" * (-len(part) % 4)))
exp = payload.get("exp")
print(json.dumps({
    "user_id": payload.get("user_id", ""),
    "tenant_id": payload.get("tenant_id", ""),
    "email": payload.get("email", ""),
    "exp_iso": datetime.fromtimestamp(exp, tz=timezone.utc).isoformat() if exp else "",
}))
PY
}

probe() {
  local token="$1"
  curl -sS -o /dev/null -w "%{http_code}" \
    -H "app-id: ${TROVE_APP_ID:-app-hr}" \
    -H "authorization: Bearer ${token}" \
    -H "bus-id: ${TROVE_BUS_ID}" \
    -H "loc-id: ${TROVE_LOC_ID}" \
    -H "org-id: ${TROVE_ORG_ID}" \
    "${BASE}/api/v1/health"
}

NEW_TOKEN=""
if [[ "${MODE}" == "--mint" ]]; then
  if [[ ! -f "${JWT_FILE}" || -z "${TROVESUITE_JWT_SECRET:-}" ]]; then
    echo "Cannot mint: missing ${JWT_FILE} or TROVESUITE_JWT_SECRET" >&2
    exit 1
  fi
  NEW_TOKEN="$(mint_token)"
  echo "Minted JWT from existing claims + .jwt-secret.local"
elif [[ -n "${MODE}" ]]; then
  NEW_TOKEN="${MODE}"
elif [[ -n "${NEW_TOKEN:-}" ]]; then
  :
else
  cat >&2 <<'EOF'
Paste the Bearer token from browser DevTools (Network → filter "backend" → any /api/v1/ call):

  ./scripts/local-dev/refresh-live-session.sh 'eyJhbG...'

Copy from the authorization header — omit the "Bearer " prefix.
Use the same request for org-id, bus-id, loc-id if your session changed.
EOF
  exit 1
fi

NEW_TOKEN="${NEW_TOKEN#Bearer }"
NEW_TOKEN="${NEW_TOKEN#bearer }"

CLAIMS_JSON="$(decode_claims "${NEW_TOKEN}")"
USER_ID="$(echo "${CLAIMS_JSON}" | python3 -c "import json,sys; print(json.load(sys.stdin)['user_id'])")"
TENANT_ID="$(echo "${CLAIMS_JSON}" | python3 -c "import json,sys; print(json.load(sys.stdin)['tenant_id'])")"
EMAIL="$(echo "${CLAIMS_JSON}" | python3 -c "import json,sys; print(json.load(sys.stdin)['email'])")"
EXP_ISO="$(echo "${CLAIMS_JSON}" | python3 -c "import json,sys; print(json.load(sys.stdin)['exp_iso'])")"

CODE="$(probe "${NEW_TOKEN}")"
if [[ ! "${CODE}" =~ ^2 ]]; then
  cat >&2 <<EOF
Auth probe failed (HTTP ${CODE}) on GET /api/v1/health.

Check:
  • Token is from a request to ${BASE} (not the Next.js frontend host).
  • TROVE_ORG_ID / TROVE_BUS_ID / TROVE_LOC_ID match that same request.
  • TROVE_TENANT_ID claim matches tenant in JWT: ${TENANT_ID}

Current headers: org=${TROVE_ORG_ID} bus=${TROVE_BUS_ID} loc=${TROVE_LOC_ID}
JWT user: ${USER_ID} (${EMAIL}) exp=${EXP_ISO}
EOF
  exit 1
fi

python3 - <<PY
from pathlib import Path
import re

path = Path("${ENV_FILE}")
text = path.read_text()

def set_var(name, value):
    global text
    pattern = rf"^{re.escape(name)}=.*$"
    line = f"{name}={value}"
    if re.search(pattern, text, flags=re.M):
        text = re.sub(pattern, line, text, count=1, flags=re.M)
    else:
        text = text.rstrip() + "\\n" + line + "\\n"

set_var("TROVE_BEARER_TOKEN", "${NEW_TOKEN}")
set_var("TROVE_USER_ID", "${USER_ID}")
set_var("TROVE_TENANT_ID", "${TENANT_ID}")
path.write_text(text)
PY

echo "Updated ${ENV_FILE}"
echo "  user:   ${USER_ID} (${EMAIL})"
echo "  tenant: ${TENANT_ID}"
echo "  exp:    ${EXP_ISO}"
echo "  probe:  GET /api/v1/health → HTTP ${CODE}"
echo ""
echo "Run: ./scripts/local-dev/post-deploy-verify.sh"
