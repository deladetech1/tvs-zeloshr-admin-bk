#!/usr/bin/env bash
# ZelosHR local dev & test — Docker Compose only.
# Usage: ./scripts/compose.sh <command>
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "${ROOT}"

export COMPOSE_ENV_FILES="${COMPOSE_ENV_FILES:-app/.env}"
export TVS_SQLSCRIPT_PATH="${TVS_SQLSCRIPT_PATH:-${ROOT}/../tvs-sqlscript}"

if [[ ! -f "${COMPOSE_ENV_FILES}" ]]; then
  echo "Missing ${COMPOSE_ENV_FILES}. Run: cp app/.env.example app/.env" >&2
  exit 1
fi

# shellcheck source=/dev/null
source "${COMPOSE_ENV_FILES}" 2>/dev/null || true

compose() {
  docker compose "$@"
}

usage() {
  cat <<'EOF'
ZelosHR Compose workflow (see docs/LOCAL_DEV.md)

  ./scripts/compose.sh test       Unit tests (dotnet in SDK container; no DB)
  ./scripts/compose.sh migrate    Deploy schema + seeds via tvs-sqlscript
  ./scripts/compose.sh db         Start Postgres + Redis only
  ./scripts/compose.sh dev        db → migrate → build & start API
  ./scripts/compose.sh up         Alias for dev
  ./scripts/compose.sh api        Start API (assumes DB + migrate already ran)
  ./scripts/compose.sh reset      Remove volumes, fresh DB, migrate, start API
  ./scripts/compose.sh down       Stop all services
  ./scripts/compose.sh ci         test + docker build (matches GitHub Actions)
  ./scripts/compose.sh smoke      Quick HTTP checks (API must be running)
  ./scripts/compose.sh logs       docker compose logs -f api

Env:
  TVS_SQLSCRIPT_PATH   Path to tvs-sqlscript repo (default: ../tvs-sqlscript)
  TVS_SEED_ZELOSHR_DEMO  1 to seed demo-tenant data on migrate (default: 1)
EOF
}

cmd="${1:-}"
shift || true

case "${cmd}" in
  test)
    compose --profile tools run --rm test
    ;;
  migrate)
    compose up -d db
    compose --profile tools run --rm migrate
    ;;
  db)
    compose up -d db redis
    compose ps
    ;;
  dev|up)
    compose up -d db redis
    compose --profile tools run --rm migrate
    compose up -d --build api
    echo ""
    echo "Swagger: http://localhost:${API_PORT:-8000}/swagger"
    echo "Headers:  app-id, authorization (Bearer JWT), bus-id, loc-id, org-id"
    echo "JWT:      ./scripts/gen-trovesuite-jwt.sh"
    ;;
  api)
    compose up -d --build api
    ;;
  reset)
    compose down -v
    compose up -d db redis
    compose --profile tools run --rm migrate
    compose up -d --build api
    ;;
  down)
    compose down "$@"
    ;;
  ci)
    compose --profile tools run --rm test
    docker build \
      --build-arg GITHUB_PACKAGES_TOKEN="${GITHUB_PACKAGES_TOKEN:-}" \
      -f app/Dockerfile \
      .
    ;;
  smoke)
    BASE="http://localhost:${API_PORT:-8000}"
    TOKEN="$(ROOT="${ROOT}" "${ROOT}/scripts/gen-trovesuite-jwt.sh" 2>/dev/null | tail -1 || true)"
    if [[ -z "${TOKEN}" ]]; then
      echo "smoke: could not generate JWT (run ./scripts/gen-trovesuite-jwt.sh)" >&2
      exit 1
    fi
    curl_headers=(
      -H "app-id: app-hr"
      -H "authorization: Bearer ${TOKEN}"
      -H "bus-id: ${TROVE_BUS_ID:-bus_demo}"
      -H "loc-id: ${TROVE_LOC_ID:-loc_demo}"
      -H "org-id: ${TROVE_ORG_ID:-demo-org}"
    )
    code=$(curl -s -o /dev/null -w "%{http_code}" "${curl_headers[@]}" "${BASE}/api/v1/health" || echo "000")
    echo "/api/v1/health → HTTP ${code}"
    for path in /api/v1/navigation /api/v1/employees/directory/summary; do
      code=$(curl -s -o /dev/null -w "%{http_code}" "${curl_headers[@]}" "${BASE}${path}" || echo "000")
      echo "${path} → HTTP ${code}"
    done
    ;;
  logs)
    compose logs -f api "$@"
    ;;
  ""|help|-h|--help)
    usage
    ;;
  *)
    echo "Unknown command: ${cmd}" >&2
    usage >&2
    exit 1
    ;;
esac
