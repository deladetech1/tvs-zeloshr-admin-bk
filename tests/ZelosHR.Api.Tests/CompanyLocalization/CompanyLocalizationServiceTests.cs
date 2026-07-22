using FluentAssertions;
using NSubstitute;
using ZelosHR.Api.Entities.CompanyLocalization;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Persistence.Entities;
using ZelosHR.Api.Persistence.Repositories;

namespace ZelosHR.Api.Tests.CompanyLocalization;

public class CompanyLocalizationServiceTests
{
    private static CreateCompanyLocalizationDto ValidCreateBody() => new()
    {
        TimeZone = "UTC",
        CurrencyId = "GHS",
        DateFormat = "DD/MM/YYYY",
        NumberFormat = "1,234.56",
        FirstDayOfWeek = "Monday",
        YearStartMonth = "January",
        YearStartDay = 1,
    };

    private static CompanyLocalizationService CreateService(
        ICompanyLocalizationRepository? localization = null,
        ICpCurrencyRepository? currencies = null)
    {
        var currencyRepo = currencies ?? Substitute.For<ICpCurrencyRepository>();
        if (currencies is null)
            currencyRepo.ExistsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        return new CompanyLocalizationService(
            localization ?? Substitute.For<ICompanyLocalizationRepository>(),
            currencyRepo,
            Substitute.For<ICpUserRepository>());
    }

