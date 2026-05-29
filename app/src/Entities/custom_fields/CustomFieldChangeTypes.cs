namespace ZelosHR.Api.Entities.CustomFields;

/// <summary>Values for <c>change_type</c> on custom field audit logs.</summary>
public static class CustomFieldChangeTypes
{
    public const string Create = "create";
    public const string Update = "update";
    public const string Delete = "delete";

    public static readonly IReadOnlyList<string> All = [Create, Update, Delete];
}
