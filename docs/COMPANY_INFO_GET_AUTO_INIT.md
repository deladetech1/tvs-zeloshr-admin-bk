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

**Localization** — same pattern: `GET /api/v1/company/localization/get` auto-creates settings with tenant default currency and standard formats (see below).

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
| **Permissions** | Stub creation runs on **GET** (`EmployeeGet`), not **`EmployeeUpdate`**. Any user who can read HR can trigger row creation for that org. |
| **Retries / parallel tabs** | Duplicate GETs are handled via unique `(tenant_id, org_id)` + re-fetch, but first load is a write path (slower, more pool pressure). |
| **Business name as legal name** | May not match registered legal entity — user should review/edit on first load. |
| **Analytics** | Count of profile rows overstates “configured companies”. Filter on non-null `legal_name`. |
| **Emails / branding** | Until user confirms profile, invite/activation emails may use business name or fall back to app name **ZelosHR**. |
| **Caching** | GET responses must not be cached by proxies/clients (they were already authenticated; document for integrators). |
| **Localization** | GET also auto-creates rows (prefilled defaults). POST still 400 if row exists — use PUT /update. |

## Alternatives we rejected (for future reconsideration)

- **`POST /company/info/bootstrap`** — explicit write, clearer permissions (preferred long-term if we revisit).
- **200 without insert** — no DB side effect; frontend handles empty state via 404 or null payload only.

## Localization GET auto-init

`GET /api/v1/company/localization/get` **returns 200** when the tenant has at least one active currency.

If no `zhr_company_localization` row exists, the API **creates a stub on that GET**:

- **`currency_id`** — tenant default currency (`cp_currencies.is_default`), else first active currency
- **`time_zone`** — `Africa/Accra`
- **`date_format`** — `DD/MM/YYYY`
- **`number_format`** — `1,234.56`
- **`first_day_of_week`** — `Monday`
- **`year_start_month`** / **`year_start_day`** — `January` / `1`

**503** when no active currency exists for the tenant.

Frontend: load GET on settings page; user edits and saves via **`PUT /company/localization/update`**. Do not rely on 404 for localization anymore.

---

## Dependencies

- `core_platform.cp_businesses` must exist for the session's `bus-id` (already required for Trove context validation).

## Ops / support

- **Stub with only legal_name set** — expected after first GET when business name resolves.
- **legal_name still null** — business row missing or inactive; check `bus-id` header matches `cp_businesses.id`.

---

*Document for PR reviewers, frontend, and product. Cons section is intentional — auto-init on GET is a tradeoff, not a free win.*
