using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Notifications;
using Darwin.Application;
using Darwin.Domain.Entities.Integration;
using Darwin.Shared.Results;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Moq;

namespace Darwin.WebApi.Tests.Services;

public sealed class ProcessBrevoTransactionalEmailWebhookHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_ReturnFail_WhenPayloadIsInvalid()
    {
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(DateTime.UtcNow), localizer.Object);

        var result = await handler.HandleAsync("{", TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("BrevoWebhookPayloadInvalid");
    }

    [Fact]
    public async Task HandleAsync_Should_RejectNullPayload()
    {
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(DateTime.UtcNow), localizer.Object);

        var payload = (string)null!;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("BrevoWebhookPayloadInvalid");
    }

    [Fact]
    public async Task HandleAsync_Should_RejectWhitespacePayload()
    {
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(DateTime.UtcNow), localizer.Object);

        var payload = "   \t \r\n";

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("BrevoWebhookPayloadInvalid");
    }

    [Fact]
    public async Task HandleAsync_Should_RejectEmptyEventText()
    {
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-empty-event",
            CorrelationKey = "corr-empty-event",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = DateTime.UtcNow,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(DateTime.UtcNow), localizer.Object);

        var payload = "{" +
            "\"event\": \"   \"," +
            "\"message-id\": \"msg-empty-event\"," +
            "\"ts\": \"1700000000\"}";

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("BrevoWebhookPayloadInvalid");
    }

    [Fact]
    public async Task HandleAsync_Should_MatchAudit_ByCorrelationKey_WhenMessageIdMissing()
    {
        var now = DateTime.UtcNow;
        var eventAt = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-3)).ToUnixTimeSeconds()).UtcDateTime;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-ignored",
            CorrelationKey = "corr-key-1",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "Delivered",
          "X-Correlation-Key": "corr-key-1",
          "message-id": "",
          "ts": {{new DateTimeOffset(eventAt).ToUnixTimeSeconds()}}
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var audit = await db.EmailDispatchAudits.SingleAsync(x => x.Provider == "Brevo" && x.CorrelationKey == "corr-key-1", TestContext.Current.CancellationToken);
        audit.Status.Should().Be("Sent");
        audit.CompletedAtUtc.Should().Be(eventAt);
    }

    [Fact]
    public async Task HandleAsync_Should_PreferMessageId_WhenCorrelationKeyAlsoProvided()
    {
        var now = DateTime.UtcNow;
        var eventAt = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-4)).ToUnixTimeSeconds()).UtcDateTime;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "primary-msg",
            CorrelationKey = "corr-primary",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "corr-msg",
            CorrelationKey = "corr-fallback",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "request",
          "message-id": " primary-msg ",
          "X-Correlation-Key": "corr-fallback",
          "ts": {{new DateTimeOffset(eventAt).ToUnixTimeSeconds()}}
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var primary = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "primary-msg", TestContext.Current.CancellationToken);
        var fallback = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "corr-msg", TestContext.Current.CancellationToken);
        primary.Status.Should().Be("Sent");
        primary.CompletedAtUtc.Should().Be(eventAt);
        fallback.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task HandleAsync_Should_UseTsEvent_WhenTsAndTsEventProvided()
    {
        var now = DateTime.UtcNow;
        var tsEventAt = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-12)).ToUnixTimeSeconds()).UtcDateTime;
        var tsAt = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-4)).ToUnixTimeSeconds()).UtcDateTime;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-priority",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "opened",
          "message-id": "msg-priority",
          "ts": {{new DateTimeOffset(tsAt).ToUnixTimeSeconds()}},
          "ts_event": {{new DateTimeOffset(tsEventAt).ToUnixTimeSeconds()}}
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var audit = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-priority", TestContext.Current.CancellationToken);
        audit.CompletedAtUtc.Should().Be(tsEventAt);
    }

    [Fact]
    public async Task HandleAsync_Should_FallbackToClockNow_WhenTsFieldsAreInvalid()
    {
        var now = DateTime.UtcNow;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-fallback-now",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = """
        {
          "event": "request",
          "message-id": "msg-fallback-now",
          "ts": "bad-ts",
          "ts_epoch": "bad",
          "date": "invalid"
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var audit = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-fallback-now", TestContext.Current.CancellationToken);
        audit.CompletedAtUtc.Should().Be(now);
    }

    [Fact]
    public async Task HandleAsync_Should_MatchNewestAudit_ByEmailAndSubject_WhenMessageIdAndCorrelationMissing()
    {
        var now = DateTime.UtcNow;
        var eventAt = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-1)).ToUnixTimeSeconds()).UtcDateTime;
        var recentAttempt = now.AddDays(-2);
        var oldAttempt = now.AddDays(-8);
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-old",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = oldAttempt,
            Status = "Pending"
        });
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-new",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = recentAttempt,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": " opened ",
          "email": "user@example.com",
          "subject": "Welcome",
          "ts": {{new DateTimeOffset(eventAt).ToUnixTimeSeconds()}}
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var updated = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-new", TestContext.Current.CancellationToken);
        updated.Status.Should().Be("Sent");
        updated.CompletedAtUtc.Should().Be(eventAt);
        var stale = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-old", TestContext.Current.CancellationToken);
        stale.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task HandleAsync_Should_NotMatchAudit_WhenOnlyOldEventsMatchEmailAndSubject()
    {
        var now = DateTime.UtcNow;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-old",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now.AddDays(-8),
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "opened",
          "email": "user@example.com",
          "subject": "Welcome"
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var audit = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-old", TestContext.Current.CancellationToken);
        audit.Status.Should().Be("Pending");
        audit.CompletedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_Should_RecordFailureMessage_WhenFailureEventOccurs()
    {
        var now = DateTime.UtcNow;
        var eventAt = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-6)).ToUnixTimeSeconds()).UtcDateTime;
        var original = "already failed before";
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-failure",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending",
            FailureMessage = original
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "hard_bounce",
          "message-id": "msg-failure",
          "reason": "  recipient not found  ",
          "ts": {{new DateTimeOffset(eventAt).ToUnixTimeSeconds()}}
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var audit = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-failure", TestContext.Current.CancellationToken);
        audit.Status.Should().Be("Failed");
        audit.CompletedAtUtc.Should().Be(eventAt);
        audit.FailureMessage.Should().Be("Brevo event 'hard_bounce': recipient not found");
    }

    [Fact]
    public async Task HandleAsync_Should_RecordDefaultFailureMessage_WhenFailureReasonIsMissing()
    {
        var now = DateTime.UtcNow;
        var eventAt = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-6)).ToUnixTimeSeconds()).UtcDateTime;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-failure-missing-reason",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "hard_bounce",
          "message-id": "msg-failure-missing-reason",
          "ts": {{new DateTimeOffset(eventAt).ToUnixTimeSeconds()}}
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var audit = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-failure-missing-reason", TestContext.Current.CancellationToken);
        audit.Status.Should().Be("Failed");
        audit.CompletedAtUtc.Should().Be(eventAt);
        audit.FailureMessage.Should().Be("Brevo event 'hard_bounce': No provider reason supplied.");
    }

    [Fact]
    public async Task HandleAsync_Should_RecordFailureMessage_WhenFailureEventIsUppercase()
    {
        var now = DateTime.UtcNow;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-failure-upper",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "HARD_BOUNCE",
          "message-id": "msg-failure-upper",
          "reason": "  SMTP bounce  ",
          "ts": 1700000000
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var audit = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-failure-upper", TestContext.Current.CancellationToken);
        audit.Status.Should().Be("Failed");
        audit.FailureMessage.Should().Be("Brevo event 'hard_bounce': SMTP bounce");
        audit.CompletedAtUtc.Should().Be(new DateTimeOffset(2023, 11, 14, 22, 13, 20, TimeSpan.Zero).UtcDateTime);
    }

    [Fact]
    public async Task HandleAsync_Should_DefaultFailureMessage_WhenFailureReasonIsWhitespace()
    {
        var now = DateTime.UtcNow;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-failure-whitespace",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "soft_bounce",
          "message-id": "msg-failure-whitespace",
          "reason": "   \t\n  ",
          "ts": 1700000000
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var audit = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-failure-whitespace", TestContext.Current.CancellationToken);
        audit.Status.Should().Be("Failed");
        audit.FailureMessage.Should().Be("Brevo event 'soft_bounce': No provider reason supplied.");
    }

    [Fact]
    public async Task HandleAsync_Should_TrimFailureMessageToTwoThousandCharacters()
    {
        var now = DateTime.UtcNow;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-failure-truncate",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var longReason = new string('r', 3_000);
        var payload = $$"""
        {
          "event": "hard_bounce",
          "message-id": "msg-failure-truncate",
          "reason": "{{longReason}}",
          "ts": 1700000000
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var audit = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-failure-truncate", TestContext.Current.CancellationToken);
        var expected = $"Brevo event 'hard_bounce': {longReason}";
        var expectedClamped = expected.Length <= 2000 ? expected : expected[..2000];

        audit.Status.Should().Be("Failed");
        audit.FailureMessage.Should().Be(expectedClamped);
        audit.FailureMessage!.Length.Should().Be(2000);
    }

    [Fact]
    public async Task HandleAsync_Should_NotAlterAudit_WhenEventIsUnsupported()
    {
        var now = DateTime.UtcNow;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-unsupported-event",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Sent",
            CompletedAtUtc = now.AddMinutes(-20)
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "unsupported_event",
          "message-id": "msg-unsupported-event",
          "ts": 1700000000
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var audit = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-unsupported-event", TestContext.Current.CancellationToken);
        audit.Status.Should().Be("Sent");
        audit.CompletedAtUtc.Should().Be(now.AddMinutes(-20));
        audit.FailureMessage.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_Should_UseDateField_WhenOtherTimestampFieldsAreInvalid()
    {
        var now = DateTime.UtcNow;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-date-field",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var date = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-9)).ToUnixTimeSeconds()).UtcDateTime;
        var payload = $$"""
        {
          "event": "request",
          "message-id": "msg-date-field",
          "ts": "invalid-ts",
          "ts_event": {},
          "ts_epoch": "bad-milliseconds",
          "date": "{{new DateTimeOffset(date).ToUnixTimeSeconds()}}"
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var audit = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-date-field", TestContext.Current.CancellationToken);
        audit.Status.Should().Be("Sent");
        audit.CompletedAtUtc.Should().Be(date);
    }

    [Fact]
    public async Task HandleAsync_Should_MatchAudit_OnEmailSubject_WhenDateExactlySevenDaysOld()
    {
        var now = DateTime.UtcNow;
        var cutoffDate = now.AddDays(-7);
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-old-edge",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = cutoffDate.AddSeconds(-1),
            Status = "Pending"
        });
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-boundary",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = cutoffDate,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "opened",
          "email": "user@example.com",
          "subject": "Welcome"
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var matched = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-boundary", TestContext.Current.CancellationToken);
        var outside = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-old-edge", TestContext.Current.CancellationToken);

        matched.Status.Should().Be("Sent");
        outside.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task HandleAsync_Should_FallbackToEmailSubject_WhenMessageIdAndCorrelationAreWhitespace()
    {
        var now = DateTime.UtcNow;
        var eventAt = now.AddMinutes(-11);
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-secondary",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now.AddMinutes(-1),
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "opened",
          "message-id": "   ",
          "X-Correlation-Key": " \t",
          "email": "user@example.com",
          "subject": "Welcome",
          "ts": {{new DateTimeOffset(eventAt).ToUnixTimeSeconds()}}
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var matched = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-secondary", TestContext.Current.CancellationToken);
        matched.Status.Should().Be("Sent");
        matched.CompletedAtUtc.Should().Be(DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(eventAt).ToUnixTimeSeconds()).UtcDateTime);
    }

    [Fact]
    public async Task HandleAsync_Should_ReplaceFailureMessage_WhenAlreadyFailed()
    {
        var now = DateTime.UtcNow;
        var nextFailureAt = now.AddMinutes(-4);
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-failure-update",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Failed",
            FailureMessage = "Brevo event 'hard_bounce': old reason",
            CompletedAtUtc = now.AddMinutes(-20)
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "blocked",
          "message-id": "msg-failure-update",
          "reason": "mailbox unavailable",
          "ts": {{new DateTimeOffset(nextFailureAt).ToUnixTimeSeconds()}}
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var updated = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-failure-update", TestContext.Current.CancellationToken);
        updated.Status.Should().Be("Failed");
        updated.FailureMessage.Should().Be("Brevo event 'blocked': mailbox unavailable");
        updated.CompletedAtUtc.Should().Be(DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(nextFailureAt).ToUnixTimeSeconds()).UtcDateTime);
    }

    [Fact]
    public async Task HandleAsync_Should_MatchByIntendedRecipientEmail_FallbackPath()
    {
        var now = DateTime.UtcNow;
        var eventAt = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-3)).ToUnixTimeSeconds()).UtcDateTime;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-intended",
            RecipientEmail = "other@example.com",
            IntendedRecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "opened",
          "message-id": "",
          "email": "user@example.com",
          "subject": "Welcome",
          "ts": {{new DateTimeOffset(eventAt).ToUnixTimeSeconds()}}
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var matched = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-intended", TestContext.Current.CancellationToken);
        matched.Status.Should().Be("Sent");
        matched.CompletedAtUtc.Should().Be(eventAt);
    }

    [Fact]
    public async Task HandleAsync_Should_PreferTsEvent_WhenTsEventIsNumeric()
    {
        var now = DateTime.UtcNow;
        var tsAt = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-7)).ToUnixTimeSeconds()).UtcDateTime;
        var tsEventAt = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-2)).ToUnixTimeSeconds()).UtcDateTime;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-ts-prefer",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "delivered",
          "message-id": "msg-ts-prefer",
          "ts": {{new DateTimeOffset(tsAt).ToUnixTimeSeconds()}},
          "ts_event": {{new DateTimeOffset(tsEventAt).ToUnixTimeSeconds()}}
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var matched = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-ts-prefer", TestContext.Current.CancellationToken);
        matched.CompletedAtUtc.Should().Be(tsEventAt);
    }

    [Fact]
    public async Task HandleAsync_Should_AcceptTimestampFromString()
    {
        var now = DateTime.UtcNow;
        var eventAt = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-13)).ToUnixTimeSeconds()).UtcDateTime;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-string-ts",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "delivered",
          "message-id": "msg-string-ts",
          "ts": "{{new DateTimeOffset(eventAt).ToUnixTimeSeconds()}}"
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var matched = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-string-ts", TestContext.Current.CancellationToken);
        matched.Status.Should().Be("Sent");
        matched.CompletedAtUtc.Should().Be(eventAt);
    }

    [Fact]
    public async Task HandleAsync_Should_RejectPayloadWhenEventIsNotAString()
    {
        var now = DateTime.UtcNow;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-non-string-event",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = """
        {
          "event": 123,
          "message-id": "msg-non-string-event"
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("BrevoWebhookPayloadInvalid");
    }

    [Fact]
    public async Task HandleAsync_Should_DoNothing_WhenNoAuditCanBeMatched()
    {
        var now = DateTime.UtcNow;
        var originalCompletedAt = now.AddMinutes(-20);
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-existing",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Failed",
            FailureMessage = "already",
            CompletedAtUtc = originalCompletedAt
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "opened",
          "message-id": "msg-non-existent",
          "email": "user@example.com",
          "subject": "Welcome"
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var audit = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-existing", TestContext.Current.CancellationToken);
        audit.Status.Should().Be("Failed");
        audit.FailureMessage.Should().Be("already");
        audit.CompletedAtUtc.Should().Be(originalCompletedAt);
    }

    [Fact]
    public async Task HandleAsync_Should_NotMatchNonBrevoProviderRows()
    {
        var now = DateTime.UtcNow;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "SMTP",
            ProviderMessageId = "msg-cross-provider",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-brevo-other",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "delivered",
          "message-id": "msg-cross-provider"
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var smtp = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-cross-provider", TestContext.Current.CancellationToken);
        var brevo = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-brevo-other", TestContext.Current.CancellationToken);
        smtp.Status.Should().Be("Pending");
        smtp.CompletedAtUtc.Should().BeNull();
        brevo.Status.Should().Be("Pending");
        brevo.CompletedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_Should_UseTsEpochMilliseconds_WhenDateIsUnavailable()
    {
        var now = DateTime.UtcNow;
        var eventAt = new DateTimeOffset(now.AddMinutes(-4)).ToUnixTimeMilliseconds();
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-ts-epoch",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "opened",
          "message-id": "msg-ts-epoch",
          "ts": "invalid-ts",
          "ts_event": "invalid-ts-event",
          "ts_epoch": {{eventAt}}
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var matched = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-ts-epoch", TestContext.Current.CancellationToken);
        matched.Status.Should().Be("Sent");
        matched.CompletedAtUtc.Should().Be(DateTimeOffset.FromUnixTimeMilliseconds(eventAt).UtcDateTime);
    }

    [Fact]
    public async Task HandleAsync_Should_IgnoreDeletedAudits_WhenSearchingMatches()
    {
        var now = DateTime.UtcNow;
        var eventAt = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-4)).ToUnixTimeSeconds()).UtcDateTime;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-deleted",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending",
            IsDeleted = true
        });
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-valid",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "delivered",
          "message-id": "msg-valid",
          "ts": {{new DateTimeOffset(eventAt).ToUnixTimeSeconds()}}
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var invalid = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-deleted", TestContext.Current.CancellationToken);
        var valid = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-valid", TestContext.Current.CancellationToken);

        invalid.Status.Should().Be("Pending");
        invalid.CompletedAtUtc.Should().BeNull();
        valid.Status.Should().Be("Sent");
        valid.CompletedAtUtc.Should().Be(eventAt);
    }

    [Fact]
    public async Task HandleAsync_Should_NotOverwriteFailedStatus_WhenSuccessEventArrives()
    {
        var now = DateTime.UtcNow;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-failed-no-override",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Failed",
            FailureMessage = "already failed",
            CompletedAtUtc = now.AddMinutes(-10)
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "opened",
          "message-id": "msg-failed-no-override",
          "ts": 1700000000
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var audit = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-failed-no-override", TestContext.Current.CancellationToken);
        audit.Status.Should().Be("Failed");
        audit.FailureMessage.Should().Be("already failed");
        audit.CompletedAtUtc.Should().Be(now.AddMinutes(-10));
    }

    [Fact]
    public async Task HandleAsync_Should_ParseTsEpochWhenProvidedAsString()
    {
        var now = DateTime.UtcNow;
        var epochMilliseconds = new DateTimeOffset(now.AddMinutes(-6)).ToUnixTimeMilliseconds().ToString();
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-ts-epoch-string",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "delivered",
          "message-id": "msg-ts-epoch-string",
          "ts_epoch": "{{epochMilliseconds}}"
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var matched = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-ts-epoch-string", TestContext.Current.CancellationToken);
        var expected = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(epochMilliseconds)).UtcDateTime;
        matched.Status.Should().Be("Sent");
        matched.CompletedAtUtc.Should().Be(expected);
    }

    [Fact]
    public async Task HandleAsync_Should_FallbackToClock_WhenTimestampIsDecimal()
    {
        var now = DateTime.UtcNow;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-decimal-ts",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = """
        {
          "event": "opened",
          "message-id": "msg-decimal-ts",
          "ts": 1700000000.123
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var matched = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-decimal-ts", TestContext.Current.CancellationToken);
        matched.Status.Should().Be("Sent");
        matched.CompletedAtUtc.Should().Be(now);
    }

    [Fact]
    public async Task HandleAsync_Should_RejectArrayPayload()
    {
        var now = DateTime.UtcNow;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = """
        [
          {"event":"delivered","message-id":"ignored"}
        ]
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("BrevoWebhookPayloadInvalid");
    }

    [Fact]
    public async Task HandleAsync_Should_UseTs_WhenTsEventIsInvalidString()
    {
        var now = DateTime.UtcNow;
        var eventAt = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-5)).ToUnixTimeSeconds()).UtcDateTime;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-ts-precedence",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "delivered",
          "message-id": "msg-ts-precedence",
          "ts_event": "not-a-number",
          "ts": {{new DateTimeOffset(eventAt).ToUnixTimeSeconds()}}
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var matched = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-ts-precedence", TestContext.Current.CancellationToken);
        matched.CompletedAtUtc.Should().Be(eventAt);
    }

    [Fact]
    public async Task HandleAsync_Should_ParseCaseInsensitiveJsonPropertyNames()
    {
        var now = DateTime.UtcNow;
        var eventAt = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-6)).ToUnixTimeSeconds()).UtcDateTime;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-case-insensitive",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "EvEnT": "delivered",
          "MeSsAgE-Id": "msg-case-insensitive",
          "tS": {{new DateTimeOffset(eventAt).ToUnixTimeSeconds()}}
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var matched = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-case-insensitive", TestContext.Current.CancellationToken);
        matched.Status.Should().Be("Sent");
        matched.CompletedAtUtc.Should().Be(eventAt);
    }

    [Fact]
    public async Task HandleAsync_Should_NormalizeDeliveryEventWithWhitespaceAndCase()
    {
        var now = DateTime.UtcNow;
        var eventAt = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-2)).ToUnixTimeSeconds()).UtcDateTime;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-delivery-norm",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "  DeLiVeReD  ",
          "message-id": "msg-delivery-norm",
          "ts": {{new DateTimeOffset(eventAt).ToUnixTimeSeconds()}}
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var matched = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-delivery-norm", TestContext.Current.CancellationToken);
        matched.Status.Should().Be("Sent");
        matched.CompletedAtUtc.Should().Be(eventAt);
    }

    [Fact]
    public async Task HandleAsync_Should_UseTsEpochString_WhenTsAndTsEventInvalid()
    {
        var now = DateTime.UtcNow;
        var expectedAt = DateTimeOffset.FromUnixTimeMilliseconds(new DateTimeOffset(now.AddMinutes(-10)).ToUnixTimeMilliseconds()).UtcDateTime;
        var epochMs = new DateTimeOffset(expectedAt).ToUnixTimeMilliseconds().ToString();
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-epoch-fallback",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "opened",
          "message-id": "msg-epoch-fallback",
          "ts": "invalid-ts",
          "ts_event": [],
          "ts_epoch": "{{epochMs}}"
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var matched = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-epoch-fallback", TestContext.Current.CancellationToken);
        matched.Status.Should().Be("Sent");
        matched.CompletedAtUtc.Should().Be(expectedAt);
    }

    [Fact]
    public async Task HandleAsync_Should_DoNothing_WhenNoMatchingKeysProvidedForFallback()
    {
        var now = DateTime.UtcNow;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-existing",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending",
            CompletedAtUtc = now.AddMinutes(-15)
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = """
        {
          "event": "opened",
          "message-id": "   ",
          "X-Correlation-Key": "",
          "ts": 1700000000
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var unchanged = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-existing", TestContext.Current.CancellationToken);
        unchanged.Status.Should().Be("Pending");
        unchanged.CompletedAtUtc.Should().Be(now.AddMinutes(-15));
    }

    [Fact]
    public async Task HandleAsync_Should_UseDateString_WhenDateOtherTimestampsMissing()
    {
        var now = DateTime.UtcNow;
        var dateSeconds = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-3)).ToUnixTimeSeconds()).ToUnixTimeSeconds().ToString();
        var expected = DateTimeOffset.FromUnixTimeSeconds(long.Parse(dateSeconds)).UtcDateTime;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-date-string",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "opened",
          "message-id": "msg-date-string",
          "date": "{{dateSeconds}}"
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var matched = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-date-string", TestContext.Current.CancellationToken);
        matched.CompletedAtUtc.Should().Be(expected);
    }

    [Fact]
    public async Task HandleAsync_Should_FallbackToClock_WhenDateIsInvalid()
    {
        var now = DateTime.UtcNow;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-invalid-date",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "opened",
          "message-id": "msg-invalid-date",
          "ts": "bad-ts",
          "ts_event": "bad-ts-event",
          "ts_epoch": "bad-ms",
          "date": ""
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var matched = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-invalid-date", TestContext.Current.CancellationToken);
        matched.CompletedAtUtc.Should().Be(now);
    }

    [Fact]
    public async Task HandleAsync_Should_UseCorrelation_WhenMessageIdIsNotAString()
    {
        var now = DateTime.UtcNow;
        var eventAt = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-8)).ToUnixTimeSeconds()).UtcDateTime;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "ignored-msg-id-type",
            CorrelationKey = "corr-non-string-msgid",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "delivered",
          "message-id": 123456,
          "X-Correlation-Key": "corr-non-string-msgid",
          "ts": {{new DateTimeOffset(eventAt).ToUnixTimeSeconds()}}
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var matched = await db.EmailDispatchAudits.SingleAsync(x => x.CorrelationKey == "corr-non-string-msgid", TestContext.Current.CancellationToken);
        matched.Status.Should().Be("Sent");
        matched.CompletedAtUtc.Should().Be(eventAt);
    }

    [Fact]
    public async Task HandleAsync_Should_UpdateMostRecentAudit_WhenDuplicateMessageIdExists()
    {
        var now = DateTime.UtcNow;
        var eventAt = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-2)).ToUnixTimeSeconds()).UtcDateTime;
        var olderAttempt = now.AddHours(-2);
        var newerAttempt = now.AddMinutes(-30);
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-duplicate",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = olderAttempt,
            Status = "Pending"
        });
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-duplicate",
            RecipientEmail = "user2@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = newerAttempt,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "opened",
          "message-id": "msg-duplicate",
          "ts": {{new DateTimeOffset(eventAt).ToUnixTimeSeconds()}}
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var old = await db.EmailDispatchAudits.SingleAsync(x => x.AttemptedAtUtc == olderAttempt && x.ProviderMessageId == "msg-duplicate", TestContext.Current.CancellationToken);
        var latest = await db.EmailDispatchAudits.SingleAsync(x => x.AttemptedAtUtc == newerAttempt && x.ProviderMessageId == "msg-duplicate", TestContext.Current.CancellationToken);
        old.Status.Should().Be("Pending");
        latest.Status.Should().Be("Sent");
        latest.CompletedAtUtc.Should().Be(eventAt);
    }

    [Fact]
    public async Task HandleAsync_Should_FallbackToMessageIdWhenCorrelationIsNotAString()
    {
        var now = DateTime.UtcNow;
        var eventAt = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-6)).ToUnixTimeSeconds()).UtcDateTime;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-corr-type",
            CorrelationKey = "corr-should-ignore",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "delivered",
          "message-id": "msg-corr-type",
          "X-Correlation-Key": 99999,
          "ts": {{new DateTimeOffset(eventAt).ToUnixTimeSeconds()}}
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var matched = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-corr-type", TestContext.Current.CancellationToken);
        matched.Status.Should().Be("Sent");
        matched.CompletedAtUtc.Should().Be(eventAt);
    }

    [Fact]
    public async Task HandleAsync_Should_UseTsEventFromStringWhenTsStringInvalid()
    {
        var now = DateTime.UtcNow;
        var eventAt = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-7)).ToUnixTimeSeconds()).UtcDateTime;
        var tsEventAt = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-3)).ToUnixTimeSeconds()).UtcDateTime;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-ts-event-string",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "opened",
          "message-id": "msg-ts-event-string",
          "ts": "not-a-number",
          "ts_event": "{{new DateTimeOffset(tsEventAt).ToUnixTimeSeconds()}}"
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var matched = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-ts-event-string", TestContext.Current.CancellationToken);
        matched.Status.Should().Be("Sent");
        matched.CompletedAtUtc.Should().Be(tsEventAt);
    }

    [Fact]
    public async Task HandleAsync_Should_RejectWhenEventIsNull()
    {
        var now = DateTime.UtcNow;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-null-event",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = """
        {
          "event": null,
          "message-id": "msg-null-event"
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("BrevoWebhookPayloadInvalid");
    }

    [Fact]
    public async Task HandleAsync_Should_RejectWhenEventIsNonStringPrimitive()
    {
        var now = DateTime.UtcNow;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-number-event",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = """
        {
          "event": 123,
          "message-id": "msg-number-event"
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("BrevoWebhookPayloadInvalid");
        var unchanged = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-number-event", TestContext.Current.CancellationToken);
        unchanged.Status.Should().Be("Pending");
        unchanged.CompletedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_Should_UseTsEpochWhenTsAndTsEventInvalidAndDateIsNonString()
    {
        var now = DateTime.UtcNow;
        var expectedAt = DateTimeOffset.FromUnixTimeMilliseconds(new DateTimeOffset(now.AddMinutes(-10)).ToUnixTimeMilliseconds()).UtcDateTime;
        var epochMs = new DateTimeOffset(expectedAt).ToUnixTimeMilliseconds().ToString();
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-epoch-priority",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "opened",
          "message-id": "msg-epoch-priority",
          "ts": [],
          "ts_event": {},
          "date": [],
          "ts_epoch": "{{epochMs}}"
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var matched = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-epoch-priority", TestContext.Current.CancellationToken);
        matched.CompletedAtUtc.Should().Be(expectedAt);
    }

    [Fact]
    public async Task HandleAsync_Should_NotOverrideFailedStatus_WhenDeliveryEventMatchedByCorrelation()
    {
        var now = DateTime.UtcNow;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-ignore-maybe-fail",
            CorrelationKey = "corr-fail-keep",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now.AddHours(-1),
            Status = "Failed",
            FailureMessage = "external bounce",
            CompletedAtUtc = now.AddMinutes(-15)
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "deferred",
          "message-id": "",
          "X-Correlation-Key": "corr-fail-keep",
          "ts": {{new DateTimeOffset(now.AddMinutes(-5)).ToUnixTimeSeconds()}}
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var unchanged = await db.EmailDispatchAudits.SingleAsync(x => x.CorrelationKey == "corr-fail-keep", TestContext.Current.CancellationToken);
        unchanged.Status.Should().Be("Failed");
        unchanged.FailureMessage.Should().Be("external bounce");
        unchanged.CompletedAtUtc.Should().Be(now.AddMinutes(-15));
    }

    [Fact]
    public async Task HandleAsync_Should_FallBackToMessageIdAliasWhenPrimaryMessageIdIsInvalidType()
    {
        var now = DateTime.UtcNow;
        var eventAt = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-3)).ToUnixTimeSeconds()).UtcDateTime;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "alias-message-id",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "opened",
          "message-id": 456,
          "messageId": "alias-message-id",
          "ts": {{new DateTimeOffset(eventAt).ToUnixTimeSeconds()}}
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var matched = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "alias-message-id", TestContext.Current.CancellationToken);
        matched.Status.Should().Be("Sent");
        matched.CompletedAtUtc.Should().Be(eventAt);
    }

    [Fact]
    public async Task HandleAsync_Should_UseTsWhenTsEventStringIsInvalidDecimal()
    {
        var now = DateTime.UtcNow;
        var eventAt = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-9)).ToUnixTimeSeconds()).UtcDateTime;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-ts-event-decimal-string",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "opened",
          "message-id": "msg-ts-event-decimal-string",
          "ts": {{new DateTimeOffset(eventAt).ToUnixTimeSeconds()}},
          "ts_event": "1700000000.987"
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var matched = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-ts-event-decimal-string", TestContext.Current.CancellationToken);
        matched.Status.Should().Be("Sent");
        matched.CompletedAtUtc.Should().Be(eventAt);
    }

    [Fact]
    public async Task HandleAsync_Should_ReadMessageIdFromAlternativeFieldName()
    {
        var now = DateTime.UtcNow;
        var eventAt = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-5)).ToUnixTimeSeconds()).UtcDateTime;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-alt-id",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "delivered",
          "messageIdLong": "msg-alt-id",
          "ts": {{new DateTimeOffset(eventAt).ToUnixTimeSeconds()}}
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var matched = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-alt-id", TestContext.Current.CancellationToken);
        matched.Status.Should().Be("Sent");
        matched.CompletedAtUtc.Should().Be(eventAt);
    }

    [Fact]
    public async Task HandleAsync_Should_ReadCorrelationKeyFromIdempotencyAlias()
    {
        var now = DateTime.UtcNow;
        var eventAt = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-4)).ToUnixTimeSeconds()).UtcDateTime;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "ignored-id",
            CorrelationKey = "alt-corr-id",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "opened",
          "message-id": "",
          "Idempotency-Key": "alt-corr-id",
          "ts": {{new DateTimeOffset(eventAt).ToUnixTimeSeconds()}}
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var matched = await db.EmailDispatchAudits.SingleAsync(x => x.CorrelationKey == "alt-corr-id", TestContext.Current.CancellationToken);
        matched.Status.Should().Be("Sent");
        matched.CompletedAtUtc.Should().Be(eventAt);
    }

    [Fact]
    public async Task HandleAsync_Should_ReadMessageIdFromMessageIdField()
    {
        var now = DateTime.UtcNow;
        var eventAt = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-4)).ToUnixTimeSeconds()).UtcDateTime;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-id-field",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "delivered",
          "messageId": "msg-id-field",
          "ts": {{new DateTimeOffset(eventAt).ToUnixTimeSeconds()}}
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var matched = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-id-field", TestContext.Current.CancellationToken);
        matched.Status.Should().Be("Sent");
        matched.CompletedAtUtc.Should().Be(eventAt);
    }

    [Fact]
    public async Task HandleAsync_Should_ReadMessageIdFromMessageIdUnderscoreAlias()
    {
        var now = DateTime.UtcNow;
        var eventAt = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-4)).ToUnixTimeSeconds()).UtcDateTime;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-id-underscore",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "delivered",
          "message_id": "msg-id-underscore",
          "ts": {{new DateTimeOffset(eventAt).ToUnixTimeSeconds()}}
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var matched = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-id-underscore", TestContext.Current.CancellationToken);
        matched.Status.Should().Be("Sent");
        matched.CompletedAtUtc.Should().Be(eventAt);
    }

    [Fact]
    public async Task HandleAsync_Should_ReadCorrelationFromXMailinCustomAlias()
    {
        var now = DateTime.UtcNow;
        var eventAt = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-3)).ToUnixTimeSeconds()).UtcDateTime;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "ignored-message-id",
            CorrelationKey = "custom-corr",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "opened",
          "message-id": "",
          "x-mailin-custom": "custom-corr",
          "ts": {{new DateTimeOffset(eventAt).ToUnixTimeSeconds()}}
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var matched = await db.EmailDispatchAudits.SingleAsync(x => x.CorrelationKey == "custom-corr", TestContext.Current.CancellationToken);
        matched.Status.Should().Be("Sent");
        matched.CompletedAtUtc.Should().Be(eventAt);
    }

    [Fact]
    public async Task HandleAsync_Should_ReadCorrelationFromLowercaseIdempotencyAlias()
    {
        var now = DateTime.UtcNow;
        var eventAt = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-2)).ToUnixTimeSeconds()).UtcDateTime;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-lower-idemp",
            CorrelationKey = "lower-idemp",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "opened",
          "message-id": "",
          "idempotency-key": "lower-idemp",
          "ts": {{new DateTimeOffset(eventAt).ToUnixTimeSeconds()}}
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var matched = await db.EmailDispatchAudits.SingleAsync(x => x.CorrelationKey == "lower-idemp", TestContext.Current.CancellationToken);
        matched.Status.Should().Be("Sent");
        matched.CompletedAtUtc.Should().Be(eventAt);
    }

    [Fact]
    public async Task HandleAsync_Should_ReadCorrelationFromLowercaseXCorrelationAlias()
    {
        var now = DateTime.UtcNow;
        var eventAt = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-3)).ToUnixTimeSeconds()).UtcDateTime;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-lc-xcorr",
            CorrelationKey = "lc-xcorr",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "opened",
          "message-id": "",
          "x-correlation-key": "lc-xcorr",
          "ts": {{new DateTimeOffset(eventAt).ToUnixTimeSeconds()}}
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var matched = await db.EmailDispatchAudits.SingleAsync(x => x.CorrelationKey == "lc-xcorr", TestContext.Current.CancellationToken);
        matched.Status.Should().Be("Sent");
        matched.CompletedAtUtc.Should().Be(eventAt);
    }

    [Fact]
    public async Task HandleAsync_Should_ReadCorrelationFromXMailinCustomTitleCaseAlias()
    {
        var now = DateTime.UtcNow;
        var eventAt = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-3)).ToUnixTimeSeconds()).UtcDateTime;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-title-mailin",
            CorrelationKey = "title-mailin-corr",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "opened",
          "message-id": "",
          "X-Mailin-Custom": "title-mailin-corr",
          "ts": {{new DateTimeOffset(eventAt).ToUnixTimeSeconds()}}
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var matched = await db.EmailDispatchAudits.SingleAsync(x => x.CorrelationKey == "title-mailin-corr", TestContext.Current.CancellationToken);
        matched.Status.Should().Be("Sent");
        matched.CompletedAtUtc.Should().Be(eventAt);
    }

    [Fact]
    public async Task HandleAsync_Should_ReadFailureReasonFromUppercaseReasonAlias()
    {
        var now = DateTime.UtcNow;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-uppercase-reason",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = """
        {
          "event": "hard_bounce",
          "message-id": "msg-uppercase-reason",
          "Reason": "CAPS reason field",
          "ts": 1700000000
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var matched = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-uppercase-reason", TestContext.Current.CancellationToken);
        matched.Status.Should().Be("Failed");
        matched.FailureMessage.Should().Be("Brevo event 'hard_bounce': CAPS reason field");
    }

    [Fact]
    public async Task HandleAsync_Should_ReadFailureReasonFromMessageField()
    {
        var now = DateTime.UtcNow;
        var eventAt = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-8)).ToUnixTimeSeconds()).UtcDateTime;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-failure-message-reason",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "hard_bounce",
          "message-id": "msg-failure-message-reason",
          "message": "reason from message alias",
          "ts": {{new DateTimeOffset(eventAt).ToUnixTimeSeconds()}}
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var matched = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-failure-message-reason", TestContext.Current.CancellationToken);
        matched.Status.Should().Be("Failed");
        matched.FailureMessage.Should().Be("Brevo event 'hard_bounce': reason from message alias");
        matched.CompletedAtUtc.Should().Be(eventAt);
    }

    [Fact]
    public async Task HandleAsync_Should_HandleErrorFailureEvent()
    {
        var now = DateTime.UtcNow;
        var eventAt = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-5)).ToUnixTimeSeconds()).UtcDateTime;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-error-event",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "error",
          "message-id": "msg-error-event",
          "reason": "downstream transient failure",
          "ts": {{new DateTimeOffset(eventAt).ToUnixTimeSeconds()}}
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var matched = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-error-event", TestContext.Current.CancellationToken);
        matched.Status.Should().Be("Failed");
        matched.FailureMessage.Should().Be("Brevo event 'error': downstream transient failure");
        matched.CompletedAtUtc.Should().Be(eventAt);
    }

    [Fact]
    public async Task HandleAsync_Should_MatchByEmailSubjectFromAlternativeFieldNames()
    {
        var now = DateTime.UtcNow;
        var eventAt = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-1)).ToUnixTimeSeconds()).UtcDateTime;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-alt-email-subject",
            RecipientEmail = "alt@example.com",
            Subject = "Welcome Alt",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "opened",
          "Email": "alt@example.com",
          "Subject": "Welcome Alt",
          "ts": {{new DateTimeOffset(eventAt).ToUnixTimeSeconds()}}
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var matched = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-alt-email-subject", TestContext.Current.CancellationToken);
        matched.Status.Should().Be("Sent");
        matched.CompletedAtUtc.Should().Be(eventAt);
    }

    [Fact]
    public async Task HandleAsync_Should_ReadFailureReasonFromMessageUpperAlias()
    {
        var now = DateTime.UtcNow;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-failure-message-upper",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = """
        {
          "event": "blocked",
          "message-id": "msg-failure-message-upper",
          "Message": "CAPS message alias",
          "ts": 1700000000
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var matched = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-failure-message-upper", TestContext.Current.CancellationToken);
        matched.Status.Should().Be("Failed");
        matched.FailureMessage.Should().Be("Brevo event 'blocked': CAPS message alias");
    }

    [Fact]
    public async Task HandleAsync_Should_RejectPayload_WhenReasonIsNonStringObject()
    {
        var now = DateTime.UtcNow;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-nonstring-reason",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = """
        {
          "event": "blocked",
          "message-id": "msg-nonstring-reason",
          "reason": {"text":"object"}
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var matched = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-nonstring-reason", TestContext.Current.CancellationToken);
        matched.Status.Should().Be("Failed");
        matched.FailureMessage.Should().Be("Brevo event 'blocked': No provider reason supplied.");
    }

    [Fact]
    public async Task HandleAsync_Should_DoNothing_WhenFallbackFieldsAreMissing()
    {
        var now = DateTime.UtcNow;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-missing-fallback",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending",
            CompletedAtUtc = now.AddMinutes(-10)
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = """
        {
          "event": "opened",
          "message-id": "   "
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var unchanged = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-missing-fallback", TestContext.Current.CancellationToken);
        unchanged.Status.Should().Be("Pending");
        unchanged.CompletedAtUtc.Should().Be(now.AddMinutes(-10));
    }

    [Fact]
    public async Task HandleAsync_Should_HandleDuplicateMessageIdsWithSameAttemptedAtDeterministically()
    {
        var now = DateTime.UtcNow;
        var eventAt = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-2)).ToUnixTimeSeconds()).UtcDateTime;
        var sameAttempt = now.AddHours(-1);
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-same-attempt",
            RecipientEmail = "user1@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = sameAttempt,
            Status = "Pending"
        });
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-same-attempt",
            RecipientEmail = "user2@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = sameAttempt,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "delivered",
          "message-id": "msg-same-attempt",
          "ts": {{new DateTimeOffset(eventAt).ToUnixTimeSeconds()}}
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var updated = await db.EmailDispatchAudits
            .Where(x => x.ProviderMessageId == "msg-same-attempt")
            .ToListAsync(TestContext.Current.CancellationToken);

        updated.Count.Should().Be(2);
        updated.Count(x => x.Status == "Sent").Should().Be(1);
        updated.Count(x => x.Status == "Pending").Should().Be(1);
        updated.Should().ContainSingle(x => x.CompletedAtUtc == eventAt);
    }

    [Fact]
    public async Task HandleAsync_Should_NotChangeStatusForUnknownEventType()
    {
        var now = DateTime.UtcNow;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-unknown-event",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Sent",
            CompletedAtUtc = now.AddMinutes(-50)
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "mystery_event",
          "message-id": "msg-unknown-event",
          "ts": {{new DateTimeOffset(now.AddMinutes(-10)).ToUnixTimeSeconds()}}
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var matched = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-unknown-event", TestContext.Current.CancellationToken);
        matched.Status.Should().Be("Sent");
        matched.CompletedAtUtc.Should().Be(now.AddMinutes(-50));
        matched.FailureMessage.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_Should_RejectEmptyEventValue()
    {
        var now = DateTime.UtcNow;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-empty-event-2",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "   ",
          "message-id": "msg-empty-event-2",
          "ts": {{new DateTimeOffset(now.AddMinutes(-10)).ToUnixTimeSeconds()}}
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("BrevoWebhookPayloadInvalid");
    }

    [Fact]
    public async Task HandleAsync_Should_FallbackToCorrelation_WhenMessageIdDoesNotMatchAnyAudit()
    {
        var now = DateTime.UtcNow;
        var eventAt = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-10)).ToUnixTimeSeconds()).UtcDateTime;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-real",
            CorrelationKey = "corr-fallback",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "opened",
          "message-id": "msg-missing",
          "X-Correlation-Key": "corr-fallback",
          "ts": {{new DateTimeOffset(eventAt).ToUnixTimeSeconds()}}
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var matched = await db.EmailDispatchAudits.SingleAsync(x => x.CorrelationKey == "corr-fallback", TestContext.Current.CancellationToken);
        matched.Status.Should().Be("Sent");
        matched.CompletedAtUtc.Should().Be(eventAt);
    }

    [Fact]
    public async Task HandleAsync_Should_SelectMostRecentAudit_ByCorrelation_WhenMultipleMatchesExist()
    {
        var now = DateTime.UtcNow;
        var eventAt = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-7)).ToUnixTimeSeconds()).UtcDateTime;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-old-corr",
            CorrelationKey = "corr-duplicate",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now.AddHours(-2),
            Status = "Pending"
        });
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-new-corr",
            CorrelationKey = "corr-duplicate",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now.AddMinutes(-15),
            Status = "Pending"
        });
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-nope",
            CorrelationKey = "corr-nope",
            RecipientEmail = "other@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "delivered",
          "message-id": "missing-msg",
          "X-Correlation-Key": "corr-duplicate",
          "ts": {{new DateTimeOffset(eventAt).ToUnixTimeSeconds()}}
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var updated = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-new-corr", TestContext.Current.CancellationToken);
        var skipped = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-old-corr", TestContext.Current.CancellationToken);
        var untouched = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-nope", TestContext.Current.CancellationToken);

        updated.Status.Should().Be("Sent");
        updated.CompletedAtUtc.Should().Be(eventAt);
        skipped.Status.Should().Be("Pending");
        untouched.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task HandleAsync_Should_MatchIntendedRecipientEmail_WhenRecipientEmailIsDifferent()
    {
        var now = DateTime.UtcNow;
        var eventAt = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-4)).ToUnixTimeSeconds()).UtcDateTime;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-intended-recipient",
            RecipientEmail = "old-recipient@example.com",
            IntendedRecipientEmail = "intended@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "opened",
          "email": "intended@example.com",
          "subject": "Welcome",
          "ts": {{new DateTimeOffset(eventAt).ToUnixTimeSeconds()}}
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var matched = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-intended-recipient", TestContext.Current.CancellationToken);
        matched.Status.Should().Be("Sent");
        matched.CompletedAtUtc.Should().Be(eventAt);
    }

    [Fact]
    public async Task HandleAsync_Should_NotMatchIntendedRecipientEmail_WhenOutOfWindow()
    {
        var now = DateTime.UtcNow;
        var attemptAt = now.AddDays(-10);
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-intended-old",
            RecipientEmail = "other-recipient@example.com",
            IntendedRecipientEmail = "intended-old@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = attemptAt,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = """
        {
          "event": "opened",
          "email": "intended-old@example.com",
          "subject": "Welcome"
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var matched = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-intended-old", TestContext.Current.CancellationToken);
        matched.Status.Should().Be("Pending");
        matched.CompletedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_Should_DoNothing_WhenMessageIdAndCorrelationAreNonString()
    {
        var now = DateTime.UtcNow;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-string",
            CorrelationKey = "corr-string",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "delivered",
          "message-id": 12345,
          "X-Correlation-Key": 67890,
          "ts": {{new DateTimeOffset(now.AddMinutes(-10)).ToUnixTimeSeconds()}}
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var unchanged = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-string", TestContext.Current.CancellationToken);
        unchanged.Status.Should().Be("Pending");
        unchanged.CompletedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_Should_RecordDefaultFailureMessageForSpamEvent()
    {
        var now = DateTime.UtcNow;
        var eventAt = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-6)).ToUnixTimeSeconds()).UtcDateTime;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-spam",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": " spam ",
          "message-id": "msg-spam",
          "ts": {{new DateTimeOffset(eventAt).ToUnixTimeSeconds()}}
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var matched = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-spam", TestContext.Current.CancellationToken);
        matched.Status.Should().Be("Failed");
        matched.FailureMessage.Should().Be("Brevo event 'spam': No provider reason supplied.");
        matched.CompletedAtUtc.Should().Be(eventAt);
    }

    [Fact]
    public async Task HandleAsync_Should_TrimFallbackEmailAndSubjectBeforeMatching()
    {
        var now = DateTime.UtcNow;
        var eventAt = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-2)).ToUnixTimeSeconds()).UtcDateTime;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-trim-fallback",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now.AddMinutes(-30),
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "opened",
          "email": "  user@example.com  ",
          "subject": "   Welcome   ",
          "ts": {{new DateTimeOffset(eventAt).ToUnixTimeSeconds()}}
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var matched = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-trim-fallback", TestContext.Current.CancellationToken);
        matched.Status.Should().Be("Sent");
        matched.CompletedAtUtc.Should().Be(eventAt);
    }

    [Fact]
    public async Task HandleAsync_Should_TrimCorrelationKeyFromAliasValue()
    {
        var now = DateTime.UtcNow;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-trim-corr",
            CorrelationKey = "trim-corr",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "opened",
          "message-id": "",
          "idempotency-key": "  trim-corr  ",
          "ts": 1700000000
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var matched = await db.EmailDispatchAudits.SingleAsync(x => x.CorrelationKey == "trim-corr", TestContext.Current.CancellationToken);
        matched.Status.Should().Be("Sent");
        matched.CompletedAtUtc.Should().Be(new DateTimeOffset(2023, 11, 14, 22, 13, 20, TimeSpan.Zero).UtcDateTime);
    }

    [Fact]
    public async Task HandleAsync_Should_HandleUnsubscribedAsDeliveryEvent()
    {
        var now = DateTime.UtcNow;
        var eventAt = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-4)).ToUnixTimeSeconds()).UtcDateTime;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-unsubscribed",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "unsubscribed",
          "message-id": "msg-unsubscribed",
          "ts": {{new DateTimeOffset(eventAt).ToUnixTimeSeconds()}}
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var matched = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-unsubscribed", TestContext.Current.CancellationToken);
        matched.Status.Should().Be("Sent");
        matched.CompletedAtUtc.Should().Be(eventAt);
    }

    [Fact]
    public async Task HandleAsync_Should_HandleProxyOpenAsDeliveryEvent()
    {
        var now = DateTime.UtcNow;
        var eventAt = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-2)).ToUnixTimeSeconds()).UtcDateTime;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-proxy-open",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "proxy_open",
          "message-id": "msg-proxy-open",
          "ts": {{new DateTimeOffset(eventAt).ToUnixTimeSeconds()}}
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var matched = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-proxy-open", TestContext.Current.CancellationToken);
        matched.Status.Should().Be("Sent");
        matched.CompletedAtUtc.Should().Be(eventAt);
    }

    [Fact]
    public async Task HandleAsync_Should_HandleUniqueOpenedAsDeliveryEvent()
    {
        var now = DateTime.UtcNow;
        var eventAt = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-2)).ToUnixTimeSeconds()).UtcDateTime;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-unique-opened",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "unique_opened",
          "message-id": "msg-unique-opened",
          "ts": {{new DateTimeOffset(eventAt).ToUnixTimeSeconds()}}
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var matched = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-unique-opened", TestContext.Current.CancellationToken);
        matched.Status.Should().Be("Sent");
        matched.CompletedAtUtc.Should().Be(eventAt);
    }

    [Fact]
    public async Task HandleAsync_Should_HandleUniqueProxyOpenAsDeliveryEvent()
    {
        var now = DateTime.UtcNow;
        var eventAt = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-2)).ToUnixTimeSeconds()).UtcDateTime;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-unique-proxy-open",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "unique_proxy_open",
          "message-id": "msg-unique-proxy-open",
          "ts": {{new DateTimeOffset(eventAt).ToUnixTimeSeconds()}}
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var matched = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-unique-proxy-open", TestContext.Current.CancellationToken);
        matched.Status.Should().Be("Sent");
        matched.CompletedAtUtc.Should().Be(eventAt);
    }

    [Fact]
    public async Task HandleAsync_Should_HandleClickAsDeliveryEvent()
    {
        var now = DateTime.UtcNow;
        var eventAt = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-2)).ToUnixTimeSeconds()).UtcDateTime;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-click",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "click",
          "message-id": "msg-click",
          "ts": {{new DateTimeOffset(eventAt).ToUnixTimeSeconds()}}
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var matched = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-click", TestContext.Current.CancellationToken);
        matched.Status.Should().Be("Sent");
        matched.CompletedAtUtc.Should().Be(eventAt);
    }

    [Fact]
    public async Task HandleAsync_Should_HandleDeferredAsDeliveryEvent()
    {
        var now = DateTime.UtcNow;
        var eventAt = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-2)).ToUnixTimeSeconds()).UtcDateTime;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-deferred",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "deferred",
          "message-id": "msg-deferred",
          "ts": {{new DateTimeOffset(eventAt).ToUnixTimeSeconds()}}
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var matched = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-deferred", TestContext.Current.CancellationToken);
        matched.Status.Should().Be("Sent");
        matched.CompletedAtUtc.Should().Be(eventAt);
    }

    [Fact]
    public async Task HandleAsync_Should_HandleRequestAsDeliveryEvent()
    {
        var now = DateTime.UtcNow;
        var eventAt = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-2)).ToUnixTimeSeconds()).UtcDateTime;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-request",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "request",
          "message-id": "msg-request",
          "ts": {{new DateTimeOffset(now.AddMinutes(-20)).ToUnixTimeSeconds()}},
          "ts_event": {{new DateTimeOffset(eventAt).ToUnixTimeSeconds()}}
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var matched = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-request", TestContext.Current.CancellationToken);
        matched.Status.Should().Be("Sent");
        matched.CompletedAtUtc.Should().Be(eventAt);
    }

    [Fact]
    public async Task HandleAsync_Should_HandleInvalidFailureEvent()
    {
        var now = DateTime.UtcNow;
        var eventAt = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-1)).ToUnixTimeSeconds()).UtcDateTime;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-invalid-failure",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "invalid",
          "message-id": "msg-invalid-failure",
          "reason": "bad recipient",
          "ts": {{new DateTimeOffset(eventAt).ToUnixTimeSeconds()}}
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var matched = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-invalid-failure", TestContext.Current.CancellationToken);
        matched.Status.Should().Be("Failed");
        matched.FailureMessage.Should().Be("Brevo event 'invalid': bad recipient");
        matched.CompletedAtUtc.Should().Be(eventAt);
    }

    [Fact]
    public async Task HandleAsync_Should_TrimMessageIdBeforeLookup()
    {
        var now = DateTime.UtcNow;
        var eventAt = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-4)).ToUnixTimeSeconds()).UtcDateTime;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-trimmed",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "opened",
          "message-id": "  msg-trimmed  ",
          "ts": {{new DateTimeOffset(eventAt).ToUnixTimeSeconds()}}
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var matched = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-trimmed", TestContext.Current.CancellationToken);
        matched.Status.Should().Be("Sent");
        matched.CompletedAtUtc.Should().Be(eventAt);
    }

    [Fact]
    public async Task HandleAsync_Should_FallbackToCorrelationFromIdempotencyAliasWhenMessageIdEmpty()
    {
        var now = DateTime.UtcNow;
        var eventAt = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-4)).ToUnixTimeSeconds()).UtcDateTime;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-nocorrelation",
            CorrelationKey = "corr-empty-msgid",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "opened",
          "message-id": "   ",
          "idempotency-key": "corr-empty-msgid",
          "ts": {{new DateTimeOffset(eventAt).ToUnixTimeSeconds()}}
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var matched = await db.EmailDispatchAudits.SingleAsync(x => x.CorrelationKey == "corr-empty-msgid", TestContext.Current.CancellationToken);
        matched.Status.Should().Be("Sent");
        matched.CompletedAtUtc.Should().Be(eventAt);
    }

    [Fact]
    public async Task HandleAsync_Should_FallbackToClock_WhenTimestampIsOutOfRange()
    {
        var now = DateTime.UtcNow;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-out-of-range-ts",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "opened",
          "message-id": "msg-out-of-range-ts",
          "ts": "9223372036854775807"
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var matched = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-out-of-range-ts", TestContext.Current.CancellationToken);
        matched.Status.Should().Be("Sent");
        matched.CompletedAtUtc.Should().Be(now);
    }

    [Fact]
    public async Task HandleAsync_Should_UseTsWhenTsEventIsOutOfRange()
    {
        var now = DateTime.UtcNow;
        var eventAt = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-15)).ToUnixTimeSeconds()).UtcDateTime;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-out-of-range-ts-event",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $@"{{""event"":""opened"",""message-id"":""msg-out-of-range-ts-event"",""ts_event"":""9223372036854775807"",""ts"":{new DateTimeOffset(eventAt).ToUnixTimeSeconds()}}}";

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var matched = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-out-of-range-ts-event", TestContext.Current.CancellationToken);
        matched.Status.Should().Be("Sent");
        matched.CompletedAtUtc.Should().Be(eventAt);
    }

    [Fact]
    public async Task HandleAsync_Should_FallbackToClock_WhenTsEpochIsOutOfRange()
    {
        var now = DateTime.UtcNow;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-out-of-range-ts-epoch",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = @"{""event"":""opened"",""message-id"":""msg-out-of-range-ts-epoch"",""ts"":""bad-ts"",""ts_epoch"":9223372036854775807}";

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var matched = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-out-of-range-ts-epoch", TestContext.Current.CancellationToken);
        matched.Status.Should().Be("Sent");
        matched.CompletedAtUtc.Should().Be(now);
    }

    [Fact]
    public async Task HandleAsync_Should_MatchAudit_ByCorrelationAliasUsingCaseVariantFieldName()
    {
        var now = DateTime.UtcNow;
        var eventAt = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-2)).ToUnixTimeSeconds()).UtcDateTime;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-case-corr",
            CorrelationKey = "corr-case-1",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "opened",
          "message-id": "",
          "x-CoRrElAtIoN-kEy": " corr-case-1 ",
          "ts": {{new DateTimeOffset(eventAt).ToUnixTimeSeconds()}}
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var audit = await db.EmailDispatchAudits.SingleAsync(x => x.CorrelationKey == "corr-case-1", TestContext.Current.CancellationToken);
        audit.Status.Should().Be("Sent");
        audit.CompletedAtUtc.Should().Be(eventAt);
    }

    [Fact]
    public async Task HandleAsync_Should_SkipDeletedAudit_WhenMessageIdMatchesOnlyDeletedRows()
    {
        var now = DateTime.UtcNow;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-deleted-only",
            CorrelationKey = "corr-deleted-only",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now.AddHours(-1),
            Status = "Pending",
            IsDeleted = true
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = """
        {
          "event": "opened",
          "message-id": "msg-deleted-only",
          "ts": 1700000000
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var audit = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-deleted-only", TestContext.Current.CancellationToken);
        audit.IsDeleted.Should().BeTrue();
        audit.Status.Should().Be("Pending");
        audit.CompletedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_Should_IgnoreDeletedAudit_WhenActiveAuditSharesSameMessageId()
    {
        var now = DateTime.UtcNow;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-soft-dupe",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now.AddDays(-1),
            Status = "Pending",
            IsDeleted = true
        });
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-soft-dupe",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending",
            IsDeleted = false
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "opened",
          "message-id": "msg-soft-dupe",
          "ts": {{new DateTimeOffset(now.AddMinutes(-20)).ToUnixTimeSeconds()}}
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var active = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-soft-dupe" && !x.IsDeleted, TestContext.Current.CancellationToken);
        var deleted = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-soft-dupe" && x.IsDeleted, TestContext.Current.CancellationToken);

        active.Status.Should().Be("Sent");
        var expectedEventAt = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-20)).ToUnixTimeSeconds()).UtcDateTime;
        active.CompletedAtUtc.Should().Be(expectedEventAt);
        deleted.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task HandleAsync_Should_FallbackToCorrelation_WhenMessageIdMatchesOnlyDeletedAudit()
    {
        var now = DateTime.UtcNow;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-deleted",
            CorrelationKey = "corr-removed",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now.AddHours(-4),
            Status = "Pending",
            IsDeleted = true
        });
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-active",
            CorrelationKey = "corr-active",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now.AddHours(-1),
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = """
        {
          "event": "opened",
          "message-id": "msg-deleted",
          "x-correlation-key": "corr-active",
          "ts": 1700000000
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var matched = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-active", TestContext.Current.CancellationToken);
        matched.Status.Should().Be("Sent");
        matched.CompletedAtUtc.Should().Be(DateTimeOffset.FromUnixTimeSeconds(1700000000).UtcDateTime);
        var deleted = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-deleted", TestContext.Current.CancellationToken);
        deleted.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task HandleAsync_Should_NotMatchAudit_WhenOnlyDeletedCorrelationIsPresent()
    {
        var now = DateTime.UtcNow;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-corr-deleted",
            CorrelationKey = "corr-deleted-only",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now.AddHours(-2),
            Status = "Pending",
            IsDeleted = true
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = """
        {
          "event": "opened",
          "message-id": "",
          "X-Correlation-Key": "corr-deleted-only",
          "ts": 1700000000
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var deleted = await db.EmailDispatchAudits.SingleAsync(x => x.CorrelationKey == "corr-deleted-only", TestContext.Current.CancellationToken);
        deleted.Status.Should().Be("Pending");
        deleted.CompletedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_Should_PreferTsEpoch_Milliseconds_WhenProvidedAsString()
    {
        var now = DateTime.UtcNow;
        var eventAt = DateTimeOffset.FromUnixTimeMilliseconds(new DateTimeOffset(now.AddMinutes(-5)).ToUnixTimeMilliseconds()).UtcDateTime;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-ts-epoch-string",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "opened",
          "message-id": "msg-ts-epoch-string",
          "ts_epoch": "{{new DateTimeOffset(eventAt).ToUnixTimeMilliseconds()}}"
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var matched = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-ts-epoch-string", TestContext.Current.CancellationToken);
        matched.Status.Should().Be("Sent");
        matched.CompletedAtUtc.Should().Be(eventAt);
    }

    [Fact]
    public async Task HandleAsync_Should_NotMatchAudit_WhenNoIdentifiersAndNoFallbackFields()
    {
        var now = DateTime.UtcNow;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-unmatched",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now.AddHours(-1),
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = """
        {
          "event": "opened",
          "message-id": "   ",
          "ts": 1700000000
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var unchanged = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-unmatched", TestContext.Current.CancellationToken);
        unchanged.Status.Should().Be("Pending");
        unchanged.CompletedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_Should_NotChangeAnyFields_WhenEventTypeIsUnsupported()
    {
        var now = DateTime.UtcNow;
        var existingCompletedAt = now.AddMinutes(-20);
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-unsupported",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Sent",
            CompletedAtUtc = existingCompletedAt
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = """
        {
          "event": "unknown_event",
          "message-id": "msg-unsupported",
          "ts": 1700000000
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var unchanged = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-unsupported", TestContext.Current.CancellationToken);
        unchanged.Status.Should().Be("Sent");
        unchanged.CompletedAtUtc.Should().Be(existingCompletedAt);
    }

    [Fact]
    public async Task HandleAsync_Should_NotOverrideCompletedAt_WhenDeliveryEventAlreadyHasCompletedAt()
    {
        var now = DateTime.UtcNow;
        var existingCompletedAt = now.AddMinutes(-35);
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-delivery-no-overwrite",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now.AddHours(-1),
            Status = "Sent",
            CompletedAtUtc = existingCompletedAt
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "delivered",
          "message-id": "msg-delivery-no-overwrite",
          "ts": {{new DateTimeOffset(now.AddMinutes(-5)).ToUnixTimeSeconds()}}
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var unchanged = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-delivery-no-overwrite", TestContext.Current.CancellationToken);
        unchanged.Status.Should().Be("Sent");
        unchanged.CompletedAtUtc.Should().Be(existingCompletedAt);
    }

    [Fact]
    public async Task HandleAsync_Should_PreserveFailureDetails_WhenDeliveryEventArrivesAfterFailure()
    {
        var now = DateTime.UtcNow;
        var existingCompletedAt = now.AddMinutes(-45);
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-delivery-after-failed",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now.AddHours(-1),
            Status = "Failed",
            CompletedAtUtc = existingCompletedAt,
            FailureMessage = "legacy failure"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = $$"""
        {
          "event": "request",
          "message-id": "msg-delivery-after-failed",
          "ts": {{new DateTimeOffset(now.AddMinutes(-10)).ToUnixTimeSeconds()}}
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var unchanged = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-delivery-after-failed", TestContext.Current.CancellationToken);
        unchanged.Status.Should().Be("Failed");
        unchanged.CompletedAtUtc.Should().Be(existingCompletedAt);
        unchanged.FailureMessage.Should().Be("legacy failure");
    }

    [Fact]
    public void Ctor_Should_Throw_WhenDbIsNull()
    {
        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));

        var action = () => new ProcessBrevoTransactionalEmailWebhookHandler(null!, new FixedClock(DateTime.UtcNow), localizer.Object);

        action.Should().Throw<ArgumentNullException>().WithParameterName("db");
    }

    [Fact]
    public void Ctor_Should_Throw_WhenClockIsNull()
    {
        using var db = ProcessBrevoWebhookTestDbContext.Create();
        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));

        var action = () => new ProcessBrevoTransactionalEmailWebhookHandler(db, null!, localizer.Object);

        action.Should().Throw<ArgumentNullException>().WithParameterName("clock");
    }

    [Fact]
    public void Ctor_Should_Throw_WhenLocalizerIsNull()
    {
        using var db = ProcessBrevoWebhookTestDbContext.Create();

        var action = () => new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(DateTime.UtcNow), null!);

        action.Should().Throw<ArgumentNullException>().WithParameterName("localizer");
    }

    [Fact]
    public async Task HandleAsync_Should_NotMatch_WhenMessageIdAndCorrelationAreBlank()
    {
        var now = DateTime.UtcNow;
        await using var db = ProcessBrevoWebhookTestDbContext.Create();
        db.EmailDispatchAudits.Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg-keep",
            CorrelationKey = "corr-keep",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var payload = """
        {
          "event": "opened",
          "message-id": "   ",
          "X-Correlation-Key": "   ",
          "ts": 1700000000
        }
        """;

        var result = await handler.HandleAsync(payload, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var unchanged = await db.EmailDispatchAudits.SingleAsync(x => x.ProviderMessageId == "msg-keep", TestContext.Current.CancellationToken);
        unchanged.Status.Should().Be("Pending");
        unchanged.CompletedAtUtc.Should().BeNull();
    }

    private sealed class FixedClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }

    private sealed class ProcessBrevoWebhookTestDbContext : DbContext, IAppDbContext
    {
        public DbSet<EmailDispatchAudit> EmailDispatchAudits { get; set; } = null!;

        private ProcessBrevoWebhookTestDbContext(DbContextOptions<ProcessBrevoWebhookTestDbContext> options)
            : base(options)
        { }

        public static ProcessBrevoWebhookTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<ProcessBrevoWebhookTestDbContext>()
                .UseInMemoryDatabase($"darwin_process_brevo_webhook_handler_tests_{Guid.NewGuid()}")
                .Options;

            return new ProcessBrevoWebhookTestDbContext(options);
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();
    }
}
