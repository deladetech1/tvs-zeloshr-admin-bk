using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Entities.CompanyInfo;

internal static class CompanyProfileState
{
    internal static bool IsConfigured(CompanyProfileEntity profile) =>
        !string.IsNullOrWhiteSpace(profile.LegalName);
}
