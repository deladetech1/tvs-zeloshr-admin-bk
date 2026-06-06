using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Authorization;
using ZelosHR.Api.Shared.Constants;

namespace ZelosHR.Api.Entities.Files;

/// <summary>File upload registry for employee documents.</summary>
[ApiController]
[ApiExplorerSettings(GroupName = SwaggerGroups.FileManagement)]
[Route("api/v1/file")]
[Produces("application/json")]
public class FileManagementController : ControllerBase
{
    private readonly FileManagementService _files;

    public FileManagementController(FileManagementService files) => _files = files;

    /// <summary>Upload multiple files to Azure Storage and register document IDs.</summary>
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeUpdate)]
    [HttpPost("post/multiple")]
    [RequestSizeLimit(50 * 1024 * 1024)]
    [ProducesResponseType(typeof(Respons<IReadOnlyList<FileUploadMultipleReadDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<IReadOnlyList<FileUploadMultipleReadDto>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Respons<IReadOnlyList<FileUploadMultipleReadDto>>>> UploadMultiple(
        [FromQuery] string? blob_paths,
        [FromQuery] string? descriptions,
        [FromForm(Name = "files")] IReadOnlyList<IFormFile>? files,
        CancellationToken ct)
    {
        var uploadFiles = ResolveUploadFiles(files, Request);
        var result = await _files.UploadMultipleAsync(uploadFiles, blob_paths ?? string.Empty, descriptions, ct);
        return StatusCode(result.StatusCode, result);
    }

    internal static IReadOnlyList<IFormFile> ResolveUploadFiles(IReadOnlyList<IFormFile>? files, HttpRequest request)
    {
        if (files is { Count: > 0 })
            return files;

        var bound = request.Form.Files.GetFiles("files");
        if (bound.Count > 0)
            return bound;

        bound = request.Form.Files.GetFiles("files[]");
        if (bound.Count > 0)
            return bound;

        return request.Form.Files.Count > 0
            ? request.Form.Files.ToList()
            : [];
    }

    /// <summary>Replace an uploaded file (same registry ID).</summary>
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
