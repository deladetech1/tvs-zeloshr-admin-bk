using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Tests.Employees;

public class EmployeeCsvExportTests
{
    [Fact]
    public void Escape_quotes_commas_and_newlines()
    {
        Assert.Equal("plain", EmployeeCsvExport.Escape("plain"));
        Assert.Equal("\"a,b\"", EmployeeCsvExport.Escape("a,b"));
        Assert.Equal("\"say \"\"hi\"\"\"", EmployeeCsvExport.Escape("say \"hi\""));
    }

    [Fact]
    public void Build_includes_header_and_row_values()
    {
        var deptId = Guid.Parse("d1111111-1111-1111-1111-111111111101");
        var branchId = Guid.Parse("b1111111-1111-1111-1111-111111111101");
        var employeeId = Guid.Parse("e1111111-1111-1111-1111-111111111101");

        var rows = new[]
        {
            new EmployeeEntity
            {
                Id = employeeId,
                EmployeeCode = "EMP-001",
                FullName = "Ada Lovelace",
                FirstName = "Ada",
                LastName = "Lovelace",
                WorkEmail = "ada@company.com",
                PersonalEmail = "ada.personal@example.com",
                Phone = "+233201234567",
                Nationality = "Ghana",
                JobTitle = "Engineer",
                DepartmentId = deptId,
                Department = new DepartmentEntity { Id = deptId, Name = "Engineering" },
                BranchId = branchId,
                Branch = new BranchEntity { Id = branchId, Name = "Accra HQ" },
                EmploymentType = "Full-time",
                EmploymentStatus = EmploymentStatusValues.Active,
                StartDate = new DateOnly(2025, 6, 1),
                WorkLocation = "Accra",
                TenantId = "t1",
                OrgId = "o1",
                LifecycleState = EmployeeLifecycleStates.Active,
                LifecycleStatus = "active",
            },
        };

        var csv = System.Text.Encoding.UTF8.GetString(EmployeeCsvExport.Build(rows, new Dictionary<string, CpUserDto>()));
        var lines = csv.TrimEnd().Split('\n');

        Assert.Equal(string.Join(",", EmployeeCsvExport.Headers), lines[0]);
        Assert.Contains("Ada Lovelace", lines[1]);
        Assert.Contains("2025-06-01", lines[1]);
        Assert.Contains("Engineering", lines[1]);
    }
}
