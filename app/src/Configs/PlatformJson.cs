using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZelosHR.Api.Configs;

/// <summary>
/// JSON settings aligned with Trovesuite platform APIs (Mystoreguard / FastAPI snake_case).
/// </summary>
public static class PlatformJson
{
    public static JsonSerializerOptions SerializerOptions { get; } = CreateOptions();

    public static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
        options.Converters.Add(new NullableGuidJsonConverter());
        return options;
    }

    public static void Configure(JsonSerializerOptions options)
    {
        options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        options.DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower;
        options.PropertyNameCaseInsensitive = true;
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        if (!options.Converters.Any(c => c is NullableGuidJsonConverter))
            options.Converters.Add(new NullableGuidJsonConverter());
    }
}
