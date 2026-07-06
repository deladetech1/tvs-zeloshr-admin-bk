#!/usr/bin/env bash
# Shared Trove session auth for local live scripts.
# Source after loading live-session.env (+ optional .jwt-secret.local).
#
#   source scripts/local-dev/live-session-auth.sh
#   ensure_live_session_auth   # sets TROVE_BEARER_TOKEN to working token; exits 1 on failure
ensure_live_session_auth() {
  local base="${ZELOSHR_API_BASE:-https://zeloshr.app.backend.dev.trovesuite.com}"

  if [[ -z "${TROVE_BEARER_TOKEN:-}" ]]; then
    echo "TROVE_BEARER_TOKEN is empty in live-session.env" >&2
    echo "  ./scripts/local-dev/refresh-live-session.sh 'eyJhbG...'" >&2
    return 1
  fi

  local token="${TROVE_BEARER_TOKEN}"
  local minted=""

  if [[ -f "${LIVE_SESSION_JWT_FILE:-}" && -n "${TROVESUITE_JWT_SECRET:-}" ]]; then
    minted="$(TROVE_BEARER_TOKEN="${token}" TROVESUITE_JWT_SECRET="${TROVESUITE_JWT_SECRET}" python3 - <<'PY'
import os, json, base64, time
import jwt

old = os.environ["TROVE_BEARER_TOKEN"]
part = old.split(".")[1]
payload = json.loads(base64.urlsafe_b64decode(part + "=" * (-len(part) % 4)))
secret = os.environ["TROVESUITE_JWT_SECRET"]
claims = {k: v for k, v in payload.items() if k not in ("exp", "iat", "nbf")}
now = int(time.time())
claims["iat"] = now
claims["exp"] = now + 7200
print(jwt.encode(claims, secret, algorithm="HS256"))
PY
)" || true
    if [[ -n "$minted" ]]; then
      token="$minted"
      echo "Using refreshed JWT (minted from .jwt-secret.local)"
    fi
  else
    echo "Using TROVE_BEARER_TOKEN from live-session.env"
  fi

  local code
  code="$(live_session_auth_probe "$token" "$base")"

  if [[ ! "$code" =~ ^2 && -n "$minted" && "$token" != "${TROVE_BEARER_TOKEN}" ]]; then
    local fallback
    fallback="$(live_session_auth_probe "${TROVE_BEARER_TOKEN}" "$base")"
    if [[ "$fallback" =~ ^2 ]]; then
      echo "Minted JWT rejected (HTTP ${code}); using token from live-session.env"
      token="${TROVE_BEARER_TOKEN}"
      code="$fallback"
    fi
  fi

  if [[ ! "$code" =~ ^2 ]]; then
    cat >&2 <<EOF
Auth failed (HTTP ${code}) on GET /api/v1/health.

Refresh your session (Bearer tokens expire):

  ./scripts/local-dev/refresh-live-session.sh 'eyJhbG...'

Or auto-mint when .jwt-secret.local matches the dev Container App:

  ./scripts/local-dev/refresh-live-session.sh --mint

See scripts/local-dev/README.md
EOF
    return 1
  fi

  export TROVE_BEARER_TOKEN="$token"
  return 0
}

live_session_auth_probe() {
  local token="$1"
  local base="$2"
  curl -sS -o /dev/null -w "%{http_code}" \
    -H "app-id: ${TROVE_APP_ID:-app-zeloshr}" \
    -H "authorization: Bearer ${token}" \
    -H "bus-id: ${TROVE_BUS_ID}" \
    -H "loc-id: ${TROVE_LOC_ID}" \
    -H "org-id: ${TROVE_ORG_ID}" \
    "${base}/api/v1/health"
}
