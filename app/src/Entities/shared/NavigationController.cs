using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Shared.Tenant;

namespace ZelosHR.Api.Entities.Shared;

/// <summary>Machine-readable route map for frontends (mirrors Swagger paths).</summary>
[ApiController]
[ApiExplorerSettings(GroupName = SwaggerGroups.Discovery)]
[Route("api/v1/navigation")]
public class NavigationController : ControllerBase
{
    [HttpGet]
    public IActionResult GetNavigationMap() => Ok(new NavigationMapResponse());
}

public sealed class NavigationMapResponse
{
    public IReadOnlyList<NavigationModuleDto> Modules { get; init; } =
    [
        Nav("dashboard", "Dashboard", endpoints:
        [
            "GET /api/v1/dashboard",
            "GET /api/v1/dashboard/summary",
        ]),
        Nav("employees", "Employee", children:
        [
            Nav("directory", "Directory", endpoints:
            [
                "GET /api/v1/employees/directory/summary",
                "GET /api/v1/employees/directory",
                "GET /api/v1/employees/directory/filter-options",
                "GET /api/v1/employees",
                "GET /api/v1/employees/{id}",
                "POST /api/v1/employees",
                "PATCH /api/v1/employees/{id}",
                "PATCH /api/v1/employees/{id}/employment",
                "PATCH /api/v1/employees/{id}/lifecycle-state",
                "DELETE /api/v1/employees/{id}",
            ]),
            Nav("org-chart", "Org Chart", endpoints:
            [
                "GET /api/v1/org-structure/summary",
                "GET /api/v1/org-structure/departments",
                "GET /api/v1/org-structure/branches",
                "GET /api/v1/org-structure/chart",
                "POST /api/v1/org-structure/departments",
                "PATCH /api/v1/org-structure/departments/{id}",
                "DELETE /api/v1/org-structure/departments/{id}",
                "POST /api/v1/org-structure/branches",
                "PATCH /api/v1/org-structure/branches/{id}",
                "DELETE /api/v1/org-structure/branches/{id}",
            ]),
            Nav("lifecycle-events", "Lifecycle Events", endpoints:
            [
                "GET /api/v1/lifecycle-events/summary",
                "GET /api/v1/lifecycle-events",
                "GET /api/v1/lifecycle-events/{id}",
                "POST /api/v1/lifecycle-events",
                "PATCH /api/v1/lifecycle-events/{id}",
                "DELETE /api/v1/lifecycle-events/{id}",
            ]),
            Nav("audit-logs", "Audit Logs", endpoints:
            [
                "GET /api/v1/audit-logs/summary",
                "GET /api/v1/audit-logs",
                "GET /api/v1/audit-logs/{id}",
            ]),
        ]),
        Nav("attendance", "Attendance", endpoints:
        [
            "GET /api/v1/attendance/summary",
            "GET /api/v1/attendance",
            "GET /api/v1/attendance/{id}",
            "POST /api/v1/attendance",
            "PATCH /api/v1/attendance/{id}",
            "DELETE /api/v1/attendance/{id}",
        ]),
        Nav("leave", "Leave", endpoints:
        [
            "GET /api/v1/leave/summary",
            "GET /api/v1/leave",
            "GET /api/v1/leave/requests/{id}",
            "POST /api/v1/leave/requests",
            "PATCH /api/v1/leave/requests/{id}",
            "DELETE /api/v1/leave/requests/{id}",
        ]),
        Nav("recruitment", "Recruitment", endpoints:
        [
            "GET /api/v1/recruitment/summary",
            "GET /api/v1/recruitment",
            "GET /api/v1/recruitment/{id}",
            "POST /api/v1/recruitment",
            "PATCH /api/v1/recruitment/{id}",
            "DELETE /api/v1/recruitment/{id}",
        ]),
        Nav("onboarding", "Onboarding", endpoints:
        [
            "GET /api/v1/onboarding/summary",
            "GET /api/v1/onboarding",
            "GET /api/v1/onboarding/{id}",
            "POST /api/v1/onboarding",
            "PATCH /api/v1/onboarding/{id}",
            "DELETE /api/v1/onboarding/{id}",
        ]),
        Nav("performance", "Performance", endpoints:
        [
            "GET /api/v1/performance/summary",
            "GET /api/v1/performance",
            "GET /api/v1/performance/{id}",
            "POST /api/v1/performance",
            "PATCH /api/v1/performance/{id}",
            "DELETE /api/v1/performance/{id}",
        ]),
        Nav("disciplinary", "Disciplinary", endpoints:
        [
            "GET /api/v1/disciplinary/summary",
            "GET /api/v1/disciplinary",
            "GET /api/v1/disciplinary/{id}",
            "POST /api/v1/disciplinary",
            "PATCH /api/v1/disciplinary/{id}",
            "DELETE /api/v1/disciplinary/{id}",
        ]),
        Nav("documents", "Document", endpoints:
        [
            "GET /api/v1/documents/summary",
            "GET /api/v1/documents",
            "GET /api/v1/documents/{id}",
            "POST /api/v1/documents",
            "PATCH /api/v1/documents/{id}",
            "DELETE /api/v1/documents/{id}",
        ]),
        Nav("platform", "Trovesuite Platform", endpoints:
        [
            "POST /api/v1/platform/auth/verify",
            "POST /api/v1/platform/auth/authorize",
            "GET /api/v1/platform/auth/context",
            "POST /api/v1/platform/notifications/email",
            "POST /api/v1/platform/storage/file-url",
        ]),
    ];

    public TenantHeadersDto TenantHeaders { get; init; } = new();

    private static NavigationModuleDto Nav(
        string id,
        string label,
        IReadOnlyList<NavigationModuleDto>? children = null,
        IReadOnlyList<string>? endpoints = null) =>
        new() { Id = id, Label = label, Children = children, Endpoints = endpoints };
}

public sealed class NavigationModuleDto
{
    public required string Id { get; init; }
    public required string Label { get; init; }
    public IReadOnlyList<NavigationModuleDto>? Children { get; init; }
    public IReadOnlyList<string>? Endpoints { get; init; }
}

/// <summary>Development fallback when JWT is disabled. Production uses Trovesuite JWT claims.</summary>
public sealed class TenantHeadersDto
{
    public string TenantId { get; init; } = TenantContext.DefaultTenantId;
    public string OrgId { get; init; } = TenantContext.DefaultOrgId;
}
