using FluentAssertions;
using ZelosHR.Api.Entities.Employees;

namespace ZelosHR.Api.Tests.Employees;

public sealed class EmployeeWorkingHoursConverterTests
{
    [Fact]
    public void Round_trips_decimal_working_hours()
    {
        EmployeeWorkingHoursConverter.FromStorage(
                EmployeeWorkingHoursConverter.ToStorage(40m))
            .Should().Be(40m);
    }

    [Fact]
    public void FromStorage_returns_null_for_invalid_value()
    {
        EmployeeWorkingHoursConverter.FromStorage("not-a-number").Should().BeNull();
    }
}
