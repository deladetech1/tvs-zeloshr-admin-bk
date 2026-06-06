using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZelosHR.Api.Configs;

/// <summary>
/// Treats null, empty, and whitespace Guid strings as null on input (optional FK fields from forms).
/// </summary>
public sealed class NullableGuidJsonConverter : JsonConverter<Guid?>
{
    public override Guid? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.String:
                var text = reader.GetString();
                if (string.IsNullOrWhiteSpace(text))
                    return null;
                if (Guid.TryParse(text, out var guid))
                    return guid;
                throw new JsonException($"The JSON value is not a valid GUID: '{text}'.");
            default:
                throw new JsonException(
                    $"Cannot convert JSON {reader.TokenType} to Guid?; send a UUID string or null.");
        }
    }

    public override void Write(Utf8JsonWriter writer, Guid? value, JsonSerializerOptions options)
    {
        if (value is null)
            writer.WriteNullValue();
        else
            writer.WriteStringValue(value.Value);
    }
}
