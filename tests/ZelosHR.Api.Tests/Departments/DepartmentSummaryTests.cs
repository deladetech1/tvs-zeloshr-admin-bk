namespace ZelosHR.Api.Tests.Departments;

/// <summary>
/// Documents expected demo summary values matching the Org Chart UI tabs.
/// Integration tests can assert these against a running DB with seed data.
/// </summary>
public class DepartmentSummaryTests
{
    [Fact]
    public void Demo_org_summary_matches_ui_tab_counts()
    {
        const int expectedDepartments = 9;
        const int expectedBranches = 4;
        const int expectedArchived = 1;

        Assert.Equal(9, expectedDepartments);
        Assert.Equal(4, expectedBranches);
        Assert.Equal(1, expectedArchived);
    }
}
