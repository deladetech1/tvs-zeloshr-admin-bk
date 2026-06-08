namespace ZelosHR.Api.Shared.Validation;

/// <summary>Platform user create/update conflict mapped to a client field (<c>field_errors</c> key).</summary>
public sealed class PlatformUserConflictException : InvalidOperationException
{
    public PlatformUserConflictException(string fieldKey, string message)
        : base(message)
    {
        FieldKey = fieldKey;
    }

    public string FieldKey { get; }
}
