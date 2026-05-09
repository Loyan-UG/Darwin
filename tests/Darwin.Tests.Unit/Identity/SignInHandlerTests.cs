using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Security;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Identity.Auth.Commands;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Validators;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Identity;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.Identity;

public sealed class SignInHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Fail_When_EmailIsNotConfirmed()
    {
        await using var db = SignInTestDbContext.Create();
        var user = CreateUser("webadmin-unconfirmed@darwin.de");
        user.EmailConfirmed = false;

        db.Set<User>().Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SignInHandler(
            db,
            new FakeUserPasswordHasher(),
            new SignInValidator(),
            new TestStringLocalizer<ValidationResource>(),
            new AllowingAntiBotVerifier(),
            new FakeLoginRateLimiter(),
            new FixedClock(new DateTime(2030, 1, 1, 8, 0, 0, DateTimeKind.Utc)));

        var result = await handler.HandleAsync(new SignInDto
        {
            Email = user.Email,
            Password = "Password123!",
            ClientIpAddress = "127.0.0.1",
            UserAgent = "unit-test"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.RequiresTwoFactor.Should().BeFalse();
        result.FailureReason.Should().Be("EmailAddressNotConfirmed");
        result.UserId.Should().BeNull();
    }

    private static User CreateUser(string email)
    {
        return new User(email, "hashed-password", "security-stamp")
        {
            FirstName = "Mila",
            LastName = "Wagner",
            IsActive = true,
            EmailConfirmed = true,
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

    private sealed class FakeUserPasswordHasher : IUserPasswordHasher
    {
        public string Hash(string password) => "hashed-password";

        public bool Verify(string hashedPassword, string password)
            => string.Equals(hashedPassword, "hashed-password", StringComparison.Ordinal) &&
               string.Equals(password, "Password123!", StringComparison.Ordinal);
    }

    private sealed class AllowingAntiBotVerifier : IAuthAntiBotVerifier
    {
        public Task<AuthAntiBotVerificationResult> VerifyAsync(AuthAntiBotCheck check, CancellationToken ct = default)
            => Task.FromResult(AuthAntiBotVerificationResult.Success());
    }

    private sealed class FakeLoginRateLimiter : ILoginRateLimiter
    {
        public Task<bool> IsAllowedAsync(string key, int maxAttempts, int windowSeconds, CancellationToken ct = default)
            => Task.FromResult(true);

        public Task RecordAsync(string key, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }

    private sealed class TestStringLocalizer<TResource> : IStringLocalizer<TResource>
    {
        public LocalizedString this[string name] => new(name, name, resourceNotFound: false);

        public LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(name, arguments), resourceNotFound: false);

        public System.Collections.Generic.IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            Array.Empty<LocalizedString>();

        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
    }

    private sealed class SignInTestDbContext : DbContext, IAppDbContext
    {
        private SignInTestDbContext(DbContextOptions<SignInTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static SignInTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<SignInTestDbContext>()
                .UseInMemoryDatabase($"darwin_sign_in_tests_{Guid.NewGuid()}")
                .Options;

            return new SignInTestDbContext(options);
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
