# File management (employee documents)

ZelosHR stores employee attachments and profile photos in **Azure Blob Storage** and registers each upload in **`human_resource.hr_document_paths`**. Clients never send blob URLs on write — they send **document registry IDs** (MyStoreGuard pattern).

Reference retail spec: [MYSTOREGUARD_API_CONFORMANCE.md](MYSTOREGUARD_API_CONFORMANCE.md) · OpenAPI mirror: [`mystoreguard.json`](mystoreguard.json).

Swagger tag: **File Management** · Base path: `/api/v1/file`

---

## MyStoreGuard: two document shapes

Mystoreguard (and ZelosHR) use **different DTOs** depending on context. Do not mix field names.

| Context | Wire shape | Key fields |
|---------|------------|------------|
| **`GET /file/list`** (standalone lookup) | `FileResponseReadDto` | `id`, `presigned_url`, `description`, `file_name` |
| **Employee GET** (embedded on entity) | `DocumentReadDto` | `doc_id`, `name`, `presigned_url`, `description` |
| **Employee write** (create/update) | string array | `document_ids` — registry IDs from upload |
| **Profile photo write** | string | `identity.profile_url` — one document id |
| **Profile photo read** | `DocumentReadDto` | same as embedded documents |

Presigned URLs expire after **24 hours**. Re-call `GET /file/list` or `GET /employees/get` when a link expires.

**Not used on employees:** product `metadata[]` (`MetadataReadDto` — tags, categories, brands). HR has no equivalent.

---

## Endpoints

| Method | Path | Purpose |
|--------|------|---------|
| `POST` | `/api/v1/file/post/multiple` | Upload one or more files → registry IDs |
| `GET` | `/api/v1/file/list?document_ids=` | Resolve IDs to presigned URLs + metadata |
| `PUT` | `/api/v1/file/put?document_id=` | Replace blob content (same registry ID) |
| `DELETE` | `/api/v1/file/delete?document_id=` | Delete blob + soft-delete registry row |

All routes require Trove headers (`app-id`, `authorization`, `bus-id`, `loc-id`, `org-id`). See [SWAGGER.md](SWAGGER.md).

Permissions: upload/update/delete need **employee update**; list needs **employee get**.

---

## Recommended client flow

### 1. Attachments (contracts, ID scans, etc.)

```
POST /file/post/multiple          → data[].id
POST or PUT /employees/...        → document_ids: ["id1", "id2"]
GET /employees/get?employee_id=   → documents[] (DocumentReadDto)
```

### 2. Profile photo

```
POST /file/post/multiple          → data[0].id
PUT /employees/update?employee_id= → identity.profile_url: "id"
GET /employees/get?employee_id=   → identity.profile_url (DocumentReadDto)
```

Profile photos are **not** attached via top-level `document_ids`. Use `identity.profile_url` only.

### 3. Remove an attachment

```
PUT /employees/update?employee_id= → delete_document_ids: ["id"]
```

Optionally `DELETE /file/delete?document_id=` to remove the blob from storage.

---

## `POST /file/post/multiple`

**Content-Type:** `multipart/form-data`

| Part / query | Required | Notes |
|--------------|----------|-------|
| `files` | **yes** | Repeat the same field name for each file: `formData.append("files", file)`. Also accepts `files[]` or any file parts in the body. |
| `blob_paths` | no | **Leave empty** in almost all cases (see below). |
| `descriptions` | no | Comma-separated labels, one per file (optional). |

**Response** (`200`):

```json
{
  "success": true,
  "status_code": 200,
  "data": [
    { "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890" }
  ]
}
```

Use `data[].id` on employee create/update — not blob paths, not presigned URLs.

### `blob_paths` (optional — usually omit)

When **omitted or empty**, the server auto-generates paths:

```
{tenant_id}/{org_id}/{bus_id}/employees/documents/{8-char-uuid}-{sanitized_filename}
```

Container name is server config (**`zeloshr`**) — clients do not send it.

When **provided**, comma-separated paths inside that container:

| Paths sent | Files uploaded | Behaviour |
|------------|----------------|-----------|
| *(omit)* | N | Auto-generate N paths |
| 1 | N | Same path used for every file |
| N | N | Path *i* → file *i* |
| anything else | — | **400** `field_errors.blob_paths` |

**Common 400 cause:** sending two comma-separated paths (e.g. from an old Swagger example) while uploading **one** file. Fix: **remove `blob_paths` from the query string**.

Values `undefined` and `null` (as strings) are treated as omitted.

### Example — curl (auto path)

```bash
curl -X POST "${BASE}/api/v1/file/post/multiple?descriptions=Employment%20contract" \
  -H "app-id: app-hr" \
  -H "authorization: Bearer ${TOKEN}" \
  -H "org-id: ${ORG_ID}" \
  -H "bus-id: ${BUS_ID}" \
  -H "loc-id: ${LOC_ID}" \
  -F "files=@./contract.pdf"
```

### Example — JavaScript

