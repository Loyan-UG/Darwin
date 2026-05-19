using System;
using Darwin.Application;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Businesses.Commands;
using Darwin.Application.Businesses.DTOs;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Integration;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.Businesses;

public sealed class BusinessOperationsRowVersionTests
{
    [Fact]
    public async Task ProvisionBusinessOnboarding_Should_Reject_WhenRowVersionIsMissing()
    {
        await using var db = BusinessOperationsRowVersionTestDbContext.Create();
        var businessId = Guid.NewGuid();

        db.Set<Business>().Add(new Business
        {
            Id = businessId,
            Name = "Aurora Bakery",
            DefaultCurrency = "EUR",
            DefaultCulture = "de-DE",
            DefaultTimeZoneId = "Europe/Berlin",
            RowVersion = [1, 2, 3]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ProvisionBusinessOnboardingHandler(
            db,
            new FixedClock(new DateTime(2030, 1, 1, 10, 0, 0, DateTimeKind.Utc)),
            new TestStringLocalizer());

        var result = await handler.HandleAsync(new BusinessLifecycleActionDto
        {
            Id = businessId,
            RowVersion = []
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("RowVersionRequired");
    }

    [Fact]
    public async Task ProvisionBusinessOnboarding_Should_Reject_WhenRowVersionIsStale()
    {
        await using var db = BusinessOperationsRowVersionTestDbContext.Create();
        var businessId = Guid.NewGuid();

        db.Set<Business>().Add(new Business
        {
            Id = businessId,
            Name = "Aurora Bakery",
            DefaultCurrency = "EUR",
            DefaultCulture = "de-DE",
            DefaultTimeZoneId = "Europe/Berlin",
            RowVersion = [1, 2, 3]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ProvisionBusinessOnboardingHandler(
            db,
            new FixedClock(new DateTime(2030, 1, 1, 10, 0, 0, DateTimeKind.Utc)),
            new TestStringLocalizer());

        var result = await handler.HandleAsync(new BusinessLifecycleActionDto
        {
            Id = businessId,
            RowVersion = [9, 9, 9]
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("ConcurrencyConflictDetected");
    }

    [Fact]
    public async Task ProvisionBusinessOnboarding_Should_Reject_WhenStoredRowVersionIsNull()
    {
        await using var db = BusinessOperationsRowVersionTestDbContext.Create();
        var businessId = Guid.NewGuid();

        db.Set<Business>().Add(new Business
        {
            Id = businessId,
            Name = "Aurora Bakery",
            DefaultCurrency = "EUR",
            DefaultCulture = "de-DE",
            DefaultTimeZoneId = "Europe/Berlin",
            RowVersion = null!
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ProvisionBusinessOnboardingHandler(
            db,
            new FixedClock(new DateTime(2030, 1, 1, 10, 0, 0, DateTimeKind.Utc)),
            new TestStringLocalizer());

        var result = await handler.HandleAsync(new BusinessLifecycleActionDto
        {
            Id = businessId,
            RowVersion = [1]
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("ConcurrencyConflictDetected");
    }

    [Fact]
    public async Task UpdateProviderCallbackInboxMessage_Should_Reject_WhenRowVersionIsMissing()
    {
        await using var db = BusinessOperationsRowVersionTestDbContext.Create();
        var messageId = Guid.NewGuid();

        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Id = messageId,
            Provider = "Stripe",
            CallbackType = "event",
            PayloadJson = "{\"id\":\"evt-1\"}",
            Status = "Pending",
            RowVersion = [1, 2, 3]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateProviderCallbackInboxMessageHandler(
            db,
            new FixedClock(new DateTime(2030, 1, 1, 10, 0, 0, DateTimeKind.Utc)),
            new TestStringLocalizer());

        var result = await handler.HandleAsync(new UpdateProviderCallbackInboxMessageDto
        {
            Id = messageId,
            RowVersion = []
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("RowVersionRequired");
    }

    [Fact]
    public async Task UpdateProviderCallbackInboxMessage_Should_Reject_WhenRowVersionIsStale()
    {
        await using var db = BusinessOperationsRowVersionTestDbContext.Create();
        var messageId = Guid.NewGuid();

        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Id = messageId,
            Provider = "Stripe",
            CallbackType = "event",
            PayloadJson = "{\"id\":\"evt-1\"}",
            Status = "Pending",
            RowVersion = [1, 2, 3]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateProviderCallbackInboxMessageHandler(
            db,
            new FixedClock(new DateTime(2030, 1, 1, 10, 0, 0, DateTimeKind.Utc)),
            new TestStringLocalizer());

        var result = await handler.HandleAsync(new UpdateProviderCallbackInboxMessageDto
        {
            Id = messageId,
            RowVersion = [9, 9, 9],
            Action = "MarkProcessed"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("ItemConcurrencyConflict");
    }

    [Fact]
    public async Task UpdateProviderCallbackInboxMessage_Should_Reject_WhenStoredRowVersionIsNull()
    {
        await using var db = BusinessOperationsRowVersionTestDbContext.Create();
        var messageId = Guid.NewGuid();

        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Id = messageId,
            Provider = "Stripe",
            CallbackType = "event",
            PayloadJson = "{\"id\":\"evt-1\"}",
            Status = "Pending",
            RowVersion = null!
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateProviderCallbackInboxMessageHandler(
            db,
            new FixedClock(new DateTime(2030, 1, 1, 10, 0, 0, DateTimeKind.Utc)),
            new TestStringLocalizer());

        var result = await handler.HandleAsync(new UpdateProviderCallbackInboxMessageDto
        {
            Id = messageId,
            RowVersion = [1],
            Action = "MarkFailed"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("ItemConcurrencyConflict");
    }

    [Fact]
    public async Task CancelCommunicationDispatchOperation_Should_Reject_WhenRowVersionIsMissing()
    {
        await using var db = BusinessOperationsRowVersionTestDbContext.Create();
        var operationId = Guid.NewGuid();

        db.Set<EmailDispatchOperation>().Add(new EmailDispatchOperation
        {
            Id = operationId,
            Provider = "SMTP",
            RecipientEmail = "ops@example.de",
            Subject = "Subject",
            HtmlBody = "<p>Hi</p>",
            RowVersion = [1, 2, 3]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CancelCommunicationDispatchOperationHandler(
            db,
            new FixedClock(new DateTime(2030, 1, 1, 10, 0, 0, DateTimeKind.Utc)),
            new TestStringLocalizer());

        var result = await handler.HandleAsync(new CancelCommunicationDispatchOperationDto
        {
            Id = operationId,
            Channel = "Email",
            RowVersion = []
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("RowVersionRequired");
    }

    [Fact]
    public async Task CancelCommunicationDispatchOperation_Should_Reject_Email_WhenRowVersionIsStale()
    {
        await using var db = BusinessOperationsRowVersionTestDbContext.Create();
        var operationId = Guid.NewGuid();

        db.Set<EmailDispatchOperation>().Add(new EmailDispatchOperation
        {
            Id = operationId,
            Provider = "SMTP",
            RecipientEmail = "ops@example.de",
            Subject = "Subject",
            HtmlBody = "<p>Hi</p>",
            RowVersion = [1, 2, 3]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CancelCommunicationDispatchOperationHandler(
            db,
            new FixedClock(new DateTime(2030, 1, 1, 10, 0, 0, DateTimeKind.Utc)),
            new TestStringLocalizer());

        var result = await handler.HandleAsync(new CancelCommunicationDispatchOperationDto
        {
            Id = operationId,
            Channel = "Email",
            RowVersion = [9, 9, 9]
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("ItemConcurrencyConflict");
    }

    [Fact]
    public async Task CancelCommunicationDispatchOperation_Should_Reject_Channel_WhenRowVersionIsStale()
    {
        await using var db = BusinessOperationsRowVersionTestDbContext.Create();
        var operationId = Guid.NewGuid();

        db.Set<ChannelDispatchOperation>().Add(new ChannelDispatchOperation
        {
            Id = operationId,
            Channel = "SMS",
            Provider = "Twilio",
            RecipientAddress = "+49123456789",
            MessageText = "Welcome",
            RowVersion = [1, 2, 3]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CancelCommunicationDispatchOperationHandler(
            db,
            new FixedClock(new DateTime(2030, 1, 1, 10, 0, 0, DateTimeKind.Utc)),
            new TestStringLocalizer());

        var result = await handler.HandleAsync(new CancelCommunicationDispatchOperationDto
        {
            Id = operationId,
            Channel = "SMS",
            RowVersion = [9, 9, 9]
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("ItemConcurrencyConflict");
    }

    [Fact]
    public async Task CancelCommunicationDispatchOperation_Should_Reject_WhenStoredRowVersionIsNull()
    {
        await using var db = BusinessOperationsRowVersionTestDbContext.Create();
        var operationId = Guid.NewGuid();

        db.Set<ChannelDispatchOperation>().Add(new ChannelDispatchOperation
        {
            Id = operationId,
            Channel = "SMS",
            Provider = "Twilio",
            RecipientAddress = "+49123456789",
            MessageText = "Welcome",
            RowVersion = null!
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CancelCommunicationDispatchOperationHandler(
            db,
            new FixedClock(new DateTime(2030, 1, 1, 10, 0, 0, DateTimeKind.Utc)),
            new TestStringLocalizer());

        var result = await handler.HandleAsync(new CancelCommunicationDispatchOperationDto
        {
            Id = operationId,
            Channel = "SMS",
            RowVersion = [1]
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("ItemConcurrencyConflict");
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }

    private sealed class TestStringLocalizer : IStringLocalizer<ValidationResource>
    {
        public LocalizedString this[string name] => new(name, name, resourceNotFound: false);
        public LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(name, arguments), resourceNotFound: false);
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
    }

    private sealed class BusinessOperationsRowVersionTestDbContext : DbContext, IAppDbContext
    {
        private BusinessOperationsRowVersionTestDbContext(DbContextOptions<BusinessOperationsRowVersionTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static BusinessOperationsRowVersionTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<BusinessOperationsRowVersionTestDbContext>()
                .UseInMemoryDatabase($"darwin_business_operations_rowversion_tests_{Guid.NewGuid()}")
                .Options;

            return new BusinessOperationsRowVersionTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Business>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Name).IsRequired();
                builder.Property(x => x.DefaultCurrency).IsRequired();
                builder.Property(x => x.DefaultCulture).IsRequired();
                builder.Property(x => x.DefaultTimeZoneId).IsRequired();
                builder.Ignore(x => x.Members);
                builder.Ignore(x => x.Locations);
                builder.Ignore(x => x.Favorites);
                builder.Ignore(x => x.Likes);
                builder.Ignore(x => x.Reviews);
                builder.Ignore(x => x.EngagementStats);
                builder.Ignore(x => x.Invitations);
                builder.Ignore(x => x.StaffQrCodes);
                builder.Ignore(x => x.Subscriptions);
                builder.Ignore(x => x.AnalyticsExportJobs);
            });

            modelBuilder.Entity<ProviderCallbackInboxMessage>(builder =>
            {
                builder.HasKey(x => x.Id);
            });

            modelBuilder.Entity<EmailDispatchOperation>(builder =>
            {
                builder.HasKey(x => x.Id);
            });

            modelBuilder.Entity<ChannelDispatchOperation>(builder =>
            {
                builder.HasKey(x => x.Id);
            });
        }
    }
}
