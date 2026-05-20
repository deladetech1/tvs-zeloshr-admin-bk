using ZelosHR.Api.Entities.Employees;

namespace ZelosHR.Api.Tests.Employees;

public class EmployeeDirectoryQueryBuilderTests
{
    private const string Table = "zeloshr.zhr_employees";

    [Fact]
    public void Build_includes_tenant_and_org_scope()
    {
        var (where, parameters) = EmployeeDirectoryQueryBuilder.Build(
            new EmployeeDirectoryQuery(),
            Table,
            "demo-tenant",
            "demo-org");

        Assert.Contains("e.tenant_id = @TenantId", where);
        Assert.Contains("e.org_id = @OrgId", where);
        Assert.Equal("demo-tenant", parameters["TenantId"]);
        Assert.Equal("demo-org", parameters["OrgId"]);
    }

    [Fact]
    public void Build_excludes_terminated_by_default()
    {
        var (where, _) = EmployeeDirectoryQueryBuilder.Build(
            new EmployeeDirectoryQuery(),
            Table,
            "demo-tenant",
            "demo-org");

        Assert.Contains("employment_status NOT IN ('Terminated', 'Resigned')", where);
    }

    [Fact]
    public void Build_includes_search_when_at_least_three_characters()
    {
        var (where, parameters) = EmployeeDirectoryQueryBuilder.Build(
            new EmployeeDirectoryQuery { Search = "Ama" },
            Table,
            "demo-tenant",
            "demo-org");

        Assert.Contains("ILIKE @Search", where);
        Assert.Equal("%Ama%", parameters["Search"]);
    }

    [Fact]
    public void Build_ignores_search_shorter_than_three_characters()
    {
        var (where, parameters) = EmployeeDirectoryQueryBuilder.Build(
            new EmployeeDirectoryQuery { Search = "Am" },
            Table,
            "demo-tenant",
            "demo-org");

        Assert.DoesNotContain("@Search", where);
        Assert.False(parameters.ContainsKey("Search"));
    }

    [Fact]
    public void Build_applies_status_filter_and_skips_default_exclusion()
    {
        var (where, parameters) = EmployeeDirectoryQueryBuilder.Build(
            new EmployeeDirectoryQuery { Status = "Terminated" },
            Table,
            "demo-tenant",
            "demo-org");

        Assert.Contains("e.employment_status = @EmploymentStatus", where);
        Assert.Equal("Terminated", parameters["EmploymentStatus"]);
        Assert.DoesNotContain("NOT IN ('Terminated'", where);
    }

    [Theory]
    [InlineData("name", "asc", "e.last_name, e.first_name")]
    [InlineData("employeeCode", "desc", "e.employee_code")]
    [InlineData("department", "asc", "d.name")]
    public void BuildOrderBy_maps_frontend_sort_fields(string sortBy, string sortOrder, string expectedColumn)
    {
        var orderBy = EmployeeDirectoryQueryBuilder.BuildOrderBy(sortBy, sortOrder);
        Assert.Contains(expectedColumn, orderBy);
        Assert.Contains(sortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC", orderBy);
    }
}
