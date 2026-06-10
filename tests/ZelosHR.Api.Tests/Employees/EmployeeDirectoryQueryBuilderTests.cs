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
            TestDefaults.TenantId,
            TestDefaults.OrgId);

        Assert.Contains("e.tenant_id = @TenantId", where);
        Assert.Contains("e.org_id = @OrgId", where);
        Assert.Equal(TestDefaults.TenantId, parameters["TenantId"]);
        Assert.Equal(TestDefaults.OrgId, parameters["OrgId"]);
    }

    [Fact]
    public void Build_excludes_terminated_by_default()
    {
        var (where, _) = EmployeeDirectoryQueryBuilder.Build(
            new EmployeeDirectoryQuery(),
            Table,
            TestDefaults.TenantId,
            TestDefaults.OrgId);

        Assert.Contains("employment_status NOT IN ('Terminated', 'Resigned')", where);
    }

    [Fact]
    public void Build_includes_search_when_at_least_three_characters()
    {
        var (where, parameters) = EmployeeDirectoryQueryBuilder.Build(
            new EmployeeDirectoryQuery { Search = "Ama" },
            Table,
            TestDefaults.TenantId,
            TestDefaults.OrgId);

        Assert.Contains("ILIKE @Search", where);
        Assert.Contains("cu.fullname ILIKE @Search", where);
        Assert.Equal("%Ama%", parameters["Search"]);
    }

    [Theory]
    [InlineData("larry", true)]
    [InlineData("am", false)]
    [InlineData(null, false)]
    [InlineData("  bob  ", true)]
    public void HasActiveSearch_requires_three_or_more_non_whitespace_characters(string? search, bool expected)
    {
        Assert.Equal(expected, EmployeeDirectoryQueryBuilder.HasActiveSearch(
            new EmployeeDirectoryQuery { Search = search }));
    }

    [Fact]
    public void Build_ignores_search_shorter_than_three_characters()
    {
        var (where, parameters) = EmployeeDirectoryQueryBuilder.Build(
            new EmployeeDirectoryQuery { Search = "Am" },
            Table,
            TestDefaults.TenantId,
            TestDefaults.OrgId);

        Assert.DoesNotContain("@Search", where);
        Assert.False(parameters.ContainsKey("Search"));
    }

    [Fact]
    public void Build_applies_status_filter_and_skips_default_exclusion()
    {
        var (where, parameters) = EmployeeDirectoryQueryBuilder.Build(
            new EmployeeDirectoryQuery { Status = "Terminated" },
            Table,
            TestDefaults.TenantId,
            TestDefaults.OrgId);

        Assert.Contains("e.employment_status = @EmploymentStatus", where);
        Assert.Equal("Terminated", parameters["EmploymentStatus"]);
        Assert.DoesNotContain("NOT IN ('Terminated'", where);
    }

    [Fact]
    public void Build_applies_status_filter_command()
    {
        var (where, parameters) = EmployeeDirectoryQueryBuilder.Build(
            new EmployeeDirectoryQuery { StatusFilter = "probation" },
            Table,
            TestDefaults.TenantId,
            TestDefaults.OrgId);

        Assert.Contains("e.employment_status = 'Probation'", where);
        Assert.True(parameters.ContainsKey("StatusFilterToday"));
    }

    [Fact]
    public void Build_applies_comma_separated_status_filter_commands()
    {
        var (where, parameters) = EmployeeDirectoryQueryBuilder.Build(
            new EmployeeDirectoryQuery { StatusFilter = "active,probation,on_leave,pre_hire" },
            Table,
            TestDefaults.TenantId,
            TestDefaults.OrgId);

        Assert.Contains("e.lifecycle_state IN ('Active', 'On Leave')", where);
        Assert.Contains("e.lifecycle_state = 'Pre-hire'", where);
        Assert.DoesNotContain("NOT IN ('Terminated'", where);
        Assert.True(parameters.ContainsKey("StatusFilterToday"));
    }

    [Fact]
    public void Build_applies_other_comma_separated_status_filter_commands()
    {
        var (where, _) = EmployeeDirectoryQueryBuilder.Build(
            new EmployeeDirectoryQuery { StatusFilter = "terminated,resigned" },
            Table,
            TestDefaults.TenantId,
            TestDefaults.OrgId);

        Assert.Contains("e.lifecycle_state = 'Terminated'", where);
        Assert.Contains("e.lifecycle_state = 'Resigned'", where);
    }

    [Fact]
    public void Build_applies_engagement_and_work_state_filters()
    {
        var (where, parameters) = EmployeeDirectoryQueryBuilder.Build(
            new EmployeeDirectoryQuery
            {
                Engagement = "active",
                WorkStates = ["probation"],
            },
            Table,
            TestDefaults.TenantId,
            TestDefaults.OrgId);

        Assert.Contains("e.lifecycle_state IN ('Active', 'On Leave')", where);
        Assert.Contains("e.employment_status = 'Probation'", where);
        Assert.True(parameters.ContainsKey("StatusFilterToday"));
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
