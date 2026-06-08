using System.Globalization;
using System.Text;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Entities.Employees;

/// <summary>CSV export for employees (compatible with bulk import template columns).</summary>
public static class EmployeeCsvExport
{
    public static readonly IReadOnlyList<string> Headers =
    [
        "employee_id",
        "employee_code",
        "full_name",
        "work_email",
        "personal_email",
        "phone",
        "country",
        "job_title",
        "department_id",
        "department_name",
        "branch_id",
        "branch_name",
        "employment_type",
        "employment_status",
        "start_date",
        "work_location",
    ];

    public const string FileName = "employees_export.csv";

    public static byte[] Build(
        IEnumerable<EmployeeEntity> rows,
        IReadOnlyDictionary<string, CpUserDto> platformUsers)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(',', Headers));

        foreach (var row in rows)
        {
            platformUsers.TryGetValue(row.UserId ?? string.Empty, out var cp);
            var startDate = row.StartDate ?? row.EmploymentStartDate;
            sb.AppendLine(string.Join(',',
                Escape(row.Id.ToString()),
                Escape(row.EmployeeCode),
                Escape(EmployeeIdentityResolver.ResolveFullName(row, cp)),
                Escape(EmployeeIdentityResolver.ResolveWorkEmail(row, cp)),
                Escape(row.PersonalEmail),
                Escape(EmployeeIdentityResolver.ResolvePhone(row, cp) ?? row.Phone ?? row.PersonalPhone),
                Escape(row.Nationality),
                Escape(row.JobTitle),
                Escape(row.DepartmentId?.ToString()),
                Escape(row.Department?.Name),
                Escape(row.BranchId?.ToString()),
                Escape(row.Branch?.Name),
                Escape(row.EmploymentType),
                Escape(row.EmploymentStatus),
                Escape(startDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                Escape(row.WorkLocation)));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    internal static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var needsQuotes = value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r');
        if (!needsQuotes)
            return value;

        return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }
}
