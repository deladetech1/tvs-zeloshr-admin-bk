using FluentAssertions;
using ZelosHR.Api.Entities.Employees;

namespace ZelosHR.Api.Tests.Employees;

public sealed class EmployeeSubResourceUpsertRulesTests
{
    private static readonly Guid EducationId = Guid.Parse("55555555-5555-5555-5555-555555555501");

    [Fact]
    public void HasPersistedId_false_when_null()
    {
        EmployeeSubResourceUpsertRules.HasPersistedId(null).Should().BeFalse();
    }

    [Fact]
    public void HasPersistedId_true_when_uuid_set()
    {
        EmployeeSubResourceUpsertRules.HasPersistedId(EducationId).Should().BeTrue();
    }

    [Fact]
    public void HasPersistedId_false_when_empty_guid()
    {
        EmployeeSubResourceUpsertRules.HasPersistedId(Guid.Empty).Should().BeFalse();
    }

    [Fact]
    public void ShouldUpdateExisting_false_for_client_generated_id()
    {
        var existing = new HashSet<Guid> { EducationId };
        var clientId = Guid.Parse("77777777-7777-7777-7777-777777777701");

        EmployeeSubResourceUpsertRules.ShouldUpdateExisting(clientId, existing).Should().BeFalse();
    }

    [Fact]
    public void ShouldUpdateExisting_true_when_id_on_employee()
    {
        var existing = new HashSet<Guid> { EducationId };

        EmployeeSubResourceUpsertRules.ShouldUpdateExisting(EducationId, existing).Should().BeTrue();
    }

    [Fact]
    public void ValidateDuplicateIds_education_rejects_repeated_id()
    {
        var items = new[]
        {
            new EmployeeEducationUpsertDto(EducationId, "UG", "BSc", null, null, null, false),
            new EmployeeEducationUpsertDto(EducationId, "UG", "MSc", null, null, null, false),
        };

        var errors = EmployeeSubResourceUpsertRules.ValidateDuplicateIds(items);

        errors.Should().NotBeNull();
        errors!.Should().ContainKey("education[1].id");
    }

    [Fact]
    public void ValidateDuplicateIds_certification_rejects_repeated_id()
    {
        var certId = Guid.Parse("66666666-6666-6666-6666-666666666601");
        var items = new[]
        {
            new EmployeeCertificationUpsertDto(certId, "AWS SA", null, null, null, null),
            new EmployeeCertificationUpsertDto(certId, "Azure Admin", null, null, null, null),
        };

        var errors = EmployeeSubResourceUpsertRules.ValidateDuplicateIds(items);

        errors.Should().NotBeNull();
        errors!.Should().ContainKey("certifications[1].id");
    }

    [Fact]
    public void ValidateDuplicateIds_identification_rejects_repeated_id()
    {
        var identificationId = Guid.Parse("77777777-7777-7777-7777-777777777701");
        var items = new[]
        {
            new EmployeeIdentificationUpsertDto(identificationId, SampleIdCardTypeId1, "GHA-1", null, null),
            new EmployeeIdentificationUpsertDto(identificationId, SampleIdCardTypeId2, "GHA-2", null, null),
        };

        var errors = EmployeeSubResourceUpsertRules.ValidateDuplicateIds(items);

        errors.Should().NotBeNull();
        errors!.Should().ContainKey("identity.identifications[1].id");
    }

    [Fact]
    public void ValidateDuplicateIdCardTypeIds_identification_rejects_repeated_type()
    {
        var typeId = Guid.Parse("753e2b9a-2322-4154-3456-98b8de5a4df5");
        var items = new[]
        {
            new EmployeeIdentificationUpsertDto(null, typeId, "GHA-1", null, null),
            new EmployeeIdentificationUpsertDto(null, typeId, "GHA-2", null, null),
        };

        var errors = EmployeeSubResourceUpsertRules.ValidateDuplicateIdCardTypeIds(items);

        errors.Should().NotBeNull();
        errors!.Should().ContainKey("identity.identifications[1].id_card_type_id");
    }

    private static readonly Guid SampleIdCardTypeId1 = Guid.Parse("753e2b9a-2322-4154-3456-98b8de5a4df5");
    private static readonly Guid SampleIdCardTypeId2 = Guid.Parse("345e2b9a-2322-4154-3456-98b8de5a4df5");
}
