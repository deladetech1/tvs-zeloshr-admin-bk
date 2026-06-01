using System.Text;

namespace ZelosHR.Api.Entities.Employees;

/// <summary>CSV template for <c>POST /api/v1/employees/bulk</c>.</summary>
public static class EmployeeBulkImportCsv
{
    public static readonly IReadOnlyList<string> Headers =
    [
        "full_name",
        "work_email",
        "personal_email",
        "phone",
        "country",
        "job_title",
        "department_id",
        "branch_id",
        "employment_type",
        "work_location",
    ];

    public const string FileName = "employee_import_template.csv";

    public static string TemplateContent =>
        string.Join(',', Headers) + "\n" +
        "Ada Lovelace,ada.lovelace@company.com,ada.personal@example.com,+233201234567,Ghana,Software Engineer,,,Full-time,Accra HQ\n";

    public static byte[] TemplateBytes => Encoding.UTF8.GetBytes(TemplateContent);
}
