using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZelosHR.Api.Configs;

/// <summary>
/// Accepts a JSON string or array/object on input; stores as a string (jsonb wire format).
/// Empty arrays normalize to null (typical for text fields).
/// </summary>
public sealed class JsonStoredStringConverter : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.String:
                var text = reader.GetString();
                return string.IsNullOrWhiteSpace(text) ? null : text;
            case JsonTokenType.StartArray:
                using (var doc = JsonDocument.ParseValue(ref reader))
                {
                    if (doc.RootElement.ValueKind == JsonValueKind.Array
                        && doc.RootElement.GetArrayLength() == 0)
                        return null;
                    return doc.RootElement.GetRawText();
                }
            case JsonTokenType.StartObject:
                using (var docObj = JsonDocument.ParseValue(ref reader))
                    return docObj.RootElement.GetRawText();
            default:
                throw new JsonException(
                    $"Cannot convert JSON {reader.TokenType} to stored string; send a string, array, object, or null.");
        }
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        if (value is null)
            writer.WriteNullValue();
        else
            writer.WriteStringValue(value);
    }
}
