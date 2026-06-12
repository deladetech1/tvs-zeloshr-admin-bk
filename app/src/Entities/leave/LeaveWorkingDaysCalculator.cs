namespace ZelosHR.Api.Entities.Leave;

internal static class LeaveWorkingDaysCalculator
{
    internal static decimal CountWorkingDays(DateOnly start, DateOnly end, IReadOnlySet<DateOnly> publicHolidays)
    {
        if (end < start)
            return 0;

        var count = 0;
        for (var date = start; date <= end; date = date.AddDays(1))
        {
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                continue;
            if (publicHolidays.Contains(date))
                continue;
            count++;
        }

        return count;
    }

    internal static int CountPublicHolidaysInRange(
        DateOnly start, DateOnly end, IReadOnlySet<DateOnly> publicHolidays)
    {
        if (end < start)
            return 0;

        var count = 0;
        for (var date = start; date <= end; date = date.AddDays(1))
        {
            if (publicHolidays.Contains(date))
                count++;
        }

        return count;
    }
}
