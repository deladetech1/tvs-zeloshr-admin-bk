using ZelosHR.Api.Persistence.Entities;
using ZelosHR.Api.Entities.Employees;

namespace ZelosHR.Api.Entities.EmployeePortal;

public interface IEmployeeActivationInviteSender
{
    Task IssueAndSendActivationAsync(
        EmployeeEntity employee,
        CpUserDto cpUser,
        string tenantId,
        string orgId,
        string busId,
        CancellationToken ct = default);
}
