namespace ZelosHR.Api.Entities.Employees;

/// <summary>Allowed values for change-request query params and Swagger examples.</summary>
public static class ChangeRequestFieldOptions
{
    public static readonly IReadOnlyList<string> Statuses =
    [
        ChangeRequestStatuses.Pending,
        ChangeRequestStatuses.Approved,
        ChangeRequestStatuses.Rejected,
        ChangeRequestStatuses.Superseded,
    ];
}
