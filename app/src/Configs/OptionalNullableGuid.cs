using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZelosHR.Api.Configs;

/// <summary>
/// Distinguishes a JSON property that was omitted (default) from one explicitly set to null (clear).
/// </summary>
[JsonConverter(typeof(OptionalNullableGuidJsonConverter))]
public readonly struct OptionalNullableGuid
{
    public bool IsSpecified { get; init; }
    public Guid? Value { get; init; }
}

public sealed class OptionalNullableGuidJsonConverter : JsonConverter<OptionalNullableGuid>
{
    public override OptionalNullableGuid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        Guid? value = reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.String => ParseGuid(reader.GetString()),
            _ => throw new JsonException(
                $"Cannot convert JSON {reader.TokenType} to Guid?; send a UUID string or null."),
        };

        return new OptionalNullableGuid { IsSpecified = true, Value = value };
    }

    public override void Write(Utf8JsonWriter writer, OptionalNullableGuid value, JsonSerializerOptions options)
    {
        if (!value.IsSpecified)
            return;

        if (value.Value is null)
            writer.WriteNullValue();
        else
            writer.WriteStringValue(value.Value.Value);
    }

    private static Guid? ParseGuid(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        if (Guid.TryParse(text, out var guid))
            return guid;

        throw new JsonException($"The JSON value is not a valid GUID: '{text}'.");
    }
}
