# Company info GET auto-init

**Status:** Shipped behind `feat/company-info-get-auto-init` (ZelosHR) + nullable `legal_name` migration (tvs-sqlscript).

## Behaviour

`GET /api/v1/company/info/get` **always returns 200** for a valid Trove session (`tenant_id` from JWT + `org-id` header).

If no `zhr_company_profile` row exists for that org, the API **creates a stub row on that GET**:

- All business fields are **`null`** (`legal_name`, `trading_name`, …, `logo_url`, `banner_url`)
- **`offices: []`**
- **`id`** is a real UUID — use it on **`PUT /company/info/update`**

After the user saves real data via **PUT** (or **POST /add** if the stub is still empty), fields are populated; **`legal_name` non-null** means configured.

**Localization is unchanged** — still explicit POST; still 404 until created.

## Frontend contract

```text
GET /api/v1/company/info/get
  → 200, !data.legal_name  → show setup form (empty)
  → 200, data.legal_name   → show/edit populated profile

PUT /api/v1/company/info/update
  → body includes id from GET; legal_name required; full replacement
```

Do **not** rely on 404 for company info anymore.

## Why we did this (team request)

- Avoid a 404 branch on first load of Company Settings.
- Always have a stable `id` for PUT without a separate bootstrap call.

## Known downsides (share with the team)

| Area | Risk |
|------|------|
| **HTTP semantics** | GET is no longer a pure read — it can insert a DB row (side effect). |
| **Permissions** | Stub creation runs on **GET** (`EmployeeGet`), not **`EmployeeUpdate`**. Any user who can read HR can trigger row creation for that org. |
| **Retries / parallel tabs** | Duplicate GETs are handled via unique `(tenant_id, org_id)` + re-fetch, but first load is a write path (slower, more pool pressure). |
| **Ambiguous state** | “Org opened settings once” ≠ “admin configured company”. Use **null `legal_name`**, not row existence, for gating/onboarding. |
| **Analytics** | Count of profile rows overstates “configured companies”. Filter on non-null `legal_name`. |
| **Emails / branding** | Until `legal_name` is set, invite/activation emails still fall back to app name **ZelosHR**. |
| **Caching** | GET responses must not be cached by proxies/clients (they were already authenticated; document for integrators). |
| **Localization** | Still 404 until POST — two different patterns for Company Settings sub-modules. |

## Alternatives we rejected (for future reconsideration)

- **`POST /company/info/bootstrap`** — explicit write, clearer permissions (preferred long-term if we revisit).
- **200 without insert** — no DB side effect; frontend handles empty state via 404 or null payload only.
- **Seed `legal_name` from Trovesuite org metadata** — better defaults than null; not implemented in v1.

## Dependencies

- **tvs-sqlscript:** `legal_name` on `zeloshr.zhr_company_profile` must be **nullable** (`20260720140000_MakeZhrCompanyProfileLegalNameNullable`). Merge and deploy **before** ZelosHR API that auto-inits stubs.

## Ops / support

- **Stub row exists but fields empty** — expected after first GET; not a failed migration.
- **DELETE** removes stub and configured profiles alike.
- **POST /add** after GET stub — allowed if `legal_name` is still null (completes via update path); **400** if `legal_name` is already set.

---

*Document for PR reviewers, frontend, and product. Cons section is intentional — auto-init on GET is a tradeoff, not a free win.*
