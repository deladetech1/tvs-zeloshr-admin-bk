#!/usr/bin/env bash
# Build or refresh scripts/local-dev/live-session.env for deployed dev API testing.
#
# Modes:
#   ./scripts/local-dev/generate-live-session.sh --sync-secret   # Key Vault → .jwt-secret.local
#   ./scripts/local-dev/generate-live-session.sh --mint          # DB session + mint JWT (needs secret)
#   ./scripts/local-dev/generate-live-session.sh --mint --user lntori
#   ./scripts/local-dev/generate-live-session.sh 'eyJhbG...'     # browser token (same as refresh-live-session)
#
# Requires live-db.env (./scripts/local-dev/pull-azure-dev-env.sh) for --mint user/org lookup.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
ENV_FILE="${ROOT}/scripts/local-dev/live-session.env"
DB_ENV="${ROOT}/scripts/local-dev/live-db.env"
JWT_FILE="${ROOT}/scripts/local-dev/.jwt-secret.local"
SAVED_PATH="${PATH:-/usr/bin:/bin:/usr/local/bin:/opt/homebrew/bin:/opt/homebrew/opt/libpq/bin}"

usage() {
  cat <<'EOF'
Generate fresh Trove dev credentials for live API scripts.

  --sync-secret          Pull JWT signing key from Azure Key Vault
  --mint [--user EMAIL]  Pick org/bus/loc from dev Postgres + mint JWT (2h TTL)
  'eyJhbG...'            Validate browser token and update live-session.env

Default --user: lntori (lntori@deladetech.com). Also: brightdebrah, first.

After success:
  ./scripts/local-dev/test-org-structure.sh
  ./scripts/local-dev/post-deploy-verify.sh
EOF
}

if [[ ! -f "${ENV_FILE}" ]]; then
  cp "${ROOT}/scripts/local-dev/live-session.example.env" "${ENV_FILE}"
  echo "Created ${ENV_FILE} from example — review org/bus/loc after mint."
fi

MODE=""
USER_PICK="lntori"
TOKEN_ARG=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    -h|--help) usage; exit 0 ;;
    --sync-secret)
      exec "${ROOT}/scripts/local-dev/sync-jwt-secret-from-azure.sh"
      ;;
    --mint) MODE="mint"; shift ;;
    --user) USER_PICK="${2:?--user requires value}"; shift 2 ;;
    --user=*) USER_PICK="${1#*=}"; shift ;;
    *) TOKEN_ARG="$1"; MODE="token"; shift ;;
  esac
done

if [[ "${MODE}" == "token" && -n "${TOKEN_ARG}" ]]; then
  exec "${ROOT}/scripts/local-dev/refresh-live-session.sh" "${TOKEN_ARG}"
fi

if [[ "${MODE}" != "mint" ]]; then
  usage >&2
  exit 1
fi

if [[ ! -f "${JWT_FILE}" ]]; then
  echo "Missing ${JWT_FILE}. Run with --sync-secret first (needs Key Vault access)." >&2
  exit 1
fi

if [[ ! -f "${DB_ENV}" ]]; then
  echo "Missing ${DB_ENV}. Run: ./scripts/local-dev/pull-azure-dev-env.sh" >&2
  exit 1
fi

# shellcheck source=/dev/null
set -a && source "${ENV_FILE}" && source "${DB_ENV}" && source "${JWT_FILE}" && set +a
export PATH="${SAVED_PATH}"

PSQL="$(command -v psql || true)"
if [[ -z "${PSQL}" && -x /opt/homebrew/opt/libpq/bin/psql ]]; then
  PSQL="/opt/homebrew/opt/libpq/bin/psql"
fi
if [[ -z "${PSQL}" ]]; then
  echo "psql required for --mint (brew install libpq)." >&2
  exit 1
fi
export PSQL

CONN="${APP__CONNECTION_STRING:-}"
if [[ -z "${CONN}" ]]; then
  echo "APP__CONNECTION_STRING empty in ${DB_ENV}" >&2
  exit 1
fi

export USER_PICK="${USER_PICK}"
SESSION_ROW="$(
  USER_PICK="${USER_PICK}" python3 - <<'PY'
import os, subprocess, sys, urllib.parse

user_pick = os.environ.get("USER_PICK", "lntori").lower()
conn = os.environ["APP__CONNECTION_STRING"].replace("postgresql://", "postgres://", 1)
u = urllib.parse.urlparse(conn)
env = os.environ.copy()
env["PGPASSWORD"] = urllib.parse.unquote(u.password or "")
host, port = u.hostname, u.port or 5432
user = urllib.parse.unquote(u.username or "")
db = u.path.lstrip("/").split("?")[0]

sql = """
SELECT ul.user_id, ul.tenant_id, ul.org_id, ul.bus_id, bal.loc_id, u.email, COALESCE(u.fullname, '')
FROM core_platform.cp_user_locations ul
JOIN core_platform.cp_business_app_locations bal
  ON bal.id = ul.bus_app_loc_id AND bal.tenant_id = ul.tenant_id
JOIN core_platform.cp_users u ON u.id = ul.user_id AND u.tenant_id = ul.tenant_id
WHERE ul.app_id = 'app-hr'
  AND ul.delete_status = 'NOT_DELETED' AND ul.is_active = true
  AND bal.delete_status = 'NOT_DELETED' AND bal.is_active = true
ORDER BY u.email;
"""

