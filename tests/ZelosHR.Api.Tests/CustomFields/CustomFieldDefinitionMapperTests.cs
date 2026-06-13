using ZelosHR.Api.Entities.CustomFields;
using ZelosHR.Api.Entities.Employees;

namespace ZelosHR.Api.Tests.CustomFields;

public sealed class CustomFieldDefinitionMapperTests
{
    [Fact]
    public void EnrichAuthors_maps_cp_users_fullname_to_created_by_and_updated_by()
    {
        const string userId = "uid_abc123";
        var users = new Dictionary<string, CpUserDto>
        {
            [userId] = new(userId, "Kwame Mensah", "kwame@company.com", null, true),
        };

        var source = SampleDefinition(userId);
        var enriched = CustomFieldDefinitionMapper.EnrichAuthors(source, users);

        Assert.Equal(userId, enriched.CreatedById);
        Assert.Equal(userId, enriched.UpdatedById);
        Assert.Equal("Kwame Mensah", enriched.CreatedBy);
        Assert.Equal("Kwame Mensah", enriched.UpdatedBy);
    }

    [Fact]
    public void EnrichAuthors_leaves_display_names_null_when_cp_user_missing()
    {
        var source = SampleDefinition("uid_missing");
        var enriched = CustomFieldDefinitionMapper.EnrichAuthors(source, new Dictionary<string, CpUserDto>());

        Assert.Null(enriched.CreatedBy);
        Assert.Null(enriched.UpdatedBy);
    }

    private static CustomFieldDefinitionDto SampleDefinition(string userId) => new()
    {
        Id = Guid.NewGuid().ToString(),
        EntityType = "employee",
        FieldKey = "tier",
        Label = "Tier",
        FieldType = "select",
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow,
        CreatedById = userId,
        UpdatedById = userId,
    };
}
