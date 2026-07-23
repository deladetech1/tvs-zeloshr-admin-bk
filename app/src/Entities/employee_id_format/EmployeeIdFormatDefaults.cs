namespace ZelosHR.Api.Entities.EmployeeIdFormat;

internal static class EmployeeIdFormatDefaults
{
    internal const string Prefix = "ZEL";
    internal const int DigitCount = 4;
    internal const int StartingNumber = 1;
    internal const string Separator = EmployeeIdFormatSeparator.Hyphen;
    internal const bool AutoGenerate = true;
}
