using System.Text.Json;
using FluentAssertions;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.OrgStructure;

namespace ZelosHR.Api.Tests.OrgStructure;

public class OrgStructureRequestExtensionsTests
{
    private static readonly JsonSerializerOptions Json = PlatformJson.SerializerOptions;

    [Fact]
    public void CreateDepartment_deserializes_nested_head_of_department()
    {
        const string json = """
            {
              "name": "Engineering",
              "head_of_department": {
                "employee_id": "e1000001-0000-4000-8000-000000000001"
              },
              "description": "Platform teams"
            }
            """;

        var dto = JsonSerializer.Deserialize<CreateDepartmentRequestDto>(json, Json);

        dto.Should().NotBeNull();
        dto!.Description.Should().Be("Platform teams");
        dto.ResolveHeadOfDepartmentId().Should().Be(Guid.Parse("e1000001-0000-4000-8000-000000000001"));
    }

    [Fact]
    public void UpdateDepartment_prefers_flat_head_of_department_id()
    {
        var flat = Guid.Parse("e1000001-0000-4000-8000-000000000002");
        var nested = Guid.Parse("e1000001-0000-4000-8000-000000000001");

        var dto = new UpdateDepartmentRequestDto
        {
            HeadOfDepartmentId = new OptionalNullableGuid { IsSpecified = true, Value = flat },
            HeadOfDepartment = new DepartmentHeadReferenceDto { EmployeeId = nested.ToString() },
        };

        dto.ResolveHeadOfDepartmentId().Should().Be(flat);
    }

    [Fact]
    public void UpdateDepartment_detects_head_change_from_nested_object()
    {
        var dto = new UpdateDepartmentRequestDto
        {
            HeadOfDepartment = new DepartmentHeadReferenceDto
            {
                EmployeeId = "e1000001-0000-4000-8000-000000000003",
            },
        };

        dto.HasHeadOfDepartmentChange().Should().BeTrue();
        dto.ResolveHeadOfDepartmentId().Should().Be(Guid.Parse("e1000001-0000-4000-8000-000000000003"));
    }

    [Fact]
    public void UpdateDepartment_deserializes_explicit_null_head_as_clear()
    {
        const string json = """
            {
              "name": "Backend Team",
              "head_of_department_id": null
            }
            """;

        var dto = JsonSerializer.Deserialize<UpdateDepartmentRequestDto>(json, Json);

        dto.Should().NotBeNull();
        dto!.HasHeadOfDepartmentChange().Should().BeTrue();
        dto.ResolveHeadOfDepartmentId().Should().BeNull();
    }
}
