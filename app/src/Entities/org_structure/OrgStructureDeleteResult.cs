namespace ZelosHR.Api.Entities.OrgStructure;

public enum OrgStructureDeleteResult
{
    Deleted,
    NotFound,
    InUseByEmployees,
    HasChildDepartments,
}
