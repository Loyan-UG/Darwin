using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Darwin.Application;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Identity.Queries;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Identity;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.Identity;

public sealed class GetSecurityStampHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_ReturnSecurityStamp_ForActiveUser()
    {
        await using var db = GetSecurityStampTestDbContext.Create();
        var user = CreateUser("active-user@darwin.test");

        db.Set<User>().Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetSecurityStampHandler(db, new TestStringLocalizer<ValidationResource>());

        var result = await handler.HandleAsync(user.Id, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().Be(user.SecurityStamp);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_WhenUserDoesNotExist()
    {
        await using var db = GetSecurityStampTestDbContext.Create();
        var handler = new GetSecurityStampHandler(db, new TestStringLocalizer<ValidationResource>());

        var result = await handler.HandleAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("UserNotFoundOrInactive");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_WhenUserIsInactive()
    {
        await using var db = GetSecurityStampTestDbContext.Create();
        var user = CreateUser("inactive-user@darwin.test");
        user.IsActive = false;

        db.Set<User>().Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetSecurityStampHandler(db, new TestStringLocalizer<ValidationResource>());

        var result = await handler.HandleAsync(user.Id, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("UserNotFoundOrInactive");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_WhenUserIsSoftDeleted()
    {
        await using var db = GetSecurityStampTestDbContext.Create();
        var user = CreateUser("deleted-user@darwin.test");
        user.IsDeleted = true;

        db.Set<User>().Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetSecurityStampHandler(db, new TestStringLocalizer<ValidationResource>());

        var result = await handler.HandleAsync(user.Id, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("UserNotFoundOrInactive");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_WhenSecurityStampIsBlank()
    {
        await using var db = GetSecurityStampTestDbContext.Create();
        var user = CreateUser("blank-stamp@darwin.test");
        user.SecurityStamp = " ";

        db.Set<User>().Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetSecurityStampHandler(db, new TestStringLocalizer<ValidationResource>());

        var result = await handler.HandleAsync(user.Id, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("UserNotFoundOrInactive");
    }

    private static User CreateUser(string email)
    {
        return new User(email, "hashed-password", "security-stamp-123")
        {
            FirstName = "Lina",
            LastName = "Schmidt",
            EmailConfirmed = true,
            IsActive = true,
            Locale = "de-DE",
            Currency = "EUR",
            Timezone = "Europe/Berlin",
            ChannelsOptInJson = "{}",
            FirstTouchUtmJson = "{}",
            LastTouchUtmJson = "{}",
            ExternalIdsJson = "{}",
            RowVersion = [1, 2, 3]
        };
    }

    private sealed class TestStringLocalizer<TResource> : IStringLocalizer<TResource>
    {
        public LocalizedString this[string name] => new(name, name, resourceNotFound: false);

        public LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(name, arguments), resourceNotFound: false);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => Array.Empty<LocalizedString>();

        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
    }

    private sealed class GetSecurityStampTestDbContext : DbContext, IAppDbContext
    {
        private GetSecurityStampTestDbContext(DbContextOptions<GetSecurityStampTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static GetSecurityStampTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<GetSecurityStampTestDbContext>()
                .UseInMemoryDatabase($"darwin_get_security_stamp_tests_{Guid.NewGuid()}")
                .Options;

            return new GetSecurityStampTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Ignore<GeoCoordinate>();

            modelBuilder.Entity<User>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Email).IsRequired();
                builder.Property(x => x.NormalizedEmail).IsRequired();
                builder.Property(x => x.UserName).IsRequired();
                builder.Property(x => x.NormalizedUserName).IsRequired();
                builder.Property(x => x.PasswordHash).IsRequired();
                builder.Property(x => x.SecurityStamp).IsRequired();
                builder.Property(x => x.Locale).IsRequired();
                builder.Property(x => x.Currency).IsRequired();
                builder.Property(x => x.Timezone).IsRequired();
                builder.Property(x => x.ChannelsOptInJson).IsRequired();
                builder.Property(x => x.FirstTouchUtmJson).IsRequired();
                builder.Property(x => x.LastTouchUtmJson).IsRequired();
                builder.Property(x => x.ExternalIdsJson).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });
        }
    }
}
