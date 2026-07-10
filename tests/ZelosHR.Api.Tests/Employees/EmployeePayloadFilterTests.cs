using FluentAssertions;
using System.Text.Json.Nodes;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.Employees.Authorization;

namespace ZelosHR.Api.Tests.Employees;

public class FieldPolicyTests
{
    [Theory]
    [InlineData("identity.phone", FieldAccess.Free)]
    [InlineData("identity.work_email", FieldAccess.None)]
    [InlineData("identity.full_name", FieldAccess.Approval)]
    [InlineData("employment.job_title", FieldAccess.None)]
    [InlineData("education", FieldAccess.Approval)]
    [InlineData("skills", FieldAccess.Free)]
    public void ForEmployee_ReturnsExpectedAccess(string path, FieldAccess expected) =>
        FieldPolicy.ForEmployee(path).Should().Be(expected);

    [Fact]
    public void ListForEmployeeUi_IncludesKnownFreeField() =>
        FieldPolicy.ListForEmployeeUi().Should().Contain(e =>
            e.Path == "identity.phone" && e.Access == "free");
}

public class EmployeePayloadFilterTests
{
    [Fact]
    public void Split_FreeIdentityField_GoesToApplyNow()
    {
        var payload = JsonNode.Parse("""{"identity":{"phone":"+233201234567"}}""")!.AsObject();
        var split = EmployeePayloadFilter.Split(payload, _ => null);

        split.ApplyNow["identity"]!.AsObject()["phone"]!.GetValue<string>().Should().Be("+233201234567");
        split.PendingApproval.Should().BeEmpty();
        split.Rejected.Should().BeEmpty();
    }

    [Fact]
    public void Split_AdminOnlyEmployment_IsRejected()
    {
        var payload = JsonNode.Parse("""{"employment":{"job_title":"Engineer"}}""")!.AsObject();
        var split = EmployeePayloadFilter.Split(payload, _ => null);

        split.ApplyNow.Should().BeEmpty();
        split.PendingApproval.Should().BeEmpty();
        split.Rejected.Should().Contain("employment");
    }

    [Fact]
    public void Split_ApprovalField_CreatesPendingChange()
    {
        var payload = JsonNode.Parse("""{"identity":{"full_name":"Ada Lovelace"}}""")!.AsObject();
        var split = EmployeePayloadFilter.Split(payload, _ => JsonValue.Create("Old Name"));

        split.ApplyNow.Should().BeEmpty();
        split.PendingApproval.Should().ContainSingle(p => p.FieldPath == "identity.full_name");
        split.Rejected.Should().BeEmpty();
    }

    [Fact]
    public void Split_SameApprovalValue_SkipsPending()
    {
        var payload = JsonNode.Parse("""{"identity":{"full_name":"Ada Lovelace"}}""")!.AsObject();
        var split = EmployeePayloadFilter.Split(payload, _ => JsonValue.Create("Ada Lovelace"));

        split.PendingApproval.Should().BeEmpty();
    }

    [Fact]
    public void Split_DocumentIds_IsRejected()
    {
        var payload = JsonNode.Parse("""{"document_ids":["doc-1"]}""")!.AsObject();
        var split = EmployeePayloadFilter.Split(payload, _ => null);

        split.Rejected.Should().Contain("document_ids");
    }
}
