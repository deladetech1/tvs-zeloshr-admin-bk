using System.Text;
using Microsoft.EntityFrameworkCore;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Persistence;

namespace ZelosHR.Api.Entities.Employees;

public sealed class EmployeeBulkImportRowResult
{
    public required int Index { get; init; }
    public bool Success { get; init; }
    public Guid? EmployeeId { get; init; }
    public string? Error { get; init; }
}

public sealed class EmployeeBulkImportResult
{
    public IReadOnlyList<EmployeeBulkImportRowResult> Rows { get; init; } = [];
    public int SuccessCount { get; init; }
    public int FailureCount { get; init; }
}

public sealed class EmployeeBulkImportService
{
    private readonly ZelosHrDbContext _db;
    private readonly EmployeeAggregateService _aggregate;

    public EmployeeBulkImportService(ZelosHrDbContext db, EmployeeAggregateService aggregate)
    {
        _db = db;
        _aggregate = aggregate;
    }

    public async Task<Respons<EmployeeBulkImportResult>> ImportCsvAsync(
        Stream csvStream,
        string status,
        CancellationToken ct = default)
    {
        var isFinalised = string.Equals(status, "finalised", StringComparison.OrdinalIgnoreCase);
        if (!isFinalised && !string.Equals(status, "draft", StringComparison.OrdinalIgnoreCase))
        {
            return Respons<EmployeeBulkImportResult>.ValidationError(new Dictionary<string, string>
            {
                ["status"] = "Status must be 'draft' or 'finalised'.",
            });
        }

        using var reader = new StreamReader(csvStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var headerLine = await reader.ReadLineAsync(ct);
        if (string.IsNullOrWhiteSpace(headerLine))
        {
            return Respons<EmployeeBulkImportResult>.ValidationError(new Dictionary<string, string>
            {
                ["file"] = "CSV file is empty.",
            });
        }

        var headers = ParseCsvLine(headerLine).Select(NormalizeHeader).ToList();
        if (!headers.Contains("full_name"))
        {
            return Respons<EmployeeBulkImportResult>.ValidationError(new Dictionary<string, string>
            {
                ["file"] = "Missing required CSV column: full_name.",
            });
        }

        var rows = new List<EmployeeBulkImportRowResult>();
        var lineIndex = 0;
        string? line;
        while ((line = await reader.ReadLineAsync(ct)) is not null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var values = ParseCsvLine(line);
            var map = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < headers.Count && i < values.Count; i++)
                map[headers[i]] = values[i];

            if (string.IsNullOrWhiteSpace(map.GetValueOrDefault("full_name")))
            {
                rows.Add(new EmployeeBulkImportRowResult
                {
                    Index = lineIndex,
                    Success = false,
                    Error = "full_name is required.",
                });
                lineIndex++;
                continue;
            }

            var employmentTypeRaw = map.GetValueOrDefault("employment_type");
            var employmentTypeId = ParseGuid(employmentTypeRaw);

            var request = new CreateEmployeeAggregateRequest
            {
                Identity = new EmployeeAggregateIdentityDto
                {
                    FullName = map["full_name"]!.Trim(),
                    WorkEmail = map.GetValueOrDefault("work_email"),
                    PersonalEmail = map.GetValueOrDefault("personal_email"),
                    Phone = map.GetValueOrDefault("phone"),
                    Country = map.GetValueOrDefault("country"),
                },
                Employment = HasEmployment(map)
                    ? new EmployeeAggregateEmploymentDto
                    {
                        JobTitle = map.GetValueOrDefault("job_title"),
                        DepartmentId = ParseGuid(map.GetValueOrDefault("department_id")),
                        BranchId = ParseGuid(map.GetValueOrDefault("branch_id")),
                        EmploymentTypeId = employmentTypeId,
                        EmploymentTypeName = employmentTypeId is null ? employmentTypeRaw : null,
                        WorkLocation = map.GetValueOrDefault("work_location"),
                    }
                    : null,
            };

            EmployeeBulkImportRowResult rowResult;
            try
            {
                var created = await _aggregate.CreateAsync(request, isFinalised, ct);
                rowResult = new EmployeeBulkImportRowResult
                {
                    Index = lineIndex,
                    Success = created.Success,
                    EmployeeId = created.Data?.Id,
                    Error = created.Success ? null : created.Error ?? created.Detail,
                };
            }
            catch (DbUpdateException)
            {
                _db.ChangeTracker.Clear();
                rowResult = new EmployeeBulkImportRowResult
                {
                    Index = lineIndex,
                    Success = false,
                    Error = "Could not save employee record. Please retry.",
                };
            }

            rows.Add(rowResult);
            lineIndex++;
        }

        if (rows.Count == 0)
        {
            return Respons<EmployeeBulkImportResult>.ValidationError(new Dictionary<string, string>
            {
                ["file"] = "CSV contains no data rows.",
            });
        }

        var successCount = rows.Count(r => r.Success);
        var result = new EmployeeBulkImportResult
        {
            Rows = rows,
            SuccessCount = successCount,
            FailureCount = rows.Count - successCount,
        };
        var failureMessages = rows.Where(r => !r.Success).Select(r => r.Error).ToList();
        return BatchResultResponse.FromRowCounts(
            result, successCount, result.FailureCount, "Bulk import", failureMessages);
    }

    private static bool HasEmployment(IReadOnlyDictionary<string, string?> map) =>
        !string.IsNullOrWhiteSpace(map.GetValueOrDefault("job_title"))
        || !string.IsNullOrWhiteSpace(map.GetValueOrDefault("department_id"))
        || !string.IsNullOrWhiteSpace(map.GetValueOrDefault("branch_id"))
        || !string.IsNullOrWhiteSpace(map.GetValueOrDefault("employment_type"))
        || !string.IsNullOrWhiteSpace(map.GetValueOrDefault("work_location"));

    private static Guid? ParseGuid(string? value) =>
        Guid.TryParse(value, out var id) ? id : null;

    private static string NormalizeHeader(string header) =>
        header.Trim().ToLowerInvariant().Replace(' ', '_');

    private static List<string> ParseCsvLine(string line)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                continue;
            }

            if (c == ',' && !inQuotes)
            {
                values.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(c);
        }

        values.Add(current.ToString());
        return values;
    }
}
