using System;
using System.Threading.Tasks;
using Darwin.Application;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Loyalty.Commands;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Loyalty;
using Darwin.Domain.Enums;
using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.Loyalty;

/// <summary>
/// Unit tests for <see cref="ActivateLoyaltyAccountHandler"/>
/// and <see cref="SuspendLoyaltyAccountHandler"/>.
/// </summary>
public sealed class LoyaltyAccountStatusHandlerTests
{
    // ─── ActivateLoyaltyAccountHandler ───────────────────────────────────────

    [Fact]
    public async Task Activate_Should_Fail_WhenAccountNotFound()
    {
        await using var db = StatusTestDbContext.Create();
        var handler = new ActivateLoyaltyAccountHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(new ActivateLoyaltyAccountDto
        {
            Id = Guid.NewGuid()
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("LoyaltyAccountNotFound");
    }

    [Fact]
    public async Task Activate_Should_Succeed_WhenAccountAlreadyActive()
    {
        await using var db = StatusTestDbContext.Create();
        var account = CreateAccount(LoyaltyAccountStatus.Active);
        db.Set<LoyaltyAccount>().Add(account);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ActivateLoyaltyAccountHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(new ActivateLoyaltyAccountDto
        {
            Id = account.Id
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Activate_Should_SetStatusActive_WhenAccountIsSuspended()
    {
        await using var db = StatusTestDbContext.Create();
        var account = CreateAccount(LoyaltyAccountStatus.Suspended);
        db.Set<LoyaltyAccount>().Add(account);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ActivateLoyaltyAccountHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(new ActivateLoyaltyAccountDto
        {
            Id = account.Id
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();

        var persisted = await db.Set<LoyaltyAccount>().AsNoTracking()
            .SingleAsync(x => x.Id == account.Id, TestContext.Current.CancellationToken);
        persisted.Status.Should().Be(LoyaltyAccountStatus.Active);
    }

    [Fact]
    public async Task Activate_Should_Fail_WhenConcurrencyConflict()
    {
        await using var db = StatusTestDbContext.Create();
        var account = CreateAccount(LoyaltyAccountStatus.Suspended, rowVersion: [1, 2, 3]);
        db.Set<LoyaltyAccount>().Add(account);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ActivateLoyaltyAccountHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(new ActivateLoyaltyAccountDto
        {
            Id = account.Id,
            RowVersion = [9, 9, 9] // wrong version
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("LoyaltyAccountConcurrencyConflict");
    }

    [Fact]
    public async Task Activate_Should_Throw_WhenDtoIdIsEmpty()
    {
        await using var db = StatusTestDbContext.Create();
        var handler = new ActivateLoyaltyAccountHandler(db, new TestLocalizer());

        var act = async () => await handler.HandleAsync(new ActivateLoyaltyAccountDto
        {
            Id = Guid.Empty
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>();
    }

    // ─── SuspendLoyaltyAccountHandler ────────────────────────────────────────

    [Fact]
    public async Task Suspend_Should_Fail_WhenAccountNotFound()
    {
        await using var db = StatusTestDbContext.Create();
        var handler = new SuspendLoyaltyAccountHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(new SuspendLoyaltyAccountDto
        {
            Id = Guid.NewGuid()
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("LoyaltyAccountNotFound");
    }

    [Fact]
    public async Task Suspend_Should_Succeed_WhenAccountAlreadySuspended()
    {
        await using var db = StatusTestDbContext.Create();
        var account = CreateAccount(LoyaltyAccountStatus.Suspended);
        db.Set<LoyaltyAccount>().Add(account);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SuspendLoyaltyAccountHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(new SuspendLoyaltyAccountDto
        {
            Id = account.Id
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Suspend_Should_SetStatusSuspended_WhenAccountIsActive()
    {
        await using var db = StatusTestDbContext.Create();
        var account = CreateAccount(LoyaltyAccountStatus.Active);
        db.Set<LoyaltyAccount>().Add(account);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SuspendLoyaltyAccountHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(new SuspendLoyaltyAccountDto
        {
            Id = account.Id
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();

        var persisted = await db.Set<LoyaltyAccount>().AsNoTracking()
            .SingleAsync(x => x.Id == account.Id, TestContext.Current.CancellationToken);
        persisted.Status.Should().Be(LoyaltyAccountStatus.Suspended);
    }

    [Fact]
    public async Task Suspend_Should_Fail_WhenConcurrencyConflict()
    {
        await using var db = StatusTestDbContext.Create();
        var account = CreateAccount(LoyaltyAccountStatus.Active, rowVersion: [1, 2, 3]);
        db.Set<LoyaltyAccount>().Add(account);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SuspendLoyaltyAccountHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(new SuspendLoyaltyAccountDto
        {
            Id = account.Id,
            RowVersion = [9, 9, 9] // wrong version
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("LoyaltyAccountConcurrencyConflict");
    }

    [Fact]
    public async Task Suspend_Should_Throw_WhenDtoIdIsEmpty()
    {
        await using var db = StatusTestDbContext.Create();
        var handler = new SuspendLoyaltyAccountHandler(db, new TestLocalizer());

        var act = async () => await handler.HandleAsync(new SuspendLoyaltyAccountDto
        {
            Id = Guid.Empty
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static LoyaltyAccount CreateAccount(LoyaltyAccountStatus status, byte[]? rowVersion = null) =>
        new()
        {
            BusinessId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Status = status,
            RowVersion = rowVersion ?? [1, 2, 3]
        };

    private sealed class TestLocalizer : IStringLocalizer<ValidationResource>
    {
        public LocalizedString this[string name] => new(name, name, resourceNotFound: false);
        public LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(name, arguments), resourceNotFound: false);
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            Array.Empty<LocalizedString>();
        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
    }

    private sealed class StatusTestDbContext : DbContext, IAppDbContext
    {
        private StatusTestDbContext(DbContextOptions<StatusTestDbContext> options) : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static StatusTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<StatusTestDbContext>()
                .UseInMemoryDatabase($"darwin_loyalty_status_tests_{Guid.NewGuid()}")
                .Options;
            return new StatusTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Ignore<GeoCoordinate>();

            modelBuilder.Entity<LoyaltyAccount>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.RowVersion).IsRequired();
                builder.Ignore(x => x.Transactions);
                builder.Ignore(x => x.Redemptions);
            });
        }
    }
}
