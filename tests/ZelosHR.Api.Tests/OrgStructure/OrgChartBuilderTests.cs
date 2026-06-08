using FluentAssertions;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.OrgStructure;

namespace ZelosHR.Api.Tests.OrgStructure;

public class OrgChartBuilderTests
{
    [Fact]
    public void Build_returns_three_level_reporting_tree_with_department_badges()
    {
        var ceoId = Guid.Parse("11111111-1111-1111-1111-111111111101");
        var engHeadId = Guid.Parse("11111111-1111-1111-1111-111111111102");
        var icId = Guid.Parse("11111111-1111-1111-1111-111111111105");
        var engDeptId = Guid.Parse("823eb77c-11b7-452b-9c18-6f547a0cd003");

        var employees = new List<OrgChartEmployeeRow>
        {
            new(ceoId, "Kwame Asante", "Kwame", null, "Asante", "Chief Executive Officer", null, null, null),
            new(engHeadId, "Kwame Boateng", "Kwame", null, "Boateng", "Chief Technology Officer", ceoId, null, null),
            new(icId, "Kofi Asante", "Kofi", null, "Asante", "Software Engineer", engHeadId, null, null),
        };

        var departmentByHead = new Dictionary<Guid, OrgChartDepartmentHeadRow>
        {
            [engHeadId] = new(engDeptId, "Engineering", engHeadId, 8, 10),
        };

        var roots = OrgChartBuilder.Build(
            employees,
            departmentByHead,
            new Dictionary<Guid, ZelosHR.Api.Entities.Files.DocumentReadDto?>(),
            new Dictionary<string, CpUserDto>());

        roots.Should().ContainSingle();
        var ceo = roots[0];
        ceo.FullName.Should().Be("Kwame Asante");
        ceo.NodeType.Should().Be("employee");
        ceo.ProfileUrl.Should().BeNull();
        ceo.Department.Should().BeNull();
        ceo.Children.Should().ContainSingle();

        var engHead = ceo.Children[0];
        engHead.FullName.Should().Be("Kwame Boateng");
        engHead.ParentId.Should().Be(ceoId.ToString());
        engHead.Department.Should().NotBeNull();
        engHead.Department!.Name.Should().Be("Engineering");
        engHead.Department.EmployeeCount.Should().Be(8);
        engHead.Department.HeadcountCapacity.Should().Be(10);
        engHead.Children.Should().ContainSingle();

        var ic = engHead.Children[0];
        ic.FullName.Should().Be("Kofi Asante");
        ic.ParentId.Should().Be(engHeadId.ToString());
        ic.Department.Should().BeNull();
        ic.Children.Should().BeEmpty();
    }

    [Fact]
    public void ResolveFullName_prefers_cp_user_name_when_employee_full_name_is_whitespace()
    {
        var employeeId = Guid.Parse("3db13bc3-19dc-4cc2-8a5f-7e6a0efaccc2");
        var userId = "u1000001-0000-0000-0000-000000000001";
        var row = new OrgChartEmployeeRow(
            employeeId,
            " ",
            " ",
            null,
            "",
            "Frontend developer",
            null,
            userId,
            "doc-profile");

        var cp = new CpUserDto(userId, "Gary Ntori", "gary@example.com", null, true);

        var name = OrgChartBuilder.ResolveFullName(row, cp);

        name.Should().Be("Gary Ntori");
    }
}