    [Fact]
    public async Task GetAsync_creates_stub_when_settings_missing()
    {
        var stubId = Guid.NewGuid();
        var currencies = Substitute.For<ICpCurrencyRepository>();
        currencies.GetDefaultAsync("t1", Arg.Any<CancellationToken>())
            .Returns(new CpCurrencyDto("GHS", "Ghana Cedi", "GHS", "₵", true));

        var localization = Substitute.For<ICompanyLocalizationRepository>();
        localization.EnsureStubAsync("t1", "o1", "GHS", "user-1", Arg.Any<CancellationToken>())
            .Returns(new CompanyLocalizationEntity
            {
                Id = stubId,
                TenantId = "t1",
                OrgId = "o1",
                TimeZone = "Africa/Accra",
                CurrencyId = "GHS",
                DateFormat = "DD/MM/YYYY",
                NumberFormat = "1,234.56",
                FirstDayOfWeek = "Monday",
                YearStartMonth = "January",
                YearStartDay = 1,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                CreatedBy = "user-1",
                UpdatedBy = "user-1",
            });

        var service = CreateService(localization: localization, currencies: currencies);
        var result = await service.GetAsync("t1", "o1", "user-1");

        result.Success.Should().BeTrue();
        result.StatusCode.Should().Be(200);
        result.Data!.Id.Should().Be(stubId.ToString());
        result.Data.CurrencyId.Should().Be("GHS");
        result.Data.TimeZone.Should().Be("Africa/Accra");
        await localization.Received(1).EnsureStubAsync("t1", "o1", "GHS", "user-1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAsync_returns_503_when_no_active_currency()
    {
        var currencies = Substitute.For<ICpCurrencyRepository>();
        currencies.GetDefaultAsync("t1", Arg.Any<CancellationToken>()).Returns((CpCurrencyDto?)null);
        currencies.ListAsync("t1", true, Arg.Any<CancellationToken>()).Returns(Array.Empty<CpCurrencyDto>());

        var service = CreateService(currencies: currencies);
        var result = await service.GetAsync("t1", "o1", "user-1");

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(503);
    }

    [Fact]
    public async Task CreateAsync_rejects_missing_time_zone()
    {
        var service = CreateService();
        var body = ValidCreateBody();
        body.TimeZone = null;

        var result = await service.CreateAsync(body, "t1", "o1", "user-1");

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.FieldErrors!["time_zone"].Should().Contain("required");
    }

    [Fact]
    public async Task CreateAsync_rejects_invalid_time_zone()
    {
        var service = CreateService();
        var body = ValidCreateBody();
        body.TimeZone = "Not/A/Real/Zone";

        var result = await service.CreateAsync(body, "t1", "o1", "user-1");

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.FieldErrors!["time_zone"].Should().Contain("IANA");
    }

    [Fact]
    public async Task CreateAsync_rejects_unknown_currency()
    {
        var currencies = Substitute.For<ICpCurrencyRepository>();
        currencies.ExistsAsync("BOGUS", "t1", Arg.Any<CancellationToken>()).Returns(false);

        var service = CreateService(currencies: currencies);
        var body = ValidCreateBody();
        body.CurrencyId = "BOGUS";

        var result = await service.CreateAsync(body, "t1", "o1", "user-1");

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.FieldErrors!["currency_id"].Should().Contain("not found");
    }

    [Fact]
    public async Task CreateAsync_rejects_when_settings_already_exist()
    {
        var localization = Substitute.For<ICompanyLocalizationRepository>();
        localization.GetEntityAsync("t1", "o1", Arg.Any<CancellationToken>())
            .Returns(new CompanyLocalizationEntity
            {
                Id = Guid.NewGuid(), TenantId = "t1", OrgId = "o1", TimeZone = "UTC", CurrencyId = "GHS",
                DateFormat = "DD/MM/YYYY", NumberFormat = "1,234.56", FirstDayOfWeek = "Monday",
                YearStartMonth = "January", YearStartDay = 1,
            });

        var service = CreateService(localization: localization);
        var result = await service.CreateAsync(ValidCreateBody(), "t1", "o1", "user-1");

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.FieldErrors!["time_zone"].Should().Contain("already exist");
    }

    [Fact]
    public async Task UpdateAsync_rejects_missing_id()
    {
        var service = CreateService();
        var result = await service.UpdateAsync(
            new UpdateCompanyLocalizationDto
            {
                TimeZone = "UTC", CurrencyId = "GHS", DateFormat = "DD/MM/YYYY",
                NumberFormat = "1,234.56", FirstDayOfWeek = "Monday", YearStartMonth = "January", YearStartDay = 1,
            },
            "t1", "o1", "user-1");

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.FieldErrors!["id"].Should().Contain("required");
    }

    [Fact]
    public async Task UpdateAsync_returns_404_when_settings_missing()
    {
        var service = CreateService();
        var result = await service.UpdateAsync(
            new UpdateCompanyLocalizationDto
            {
                Id = Guid.NewGuid().ToString(), TimeZone = "UTC", CurrencyId = "GHS", DateFormat = "DD/MM/YYYY",
                NumberFormat = "1,234.56", FirstDayOfWeek = "Monday", YearStartMonth = "January", YearStartDay = 1,
            },
            "t1", "o1", "user-1");

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task UpdateAsync_rejects_id_that_does_not_match_existing_settings()
    {
        var localization = Substitute.For<ICompanyLocalizationRepository>();
        localization.GetEntityAsync("t1", "o1", Arg.Any<CancellationToken>())
            .Returns(new CompanyLocalizationEntity
            {
                Id = Guid.NewGuid(), TenantId = "t1", OrgId = "o1", TimeZone = "UTC", CurrencyId = "GHS",
                DateFormat = "DD/MM/YYYY", NumberFormat = "1,234.56", FirstDayOfWeek = "Monday",
                YearStartMonth = "January", YearStartDay = 1,
            });

        var service = CreateService(localization: localization);
        var result = await service.UpdateAsync(
            new UpdateCompanyLocalizationDto
            {
                Id = Guid.NewGuid().ToString(), TimeZone = "UTC", CurrencyId = "GHS", DateFormat = "DD/MM/YYYY",
                NumberFormat = "1,234.56", FirstDayOfWeek = "Monday", YearStartMonth = "January", YearStartDay = 1,
            },
            "t1", "o1", "user-1");

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.FieldErrors!["id"].Should().Contain("does not match");
    }

    [Fact]
    public async Task UpdateAsync_rejects_invalid_year_start_day()
    {
        var service = CreateService();
        var body = ValidCreateBody();
        var result = await service.UpdateAsync(
            new UpdateCompanyLocalizationDto
            {
                Id = Guid.NewGuid().ToString(),
                TimeZone = body.TimeZone,
                CurrencyId = body.CurrencyId,
                DateFormat = body.DateFormat,
                NumberFormat = body.NumberFormat,
                FirstDayOfWeek = body.FirstDayOfWeek,
                YearStartMonth = body.YearStartMonth,
                YearStartDay = 32,
            },
            "t1", "o1", "user-1");

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.FieldErrors!["year_start_day"].Should().Contain("between 1 and 31");
    }

    [Fact]
    public async Task DeleteAsync_returns_404_when_settings_missing()
    {
        var service = CreateService();
        var result = await service.DeleteAsync("t1", "o1", Guid.NewGuid());

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task DeleteAsync_rejects_id_that_does_not_match_existing_settings()
    {
        var localization = Substitute.For<ICompanyLocalizationRepository>();
        localization.GetEntityAsync("t1", "o1", Arg.Any<CancellationToken>())
            .Returns(new CompanyLocalizationEntity
            {
                Id = Guid.NewGuid(), TenantId = "t1", OrgId = "o1", TimeZone = "UTC", CurrencyId = "GHS",
                DateFormat = "DD/MM/YYYY", NumberFormat = "1,234.56", FirstDayOfWeek = "Monday",
                YearStartMonth = "January", YearStartDay = 1,
            });

        var service = CreateService(localization: localization);
        var result = await service.DeleteAsync("t1", "o1", Guid.NewGuid());

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.FieldErrors!["id"].Should().Contain("does not match");
    }

    [Fact]
    public async Task DeleteAsync_deletes_when_id_matches()
    {
        var existingId = Guid.NewGuid();
        var localization = Substitute.For<ICompanyLocalizationRepository>();
        localization.GetEntityAsync("t1", "o1", Arg.Any<CancellationToken>())
            .Returns(new CompanyLocalizationEntity
            {
                Id = existingId, TenantId = "t1", OrgId = "o1", TimeZone = "UTC", CurrencyId = "GHS",
                DateFormat = "DD/MM/YYYY", NumberFormat = "1,234.56", FirstDayOfWeek = "Monday",
                YearStartMonth = "January", YearStartDay = 1,
            });
        localization.DeleteAsync("t1", "o1", Arg.Any<CancellationToken>()).Returns(true);

        var service = CreateService(localization: localization);
        var result = await service.DeleteAsync("t1", "o1", existingId);

        result.Success.Should().BeTrue();
        result.StatusCode.Should().Be(200);
    }
}
