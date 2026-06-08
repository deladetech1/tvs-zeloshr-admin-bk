using System.Text.Json.Serialization;
using ZelosHR.Api.Entities.Departments;

namespace ZelosHR.Api.Entities.OrgStructure;

/// <summary>Normalizes org-structure write payloads (flat ids and nested list-item shapes).</summary>
internal static class OrgStructureRequestExtensions
{
    internal static Guid? ResolveHeadOfDepartmentId(this CreateDepartmentRequestDto request) =>
        CoalesceHeadId(request.HeadOfDepartmentId, request.HeadOfDepartment);

    internal static Guid? ResolveHeadOfDepartmentId(this UpdateDepartmentRequestDto request) =>
        CoalesceHeadId(request.HeadOfDepartmentId, request.HeadOfDepartment);

    internal static bool HasHeadOfDepartmentChange(this UpdateDepartmentRequestDto request) =>
        request.HeadOfDepartmentId.HasValue
        || !string.IsNullOrWhiteSpace(request.HeadOfDepartment?.EmployeeId);

    private static Guid? CoalesceHeadId(Guid? headOfDepartmentId, DepartmentHeadReferenceDto? headOfDepartment) =>
        headOfDepartmentId ?? ParseEmployeeId(headOfDepartment?.EmployeeId);

    private static Guid? ParseEmployeeId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return Guid.TryParse(value.Trim(), out var id) ? id : null;
    }
}

/// <summary>Nested head reference from list round-trip (<c>head_of_department.employee_id</c>).</summary>
public sealed class DepartmentHeadReferenceDto
{
    [JsonPropertyName("employee_id")]
    public string? EmployeeId { get; init; }
}
