using System.Text.Json;
using System.Text.Json.Serialization;
using ZelosHR.Api.Entities.Employees;

namespace ZelosHR.Api.Configs;

/// <summary>
/// Accepts <c>education</c> as a single object. Legacy clients may still send a one-element array — first item is used.
/// </summary>
public sealed class EducationSectionJsonConverter : JsonConverter<EmployeeAggregateEducationDto?>
{
    public override EmployeeAggregateEducationDto? Read(
        ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType == JsonTokenType.StartArray)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            if (doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() == 0)
                return null;

            return doc.RootElement[0].Deserialize<EmployeeAggregateEducationDto>(options);
        }

        return JsonSerializer.Deserialize<EmployeeAggregateEducationDto>(ref reader, options);
    }

    public override void Write(Utf8JsonWriter writer, EmployeeAggregateEducationDto? value, JsonSerializerOptions options) =>
        JsonSerializer.Serialize(writer, value, options);
}
