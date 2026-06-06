using FluentAssertions;
using ZelosHR.Api.Entities.Employees;

namespace ZelosHR.Api.Tests.Employees;

public class EmployeeSubResourceUpsertMatcherTests
{
    private static readonly Guid EducationId = Guid.Parse("55555555-5555-5555-5555-555555555501");

    [Fact]
    public void ResolveEducationId_WhenIdProvided_ReturnsExplicitId()
    {
        var incoming = SampleEducationUpsert() with { Id = EducationId };
        var existing = new[] { SampleEducationDto() };

        var resolved = EmployeeSubResourceUpsertMatcher.ResolveEducationId(
            incoming, existing, new HashSet<Guid>());

        resolved.Should().Be(EducationId);
    }

    [Fact]
    public void ResolveEducationId_WhenContentMatchesSingleExistingRow_ReturnsExistingId()
    {
        var incoming = SampleEducationUpsert() with { Id = null };
        var existing = new[] { SampleEducationDto() };

        var resolved = EmployeeSubResourceUpsertMatcher.ResolveEducationId(
            incoming, existing, new HashSet<Guid>());

        resolved.Should().Be(EducationId);
    }

    [Fact]
    public void ResolveEducationId_WhenContentMatchesMultipleExistingRows_ReturnsNull()
    {
        var incoming = SampleEducationUpsert() with { Id = null };
        var existing = new[] { SampleEducationDto(), SampleEducationDto() };

        var resolved = EmployeeSubResourceUpsertMatcher.ResolveEducationId(
            incoming, existing, new HashSet<Guid>());

        resolved.Should().BeNull();
    }

    [Fact]
    public void ResolveEducationId_WhenExistingRowAlreadyConsumed_ReturnsNull()
    {
        var incoming = SampleEducationUpsert() with { Id = null };
        var existing = new[] { SampleEducationDto() };

        var resolved = EmployeeSubResourceUpsertMatcher.ResolveEducationId(
            incoming, existing, new HashSet<Guid> { EducationId });

        resolved.Should().BeNull();
    }

    private static EmployeeEducationDto SampleEducationDto() => new(
        EducationId,
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
