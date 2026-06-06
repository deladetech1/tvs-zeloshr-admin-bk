using System.Text.Json;
using FluentAssertions;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;

namespace ZelosHR.Api.Tests.Shared;

public class ResponsSerializationTests
{
    [Fact]
    public void ErrorEnvelope_DoesNotSerializeMessageOrErrorsAlias()
    {
        var body = Respons<object>.ValidationError(new Dictionary<string, string>
        {
            ["identity.full_name"] = "Full name is required.",
        });

        var json = JsonSerializer.Serialize(body, PlatformJson.SerializerOptions);

        json.Should().NotContain("\"message\"");
        json.Should().NotContain("\"errors\"");
        json.Should().Contain("\"detail\"");
        json.Should().Contain("\"field_errors\"");
    }

    [Fact]
    public void FailEnvelope_PutsTextInDetail()
    {
        var body = Respons<object>.Fail("Could not allocate a unique employee code.", statusCode: 409);

        var json = JsonSerializer.Serialize(body, PlatformJson.SerializerOptions);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("detail").GetString()
            .Should().Be("Could not allocate a unique employee code.");
        root.TryGetProperty("message", out _).Should().BeFalse();
    }
}
