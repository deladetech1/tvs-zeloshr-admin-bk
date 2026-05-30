namespace ZelosHR.Api.Entities.Files;

public sealed class FileUploadMultipleReadDto
{
    public required string Id { get; init; }
}

public sealed class FileResponseReadDto
{
    public required string Id { get; init; }
    public required string PresignedUrl { get; init; }
    public string? Description { get; init; }
    public string? FileName { get; init; }
}

public sealed class FileDeleteReadDto
{
    public required string BlobPath { get; init; }
    public required string ContainerName { get; init; }
    public required string Message { get; init; }
}
