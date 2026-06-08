using System.Text;
using FluentAssertions;
using ZelosHR.Api.Entities.AuditLogs;

namespace ZelosHR.Api.Tests.AuditLogs;

public class AuditLogCsvExportTests
{
    [Fact]
    public void Build_includes_header_and_escaped_values()
    {
        var id = Guid.Parse("a1111111-1111-1111-1111-111111111101");
        var csv = AuditLogCsvExport.Build(
        [
            new AuditLogListRow(
                id,
                new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero),
                "Title, with comma",
                "Description",
                Guid.NewGuid(),
                "EMP-1",
                "Ama Mensah",
                "u1",
                "Demo Admin",
                "Field change",
                "Medium",
                true),
        ]);

        var text = Encoding.UTF8.GetString(csv);
        text.Should().StartWith(string.Join(',', AuditLogCsvExport.Headers));
        text.Should().Contain("\"Title, with comma\"");
        text.Should().Contain(id.ToString());
        text.Should().Contain("true");
    }
}
