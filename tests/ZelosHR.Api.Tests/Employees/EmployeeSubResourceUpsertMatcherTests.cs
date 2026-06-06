using FluentAssertions;
using ZelosHR.Api.Entities.Employees;

namespace ZelosHR.Api.Tests.Employees;

public sealed class EmployeeSubResourceUpsertMatcherTests
{
    private static readonly Guid EducationId = Guid.Parse("55555555-5555-5555-5555-555555555501");
    private static readonly Guid EducationDuplicateId = Guid.Parse("55555555-5555-5555-5555-555555555502");

    [Fact]
    public void ResolveEducation_WhenIdProvided_ReturnsExplicitId()
    {
        var incoming = SampleEducationUpsert() with { Id = EducationId };
        var existing = new[] { SampleEducationDto() };

        var resolved = EmployeeSubResourceUpsertMatcher.ResolveEducation(
            incoming, existing, new HashSet<Guid>());

        resolved.UpdateId.Should().Be(EducationId);
        resolved.DuplicateIdsToRemove.Should().BeEmpty();
    }

    [Fact]
    public void ResolveEducation_WhenContentMatchesSingleExistingRow_ReturnsExistingId()
    {
        var incoming = SampleEducationUpsert() with { Id = null };
        var existing = new[] { SampleEducationDto() };

        var resolved = EmployeeSubResourceUpsertMatcher.ResolveEducation(
            incoming, existing, new HashSet<Guid>());

        resolved.UpdateId.Should().Be(EducationId);
        resolved.DuplicateIdsToRemove.Should().BeEmpty();
    }

    [Fact]
    public void ResolveEducation_WhenContentMatchesMultipleExistingRows_UpdatesFirstAndRemovesRest()
    {
        var incoming = SampleEducationUpsert() with { Id = null };
        var existing = new[]
        {
            SampleEducationDto(),
            SampleEducationDto(EducationDuplicateId),
        };

        var resolved = EmployeeSubResourceUpsertMatcher.ResolveEducation(
            incoming, existing, new HashSet<Guid>());

        resolved.UpdateId.Should().Be(EducationId);
        resolved.DuplicateIdsToRemove.Should().Equal(EducationDuplicateId);
    }

    [Fact]
    public void ResolveEducation_WhenExistingRowAlreadyConsumed_InsertsNew()
    {
        var incoming = SampleEducationUpsert() with { Id = null };
        var existing = new[] { SampleEducationDto() };

        var resolved = EmployeeSubResourceUpsertMatcher.ResolveEducation(
            incoming, existing, new HashSet<Guid> { EducationId });

        resolved.UpdateId.Should().BeNull();
        resolved.DuplicateIdsToRemove.Should().BeEmpty();
    }

    [Fact]
    public void ResolveEducation_WhenEmptyGuidProvided_TreatsAsMissingId()
    {
        var incoming = SampleEducationUpsert() with { Id = Guid.Empty };
        var existing = new[] { SampleEducationDto() };

        var resolved = EmployeeSubResourceUpsertMatcher.ResolveEducation(
            incoming, existing, new HashSet<Guid>());

        resolved.UpdateId.Should().Be(EducationId);
    }

    private static EmployeeEducationDto SampleEducationDto(Guid? id = null) => new(
        id ?? EducationId,
        Guid.NewGuid(),
        "University of Ghana",
        "BSc",
        "Computer Science",
        new DateOnly(2008, 9, 1),
        new DateOnly(2012, 6, 30),
        false);

    private static EmployeeEducationUpsertDto SampleEducationUpsert() => new(
        null,
        "University of Ghana",
        "BSc",
        "Computer Science",
        new DateOnly(2008, 9, 1),
        new DateOnly(2012, 6, 30),
        false);
}
