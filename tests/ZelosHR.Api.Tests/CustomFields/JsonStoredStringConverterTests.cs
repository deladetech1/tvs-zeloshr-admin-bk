using System.Text.Json;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.CustomFields;

namespace ZelosHR.Api.Tests.CustomFields;

public class JsonStoredStringConverterTests
{
    private static readonly JsonSerializerOptions Options = PlatformJson.CreateOptions();

    [Fact]
    public void Deserialize_empty_array_as_null()
    {
        var dto = JsonSerializer.Deserialize<CreateCustomFieldDefinitionDto>(
            """
            {
              "entity_type": "employee",
              "field_key": "ssnit",
              "label": "SSNIT",
              "field_type": "text",
              "options": []
            }
            """,
            Options);

        Assert.NotNull(dto);
        Assert.Null(dto!.Options);
    }

    [Fact]
    public void Deserialize_string_array_as_json_text()
    {
        var dto = JsonSerializer.Deserialize<CreateCustomFieldDefinitionDto>(
            """
            {
              "entity_type": "employee",
              "field_key": "bonus_eligible",
              "label": "Bonus",
              "field_type": "select",
              "options": ["yes", "no"]
            }
            """,
            Options);

        Assert.Equal("[\"yes\",\"no\"]", dto!.Options);
    }

    [Fact]
    public void Deserialize_quoted_json_string_unchanged()
    {
        var dto = JsonSerializer.Deserialize<CreateCustomFieldDefinitionDto>(
            """
            {
              "entity_type": "employee",
              "field_key": "bonus_eligible",
              "label": "Bonus",
              "field_type": "select",
              "options": "[\"yes\",\"no\"]"
            }
            """,
            Options);

        Assert.Equal("[\"yes\",\"no\"]", dto!.Options);
    }
}
