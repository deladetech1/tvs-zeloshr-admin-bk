using ZelosHR.Api.Shared.Formatting;

namespace ZelosHR.Api.Tests.Shared;

public class NameFormattingTests
{
    [Fact]
    public void BuildInitials_returns_two_letters()
    {
        Assert.Equal("AA", NameFormatting.BuildInitials("Ama", "Asante"));
    }

    [Fact]
    public void BuildFullName_includes_middle_name_when_present()
    {
        Assert.Equal("Kofi Kwame Mensah", NameFormatting.BuildFullName("Kofi", "Kwame", "Mensah"));
    }
}
