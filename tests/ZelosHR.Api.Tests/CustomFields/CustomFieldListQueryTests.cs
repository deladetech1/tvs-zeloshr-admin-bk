using ZelosHR.Api.Entities.CustomFields;

namespace ZelosHR.Api.Tests.CustomFields;

public class CustomFieldListQueryTests
{
    [Fact]
    public void EntityTypes_includes_all_host_entities()
    {
        Assert.Contains("employee", CustomFieldEntityTypes.All);
        Assert.Contains("lifecycle_event", CustomFieldEntityTypes.All);
        Assert.Equal(5, CustomFieldEntityTypes.All.Count);
    }
}
