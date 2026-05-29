namespace ZelosHR.Api.Entities.CustomFields;

/// <summary>Values for <c>field_type</c> on custom field definitions.</summary>
public static class CustomFieldFieldTypes
{
    public const string Text = "text";
    public const string Textarea = "textarea";
    public const string Number = "number";
    public const string Date = "date";
    public const string Boolean = "boolean";
    public const string Select = "select";
    public const string Multiselect = "multiselect";
    public const string Email = "email";
    public const string Phone = "phone";
    public const string Url = "url";

    public static readonly IReadOnlyList<string> All =
    [
        Text,
        Textarea,
        Number,
        Date,
        Boolean,
        Select,
        Multiselect,
        Email,
        Phone,
        Url,
    ];
}
