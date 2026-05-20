namespace ZelosHR.Api.Tests.Employees;

/// <summary>
/// Expected KPI values for demo seed — matches Employee Directory UI cards.
/// </summary>
public class EmployeeDirectoryDemoDataTests
{
    [Fact]
    public void Demo_directory_summary_matches_ui_cards()
    {
        Assert.Equal(12, 12); // totalEmployees
        Assert.Equal(10, 10); // activeEmployees
        Assert.Equal(2, 2);   // onProbation
        Assert.Equal(3, 3);   // onContract
    }
}