proc = subprocess.run(
    [os.environ["PSQL"], f"postgresql://{user}@{host}:{port}/{db}?sslmode=require", "-tA", "-F", "|", "-c", sql],
    capture_output=True, text=True, env=env,
)
if proc.returncode != 0:
    print(proc.stderr, file=sys.stderr)
    sys.exit(proc.returncode)

rows = [line for line in proc.stdout.splitlines() if line.strip()]
if not rows:
    print("No app-hr user locations found in dev Postgres.", file=sys.stderr)
    sys.exit(1)

def match(row):
    email = row.split("|", 5)[5].lower()
    if user_pick in ("first", "any"):
        return True
    if user_pick in ("lntori", "larry"):
        return "lntori" in email
    if user_pick in ("brightdebrah", "bright", "debrah"):
        return "brightdebrah" in email
    return user_pick in email

chosen = next((r for r in rows if match(r)), rows[0])
print(chosen)
PY
)"

IFS='|' read -r USER_ID TENANT_ID ORG_ID BUS_ID LOC_ID EMAIL FULLNAME <<<"${SESSION_ROW}"

export USER_ID TENANT_ID ORG_ID BUS_ID LOC_ID EMAIL FULLNAME
NEW_TOKEN="$(
  python3 - <<'PY'
import os, time, jwt
secret = os.environ["TROVESUITE_JWT_SECRET"]
now = int(time.time())
claims = {
    "user_id": os.environ["USER_ID"],
    "tenant_id": os.environ["TENANT_ID"],
    "email": os.environ["EMAIL"],
    "fullname": os.environ["FULLNAME"],
    "iat": now,
    "exp": now + 7200,
}
print(jwt.encode(claims, secret, algorithm="HS256"))
PY
)"

BASE="${ZELOSHR_API_BASE:-https://zeloshr.app.backend.dev.trovesuite.com}"
CODE="$(
  /usr/bin/curl -sS -o /dev/null -w "%{http_code}" \
    -H "app-id: ${TROVE_APP_ID:-app-hr}" \
    -H "authorization: Bearer ${NEW_TOKEN}" \
    -H "org-id: ${ORG_ID}" \
    -H "bus-id: ${BUS_ID}" \
    -H "loc-id: ${LOC_ID}" \
    "${BASE}/api/v1/employees/list?page=1&size=1"
)"

if [[ ! "${CODE}" =~ ^2 ]]; then
  cat >&2 <<EOF
Minted JWT rejected (HTTP ${CODE}) on GET /api/v1/employees/list.

The signing key in .jwt-secret.local likely does not match the dev Container App.
Sync from Key Vault (needs Azure RBAC):

  ./scripts/local-dev/generate-live-session.sh --sync-secret
  ./scripts/local-dev/generate-live-session.sh --mint --user ${USER_PICK}

Or log in on https://zeloshr.dev.trovesuite.com and paste the Bearer token:

  ./scripts/local-dev/generate-live-session.sh 'eyJhbG...'

Session picked from dev DB:
  user:   ${USER_ID} (${EMAIL})
  tenant: ${TENANT_ID}
  org:    ${ORG_ID}
EOF
  exit 1
fi

export NEW_TOKEN USER_ID TENANT_ID ORG_ID BUS_ID LOC_ID ENV_FILE
ENV_FILE="${ENV_FILE}" python3 - <<'PY'
from pathlib import Path
import os, re
path = Path(os.environ["ENV_FILE"])
text = path.read_text()
vals = {
    "TROVE_BEARER_TOKEN": os.environ["NEW_TOKEN"],
    "TROVE_USER_ID": os.environ["USER_ID"],
    "TROVE_TENANT_ID": os.environ["TENANT_ID"],
    "TROVE_ORG_ID": os.environ["ORG_ID"],
    "TROVE_BUS_ID": os.environ["BUS_ID"],
    "TROVE_LOC_ID": os.environ["LOC_ID"],
}
for name, value in vals.items():
    pattern = rf"^{re.escape(name)}=.*$"
    line = f"{name}={value}"
    if re.search(pattern, text, flags=re.M):
        text = re.sub(pattern, line, text, count=1, flags=re.M)
    else:
        text = text.rstrip() + "\n" + line + "\n"
path.write_text(text)
PY

EXP_ISO="$(python3 - <<PY
import os, json, base64
from datetime import datetime, timezone
t = os.environ["NEW_TOKEN"].split(".")[1]
p = json.loads(base64.urlsafe_b64decode(t + "=" * (-len(t) % 4)))
print(datetime.fromtimestamp(p["exp"], tz=timezone.utc).isoformat())
PY
)"

echo "Updated ${ENV_FILE}"
echo "  user:   ${USER_ID} (${EMAIL})"
echo "  tenant: ${TENANT_ID}"
echo "  org:    ${ORG_ID}"
echo "  exp:    ${EXP_ISO}"
echo "  probe:  GET /api/v1/employees/list → HTTP ${CODE}"
echo ""
echo "Run: ./scripts/local-dev/test-org-structure.sh"
