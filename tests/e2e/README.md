# API end-to-end tests (Hurl)

Plain-text HTTP scenarios with **HTML + JUnit reports** (request/response bodies included).

## Prerequisites

1. `scripts/local-dev/live-session.env` with Trove headers + JWT (see [scripts/local-dev/README.md](../../scripts/local-dev/README.md)).
2. **Hurl** locally (`brew install hurl`) **or** Docker (the runner uses `ghcr.io/orange-opensource/hurl:latest` automatically).

Optional in `live-session.env`:

- `EMP_ID` — employee for admin create/reject tests (otherwise first row from `GET /employees/list`).
- `LEAVE_TYPE_ID` — skip type discovery.

## Run Leave E2E

```bash
./scripts/local-dev/test-leave-e2e.sh
```

Reports (gitignored):

| Output | Path |
|--------|------|
| HTML (open in browser) | `reports/e2e/leave/latest/index.html` |
| JUnit (CI) | `reports/e2e/leave/latest/junit.xml` |
| Timestamped copy | `reports/e2e/leave/<run-id>/` |

```bash
./scripts/local-dev/test-leave-e2e.sh --open    # run + open HTML report
./scripts/local-dev/test-leave-e2e.sh --local   # ZELOSHR_API_BASE=http://localhost:8000
```

## Scenarios (`tests/e2e/leave/`)

| File | Use cases |
|------|-----------|
| `01-reads.hurl` | Statistics, dashboard, My Leave reads |
| `02-list-filters.hurl` | Admin list: pending final, history, search, date overlap |
| `03-settings-reads.hurl` | Types, holidays, balances |
| `04-mutations-reject.hurl` | Resolve ids/type → create → detail asserts → reject → verify |
| `05-mutations-delete.hurl` | Create pending request → delete cleanup |

**Note:** Approve workflow (LM → HoD → final) needs the **correct platform user per stage**; use separate Hurl files with different tokens when you add multi-user env files. Reject/delete work with a single admin `LeaveUpdate` token.

## CI

Upload `reports/e2e/leave/latest/` as a workflow artifact after:

```bash
./scripts/local-dev/test-leave-e2e.sh
```

## Adding modules

Copy the Leave layout: `tests/e2e/<module>/*.hurl` + wire into a dedicated runner or extend `test-leave-e2e.sh`.