```javascript
const form = new FormData();
for (const file of files) {
  form.append("files", file); // not files[]
}

const url = new URL("/api/v1/file/post/multiple", apiBase);
if (descriptions.length) {
  url.searchParams.set("descriptions", descriptions.join(","));
}
// Do not set blob_paths unless you have a deliberate custom layout.

const res = await fetch(url, {
  method: "POST",
  headers: {
    "app-id": "app-hr",
    authorization: `Bearer ${token}`,
    "org-id": orgId,
    "bus-id": busId,
    "loc-id": locId,
  },
  body: form,
});
const { data } = await res.json();
const documentIds = data.map((row) => row.id);
```

---

## `GET /file/list`

**Query:** `document_ids` — required, comma-separated registry IDs.

**Response** — array of `FileResponseReadDto`:

```json
{
  "data": [
    {
      "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "presigned_url": "https://….blob.core.windows.net/zeloshr/…?sv=…",
      "description": "Employment contract",
      "file_name": "contract.pdf"
    }
  ]
}
```

Use this endpoint when you have IDs but need fresh presigned URLs without loading the full employee aggregate.

---

## Employee read vs write

### Write (`POST /employees/add`, `PUT /employees/update`)

| Field | Type | Purpose |
|-------|------|---------|
| `document_ids` | `string[]` | Append registry IDs from upload |
| `delete_document_ids` | `string[]` | Detach IDs from employee (update only) |
| `identity.profile_url` | `string` | Profile photo document id; `""` clears |

On update, `identity.profile_url` also accepts a read-shaped object (`doc_id` or `id`) for round-trip from GET — the server stores only the id.

**Do not** send presigned HTTPS URLs on write — returns `400` with guidance to use document ids.

### Read (`GET /employees/get`, `GET /employees/list`)

| Field | Type | Shape |
|-------|------|-------|
| `documents` | array | `DocumentReadDto[]` — attachments only |
| `identity.profile_url` | object \| null | `DocumentReadDto` — profile photo |
| *(list items)* `profile_url` | object \| null | `DocumentReadDto` |

**`DocumentReadDto` example:**

```json
{
  "doc_id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "name": "contract.pdf",
  "presigned_url": "https://….blob.core.windows.net/zeloshr/…?sv=…",
  "description": "Employment contract"
}
```

### Full employee GET excerpt

```json
{
  "success": true,
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "employee_code": "EMP-000042",
    "identity": {
      "full_name": "Ada Lovelace",
      "profile_url": {
        "doc_id": "doc-profile-001",
        "name": "ada.jpg",
        "presigned_url": "https://…",
        "description": "Employee profile photo"
      }
    },
    "documents": [
      {
        "doc_id": "doc-contract-001",
        "name": "contract.pdf",
        "presigned_url": "https://…",
        "description": "Employment contract"
      }
    ]
  }
}
```

---

## Storage layout

| Path pattern | Used for |
|--------------|----------|
| `{tenant}/{org}/{bus}/employees/documents/{unique}-{filename}` | File API default (attachments) |
| `{tenant}/{org}/{bus}/employees/profile/{employee_id}.{ext}` | Profile photo (when set via dedicated profile flow) |
| `{tenant}/{org}/{bus}/employees/documents/wizard/{employee_id}/…` | Registration wizard uploads |

Config: [APPCONFIG.md](APPCONFIG.md) · Production identity: [PRODUCTION_CONFIG.md](PRODUCTION_CONFIG.md).

Registry table: `human_resource.hr_document_paths` (`id`, `tenant_id`, `document_path`, `file_name`, `description`, …).

---

## Error reference

| `field_errors` key | Typical cause | Fix |
|--------------------|---------------|-----|
| `blob_paths` | Path count ≠ file count (and ≠ 1) | Omit `blob_paths` or match counts |
| `files` | No files in multipart body | Use field name `files`; repeat per file |
| `files[i]` | Empty file | Skip zero-byte uploads |
| `document_ids` | Missing on `GET /file/list` | Pass comma-separated ids |
| `identity.profile_url` | Presigned URL sent on write | Send document id from upload response |
| `document_ids[i]` | Unknown registry id on employee save | Upload first, then attach returned id |

Envelope always includes `detail` (human-readable) and optional `field_errors` map — see [API_CONTRACTS.md](API_CONTRACTS.md).

---

## Breaking changes (frontend migration)

| Before | After |
|--------|-------|
| Employee read `document_ids[]` with `id` | `documents[]` with `doc_id` |
| Employee read `profile_url` as string URL | `identity.profile_url` as `DocumentReadDto` object |
| Mixed attachment field names | Write: `document_ids` · Read: `documents` |

Shared MyStoreGuard clients can reuse the same `DocumentReadDto` parser used for product `documents[]`.

---

## Local / live testing

```bash
# Upload → list presigned URL → delete (optional)
./scripts/local-dev/test-live-file.sh

# Broader regression (includes file step when storage credentials available)
./scripts/local-dev/test-live-all.sh
```

Requires `scripts/local-dev/live-session.env` — see [scripts/local-dev/README.md](../scripts/local-dev/README.md).

---

## Related docs

- [API_CONTRACTS.md](API_CONTRACTS.md) — employee body fields (summary)
- [MYSTOREGUARD_API_CONFORMANCE.md](MYSTOREGUARD_API_CONFORMANCE.md) — platform parity matrix
- [SWAGGER.md](SWAGGER.md) — Try it out / OpenAPI maintenance
- [APPCONFIG.md](APPCONFIG.md) — container name and blob path config
