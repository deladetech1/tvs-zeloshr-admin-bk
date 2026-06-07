using FluentAssertions;
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
            new(ceoId, "Kwame Asante", "Kwame", "Asante", "Chief Executive Officer", null, null, null),
            new(engHeadId, "Kwame Boateng", "Kwame", "Boateng", "Chief Technology Officer", ceoId, null, null),
            new(icId, "Kofi Asante", "Kofi", "Asante", "Software Engineer", engHeadId, null, null),
        };

        var departmentByHead = new Dictionary<Guid, OrgChartDepartmentHeadRow>
        {
            [engHeadId] = new(engDeptId, "Engineering", engHeadId, 8, 10),
        };

        var roots = OrgChartBuilder.Build(
            employees,
            departmentByHead,
            new Dictionary<Guid, ZelosHR.Api.Entities.Files.DocumentReadDto?>());

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
}
