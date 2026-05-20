using FluentAssertions;
using NSubstitute;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.LifecycleEvents;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Tests.LifecycleEvents;

public class LifecycleEventsServiceTests
{
    private readonly ILifecycleEventRepository _lifecycle = Substitute.For<ILifecycleEventRepository>();
    private readonly IEmployeeRepository _employees = Substitute.For<IEmployeeRepository>();
    private readonly LifecycleEventsService _sut;

    public LifecycleEventsServiceTests() =>
        _sut = new LifecycleEventsService(_lifecycle, _employees);

    [Fact]
    public async Task CreateAsync_requires_existing_employee()
    {
        _employees.GetByIdScopedAsync(Arg.Any<Guid>(), "t1", "o1", Arg.Any<CancellationToken>())
            .Returns((EmployeeEntity?)null);

        var result = await _sut.CreateAsync(
            new CreateLifecycleEventDto
            {
                EmployeeId = Guid.NewGuid(),
                EventType = "Probation Review",
                DueDate = DateOnly.FromDateTime(DateTime.UtcNow),
            },
            "t1",
            "o1");

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        await _lifecycle.DidNotReceive().CreateScopedAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateOnly>(),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_syncs_employee_lifecycle_when_status_maps()
    {
        var empId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var entity = new EmployeeEntity
        {
            Id = empId,
            TenantId = "t1",
            OrgId = "o1",
            EmployeeCode = "ZEL-0001",
            FirstName = "Ama",
            LastName = "Mensah",
            DateOfBirth = new DateOnly(1990, 1, 1),
            Gender = "Female",
            Nationality = "Ghanaian",
            GhanaCardNumber = "GHA-123456789-0",
            PersonalEmail = "a@example.com",
            PersonalPhone = "+233200000000",
            ResidentialAddress = "Accra",
            GhanaPostGps = "GA-123-4567",
            LifecycleState = EmployeeLifecycleStates.PreHire,
        };

        _lifecycle.UpdateScopedAsync(
                eventId, "t1", "o1", null, null, "Resigned", null, Arg.Any<CancellationToken>())
            .Returns(new LifecycleEventListItemDto
            {
                LifecycleEventId = eventId.ToString(),
                EmployeeId = empId.ToString(),
                EmployeeFullName = "Ama Mensah",
                EventType = "Exit",
                DueDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Status = "Resigned",
                Urgency = "Critical",
            });
        _employees.GetByIdScopedForUpdateAsync(empId, "t1", "o1", Arg.Any<CancellationToken>())
            .Returns(entity);

        var result = await _sut.UpdateAsync(
            eventId,
            new UpdateLifecycleEventDto { Status = "Resigned" },
            "t1",
            "o1");

        result.Success.Should().BeTrue();
        entity.LifecycleState.Should().Be(EmployeeLifecycleStates.Resigned);
        await _employees.Received(1).UpdateAsync(entity, Arg.Any<CancellationToken>());
    }
}
