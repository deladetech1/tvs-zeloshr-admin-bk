# Standard audit fields (all API resource items)

**Mandatory on every list/get/mutation response item** across ZelosHR.Api — no exceptions for CRUD resources.

See also [API_CONTRACTS.md](API_CONTRACTS.md) (tenancy table) and [AGENTS.md](../AGENTS.md) (PR checklist).

## Wire shape (snake_case JSON)

Every resource row returned in `data` (or `data.items[]`) must include these six properties:

```json
{
  "created_at": "2026-06-10T09:15:00+00:00",
  "updated_at": "2026-06-11T16:20:00+00:00",
  "created_by_id": "cp-user-demo-admin",
  "updated_by_id": "cp-user-demo-admin",
  "created_by": "Fiifi Boakye",
  "updated_by": "Fiifi Boakye"
}
```

| Field | Type | Source |
|-------|------|--------|
| `created_at` | ISO-8601 timestamp | DB column |
| `updated_at` | ISO-8601 timestamp | DB column |
| `created_by_id` | string or `null` | DB column — platform user id (`cp_users.id`) |
| `updated_by_id` | string or `null` | DB column — platform user id |
| `created_by` | string or `null` | Resolved at read time from `cp_users.fullname` via `created_by_id` |
| `updated_by` | string or `null` | Resolved at read time from `cp_users.fullname` via `updated_by_id` |

## Rules

1. **Store ids in the database** — never store display names for audit (`created_by` / `updated_by` are read-model only).
2. **Resolve names on read** — batch `ICpUserRepository.GetByIdsAsync` (same pattern as `manager_name`, custom fields, departments).
3. **Null is valid** — legacy rows or deleted users may have ids without resolvable names; return `null` for `created_by` / `updated_by`, never fabricate labels.
4. **Swagger examples** — illustrative JSON only; frontend must bind to live API responses, not copy example names/ids.
5. **Mutations** — set `created_by` / `updated_by` ids from `ITenantContext.UserId` on create/update/approve/reject.

## Implementation helpers

| Helper | Location |
|--------|----------|
| `ResourceAuditMapper` | `app/src/Entities/shared/ResourceAuditMapper.cs` |
| `LeaveAuditFields` | `app/src/Entities/leave/LeaveAuditFields.cs` |
| `CustomFieldDefinitionMapper.EnrichAuthors` | custom field definitions |

## PR checklist (audit)

- [ ] DTO has all six properties
- [ ] Repository/entity maps DB `created_at` / `updated_at` / `created_by` / `updated_by`
- [ ] Service enriches `created_by` / `updated_by` display names from `cp_users`
- [ ] Create/update paths stamp actor user id
- [ ] Swagger schema example includes all six fields
- [ ] tvs-sqlscript migration if new audit columns are needed
