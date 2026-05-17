using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Queries;
using Darwin.Domain.Entities.Identity;
using Darwin.Infrastructure.Persistence.Db;
using Darwin.Tests.Integration.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Darwin.Tests.Integration.Identity;

public sealed class IdentitySearchProviderNeutralIntegrationTests
    : DeterministicIntegrationTestBase,
      IClassFixture<WebApplicationFactory<Program>>
{
    public IdentitySearchProviderNeutralIntegrationTests(WebApplicationFactory<Program> factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GetUsersPage_Should_HandleEscapedSubstringAndCaseVariants_OnEmailSearch()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DarwinDbContext>();

        var marker = Guid.NewGuid().ToString("N");
        var literalTerm = $"searchcase_%_probe[{marker}]@example.test";
        var wildcardLikeTerm = $"searchcaseprobe[{marker}]@example.test";
        var exactMatch = new User(literalTerm, "hash", Guid.NewGuid().ToString("N"));
        var unrelated = new User(wildcardLikeTerm, "hash", Guid.NewGuid().ToString("N"));

        db.Set<User>().AddRange(exactMatch, unrelated);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = scope.ServiceProvider.GetRequiredService<GetUsersPageHandler>();
        var lowerCaseTerm = $"searchcase_%_probe[{marker}]";
        var upperCaseTerm = $"SEARCHCASE_%_PROBE[{marker}]";

        var lowerCaseResult = await handler.HandleAsync(
            page: 1,
            pageSize: 20,
            emailFilter: lowerCaseTerm,
            filter: UserQueueFilter.All,
            ct: TestContext.Current.CancellationToken);

        var upperCaseResult = await handler.HandleAsync(
            page: 1,
            pageSize: 20,
            emailFilter: upperCaseTerm,
            filter: UserQueueFilter.All,
            ct: TestContext.Current.CancellationToken);

        lowerCaseResult.Total.Should().Be(1);
        lowerCaseResult.Items.Should().ContainSingle(x => x.Id == exactMatch.Id);
        lowerCaseResult.Items.Should().NotContain(x => x.Id == unrelated.Id);

        upperCaseResult.Total.Should().Be(1);
        upperCaseResult.Items.Should().ContainSingle(x => x.Id == exactMatch.Id);
        upperCaseResult.Items.Should().NotContain(x => x.Id == unrelated.Id);
    }

    [Fact]
    public async Task GetPermissionsPage_Should_HandleEscapedSubstringAndCaseVariants_OnKeySearch()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DarwinDbContext>();

        var marker = Guid.NewGuid().ToString("N");
        var exactMatch = new Permission($"perm_%_manage[{marker}]", "Manage permission", false, null);
        var unrelated = new Permission($"perm-manage-{marker}", "Manage permission", false, null);

        db.Set<Permission>().AddRange(exactMatch, unrelated);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = scope.ServiceProvider.GetRequiredService<GetPermissionsPageHandler>();
        var lowerCaseTerm = $"perm_%_manage[{marker}]";
        var upperCaseTerm = $"PERM_%_MANAGE[{marker}]";

        var lowerCaseResult = await handler.HandleAsync(
            pageNumber: 1,
            pageSize: 20,
            searchTerm: lowerCaseTerm,
            ct: TestContext.Current.CancellationToken);

        var upperCaseResult = await handler.HandleAsync(
            pageNumber: 1,
            pageSize: 20,
            searchTerm: upperCaseTerm,
            ct: TestContext.Current.CancellationToken);

        lowerCaseResult.Succeeded.Should().BeTrue();
        lowerCaseResult.Value!.Items.Should().HaveCount(1);
        lowerCaseResult.Value!.Items.Should().ContainSingle(x => x.Id == exactMatch.Id);
        lowerCaseResult.Value!.Items.Should().NotContain(x => x.Id == unrelated.Id);

        upperCaseResult.Succeeded.Should().BeTrue();
        upperCaseResult.Value!.Items.Should().HaveCount(1);
        upperCaseResult.Value!.Items.Should().ContainSingle(x => x.Id == exactMatch.Id);
        upperCaseResult.Value!.Items.Should().NotContain(x => x.Id == unrelated.Id);
    }

    [Fact]
    public async Task GetMobileDevicesPage_Should_HandleEscapedSubstringAndCaseVariants_OnUserEmailSearch()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DarwinDbContext>();

        var marker = Guid.NewGuid().ToString("N");
        var literalTerm = $"mobile_device_%_probe[{marker}]@example.test";
        var literalMatchUser = new Darwin.Domain.Entities.Identity.User(literalTerm, "hash", Guid.NewGuid().ToString("N"));
        var unrelatedUser = new Darwin.Domain.Entities.Identity.User($"mobile_device_probe[{marker.Substring(0, 6)}]@example.test", "hash", Guid.NewGuid().ToString("N"));

        db.Set<Darwin.Domain.Entities.Identity.User>().AddRange(literalMatchUser, unrelatedUser);
        db.Set<Darwin.Domain.Entities.Identity.UserDevice>().AddRange(
            new Darwin.Domain.Entities.Identity.UserDevice
            {
                UserId = literalMatchUser.Id,
                DeviceId = "alpha-device-001",
                Platform = Darwin.Domain.Enums.MobilePlatform.Android
            },
            new Darwin.Domain.Entities.Identity.UserDevice
            {
                UserId = unrelatedUser.Id,
                DeviceId = "alpha-device-002",
                Platform = Darwin.Domain.Enums.MobilePlatform.iOS
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetMobileDevicesPageHandler(db, new StubClock());
        var lowerCaseTerm = literalTerm;
        var upperCaseTerm = literalTerm.ToUpperInvariant();

        var lowerCaseResult = await handler.HandleAsync(
            page: 1,
            pageSize: 20,
            query: lowerCaseTerm,
            ct: TestContext.Current.CancellationToken);

        var upperCaseResult = await handler.HandleAsync(
            page: 1,
            pageSize: 20,
            query: upperCaseTerm,
            ct: TestContext.Current.CancellationToken);

        lowerCaseResult.Total.Should().Be(1);
        lowerCaseResult.Items.Should().ContainSingle(x => x.UserId == literalMatchUser.Id);
        lowerCaseResult.Items.Should().NotContain(x => x.UserId == unrelatedUser.Id);

        upperCaseResult.Total.Should().Be(1);
        upperCaseResult.Items.Should().ContainSingle(x => x.UserId == literalMatchUser.Id);
        upperCaseResult.Items.Should().NotContain(x => x.UserId == unrelatedUser.Id);
    }

    private sealed class StubClock : Darwin.Application.Abstractions.Services.IClock
    {
        public DateTime UtcNow => new DateTime(2030, 1, 30, 8, 0, 0, DateTimeKind.Utc);
    }
}
