using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Billing.Commands;
using Darwin.Application.Billing.DTOs;
using Darwin.Application;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.Integration;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.Linq;

namespace Darwin.Tests.Unit.Billing;

public sealed class BillingUpdateHandlersTests
{
    [Fact]
    public async Task UpdateBillingWebhookDeliveryHandler_Should_ReturnValidationError_WhenRequestIsInvalid()
    {
        await using var db = BillingUpdateTestDbContext.Create();
        var handler = new UpdateBillingWebhookDeliveryHandler(db, new TestStringLocalizer());

        var result = await handler.HandleAsync(new UpdateBillingWebhookDeliveryDto
        {
            Id = Guid.Empty,
            RowVersion = []
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("InvalidDeleteRequest");
    }

    [Fact]
    public async Task UpdateBillingWebhookDeliveryHandler_Should_ReturnNotFound_WhenDeliveryDoesNotExist()
    {
        await using var db = BillingUpdateTestDbContext.Create();
        var handler = new UpdateBillingWebhookDeliveryHandler(db, new TestStringLocalizer());

        var result = await handler.HandleAsync(new UpdateBillingWebhookDeliveryDto
        {
            Id = Guid.NewGuid(),
            RowVersion = [1],
            Action = "MarkSucceeded"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("WebhookDeliveryNotFound");
    }

    [Fact]
    public async Task UpdateBillingWebhookDeliveryHandler_Should_ReturnConcurrencyError_WhenRowVersionMismatches()
    {
        await using var db = BillingUpdateTestDbContext.Create();
        var deliveryId = Guid.NewGuid();
        db.Set<WebhookDelivery>().Add(new WebhookDelivery
        {
            Id = deliveryId,
            Status = "Pending",
            RowVersion = [1, 2, 3]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateBillingWebhookDeliveryHandler(db, new TestStringLocalizer());
        var result = await handler.HandleAsync(new UpdateBillingWebhookDeliveryDto
        {
            Id = deliveryId,
            RowVersion = [9, 9, 9],
            Action = "MarkSucceeded"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("ItemConcurrencyConflict");
    }

    [Fact]
    public async Task UpdateBillingWebhookDeliveryHandler_Should_ApplyMarkSucceeded()
    {
        await using var db = BillingUpdateTestDbContext.Create();
        var deliveryId = Guid.NewGuid();
        db.Set<WebhookDelivery>().Add(new WebhookDelivery
        {
            Id = deliveryId,
            Status = "Failed",
            IsDeleted = true,
            ResponseCode = null,
            RowVersion = [2]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateBillingWebhookDeliveryHandler(db, new TestStringLocalizer());
        var result = await handler.HandleAsync(new UpdateBillingWebhookDeliveryDto
        {
            Id = deliveryId,
            RowVersion = [2],
            Action = "MarkSucceeded"
        }, TestContext.Current.CancellationToken);

        var updated = await db.Set<WebhookDelivery>().SingleAsync(x => x.Id == deliveryId, TestContext.Current.CancellationToken);
        result.Succeeded.Should().BeTrue();
        updated.Status.Should().Be("Succeeded");
        updated.IsDeleted.Should().BeFalse();
        updated.ResponseCode.Should().Be(200);
        updated.LastAttemptAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateBillingWebhookDeliveryHandler_Should_ApplyRequeueAndSuppress()
    {
        await using var db = BillingUpdateTestDbContext.Create();
        var deliveryId = Guid.NewGuid();
        db.Set<WebhookDelivery>().Add(new WebhookDelivery
        {
            Id = deliveryId,
            Status = "Failed",
            RetryCount = 2,
            ResponseCode = 500,
            RowVersion = [3]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateBillingWebhookDeliveryHandler(db, new TestStringLocalizer());
        var requeue = await handler.HandleAsync(new UpdateBillingWebhookDeliveryDto
        {
            Id = deliveryId,
            RowVersion = [3],
            Action = "Requeue"
        }, TestContext.Current.CancellationToken);

        requeue.Succeeded.Should().BeTrue();

        var afterRequeue = await db.Set<WebhookDelivery>().SingleAsync(x => x.Id == deliveryId, TestContext.Current.CancellationToken);
        afterRequeue.Status.Should().Be("Pending");
        afterRequeue.RetryCount.Should().Be(0);
        afterRequeue.ResponseCode.Should().BeNull();
        var requeueRowVersion = afterRequeue.RowVersion.ToArray();

        var suppress = await handler.HandleAsync(new UpdateBillingWebhookDeliveryDto
        {
            Id = deliveryId,
            RowVersion = requeueRowVersion,
            Action = "Suppress"
        }, TestContext.Current.CancellationToken);

        suppress.Succeeded.Should().BeTrue();
        var afterSuppress = await db.Set<WebhookDelivery>().SingleAsync(x => x.Id == deliveryId, TestContext.Current.CancellationToken);
        afterSuppress.Status.Should().Be("Suppressed");
        afterSuppress.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateBillingWebhookDeliveryHandler_Should_ReturnUnsupportedActionError()
    {
        await using var db = BillingUpdateTestDbContext.Create();
        var deliveryId = Guid.NewGuid();
        db.Set<WebhookDelivery>().Add(new WebhookDelivery
        {
            Id = deliveryId,
            Status = "Pending",
            RowVersion = [4]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateBillingWebhookDeliveryHandler(db, new TestStringLocalizer());
        var result = await handler.HandleAsync(new UpdateBillingWebhookDeliveryDto
        {
            Id = deliveryId,
            RowVersion = [4],
            Action = "Unexpected"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("WebhookDeliveryUnsupportedAction");
    }

    [Fact]
    public async Task UpdatePaymentDisputeReviewHandler_Should_ReturnValidationError_WhenRequestIsInvalid()
    {
        await using var db = BillingUpdateTestDbContext.Create();
        var handler = new UpdatePaymentDisputeReviewHandler(db, new TestStringLocalizer());

        var result = await handler.HandleAsync(new UpdatePaymentDisputeReviewDto
        {
            Id = Guid.Empty,
            RowVersion = []
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("InvalidDeleteRequest");
    }

    [Fact]
    public async Task UpdatePaymentDisputeReviewHandler_Should_ReturnRowVersionRequired_WhenRowVersionMissing()
    {
        await using var db = BillingUpdateTestDbContext.Create();
        var paymentId = Guid.NewGuid();
        db.Set<Payment>().Add(new Payment
        {
            Id = paymentId,
            Provider = "Stripe",
            Currency = "EUR",
            Status = PaymentStatus.Pending,
            RowVersion = [1]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdatePaymentDisputeReviewHandler(db, new TestStringLocalizer());
        var result = await handler.HandleAsync(new UpdatePaymentDisputeReviewDto
        {
            Id = paymentId,
            RowVersion = []
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("RowVersionRequired");
    }

    [Fact]
    public async Task UpdatePaymentDisputeReviewHandler_Should_ReturnNotFound_WhenPaymentDoesNotExist()
    {
        await using var db = BillingUpdateTestDbContext.Create();
        var handler = new UpdatePaymentDisputeReviewHandler(db, new TestStringLocalizer());

        var result = await handler.HandleAsync(new UpdatePaymentDisputeReviewDto
        {
            Id = Guid.NewGuid(),
            RowVersion = [1],
            Action = UpdatePaymentDisputeReviewHandler.UnderReviewAction
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("PaymentNotFound");
    }

    [Fact]
    public async Task UpdatePaymentDisputeReviewHandler_Should_ThrowConcurrencyException_WhenRowVersionMismatches()
    {
        await using var db = BillingUpdateTestDbContext.Create();
        var paymentId = Guid.NewGuid();
        db.Set<Payment>().Add(new Payment
        {
            Id = paymentId,
            Provider = "Stripe",
            Currency = "EUR",
            Status = PaymentStatus.Pending,
            RowVersion = [6]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdatePaymentDisputeReviewHandler(db, new TestStringLocalizer());

        var result = await handler.HandleAsync(new UpdatePaymentDisputeReviewDto
        {
            Id = paymentId,
            RowVersion = [7],
            Action = UpdatePaymentDisputeReviewHandler.ResolveLostAction
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("stale RowVersion should cause a concurrency conflict");
        result.Error.Should().Be("ItemConcurrencyConflict");
    }

    [Fact]
    public async Task UpdatePaymentDisputeReviewHandler_Should_ApplyResolveWon_AndSetCompletedStatus()
    {
        await using var db = BillingUpdateTestDbContext.Create();
        var paymentId = Guid.NewGuid();
        db.Set<Payment>().Add(new Payment
        {
            Id = paymentId,
            Provider = "Stripe",
            Currency = "EUR",
            Status = PaymentStatus.Failed,
            FailureReason = "gateway declined",
            RowVersion = [8]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdatePaymentDisputeReviewHandler(db, new TestStringLocalizer());
        var result = await handler.HandleAsync(new UpdatePaymentDisputeReviewDto
        {
            Id = paymentId,
            RowVersion = [8],
            Action = UpdatePaymentDisputeReviewHandler.ResolveWonAction,
            Note = "analyst [note]\nnew line"
        }, TestContext.Current.CancellationToken);

        var updated = await db.Set<Payment>().SingleAsync(x => x.Id == paymentId, TestContext.Current.CancellationToken);
        result.Succeeded.Should().BeTrue();
        updated.Status.Should().Be(PaymentStatus.Completed);
        updated.FailureReason.Should().Contain("gateway declined");
        updated.FailureReason.Should().Contain("[DisputeReview:Won;");
        updated.FailureReason.Should().NotContain("[note]");
        updated.FailureReason.Should().Contain("analyst (note) new line");
        UpdatePaymentDisputeReviewHandler.ResolveDisputeReviewState(updated.FailureReason).Should().Be("Won");
        UpdatePaymentDisputeReviewHandler.IsDisputeReviewResolved(updated.FailureReason).Should().BeTrue();
    }

    [Fact]
    public async Task UpdatePaymentDisputeReviewHandler_Should_NormalizeNote_NewlinesAndBrackets_WhenResolving()
    {
        await using var db = BillingUpdateTestDbContext.Create();
        var paymentId = Guid.NewGuid();
        db.Set<Payment>().Add(new Payment
        {
            Id = paymentId,
            Provider = "Stripe",
            Currency = "EUR",
            Status = PaymentStatus.Failed,
            FailureReason = "gateway declined",
            RowVersion = [11]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdatePaymentDisputeReviewHandler(db, new TestStringLocalizer());
        var result = await handler.HandleAsync(new UpdatePaymentDisputeReviewDto
        {
            Id = paymentId,
            RowVersion = [11],
            Action = UpdatePaymentDisputeReviewHandler.ResolveLostAction,
            Note = "investigator [review]\r\nnew line\ranother line"
        }, TestContext.Current.CancellationToken);

        var updated = await db.Set<Payment>().SingleAsync(x => x.Id == paymentId, TestContext.Current.CancellationToken);
        result.Succeeded.Should().BeTrue();
        updated.FailureReason.Should().Contain("investigator (review) new line another line");
        updated.FailureReason.Should().NotContain("\r");
        updated.FailureReason.Should().NotContain("\n");
    }

    [Fact]
    public async Task UpdatePaymentDisputeReviewHandler_Should_ApplyResolveLostAndClearMarker()
    {
        await using var db = BillingUpdateTestDbContext.Create();
        var paymentId = Guid.NewGuid();
        db.Set<Payment>().Add(new Payment
        {
            Id = paymentId,
            Provider = "Stripe",
            Currency = "EUR",
            Status = PaymentStatus.Pending,
            FailureReason = "provider message [DisputeReview:UnderReview;2024-01-01T00:00:00.0000000Z;-]",
            RowVersion = [9]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdatePaymentDisputeReviewHandler(db, new TestStringLocalizer());
        var resolveLost = await handler.HandleAsync(new UpdatePaymentDisputeReviewDto
        {
            Id = paymentId,
            RowVersion = [9],
            Action = UpdatePaymentDisputeReviewHandler.ResolveLostAction,
            Note = "manual decision"
        }, TestContext.Current.CancellationToken);

        resolveLost.Succeeded.Should().BeTrue();

        var afterLost = await db.Set<Payment>().SingleAsync(x => x.Id == paymentId, TestContext.Current.CancellationToken);
        afterLost.Status.Should().Be(PaymentStatus.Failed);
        UpdatePaymentDisputeReviewHandler.ResolveDisputeReviewState(afterLost.FailureReason).Should().Be("Lost");
        var clearRowVersion = afterLost.RowVersion.ToArray();

        var clear = await handler.HandleAsync(new UpdatePaymentDisputeReviewDto
        {
            Id = paymentId,
            RowVersion = clearRowVersion,
            Action = UpdatePaymentDisputeReviewHandler.ClearAction
        }, TestContext.Current.CancellationToken);

        clear.Succeeded.Should().BeTrue();
        var afterClear = await db.Set<Payment>().SingleAsync(x => x.Id == paymentId, TestContext.Current.CancellationToken);
        afterClear.FailureReason.Should().Be("provider message");
        UpdatePaymentDisputeReviewHandler.ResolveDisputeReviewState(afterClear.FailureReason).Should().BeEmpty();
        UpdatePaymentDisputeReviewHandler.IsDisputeReviewResolved(afterClear.FailureReason).Should().BeFalse();
    }

    [Fact]
    public async Task UpdatePaymentDisputeReviewHandler_Should_ReturnUnsupportedActionError()
    {
        await using var db = BillingUpdateTestDbContext.Create();
        var paymentId = Guid.NewGuid();
        db.Set<Payment>().Add(new Payment
        {
            Id = paymentId,
            Provider = "Stripe",
            Currency = "EUR",
            Status = PaymentStatus.Pending,
            RowVersion = [10]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdatePaymentDisputeReviewHandler(db, new TestStringLocalizer());
        var result = await handler.HandleAsync(new UpdatePaymentDisputeReviewDto
        {
            Id = paymentId,
            RowVersion = [10],
            Action = "Unknown"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("PaymentDisputeReviewUnsupportedAction");
    }

    [Fact]
    public async Task UpdatePaymentDisputeReviewHandler_Should_RejectWhitespaceAction()
    {
        await using var db = BillingUpdateTestDbContext.Create();
        var paymentId = Guid.NewGuid();
        db.Set<Payment>().Add(new Payment
        {
            Id = paymentId,
            Provider = "Stripe",
            Currency = "EUR",
            Status = PaymentStatus.Pending,
            RowVersion = [15]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdatePaymentDisputeReviewHandler(db, new TestStringLocalizer());
        var result = await handler.HandleAsync(new UpdatePaymentDisputeReviewDto
        {
            Id = paymentId,
            RowVersion = [15],
            Action = "   "
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("PaymentDisputeReviewUnsupportedAction");
    }

    [Fact]
    public async Task UpdatePaymentDisputeReviewHandler_Should_ApplyUnderReview_WithTrimmedActionAndNote()
    {
        await using var db = BillingUpdateTestDbContext.Create();
        var paymentId = Guid.NewGuid();
        db.Set<Payment>().Add(new Payment
        {
            Id = paymentId,
            Provider = "Stripe",
            Currency = "EUR",
            Status = PaymentStatus.Pending,
            RowVersion = [12]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdatePaymentDisputeReviewHandler(db, new TestStringLocalizer());
        var result = await handler.HandleAsync(new UpdatePaymentDisputeReviewDto
        {
            Id = paymentId,
            RowVersion = [12],
            Action = $" {UpdatePaymentDisputeReviewHandler.UnderReviewAction}  ",
            Note = "agent [note]\r\nline 2"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();

        var updated = await db.Set<Payment>().SingleAsync(x => x.Id == paymentId, TestContext.Current.CancellationToken);
        updated.Status.Should().Be(PaymentStatus.Pending);
        var state = UpdatePaymentDisputeReviewHandler.ResolveDisputeReviewState(updated.FailureReason);
        state.Should().Be("UnderReview");
        updated.FailureReason.Should().Contain("agent (note) line 2");
        UpdatePaymentDisputeReviewHandler.IsDisputeReviewResolved(updated.FailureReason).Should().BeFalse();
    }

    [Fact]
    public async Task UpdatePaymentDisputeReviewHandler_Should_ApplyEvidenceSubmitted_WithCaseInsensitiveAction()
    {
        await using var db = BillingUpdateTestDbContext.Create();
        var paymentId = Guid.NewGuid();
        db.Set<Payment>().Add(new Payment
        {
            Id = paymentId,
            Provider = "Stripe",
            Currency = "EUR",
            Status = PaymentStatus.Pending,
            RowVersion = [13]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdatePaymentDisputeReviewHandler(db, new TestStringLocalizer());
        var result = await handler.HandleAsync(new UpdatePaymentDisputeReviewDto
        {
            Id = paymentId,
            RowVersion = [13],
            Action = "eViDeNcEsUbMiTtEd"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();

        var updated = await db.Set<Payment>().SingleAsync(x => x.Id == paymentId, TestContext.Current.CancellationToken);
        updated.Status.Should().Be(PaymentStatus.Pending);
        UpdatePaymentDisputeReviewHandler.ResolveDisputeReviewState(updated.FailureReason).Should().Be("EvidenceSubmitted");
    }

    [Fact]
    public async Task UpdatePaymentDisputeReviewHandler_Should_NotChangeStatus_WhenResolveLostOnRefunded()
    {
        await using var db = BillingUpdateTestDbContext.Create();
        var paymentId = Guid.NewGuid();
        db.Set<Payment>().Add(new Payment
        {
            Id = paymentId,
            Provider = "Stripe",
            Currency = "EUR",
            Status = PaymentStatus.Refunded,
            RowVersion = [14],
            FailureReason = "initial reason"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdatePaymentDisputeReviewHandler(db, new TestStringLocalizer());
        var result = await handler.HandleAsync(new UpdatePaymentDisputeReviewDto
        {
            Id = paymentId,
            RowVersion = [14],
            Action = UpdatePaymentDisputeReviewHandler.ResolveLostAction
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var updated = await db.Set<Payment>().SingleAsync(x => x.Id == paymentId, TestContext.Current.CancellationToken);
        updated.Status.Should().Be(PaymentStatus.Refunded);
        UpdatePaymentDisputeReviewHandler.ResolveDisputeReviewState(updated.FailureReason).Should().Be("Lost");
    }

    [Fact]
    public async Task UpdatePaymentDisputeReviewHandler_Should_ClearTrimmedFailureReason_WhenNoMarker()
    {
        await using var db = BillingUpdateTestDbContext.Create();
        var paymentId = Guid.NewGuid();
        db.Set<Payment>().Add(new Payment
        {
            Id = paymentId,
            Provider = "Stripe",
            Currency = "EUR",
            Status = PaymentStatus.Pending,
            RowVersion = [16],
            FailureReason = "  initial payment failed  "
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdatePaymentDisputeReviewHandler(db, new TestStringLocalizer());
        var result = await handler.HandleAsync(new UpdatePaymentDisputeReviewDto
        {
            Id = paymentId,
            RowVersion = [16],
            Action = UpdatePaymentDisputeReviewHandler.ClearAction
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var updated = await db.Set<Payment>().SingleAsync(x => x.Id == paymentId, TestContext.Current.CancellationToken);
        updated.FailureReason.Should().Be("initial payment failed");
    }

    private sealed class BillingUpdateTestDbContext : DbContext, IAppDbContext
    {
        private BillingUpdateTestDbContext(DbContextOptions<BillingUpdateTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static BillingUpdateTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<BillingUpdateTestDbContext>()
                .UseInMemoryDatabase($"darwin_billing_update_tests_{Guid.NewGuid()}")
                .Options;
            return new BillingUpdateTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Payment>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Provider).IsRequired();
                builder.Property(x => x.Currency).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<WebhookDelivery>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Status).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });
        }
    }

    private sealed class TestStringLocalizer : IStringLocalizer<ValidationResource>
    {
        public LocalizedString this[string name] => new(name, name, resourceNotFound: false);

        public LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(name, arguments), resourceNotFound: false);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            Array.Empty<LocalizedString>();

        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
    }
}
