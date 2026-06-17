using System.ComponentModel.DataAnnotations;
using ZelosHR.Api.Configs;

namespace ZelosHR.Api.Entities.Leave;

public sealed class CreateLeaveRequestDto
{
    [Required]
    public Guid EmployeeId { get; set; }

    [Required]
    public Guid LeaveTypeId { get; set; }

    [Required]
    public DateOnly StartDate { get; set; }

    [Required]
    public DateOnly EndDate { get; set; }

    [Required]
    [Range(0.5, 365)]
    public decimal DaysRequested { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }
}

public sealed class CreateMyLeaveRequestDto
{
    [Required]
    public Guid LeaveTypeId { get; set; }

    [Required]
    public DateOnly StartDate { get; set; }

    [Required]
    public DateOnly EndDate { get; set; }

    [Required]
    [Range(0.5, 365)]
    public decimal DaysRequested { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }
}

public sealed class UpdateLeaveRequestDto
{
    [MaxLength(50)]
    [SwaggerAllowedValues(typeof(LeaveFieldOptions), nameof(LeaveFieldOptions.RequestStatuses))]
    public string? Status { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }
}

public sealed class CreateLeaveBalanceDto
{
    [Required]
    public Guid EmployeeId { get; set; }

    [Required]
    public Guid LeaveTypeId { get; set; }

    [Required]
    [Range(0, 365)]
    public decimal EntitledDays { get; set; }

    [Range(0, 365)]
    public decimal UsedDays { get; set; }
}

public sealed class UpdateLeaveBalanceDto
{
    [Range(0, 365)]
    public decimal? EntitledDays { get; set; }

    [Range(0, 365)]
    public decimal? UsedDays { get; set; }
}

public sealed class CreateLeaveTypeDto
{
    [Required]
    [MaxLength(80)]
    public string? Name { get; set; }

    [Required]
    [Range(0, 365)]
    public decimal? DefaultEntitledDays { get; set; }

    public bool IsPaid { get; set; } = true;

    [Required]
    [SwaggerAllowedValues(typeof(LeaveFieldOptions), nameof(LeaveFieldOptions.AccrualMethods))]
    public string? AccrualMethod { get; set; } = LeaveAccrualMethods.FrontLoaded;

    public bool CarryOverAllowed { get; set; }

    [Required]
    [MinLength(1)]
    [SwaggerAllowedValues(typeof(LeaveFieldOptions), nameof(LeaveFieldOptions.AppliesToEmploymentTypes))]
    public List<string>? AppliesToEmploymentTypes { get; set; }

    [Range(0, 365)]
    public int? MinNoticeWorkingDays { get; set; }

    [Range(1, 365)]
    public int? MaxConsecutiveDays { get; set; }

    public bool RequiresSupportingDocument { get; set; }
}

/// <summary>Add / update public holiday — matches frontend <c>AddPublicHolidayRequest</c>.</summary>
/// <remarks><c>country</c> is the display name from the country picker (e.g. <c>Ghana</c>).</remarks>
public sealed class CreatePublicHolidayDto
{
    [Required]
    [MaxLength(200)]
    public string? HolidayName { get; set; }

    [Required]
    public DateOnly Date { get; set; }

    /// <summary>When true, one DB row is stored; leave calculations project this anchor date into every year in range (no annual row insert job).</summary>
    public bool IsRecurringAnnually { get; set; }

    /// <summary>Country name from <c>GET /countries/list</c> (e.g. <c>Ghana</c>).</summary>
    [Required]
    public string? Country { get; set; }
}

public sealed class RejectLeaveRequestDto
{
    [MaxLength(2000)]
    public string? Notes { get; set; }
}
