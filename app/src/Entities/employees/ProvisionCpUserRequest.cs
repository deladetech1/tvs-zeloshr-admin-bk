namespace ZelosHR.Api.Entities.Employees;

/// <summary>Identity stored in core_platform.cp_users — not duplicated on zhr_employees after sync.</summary>
public sealed record ProvisionCpUserRequest(
    string TenantId,
    string OrgId,
    string BusId,
    string LocId,
    string FullName,
    string Email,
    string Contact,
    string? Gender,
    string? Dob,
    string? Address,
    string? ProfilePic,
    string? CreatedBy);
