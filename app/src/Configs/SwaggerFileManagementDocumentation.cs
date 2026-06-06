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

    internal const string FileManagementTagDescription =
        "Upload and register employee documents. Use returned `document_ids` on employee create/update — not blob paths.";
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
            operation.Description =
                "Multipart `files` + optional `blob_paths` / `descriptions`. Omit `blob_paths` to store under "
                + "`{tenant}/{org}/{bus}/employees/documents/` in the **zeloshr** container. Returns registry `id` values for employee `document_ids`.";
            SetJsonResponseExample(operation, SwaggerExamples.FileUploadMultipleResponse(), "upload_multiple");
            return;
        }

        if (method.Equals("PUT", StringComparison.OrdinalIgnoreCase)
            && path.EndsWith("file/put", StringComparison.OrdinalIgnoreCase))
        {
            operation.Summary = "Replace file content";
            operation.Description =
                "Required query `document_id` + multipart `file`. Optional `blob_path` and `description`.";
            SetJsonResponseExample(operation, SwaggerExamples.FileUpdateResponse(), "update_file");
            return;
        }

        if (method.Equals("DELETE", StringComparison.OrdinalIgnoreCase)
            && path.EndsWith("file/delete", StringComparison.OrdinalIgnoreCase))
        {
            operation.Summary = "Delete file";
            operation.Description =
                "Required query `document_id`. Remove the ID from the employee via `PUT /employees/update` → `delete_document_ids`.";
            SetJsonResponseExample(operation, SwaggerExamples.FileDeleteResponse(), "delete_file");
            return;
        }

        if (method.Equals("GET", StringComparison.OrdinalIgnoreCase)
            && path.EndsWith("file/list", StringComparison.OrdinalIgnoreCase))
        {
            operation.Summary = "List documents with presigned URLs";
            operation.Description =
                "Required query `document_ids` (comma-separated). Returns `id`, `presigned_url`, `description`, and `file_name` per ID (24h expiry). Employee GET also embeds `documents[]` without `file_name`.";
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
