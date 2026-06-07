using ZelosHR.Api.Entities.Files;

namespace ZelosHR.Api.Entities.OrgStructure;

internal static class OrgChartBuilder
{
    internal static IReadOnlyList<OrgChartNodeDto> Build(
        IReadOnlyList<OrgChartEmployeeRow> employees,
        IReadOnlyDictionary<Guid, OrgChartDepartmentHeadRow> departmentByHeadId,
        IReadOnlyDictionary<Guid, DocumentReadDto?> profileUrlsByEmployeeId)
    {
        if (employees.Count == 0)
            return [];

        var nodes = employees.ToDictionary(
            e => e.Id,
            e => new MutableNode
            {
                Id = e.Id.ToString(),
                FullName = ResolveFullName(e),
                JobTitle = e.JobTitle,
                ProfileUrl = profileUrlsByEmployeeId.GetValueOrDefault(e.Id),
                ReportsToId = e.ReportsToId?.ToString(),
                Department = departmentByHeadId.TryGetValue(e.Id, out var dept)
                    ? new OrgChartDepartmentBadgeDto
                    {
                        DepartmentId = dept.DepartmentId.ToString(),
                        Name = dept.Name,
                        EmployeeCount = dept.EmployeeCount,
                        HeadcountCapacity = dept.HeadcountCapacity,
                    }
                    : null,
            });

        var roots = new List<MutableNode>();
        foreach (var employee in employees)
        {
            var node = nodes[employee.Id];
            if (employee.ReportsToId is { } managerId
                && nodes.TryGetValue(managerId, out var parent)
                && managerId != employee.Id)
            {
                parent.Children.Add(node);
            }
            else
            {
                roots.Add(node);
            }
        }

        return roots
            .Select(r => r.ToDto())
            .OrderBy(r => r.FullName)
            .ToList();
    }

    private static string ResolveFullName(OrgChartEmployeeRow row)
    {
        if (!string.IsNullOrWhiteSpace(row.FullName))
            return row.FullName.Trim();

        return ZelosHR.Api.Shared.Formatting.NameFormatting.BuildFullName(
            row.FirstName ?? string.Empty,
            null,
            row.LastName ?? string.Empty);
    }

    private sealed class MutableNode
    {
        public required string Id { get; init; }
        public required string FullName { get; init; }
        public string? JobTitle { get; init; }
        public DocumentReadDto? ProfileUrl { get; init; }
        public string? ReportsToId { get; init; }
        public OrgChartDepartmentBadgeDto? Department { get; init; }
        public List<MutableNode> Children { get; } = [];

        public OrgChartNodeDto ToDto() => new()
        {
            Id = Id,
            FullName = FullName,
            JobTitle = JobTitle,
            ProfileUrl = ProfileUrl,
            NodeType = "employee",
            ParentId = ReportsToId,
            Department = Department,
            Children = Children
                .Select(c => c.ToDto())
                .OrderBy(c => c.FullName)
                .ToList(),
        };
    }
}
