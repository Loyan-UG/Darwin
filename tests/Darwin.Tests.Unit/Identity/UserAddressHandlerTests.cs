using System;
using System.Threading.Tasks;
using Darwin.Application;
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
/// Unit tests for user address command handlers:
/// <see cref="CreateUserAddressHandler"/>, <see cref="UpdateUserAddressHandler"/>,
/// <see cref="SoftDeleteUserAddressHandler"/>, and <see cref="SetDefaultUserAddressHandler"/>.
/// </summary>
public sealed class UserAddressHandlerTests
{
    // ─── CreateUserAddressHandler ─────────────────────────────────────────────

    [Fact]
    public async Task CreateUserAddress_Should_PersistAddress_WhenUserExists()
    {
        await using var db = AddressTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<User>().Add(CreateUser(userId));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateUserAddressHandler(db, new AddressCreateValidator(), new TestLocalizer());

        var result = await handler.HandleAsync(new AddressCreateDto
        {
            UserId = userId,
            FullName = "Max Mustermann",
            Street1 = "Hauptstraße 1",
            PostalCode = "37154",
            City = "Northeim",
            CountryCode = "DE"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);

        var address = await db.Set<Address>().SingleAsync(TestContext.Current.CancellationToken);
        address.FullName.Should().Be("Max Mustermann");
        address.UserId.Should().Be(userId);
        address.CountryCode.Should().Be("DE");
    }

    [Fact]
    public async Task CreateUserAddress_Should_Fail_WhenUserNotFound()
    {
        await using var db = AddressTestDbContext.Create();
        var handler = new CreateUserAddressHandler(db, new AddressCreateValidator(), new TestLocalizer());

        var result = await handler.HandleAsync(new AddressCreateDto
        {
            UserId = Guid.NewGuid(),
            FullName = "Unknown",
            Street1 = "Somewhere 1",
            PostalCode = "12345",
            City = "Berlin",
            CountryCode = "DE"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("UserNotFound");
    }

    [Fact]
    public async Task CreateUserAddress_Should_SetDefaultBilling_AndClearPreviousDefault()
    {
        await using var db = AddressTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<User>().Add(CreateUser(userId));

        // Pre-existing default billing address
        db.Set<Address>().Add(new Address
        {
            UserId = userId,
            FullName = "Old Default",
            Street1 = "Old Street 1",
            PostalCode = "12345",
            City = "Berlin",
            CountryCode = "DE",
            IsDefaultBilling = true
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateUserAddressHandler(db, new AddressCreateValidator(), new TestLocalizer());

        var result = await handler.HandleAsync(new AddressCreateDto
        {
            UserId = userId,
            FullName = "New Default",
            Street1 = "New Street 2",
            PostalCode = "54321",
            City = "Munich",
            CountryCode = "DE",
            IsDefaultBilling = true
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();

        var all = await db.Set<Address>().ToListAsync(TestContext.Current.CancellationToken);
        all.Count(a => a.IsDefaultBilling).Should().Be(1, "only one address should be default billing");
        all.Single(a => a.IsDefaultBilling).FullName.Should().Be("New Default");
    }

    [Fact]
    public async Task CreateUserAddress_Should_SetDefaultShipping_AndClearPreviousDefault()
    {
        await using var db = AddressTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<User>().Add(CreateUser(userId));

        db.Set<Address>().Add(new Address
        {
            UserId = userId,
            FullName = "Old Shipping",
            Street1 = "Old Street 1",
            PostalCode = "12345",
            City = "Berlin",
            CountryCode = "DE",
            IsDefaultShipping = true
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateUserAddressHandler(db, new AddressCreateValidator(), new TestLocalizer());

        await handler.HandleAsync(new AddressCreateDto
        {
            UserId = userId,
            FullName = "New Shipping",
            Street1 = "New Street 2",
            PostalCode = "54321",
            City = "Munich",
            CountryCode = "DE",
            IsDefaultShipping = true
        }, TestContext.Current.CancellationToken);

        var all = await db.Set<Address>().ToListAsync(TestContext.Current.CancellationToken);
        all.Count(a => a.IsDefaultShipping).Should().Be(1);
        all.Single(a => a.IsDefaultShipping).FullName.Should().Be("New Shipping");
    }

    // ─── UpdateUserAddressHandler ─────────────────────────────────────────────

    [Fact]
    public async Task UpdateUserAddress_Should_UpdateFields_WhenAddressExists()
    {
        await using var db = AddressTestDbContext.Create();
        var address = new Address
        {
            FullName = "Original Name",
            Street1 = "Original Street 1",
            PostalCode = "11111",
            City = "Hamburg",
            CountryCode = "DE",
            RowVersion = new byte[] { 1, 2, 3 }
        };
        db.Set<Address>().Add(address);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateUserAddressHandler(db, new AddressEditValidator(), new TestLocalizer());

        var result = await handler.HandleAsync(new AddressEditDto
        {
            Id = address.Id,
            RowVersion = address.RowVersion,
            FullName = "Updated Name",
            Street1 = "Updated Street 5",
            PostalCode = "22222",
            City = "Cologne",
            CountryCode = "DE"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();

        var updated = await db.Set<Address>().SingleAsync(TestContext.Current.CancellationToken);
        updated.FullName.Should().Be("Updated Name");
        updated.Street1.Should().Be("Updated Street 5");
        updated.City.Should().Be("Cologne");
    }

    [Fact]
    public async Task UpdateUserAddress_Should_Fail_WhenAddressNotFound()
    {
        await using var db = AddressTestDbContext.Create();
        var handler = new UpdateUserAddressHandler(db, new AddressEditValidator(), new TestLocalizer());

        var result = await handler.HandleAsync(new AddressEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1 },
            FullName = "X",
            Street1 = "X St",
            PostalCode = "12345",
            City = "Berlin",
            CountryCode = "DE"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("AddressNotFound");
    }

    [Fact]
    public async Task UpdateUserAddress_Should_Fail_WhenConcurrencyConflict()
    {
        await using var db = AddressTestDbContext.Create();
        var address = new Address
        {
            FullName = "Conflict",
            Street1 = "Street 1",
            PostalCode = "12345",
            City = "Berlin",
            CountryCode = "DE",
            RowVersion = new byte[] { 1, 2, 3 }
        };
        db.Set<Address>().Add(address);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateUserAddressHandler(db, new AddressEditValidator(), new TestLocalizer());

        var result = await handler.HandleAsync(new AddressEditDto
        {
            Id = address.Id,
            RowVersion = new byte[] { 99, 88, 77 }, // stale version
            FullName = "New Name",
            Street1 = "Street 1",
            PostalCode = "12345",
            City = "Berlin",
            CountryCode = "DE"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("ConcurrencyConflict");
    }

    [Fact]
    public async Task UpdateUserAddress_Should_SetDefaultBilling_AndClearPreviousDefault()
    {
        await using var db = AddressTestDbContext.Create();
        var userId = Guid.NewGuid();

        var oldDefault = new Address
        {
            UserId = userId,
            FullName = "Old Default",
            Street1 = "Old St 1",
            PostalCode = "11111",
            City = "Berlin",
            CountryCode = "DE",
            IsDefaultBilling = true,
            RowVersion = new byte[] { 1 }
        };
        var target = new Address
        {
            UserId = userId,
            FullName = "Target",
            Street1 = "Target St 1",
            PostalCode = "22222",
            City = "Munich",
            CountryCode = "DE",
            IsDefaultBilling = false,
            RowVersion = new byte[] { 2 }
        };
        db.Set<Address>().AddRange(oldDefault, target);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateUserAddressHandler(db, new AddressEditValidator(), new TestLocalizer());

        var result = await handler.HandleAsync(new AddressEditDto
        {
            Id = target.Id,
            RowVersion = target.RowVersion,
            FullName = "Target Updated",
            Street1 = "Target St 1",
            PostalCode = "22222",
            City = "Munich",
            CountryCode = "DE",
            IsDefaultBilling = true
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();

        var all = await db.Set<Address>().ToListAsync(TestContext.Current.CancellationToken);
        all.Count(a => a.IsDefaultBilling).Should().Be(1);
        all.Single(a => a.IsDefaultBilling).Id.Should().Be(target.Id);
    }

    // ─── SoftDeleteUserAddressHandler ─────────────────────────────────────────

    [Fact]
    public async Task SoftDeleteUserAddress_Should_MarkAsDeleted()
    {
        await using var db = AddressTestDbContext.Create();
        var address = new Address
        {
            FullName = "Delete Me",
            Street1 = "Street 1",
            PostalCode = "12345",
            City = "Berlin",
            CountryCode = "DE",
            RowVersion = new byte[] { 1, 2, 3 }
        };
        db.Set<Address>().Add(address);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SoftDeleteUserAddressHandler(db, new AddressDeleteValidator(), new TestLocalizer());

        var result = await handler.HandleAsync(new AddressDeleteDto
        {
            Id = address.Id,
            RowVersion = address.RowVersion
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();

        var persisted = await db.Set<Address>().SingleAsync(TestContext.Current.CancellationToken);
        persisted.IsDeleted.Should().BeTrue();
        persisted.IsDefaultBilling.Should().BeFalse();
        persisted.IsDefaultShipping.Should().BeFalse();
    }

    [Fact]
    public async Task SoftDeleteUserAddress_Should_Fail_WhenAddressNotFound()
    {
        await using var db = AddressTestDbContext.Create();
        var handler = new SoftDeleteUserAddressHandler(db, new AddressDeleteValidator(), new TestLocalizer());

        var result = await handler.HandleAsync(new AddressDeleteDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1 }
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("AddressNotFound");
    }

    [Fact]
    public async Task SoftDeleteUserAddress_Should_Fail_WhenConcurrencyConflict()
    {
        await using var db = AddressTestDbContext.Create();
        var address = new Address
        {
            FullName = "Conflict",
            Street1 = "Street 1",
            PostalCode = "12345",
            City = "Berlin",
            CountryCode = "DE",
            RowVersion = new byte[] { 1, 2, 3 }
        };
        db.Set<Address>().Add(address);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SoftDeleteUserAddressHandler(db, new AddressDeleteValidator(), new TestLocalizer());

        var result = await handler.HandleAsync(new AddressDeleteDto
        {
            Id = address.Id,
            RowVersion = new byte[] { 99, 88 } // stale
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("ConcurrencyConflict");
    }

    [Fact]
    public async Task SoftDeleteUserAddress_Should_ClearDefaultFlags_WhenAddressWasDefault()
    {
        await using var db = AddressTestDbContext.Create();
        var address = new Address
        {
            FullName = "Default Address",
            Street1 = "Main St 1",
            PostalCode = "12345",
            City = "Berlin",
            CountryCode = "DE",
            IsDefaultBilling = true,
            IsDefaultShipping = true,
            RowVersion = new byte[] { 5, 6 }
        };
        db.Set<Address>().Add(address);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SoftDeleteUserAddressHandler(db, new AddressDeleteValidator(), new TestLocalizer());

        await handler.HandleAsync(new AddressDeleteDto
        {
            Id = address.Id,
            RowVersion = address.RowVersion
        }, TestContext.Current.CancellationToken);

        var persisted = await db.Set<Address>().SingleAsync(TestContext.Current.CancellationToken);
        persisted.IsDefaultBilling.Should().BeFalse();
        persisted.IsDefaultShipping.Should().BeFalse();
    }

    // ─── SetDefaultUserAddressHandler ─────────────────────────────────────────

    [Fact]
    public async Task SetDefaultUserAddress_Should_SetDefaultBilling_AndClearOthers()
    {
        await using var db = AddressTestDbContext.Create();
        var userId = Guid.NewGuid();

        var old = new Address
        {
            UserId = userId,
            FullName = "Old",
            Street1 = "St 1",
            PostalCode = "11111",
            City = "Berlin",
            CountryCode = "DE",
            IsDefaultBilling = true
        };
        var target = new Address
        {
            UserId = userId,
            FullName = "Target",
            Street1 = "St 2",
            PostalCode = "22222",
            City = "Munich",
            CountryCode = "DE",
            IsDefaultBilling = false
        };
        db.Set<Address>().AddRange(old, target);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SetDefaultUserAddressHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(userId, target.Id, asBilling: true, asShipping: false, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();

        var all = await db.Set<Address>().ToListAsync(TestContext.Current.CancellationToken);
        all.Count(a => a.IsDefaultBilling).Should().Be(1);
        all.Single(a => a.IsDefaultBilling).Id.Should().Be(target.Id);
    }

    [Fact]
    public async Task SetDefaultUserAddress_Should_Fail_WhenAddressNotOwnedByUser()
    {
        await using var db = AddressTestDbContext.Create();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var address = new Address
        {
            UserId = otherUserId,
            FullName = "Someone Else",
            Street1 = "St 1",
            PostalCode = "12345",
            City = "Berlin",
            CountryCode = "DE"
        };
        db.Set<Address>().Add(address);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SetDefaultUserAddressHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(userId, address.Id, asBilling: true, asShipping: false, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("AddressNotOwnedByUser");
    }

    [Fact]
    public async Task SetDefaultUserAddress_Should_Fail_WhenNothingToSet()
    {
        await using var db = AddressTestDbContext.Create();
        var handler = new SetDefaultUserAddressHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), asBilling: false, asShipping: false, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("NothingToSet");
    }

    [Fact]
    public async Task SetDefaultUserAddress_Should_Fail_WhenUserIdIsEmpty()
    {
        await using var db = AddressTestDbContext.Create();
        var handler = new SetDefaultUserAddressHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(Guid.Empty, Guid.NewGuid(), asBilling: true, asShipping: false, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("UserIdRequired");
    }

    [Fact]
    public async Task SetDefaultUserAddress_Should_Fail_WhenAddressIdIsEmpty()
    {
        await using var db = AddressTestDbContext.Create();
        var handler = new SetDefaultUserAddressHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(Guid.NewGuid(), Guid.Empty, asBilling: true, asShipping: false, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("AddressIdRequired");
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static User CreateUser(Guid id)
    {
        return new User("test@example.com", "hash", "stamp")
        {
            Id = id,
            FirstName = "Test",
            LastName = "User",
            IsActive = true,
            Locale = "de-DE",
            Currency = "EUR",
            Timezone = "Europe/Berlin",
            ChannelsOptInJson = "{}",
            FirstTouchUtmJson = "{}",
            LastTouchUtmJson = "{}",
            ExternalIdsJson = "{}"
        };
    }

    private sealed class TestLocalizer : IStringLocalizer<ValidationResource>
    {
        public LocalizedString this[string name] => new(name, name, false);
        public LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(name, arguments), false);
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => Array.Empty<LocalizedString>();
        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
    }

    private sealed class AddressTestDbContext : DbContext, IAppDbContext
    {
        private AddressTestDbContext(DbContextOptions<AddressTestDbContext> options) : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static AddressTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<AddressTestDbContext>()
                .UseInMemoryDatabase($"darwin_address_handler_tests_{Guid.NewGuid()}")
                .Options;
            return new AddressTestDbContext(options);
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

            modelBuilder.Entity<Address>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.RowVersion).IsRequired();
            });
        }
    }
}
