using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Authorization;
using ZelosHR.Api.Shared.Constants;

namespace ZelosHR.Api.Entities.Files;

/// <summary>
/// File upload registry (Mystoreguard-aligned). Azure Blob + <c>hr_document_paths</c>.
/// See **File Management** tag in Swagger for full workflow, blob path rules, and server config notes.
/// </summary>
[ApiController]
[ApiExplorerSettings(GroupName = SwaggerGroups.FileManagement)]
[Route("api/v1/file")]
[Produces("application/json")]
public class FileManagementController : ControllerBase
{
    private readonly FileManagementService _files;

    public FileManagementController(FileManagementService files) => _files = files;

    /// <summary>Upload multiple files to Azure Storage and register document IDs.</summary>
    /// <remarks>
    /// Mystoreguard route: <c>POST /api/v1/file/post/multiple</c>.
    /// See operation description and **Examples** on the 200 response for the success envelope.
    /// </remarks>
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeUpdate)]
    [HttpPost("post/multiple")]
    [RequestSizeLimit(50 * 1024 * 1024)]
    [ProducesResponseType(typeof(Respons<IReadOnlyList<FileUploadMultipleReadDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<IReadOnlyList<FileUploadMultipleReadDto>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Respons<IReadOnlyList<FileUploadMultipleReadDto>>>> UploadMultiple(
        [FromQuery] string blob_paths,
        [FromQuery] string? descriptions,
        [FromForm(Name = "files")] IReadOnlyList<IFormFile> files,
        CancellationToken ct)
    {
        var result = await _files.UploadMultipleAsync(files, blob_paths, descriptions, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Replace an uploaded file (same registry ID).</summary>
    /// <remarks>
    /// Mystoreguard route: <c>PUT /api/v1/file/put?document_id=…</c>.
    /// Optional <c>blob_path</c> moves the blob to a new path inside the container.
    /// </remarks>
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeUpdate)]
    [HttpPut("put")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [ProducesResponseType(typeof(Respons<FileResponseReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<FileResponseReadDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Respons<FileResponseReadDto>>> UpdateFile(
        [FromQuery(Name = PlatformQueryParams.DocumentId)] string documentId,
        [FromQuery] string? blob_path,
        [FromQuery] string? description,
        IFormFile file,
        CancellationToken ct)
    {
        var result = await _files.UpdateFileAsync(documentId, file, blob_path, description, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Delete blob and soft-delete registry row.</summary>
    /// <remarks>
    /// Mystoreguard route: <c>DELETE /api/v1/file/delete?document_id=…</c>.
    /// Response includes <c>blob_path</c> and <c>container_name</c> for audit.
    /// Detach from employees via <c>PUT /employees/update</c> → <c>delete_document_ids</c>.
    /// </remarks>
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeUpdate)]
    [HttpDelete("delete")]
    [ProducesResponseType(typeof(Respons<FileDeleteReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<FileDeleteReadDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Respons<FileDeleteReadDto>>> DeleteFile(
        [FromQuery(Name = PlatformQueryParams.DocumentId)] string documentId,
        CancellationToken ct)
    {
        var result = await _files.DeleteFileAsync(documentId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Get presigned download URLs for document IDs (24h expiry).</summary>
    /// <remarks>
    /// Mystoreguard route: <c>GET /api/v1/file/list?document_ids=…</c>.
    /// Comma-separated IDs from upload or employee <c>document_ids</c> array.
    /// </remarks>
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeGet)]
    [HttpGet("list")]
    [ProducesResponseType(typeof(Respons<IReadOnlyList<FileResponseReadDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<IReadOnlyList<FileResponseReadDto>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Respons<IReadOnlyList<FileResponseReadDto>>>> ListDocuments(
        [FromQuery] string document_ids,
        CancellationToken ct)
    {
        var result = await _files.ListDocumentsAsync(document_ids, ct);
        return StatusCode(result.StatusCode, result);
    }
}
