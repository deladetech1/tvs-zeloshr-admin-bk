# Mystoreguard API conformance (ZelosHR)

Reference spec: [`mystoreguard.json`](mystoreguard.json) (OpenAPI 3.1, Trovesuite retail app).

Goal: **any client that already integrates Mystoreguard can integrate ZelosHR without learning a second dialect** — same headers, JSON casing, envelope, list patterns, and (where practical) URL verbs.

---

## Conformance matrix

| Area | Mystoreguard | ZelosHR (target) | Status |
|------|----------------|------------------|--------|
| Base path | `/api/v1` | `/api/v1` | Done |
| Auth | `HTTPBearer` + Trove headers | Same (`TroveRequestHeadersMiddleware`) | Done |
| Required headers | `app-id`, `authorization`, `bus-id`, `loc-id`, `org-id` | Same on `/api/v1/*` (Swagger + middleware) | Done |
| JSON property names | `snake_case` (`status_code`, `has_next`) | `PlatformJson` + controller JSON options | **Done (this change)** |
| Response envelope | `success`, `status_code`, `detail`, `error`, `data`, `pagination?` | `Respons<T>` with same fields (snake_case on wire) | Done |
| Validation map | FastAPI `422` + `HTTPValidationError` | `400` + `field_errors` on `Respons` | Partial (by design) |
| Pagination | `page`, `size`, `total`, `has_next` | Same on list endpoints | Done |
| List routes | `GET /{module}/list` + query filters | All HR modules: `GET /{module}/list` | Done |
| Get one | `GET /{module}/get?{resource}_id=` | All HR modules | Done |
| Create | `POST /{module}/add` | All HR modules | Done |
| Update | `PUT /{module}/update?{resource}_id=` | All HR modules (`PATCH` removed) | Done |
| Delete | module-specific (`permanent-delete`, etc.) | `DELETE /{module}/delete?{resource}_id=` + soft delete | Done |
| Statistics | `GET /{module}/statistics` | All HR modules (+ dashboard) | Done |
| `data` shape | Often **array** even for single record | **Object** in `data` (C# `Respons<T>`) | **Gap** — Phase 3 decision |
| DTO names in OpenAPI | `{Action}{Resource}ControllerWriteDto` | Mixed (`EmployeeDetailDto`, `CreateEmployeeAggregateRequest`, …) | Planned |
| HTTP status on create | Usually `200` | Mix of `200` / `201` | Acceptable variance |
| OpenAPI version | 3.1 | 3.0.3 (compat middleware for UI tools) | Documented in `SWAGGER.md` |
| Embedded documents (read) | `documents[]` → `DocumentReadDto` (`doc_id`, `name`, `presigned_url`, `description`) | Employee GET `documents[]` + `identity.profile_url` | Done |
| Document IDs (write) | `document_ids` string array | Employee create/update `document_ids` | Done |
| File list response | `FileResponseControllerReadDto` (`id`, `file_name`, …) | `GET /file/list` | Done |
| Product metadata (read) | `metadata[]` → `MetadataReadDto` | N/A (employees have no tag/category metadata) | N/A |

---

## JSON envelope (wire format)

All business endpoints should return:

```json
{
  "success": true,
  "status_code": 200,
  "detail": "Success",
  "error": null,
  "data": { },
  "field_errors": null,
  "pagination": {
    "page": 1,
    "size": 20,
    "total": 42,
    "has_next": true
  }
}
```

Implementation: `app/src/Configs/PlatformJson.cs` (snake_case) + `Respons<T>` in `app/src/Entities/shared/Respons.cs`.

**Breaking change:** clients that parsed camelCase (`statusCode`, `hasNext`, `fieldErrors`) must switch to snake_case after deploy.

---

## URL styles (two supported layers)

### 1. Platform verbs (Mystoreguard)

Example — **employees** (implemented):

| Action | Method | Path | Notes |
|--------|--------|------|--------|
| Create | `POST` | `/api/v1/employees/add` | Same body as `POST /employees` |
| List | `GET` | `/api/v1/employees/list` | Query params use snake_case names |
| Get | `GET` | `/api/v1/employees/get?employee_id={uuid}` | |
| Statistics | `GET` | `/api/v1/employees/statistics` | Directory KPIs |
| Update | `PUT` | `/api/v1/employees/update?employee_id=` | Partial body |

Controller: `EmployeesController.cs` (single surface).

### 2. HR-specific nested routes (same controller)

No path UUIDs; query `employee_id` on wizard and sub-resources:

- `GET /employees/directory`, `POST /employees/draft`, `PUT /employees/employment/update?employee_id=`, `GET /employees/education/list?employee_id=`, etc.

---

## Rollout phases

### Phase 1 — Wire format (complete)

- [x] snake_case JSON globally
- [x] Swagger schemas use same serializer
- [x] Middleware error bodies use snake_case

### Phase 2 — Employee module verbs (complete)

- [x] `/employees/add`, `/list`, `/get`

### Phase 3 — Remaining modules (backlog)

For each module (`custom-fields`, `leave`, `org-structure/branches`, …):

1. Add `{module}/add`, `{module}/list`, `{module}/get`, `{module}/update` delegating to existing services.
2. Document query param names (`department_id`, not `departmentId`) on platform routes.
3. Align OpenAPI schema names with `*ControllerWriteDto` / `*ControllerReadDto` where codegen is required.

### Phase 4 — Optional strict parity

- [ ] Wrap single-resource `data` as one-element arrays (only if platform team mandates)
- [ ] Align all creates to `200` instead of `201`
- [ ] FastAPI-style `422` for model validation (vs `400` + `field_errors`)

---

## Checklist for new endpoints

1. Under `/api/v1/{module}/…` with Trove headers documented in Swagger.
2. Return `Respons<T>` (never raw DTO).
3. Lists: `page`, `size`, filters, `pagination` block.
4. Prefer snake_case query names on **platform** routes (`employee_id`, `lifecycle_state`).
5. Add Mystoreguard aliases if the SPA uses shared API client code from retail apps.
6. Update this matrix and `ENTERPRISE_API.md`.

---

## Related docs

- [`FILE_MANAGEMENT.md`](FILE_MANAGEMENT.md) — uploads, `DocumentReadDto`, `blob_paths`, employee attach flow
- [`ENTERPRISE_API.md`](ENTERPRISE_API.md) — CRUD matrix
- [`API_CONTRACTS.md`](API_CONTRACTS.md) — frontend quick reference
- [`SWAGGER.md`](SWAGGER.md) — Try it out / production spec
