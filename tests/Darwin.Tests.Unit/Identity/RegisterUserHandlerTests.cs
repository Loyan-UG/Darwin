using System;
using System.Threading.Tasks;
using Darwin.Application;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Identity.Commands;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Validators;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Identity;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.Identity;

/// <summary>
/// Unit tests for <see cref="RegisterUserHandler"/>.
/// Covers registration, duplicate email rejection, and optional default role assignment.
/// </summary>
public sealed class RegisterUserHandlerTests
{
    [Fact]
    public async Task RegisterUser_Should_PersistUser_WithHashedPassword()
    {
        await using var db = RegisterTestDbContext.Create();
        var handler = new RegisterUserHandler(db, new FakeHasher(), new FakeStampService(), new UserCreateValidator(new TestLocalizer()), new TestLocalizer());

        var result = await handler.HandleAsync(new UserCreateDto
        {
            Email = "newuser@darwin.de",
            Password = "SecurePass1A",
            Locale = "de-DE",
            Timezone = "Europe/Berlin",
            Currency = "EUR",
            FirstName = "Anna",
            LastName = "Schmidt",
            IsActive = true
        }, ct: TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);

        var user = await db.Set<User>().SingleAsync(TestContext.Current.CancellationToken);
        user.Email.Should().Be("newuser@darwin.de");
        user.PasswordHash.Should().Be("hashed:SecurePass1A");
        user.FirstName.Should().Be("Anna");
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterUser_Should_Fail_WhenEmailAlreadyInUse()
    {
        await using var db = RegisterTestDbContext.Create();
        db.Set<User>().Add(new User("existing@darwin.de", "hash", "stamp")
        {
            Locale = "de-DE",
            Currency = "EUR",
            Timezone = "Europe/Berlin",
            ChannelsOptInJson = "{}",
            FirstTouchUtmJson = "{}",
            LastTouchUtmJson = "{}",
            ExternalIdsJson = "{}"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new RegisterUserHandler(db, new FakeHasher(), new FakeStampService(), new UserCreateValidator(new TestLocalizer()), new TestLocalizer());

        var result = await handler.HandleAsync(new UserCreateDto
        {
            Email = "existing@darwin.de",
            Password = "AnotherPass1B",
            Locale = "de-DE",
            Timezone = "Europe/Berlin",
            Currency = "EUR"
        }, ct: TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("EmailAlreadyInUse");
    }

    [Fact]
    public async Task RegisterUser_Should_AssignDefaultRole_WhenRoleExists()
    {
        await using var db = RegisterTestDbContext.Create();
        var role = new Role("member", "Member", false, null);
        db.Set<Role>().Add(role);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new RegisterUserHandler(db, new FakeHasher(), new FakeStampService(), new UserCreateValidator(new TestLocalizer()), new TestLocalizer());

        var result = await handler.HandleAsync(new UserCreateDto
        {
            Email = "newrole@darwin.de",
            Password = "RolePass1C",
            Locale = "de-DE",
            Timezone = "Europe/Berlin",
            Currency = "EUR",
            IsActive = true
        }, defaultRoleId: role.Id, ct: TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();

        var userRole = await db.Set<UserRole>().SingleAsync(TestContext.Current.CancellationToken);
        userRole.RoleId.Should().Be(role.Id);
    }

    [Fact]
    public async Task RegisterUser_Should_Fail_WhenDefaultRoleDoesNotExist()
    {
        await using var db = RegisterTestDbContext.Create();
        var handler = new RegisterUserHandler(db, new FakeHasher(), new FakeStampService(), new UserCreateValidator(new TestLocalizer()), new TestLocalizer());

        var result = await handler.HandleAsync(new UserCreateDto
        {
            Email = "noRole@darwin.de",
            Password = "NoRolePass1D",
            Locale = "de-DE",
            Timezone = "Europe/Berlin",
            Currency = "EUR"
        }, defaultRoleId: Guid.NewGuid(), ct: TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("DefaultRoleNotFound");
    }

    [Fact]
    public async Task RegisterUser_Should_NotAssignRole_WhenNoDefaultRoleProvided()
    {
        await using var db = RegisterTestDbContext.Create();
        var handler = new RegisterUserHandler(db, new FakeHasher(), new FakeStampService(), new UserCreateValidator(new TestLocalizer()), new TestLocalizer());

        var result = await handler.HandleAsync(new UserCreateDto
        {
            Email = "norole@darwin.de",
            Password = "NoRole1E",
            Locale = "de-DE",
            Timezone = "Europe/Berlin",
            Currency = "EUR"
        }, ct: TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();

        var roleCount = await db.Set<UserRole>().CountAsync(TestContext.Current.CancellationToken);
        roleCount.Should().Be(0, "no role should be assigned when defaultRoleId is null");
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private sealed class FakeHasher : IUserPasswordHasher
    {
        public string Hash(string password) => $"hashed:{password}";
        public bool Verify(string hashedPassword, string password) => hashedPassword == $"hashed:{password}";
    }

    private sealed class FakeStampService : ISecurityStampService
    {
        public string NewStamp() => "new-stamp";
        public bool AreEqual(string? a, string? b) => string.Equals(a, b, StringComparison.Ordinal);
    }

    private sealed class TestLocalizer : IStringLocalizer<ValidationResource>
    {
        public LocalizedString this[string name] => new(name, name, false);
        public LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(name, arguments), false);
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => Array.Empty<LocalizedString>();
        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
    }

    private sealed class RegisterTestDbContext : DbContext, IAppDbContext
    {
        private RegisterTestDbContext(DbContextOptions<RegisterTestDbContext> options) : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static RegisterTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<RegisterTestDbContext>()
                .UseInMemoryDatabase($"darwin_register_user_tests_{Guid.NewGuid()}")
                .Options;
            return new RegisterTestDbContext(options);
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
                builder.Ignore(x => x.UserRoles);
                builder.Ignore(x => x.Logins);
                builder.Ignore(x => x.Tokens);
                builder.Ignore(x => x.TwoFactorSecrets);
                builder.Ignore(x => x.Devices);
                builder.Ignore(x => x.BusinessFavorites);
                builder.Ignore(x => x.BusinessLikes);
                builder.Ignore(x => x.BusinessReviews);
                builder.Ignore(x => x.EngagementSnapshot);
            });

            modelBuilder.Entity<Role>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Key).IsRequired();
                builder.Property(x => x.NormalizedName).IsRequired();
                builder.Property(x => x.DisplayName).IsRequired();
                builder.Ignore(x => x.UserRoles);
                builder.Ignore(x => x.RolePermissions);
            });

            modelBuilder.Entity<UserRole>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Ignore(x => x.User);
                builder.Ignore(x => x.Role);
            });
        }
    }
}
