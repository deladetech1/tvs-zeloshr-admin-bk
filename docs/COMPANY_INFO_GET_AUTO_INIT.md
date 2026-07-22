# Company info GET auto-init

**Status:** Shipped on `dev`.

## Behaviour

`GET /api/v1/company/info/get` **always returns 200** for a valid Trove session (`tenant_id` from JWT + `org-id` header).

If no `zhr_company_profile` row exists for that org, the API **creates a stub row on that GET**:

- **`legal_name`** defaults to the Trovesuite **business name** (`bus-id` header → `core_platform.cp_businesses.bus_name`)
- Other business fields are **`null`** in JSON (`trading_name`, …, `logo_url`, `banner_url`)
- **`offices: []`**
- **`id`** is a real UUID — use it on **`PUT /company/info/update`**

Existing stubs with empty `legal_name` are **backfilled** on GET when a business name is available.

**Localization is unchanged** — still explicit POST; still 404 until created.

## Frontend contract

```text
GET /api/v1/company/info/get
  → 200, data.legal_name from business name (editable)
  → 200, other fields null until user saves

PUT /api/v1/company/info/update
  → body includes id from GET; legal_name required; full replacement
```

Do **not** rely on 404 for company info anymore.

## Known downsides (share with the team)

| Area | Risk |
|------|------|
| **HTTP semantics** | GET is no longer a pure read — it can insert a DB row (side effect). |
| **Permissions** | Stub creation runs on **GET** (`EmployeeGet`), not **`EmployeeUpdate`**. |
| **Retries / parallel tabs** | Duplicate GETs are handled via unique `(tenant_id, org_id)` + re-fetch. |
| **Business name as legal name** | May not match registered legal entity — user should review/edit on first load. |
| **Localization** | Still 404 until POST — different pattern from company info. |

## Dependencies

- `core_platform.cp_businesses` must exist for the session's `bus-id` (already required for Trove context validation).

## Ops / support

- **Stub with only legal_name set** — expected after first GET when business name resolves.
- **legal_name still null** — business row missing or inactive; check `bus-id` header matches `cp_businesses.id`.

---

*Document for PR reviewers, frontend, and product.*
