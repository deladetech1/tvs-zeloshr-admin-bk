using System.Text.Json;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Employees;

namespace ZelosHR.Api.Tests.Employees;

public class NullableGuidJsonConverterTests
{
    private static readonly JsonSerializerOptions Options = PlatformJson.CreateOptions();

    [Fact]
    public void Deserialize_employee_add_with_empty_branch_id()
    {
        var request = JsonSerializer.Deserialize<CreateEmployeeAggregateRequest>(
            """
            {
              "identity": {
                "full_name": "Demo Employee",
                "phone": "+233209998877"
              },
              "employment": {
                "branch_id": ""
              }
            }
            """,
            Options);

        Assert.NotNull(request);
        Assert.Equal("Demo Employee", request!.Identity.FullName);
        Assert.NotNull(request.Employment);
        Assert.Null(request.Employment!.BranchId);
    }

    [Fact]
    public void Deserialize_employee_add_with_null_branch_id()
    {
        var request = JsonSerializer.Deserialize<CreateEmployeeAggregateRequest>(
            """
            {
              "identity": {
                "full_name": "Demo Employee",
                "phone": "+233209998877"
              },
              "employment": {
                "branch_id": null
              }
            }
            """,
            Options);

        Assert.NotNull(request);
        Assert.Null(request!.Employment!.BranchId);
    }

    [Fact]
    public void Deserialize_employee_add_with_valid_branch_id()
    {
        var branchId = Guid.Parse("063e2b9a-9254-4154-89e7-98b8de5a4df5");
        var request = JsonSerializer.Deserialize<CreateEmployeeAggregateRequest>(
            $$"""
            {
              "identity": {
                "full_name": "Demo Employee",
                "phone": "+233209998877"
              },
              "employment": {
                "branch_id": "{{branchId}}"
              }
            }
            """,
            Options);

        Assert.Equal(branchId, request!.Employment!.BranchId);
    }
}
