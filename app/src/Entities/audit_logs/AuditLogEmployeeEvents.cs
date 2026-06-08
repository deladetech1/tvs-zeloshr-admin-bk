using ZelosHR.Api.Entities.Employees;

namespace ZelosHR.Api.Entities.AuditLogs;

internal static class AuditLogEmployeeEvents
{
    internal static AuditEvent ForCreate(bool isFinalised) => new(
        ActionTitle: "Employee record created",
        ActionDescription: isFinalised
            ? "New record created. Onboarding invite sent automatically."
            : "Draft employee record created.",
        Category: "Lifecycle",
        Severity: "Low");

    internal static AuditEvent ForUpdate(
        UpdateEmployeeAggregateRequest request,
        Guid employeeId,
        string employeeCode,
        string employeeFullName)
    {
        var baseEvent = new AuditEvent(
            ActionTitle: "Employee record updated",
            ActionDescription: "Employee profile updated.",
            Category: "Lifecycle",
            Severity: "Low",
            EmployeeId: employeeId,
            EmployeeDisplayCode: employeeCode,
            EmployeeFullName: employeeFullName);

        if (request.Employment?.EmploymentStatus is { } status
            && status.Equals("Terminated", StringComparison.OrdinalIgnoreCase))
        {
            return baseEvent with
            {
                ActionTitle = "Employment status changed",
                ActionDescription = "Status changed from Active to Terminated.",
                Category = "Lifecycle",
                Severity = "High",
                IsFlagged = true,
            };
        }

        if (request.Identity is not null)
        {
            return baseEvent with
            {
                ActionTitle = "Personal information updated",
                ActionDescription = "Employee identity details updated.",
                Category = "Field change",
                Severity = "Medium",
            };
        }

        if (request.Compensation is not null)
        {
            return baseEvent with
            {
                ActionTitle = "Compensation updated",
                ActionDescription = "Compensation details revised.",
                Category = "Field change",
                Severity = "Medium",
            };
        }

        if (request.Employment is not null)
        {
            return baseEvent with
            {
                ActionTitle = "Employment details updated",
                ActionDescription = "Employment assignment or status updated.",
                Category = "Field change",
                Severity = "Medium",
            };
        }

        if (request.DocumentIds is { Count: > 0 })
        {
            return baseEvent with
            {
                ActionTitle = "Document uploaded",
                ActionDescription = "Document attached to employee record.",
                Category = "System",
                Severity = "Low",
            };
        }

        return baseEvent;
    }

    internal static AuditEvent WithEmployee(
        AuditEvent auditEvent,
        Guid employeeId,
        string employeeCode,
        string employeeFullName) =>
        auditEvent with
        {
            EmployeeId = employeeId,
            EmployeeDisplayCode = employeeCode,
            EmployeeFullName = employeeFullName,
        };
}
