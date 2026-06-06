namespace ZelosHR.Api.Entities.Files;

/// <summary>
/// Standard blob paths inside the <c>zeloshr</c> container ({tenant}/{org}/{bus}/employees/…).
/// </summary>
internal static class EmployeeBlobPathBuilder
{
    internal static string BuildDocumentPath(
        string tenantId,
        string orgId,
        string busId,
        string fileName)
    {
        var safe = SanitizeFileName(fileName);
        var unique = Guid.NewGuid().ToString("N")[..8];
        return $"{NormalizeSegment(tenantId)}/{NormalizeSegment(orgId)}/{NormalizeSegment(busId)}/employees/documents/{unique}-{safe}";
    }

    internal static string BuildWizardDocumentPath(
        string tenantId,
        string orgId,
        string busId,
        Guid employeeId,
        string fileName)
    {
        var safe = SanitizeFileName(fileName);
        var unique = Guid.NewGuid().ToString("N")[..8];
        return $"{NormalizeSegment(tenantId)}/{NormalizeSegment(orgId)}/{NormalizeSegment(busId)}/employees/documents/wizard/{employeeId}/{unique}-{safe}";
    }

    internal static string BuildProfilePath(
        string tenantId,
        string orgId,
        string busId,
        Guid employeeId,
        string extension)
    {
        var ext = string.IsNullOrWhiteSpace(extension) ? "jpg" : extension.Trim().TrimStart('.');
        return $"{NormalizeSegment(tenantId)}/{NormalizeSegment(orgId)}/{NormalizeSegment(busId)}/employees/profile/{employeeId}.{ext}";
    }

    private static string NormalizeSegment(string value) =>
        string.IsNullOrWhiteSpace(value) ? "unknown" : value.Trim().Trim('/');

    private static string SanitizeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName.Trim());
        if (string.IsNullOrWhiteSpace(name))
            return "file";

        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(name.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
        return string.IsNullOrWhiteSpace(cleaned) ? "file" : cleaned;
    }
}
