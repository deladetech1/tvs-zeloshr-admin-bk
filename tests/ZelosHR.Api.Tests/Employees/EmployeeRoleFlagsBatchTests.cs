using FluentAssertions;
using ZelosHR.Api.Entities.Employees;

namespace ZelosHR.Api.Tests.Employees;

public class EmployeeRoleFlagsBatchTests
{
    [Fact]
    public void Empty_batch_returns_false_for_any_id()
    {
        var batch = EmployeeRoleFlagsBatch.Empty;
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");

        batch.IsLineManager(id).Should().BeFalse();
        batch.IsHeadOfDepartment(id).Should().BeFalse();
    }

    [Fact]
    public void Batch_resolves_line_manager_and_head_flags()
    {
        var managerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var headId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var otherId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        var batch = new EmployeeRoleFlagsBatch(
            new HashSet<Guid> { managerId },
            new HashSet<Guid> { headId });

        batch.IsLineManager(managerId).Should().BeTrue();
        batch.IsHeadOfDepartment(managerId).Should().BeFalse();
        batch.IsLineManager(headId).Should().BeFalse();
        batch.IsHeadOfDepartment(headId).Should().BeTrue();
        batch.IsLineManager(otherId).Should().BeFalse();
        batch.IsHeadOfDepartment(otherId).Should().BeFalse();
    }
}
