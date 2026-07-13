# API query parameters (snake_case)

All **query string** names on `/api/v1/*` use **snake_case** — never PascalCase or camelCase on the wire.

This matches frontend filter interfaces (`EmployeeParams`, `OrgStructureParams`, `CustomFieldParams`, `EmployeeAuditLogParams`, etc.).

## Rules

| Rule | Example |
|------|---------|
| Words separated by `_` | `department_id`, `is_paid`, `sort_by` |
| Resource ids end with `_id` | `employee_id`, `leave_type_id`, `holiday_id` |
| Booleans use `is_` / `has_` / `include_` prefixes | `is_paid`, `is_filterable`, `include_archived`, `active_only` |
| Dates use `_date` suffix | `start_date`, `from_date`, `anchor_date` |
| Pagination | `page`, `size` (1-based page index) |
| Sorting | `sort_by`, `sort_order` (`asc` / `desc`) |

## Implementation (backend)

- Wire names live in [`PlatformQueryParams`](../app/src/Shared/Constants/PlatformQueryParams.cs) for resource ids and shared filters.
- List/filter DTOs use `[FromQuery(Name = "snake_case")]` on **every** property (see `EmployeeListQuery`, `OrgStructureListQuery`).
- Controller action parameters that are not DTO-bound must also set `[FromQuery(Name = "...")]`.
- JSON **response** bodies use snake_case via the global JSON serializer — query params are separate and must be explicit.

## Frontend ↔ API (canonical interfaces)

### Employees — `GET /employees/list`

Backend: `EmployeeListQuery`

```typescript
export interface EmployeeParams {
  page: number;
  size: number;
  sort_by?: string;
  sort_order?: string;
  search?: string;
  employment_status?: string;
  status?: string;
  department_id?: string;
  branch_id?: string;
  employment_type?: string;
  work_location?: string;
  start_date?: string;
  end_date?: string;
  is_line_manager?: boolean;
  is_head_of_department?: boolean;
}
```

`GET /employees/export` uses the same filter names (no `page` / `size` / `sort_by`).

### Org structure — `GET /org-structure/departments/list` · `GET /org-structure/branches/list`

Backend: `OrgStructureListQuery`

```typescript
export interface OrgStructureParams {
  page: number;
  size: number;
  sort_by: string;
  sort_order: string;
  search?: string;
  include_archived?: boolean;
}
```

`sort_by`: `name` | `employeeCount`

### Custom fields — `GET /custom-fields/list`

Backend: `CustomFieldListQuery`

```typescript
export interface CustomFieldParams {
  page: number;
  size: number;
  sort_by: string;
  sort_order: string;
  search?: string;
  entity_type?: string;
  field_key?: string;
  label?: string;
  field_type?: string;
  is_required?: boolean;
  is_sensitive?: boolean;
  is_filterable?: boolean;
  is_searchable?: boolean;
  is_active?: boolean;
  section_name?: string;
  include_deleted?: boolean;
}
```

Optional boolean filters: omit when not filtering (do not send empty strings).

`include_inactive` is supported on `GET /employees/list` and export but is not part of the frontend `EmployeeParams` interface.

### Employee audit logs — `GET /audit-logs/list`

Backend: `AuditLogListQuery` (extends `AuditLogFilterQuery`)

```typescript
export interface EmployeeAuditLogParams {
  page: number;
  size: number;
  search?: string;
  action?: string;
  severity?: string;
  actor?: string;
  start_date?: string;
  end_date?: string;
}
```

### Leave types — `GET /leave/types/list`

Backend: `LeaveTypeListQuery`

```typescript
export interface LeaveTypeParams {
  search?: string;
  is_paid?: boolean;
  accrual_method?: string;
  active_only?: boolean;
  page: number;
  size: number;
}
```

### Public holidays — `GET /leave/holidays/list`

Backend: `PublicHolidayListQuery`

```typescript
export interface PublicHolidayParams {
  search?: string;
  year?: number | null;
  country?: string;
  page: number;
  size: number;
}
```

## Checklist for new list endpoints

1. Add or reuse names in `PlatformQueryParams`.
2. Create a `*ListQuery` record/class with `[FromQuery(Name = ...)]` on each property.
3. Bind with `[FromQuery] MyListQuery query` on the controller.
4. Add the TypeScript interface to this doc.
5. Document params in Swagger (`SwaggerQueryParameterExamplesFilter` / operation filter).
