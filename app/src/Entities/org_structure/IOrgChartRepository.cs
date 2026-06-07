namespace ZelosHR.Api.Entities.OrgStructure;

public interface IOrgChartRepository
{
    Task<(IReadOnlyList<OrgChartEmployeeRow> Employees, IReadOnlyList<OrgChartDepartmentHeadRow> DepartmentHeads)>
        GetReportingHierarchyScopedAsync(string tenantId, string orgId, CancellationToken ct = default);
}
