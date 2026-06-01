using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using ZelosHR.Api.Configs;

namespace ZelosHR.Api.Configs;

/// <summary>Tag-level and operation-level documentation for the File Management Swagger group.</summary>
public sealed class SwaggerFileManagementTagDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var tag = swaggerDoc.Tags.FirstOrDefault(t =>
            string.Equals(t.Name, SwaggerGroups.FileManagement, StringComparison.Ordinal));
        if (tag is null)
        {
            swaggerDoc.Tags.Add(new OpenApiTag
            {
                Name = SwaggerGroups.FileManagement,
                Description = FileManagementTagDescription,
            });
            return;
        }

        tag.Description = FileManagementTagDescription;
    }

    internal const string FileManagementTagDescription = """
        Mystoreguard-aligned file registry backed by **Azure Blob Storage** (via Trovesuite `IStorageService`).

        ### Client responsibilities (API)
        | You send | Purpose |
        |----------|---------|
        | `blob_paths` / `blob_path` | Logical path **inside** the container (you choose the folder/filename) |
        | `files` (multipart) | File bytes |
        | `document_ids` / `document_id` | Registry IDs returned from upload — **not** blob paths |

        ### Server responsibilities (not in Swagger — ops/deployment)
        | Config | Purpose |
        |--------|---------|
        | `Trovesuite:AzureStorage:AccountName` | Storage account URL (managed identity in production) |
        | `Trovesuite:AzureStorage:ConnectionString` | Optional connection string (local dev) |
        | `AzureStorage:DocumentsContainer` | Blob container name (default `employee-documents`) |

        Azure account + container are **never** sent by the frontend. The API builds `StorageAccountUrl` + `ContainerName` server-side.

        ### End-to-end workflow
        1. **Upload** — `POST /api/v1/file/post/multiple?blob_paths={tenant}/{org}/{bus}/employees/file.pdf` + multipart `files`
        2. **Registry** — Response `{ data: [{ id: "doc_…" }] }` stored in `human_resource.hr_document_paths`
        3. **Attach** — Pass IDs on `POST /employees/add` or `PUT /employees/update` → `document_ids: ["doc_…"]`
        4. **Download** — `GET /api/v1/file/list?document_ids=doc_a,doc_b` → `presigned_url` (24h expiry)
        5. **Replace** — `PUT /api/v1/file/put?document_id=…` + new multipart `file`
        6. **Remove** — `DELETE /api/v1/file/delete?document_id=…` (soft-deletes registry row + removes blob)

        ### `blob_paths` rules
        - **Required** on upload. Pattern: `{tenant_id}/{org_id}/{bus_id}/employees/{filename}`
        - Comma-separated: one path for **all** files, **or** one path per file (count must match file count)
        - Optional `descriptions` query — comma-separated, can be shorter than file count

        ### Mystoreguard parity
        Same four routes: `post/multiple`, `put`, `delete`, `list`. Entities reference `document_ids` (strings), not blob paths.
        """;
}

/// <summary>Response examples and extra operation descriptions for file routes.</summary>
public sealed class SwaggerFileManagementOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (!string.Equals(context.ApiDescription.GroupName, SwaggerGroups.FileManagement, StringComparison.Ordinal))
            return;

        var method = context.ApiDescription.HttpMethod ?? "";
        var path = context.ApiDescription.RelativePath ?? "";

        if (method.Equals("POST", StringComparison.OrdinalIgnoreCase)
            && path.EndsWith("file/post/multiple", StringComparison.OrdinalIgnoreCase))
        {
            operation.Summary = "Upload multiple files";
            operation.Description = """
                Upload one or more files to Azure Blob Storage and register each in `hr_document_paths`.

                **Query (required):** `blob_paths` — see File Management tag description for path pattern.

                **Query (optional):** `descriptions` — comma-separated labels stored on the registry row.

                **Body:** `multipart/form-data` with field `files` (one or more files, max 50MB total request).

                **Returns:** `{ data: [{ id: "…" }, …] }` — use these string IDs in employee `document_ids`.

                **Does not return** blob URL on upload — call `GET /file/list` for presigned URLs.
                """;
            SetJsonResponseExample(operation, SwaggerExamples.FileUploadMultipleResponse(), "upload_multiple");
            return;
        }

        if (method.Equals("PUT", StringComparison.OrdinalIgnoreCase)
            && path.EndsWith("file/put", StringComparison.OrdinalIgnoreCase))
        {
            operation.Summary = "Replace file content";
            operation.Description = """
                Replace blob bytes for an existing registry row. Updates `hr_document_paths` metadata.

                **Query (required):** `document_id` — registry ID from upload (not the blob path).

                **Query (optional):** `blob_path` — new storage path if moving the file; omit to overwrite same path.

                **Query (optional):** `description` — updated label.

                **Body:** `multipart/form-data` with single `file` (max 10MB).

                **Returns:** `{ data: { id, presigned_url, description, file_name } }`.
                """;
            SetJsonResponseExample(operation, SwaggerExamples.FileUpdateResponse(), "update_file");
            return;
        }

        if (method.Equals("DELETE", StringComparison.OrdinalIgnoreCase)
            && path.EndsWith("file/delete", StringComparison.OrdinalIgnoreCase))
        {
            operation.Summary = "Delete file";
            operation.Description = """
                Deletes the blob from Azure Storage and marks the registry row deleted (`delete_status = DELETED`).

                **Query (required):** `document_id` — registry ID from upload.

                **Returns:** `{ data: { blob_path, container_name, message } }` — echoes where the file lived.
                Remove the ID from employee `document_ids` separately via `PUT /employees/update` → `delete_document_ids`.
                """;
            SetJsonResponseExample(operation, SwaggerExamples.FileDeleteResponse(), "delete_file");
            return;
        }

        if (method.Equals("GET", StringComparison.OrdinalIgnoreCase)
            && path.EndsWith("file/list", StringComparison.OrdinalIgnoreCase))
        {
            operation.Summary = "List documents with presigned URLs";
            operation.Description = """
                Resolve download URLs for one or more registry IDs.

                **Query (required):** `document_ids` — comma-separated IDs (from upload or employee `document_ids`).

                **Returns:** `{ data: [{ id, presigned_url, description, file_name }, …] }`.

                **Presigned URL expiry:** 24 hours (Mystoreguard-aligned). Re-call this endpoint when URLs expire.

                IDs not found or inactive are omitted from the array (no error per missing ID).
                """;
            SetJsonResponseExample(operation, SwaggerExamples.FileListResponse(), "list_documents");
        }
    }

    private static void SetJsonResponseExample(OpenApiOperation operation, JsonObject example, string exampleKey)
    {
        if (!operation.Responses.TryGetValue("200", out var response) || response.Content is null)
            return;

        if (!response.Content.TryGetValue("application/json", out var media))
            return;

        SwaggerMediaExamples.SetNamedExamples(media, new Dictionary<string, IOpenApiExample>
        {
            [exampleKey] = new OpenApiExample
            {
                Summary = "Success envelope",
                Value = example,
            },
        });
    }
}
