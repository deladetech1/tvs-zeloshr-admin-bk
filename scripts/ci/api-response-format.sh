#!/usr/bin/env bash
# Human-readable API response lines for live/CI smoke scripts.
# Usage: format_api_response "$json_body"
format_api_response() {
  local json="${1:-}"
  if [[ -z "$json" ]]; then
    echo "(empty body)"
    return
  fi

  API_JSON="$json" python3 - <<'PY'
import json
import os
import sys

raw = os.environ.get("API_JSON", "")
try:
    body = json.loads(raw)
except json.JSONDecodeError:
    print(raw[:600].replace("\n", " "))
    sys.exit(0)

if not isinstance(body, dict):
    print(str(body)[:600])
    sys.exit(0)

lines: list[str] = []
detail = body.get("detail") or body.get("error")
if detail:
    lines.append(str(detail).strip())

field_errors = body.get("field_errors") or {}
if isinstance(field_errors, dict):
    for key, message in field_errors.items():
        if message:
            lines.append(f"{key}: {message}")

if lines:
    print("\n  ".join(lines))
else:
    success = body.get("success")
    if success is True:
        data = body.get("data")
        if isinstance(data, dict) and data:
            preview = json.dumps(data, separators=(",", ":"))[:400]
            print(f"OK — data: {preview}")
        else:
            print("OK")
    else:
        print(raw[:600].replace("\n", " "))
PY
}

# Wait until GET /api/v1/health returns 2xx (Container App warm-up after deploy).
wait_for_api_health() {
  local base="${1:?base URL required}"
  local max_attempts="${2:-18}"
  local sleep_seconds="${3:-10}"

  for ((attempt = 1; attempt <= max_attempts; attempt++)); do
    local code
    code="$(curl -sS -o /dev/null -w "%{http_code}" "${base}/api/v1/health" 2>/dev/null || echo "000")"
    if [[ "$code" =~ ^2 ]]; then
      echo "API ready (${base}/api/v1/health → HTTP ${code}) after ${attempt} attempt(s)."
      return 0
    fi
    echo "Waiting for API (${attempt}/${max_attempts}): HTTP ${code}"
    sleep "$sleep_seconds"
  done

  echo "API did not become healthy at ${base} within $((max_attempts * sleep_seconds))s." >&2
  return 1
}
