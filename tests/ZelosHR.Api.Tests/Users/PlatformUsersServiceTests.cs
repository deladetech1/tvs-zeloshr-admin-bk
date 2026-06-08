using FluentAssertions;
using NSubstitute;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.Users;

namespace ZelosHR.Api.Tests.Users;

public class PlatformUsersServiceTests
{
    private readonly ICpUserRepository _repo = Substitute.For<ICpUserRepository>();
    private readonly PlatformUsersService _sut;

    public PlatformUsersServiceTests() => _sut = new PlatformUsersService(_repo);

    [Fact]
    public async Task GetUsers_passes_query_to_repository()
    {
        var query = new GetUsersQuery
        {
            IsActive = true,
            DeleteStatus = "NOT_DELETED",
            CanLogin = true,
            Email = "admin",
            Fullname = "Demo",
            Gender = "MALE",
            UseOr = false,
            Page = 2,
            Size = 15,
        };
        _repo.ListPlatformMembersScopedAsync(query, "t1", 2, 15, Arg.Any<CancellationToken>())
            .Returns((Array.Empty<PlatformUserListRow>(), 0));

        var result = await _sut.GetUsersAsync(query, "t1");

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Detail.Should().Be("No users found");
        await _repo.Received(1).ListPlatformMembersScopedAsync(query, "t1", 2, 15, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetUsers_maps_rows()
    {
        var query = new GetUsersQuery();
        _repo.ListPlatformMembersScopedAsync(Arg.Any<GetUsersQuery>(), "t1", 1, 10, Arg.Any<CancellationToken>())
            .Returns((
            [
                new PlatformUserListRow(
                    "u1",
                    "t1",
                    "Demo Admin",
                    "admin@demo.com",
                    "+233200000001",
                    "Accra",
                    "MALE",
                    "1990-01-01",
                    null,
                    true,
                    "NOT_DELETED",
                    true,
                    false,
                    null,
                    "2026-01-01",
                    "10:00:00",
                    DateTimeOffset.Parse("2026-01-01T10:00:00+00:00")),
            ],
            1));

        var result = await _sut.GetUsersAsync(query, "t1");

        result.Success.Should().BeTrue();
        result.Data!.Should().HaveCount(1);
        result.Data[0].Fullname.Should().Be("Demo Admin");
        result.Pagination!.Total.Should().Be(1);
    }
}
