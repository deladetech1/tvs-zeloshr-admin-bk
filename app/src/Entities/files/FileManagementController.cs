using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Authorization;
using ZelosHR.Api.Shared.Constants;

namespace ZelosHR.Api.Entities.Files;

/// <summary>Azure file storage — same four routes as Mystoreguard.</summary>
[ApiController]
[ApiExplorerSettings(GroupName = SwaggerGroups.FileManagement)]
[Route("api/v1/file")]
[Produces("application/json")]
public class FileManagementController : ControllerBase
{
    private readonly FileManagementService _files;

    public FileManagementController(FileManagementService files) => _files = files;

    /// <summary>Upload multiple files to Azure Storage and store document information.</summary>
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeUpdate)]
    [HttpPost("post/multiple")]
    [RequestSizeLimit(50 * 1024 * 1024)]
    [ProducesResponseType(typeof(Respons<IReadOnlyList<FileUploadMultipleReadDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<IReadOnlyList<FileUploadMultipleReadDto>>>> UploadMultiple(
        [FromQuery] string blob_paths,
        [FromQuery] string? descriptions,
        [FromForm(Name = "files")] IReadOnlyList<IFormFile> files,
        CancellationToken ct)
    {
        var result = await _files.UploadMultipleAsync(files, blob_paths, descriptions, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Update file in Azure Storage and update description/path in database.</summary>
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeUpdate)]
    [HttpPut("put")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [ProducesResponseType(typeof(Respons<FileResponseReadDto>), StatusCodes.Status200OK)]
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

    /// <summary>Delete file from Azure Storage and database.</summary>
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeUpdate)]
    [HttpDelete("delete")]
    [ProducesResponseType(typeof(Respons<FileDeleteReadDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<FileDeleteReadDto>>> DeleteFile(
        [FromQuery(Name = PlatformQueryParams.DocumentId)] string documentId,
        CancellationToken ct)
    {
        var result = await _files.DeleteFileAsync(documentId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Get documents by IDs with presigned URLs (24h expiry).</summary>
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeGet)]
    [HttpGet("list")]
    [ProducesResponseType(typeof(Respons<IReadOnlyList<FileResponseReadDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<IReadOnlyList<FileResponseReadDto>>>> ListDocuments(
        [FromQuery] string document_ids,
        CancellationToken ct)
    {
        var result = await _files.ListDocumentsAsync(document_ids, ct);
        return StatusCode(result.StatusCode, result);
    }
}
