using System.Reflection;

namespace ZelosHR.Api.Configs;

/// <summary>Documents allowed string values on Swagger schemas and query parameters.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.Field)]
public sealed class SwaggerAllowedValuesAttribute : Attribute
{
    public SwaggerAllowedValuesAttribute(params string[] values) => Values = values;

    public SwaggerAllowedValuesAttribute(Type declaringType, string memberName)
    {
        DeclaringType = declaringType;
        MemberName = memberName;
    }

    public string[] Values { get; } = [];

    public Type? DeclaringType { get; }

    public string? MemberName { get; }

    public string? Description { get; init; }

    public IReadOnlyList<string> Resolve()
    {
        if (Values.Length > 0)
            return Values;

        if (DeclaringType is null || MemberName is null)
            return [];

        const BindingFlags flags = BindingFlags.Public | BindingFlags.Static;

        if (DeclaringType.GetField(MemberName, flags)?.GetValue(null) is IReadOnlyList<string> fieldList)
            return fieldList;

        if (DeclaringType.GetProperty(MemberName, flags)?.GetValue(null) is IReadOnlyList<string> propList)
            return propList;

        return [];
    }
}
