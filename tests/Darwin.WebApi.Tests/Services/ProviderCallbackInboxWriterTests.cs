using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Integration;
using Darwin.WebApi.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Darwin.WebApi.Tests.Services;

public sealed class ProviderCallbackInboxWriterTests
{
    [Fact]
    public void Ctor_Should_Throw_WhenDbContextIsNull()
    {
        Action act = () => new ProviderCallbackInboxWriter(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("db");
    }

    [Fact]
    public async Task AddIfNewAsync_Should_SaveMessageAndReturnFalse_WhenNoExistingMessage()
    {
        await using var db = ProviderCallbackInboxWriterTestDbContext.Create();
        var writer = new ProviderCallbackInboxWriter(db);

        var duplicate = await writer.AddIfNewAsync(
            provider: "Stripe",
            callbackType: "payment_intent.succeeded",
            idempotencyKey: "evt_test_new",
            rawPayload: "{}",
            TestContext.Current.CancellationToken);

        duplicate.Should().BeFalse();

        var count = await db.Set<ProviderCallbackInboxMessage>()
            .CountAsync(x => x.Provider == "Stripe" && x.IdempotencyKey == "evt_test_new", TestContext.Current.CancellationToken);
        count.Should().Be(1);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Provider == "Stripe" && x.IdempotencyKey == "evt_test_new", TestContext.Current.CancellationToken);
        item.CallbackType.Should().Be("payment_intent.succeeded");
        item.PayloadJson.Should().Be("{}");
        item.Status.Should().Be("Pending");
        item.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task AddIfNewAsync_Should_ReturnTrue_WhenMessageAlreadyExists()
    {
        await using var db = ProviderCallbackInboxWriterTestDbContext.Create();
        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "Stripe",
            CallbackType = "payment_intent.succeeded",
            IdempotencyKey = "evt_test_duplicate",
            PayloadJson = "{}"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var writer = new ProviderCallbackInboxWriter(db);
        var duplicate = await writer.AddIfNewAsync(
            provider: "Stripe",
            callbackType: "charge.refunded",
            idempotencyKey: "evt_test_duplicate",
            rawPayload: "{}",
            TestContext.Current.CancellationToken);

        duplicate.Should().BeTrue();

        var count = await db.Set<ProviderCallbackInboxMessage>()
            .CountAsync(x => x.Provider == "Stripe" && x.IdempotencyKey == "evt_test_duplicate", TestContext.Current.CancellationToken);
        count.Should().Be(1);
    }

    [Fact]
    public async Task AddIfNewAsync_Should_KeepOriginalPayload_WhenDuplicateDetected()
    {
        await using var db = ProviderCallbackInboxWriterTestDbContext.Create();
        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "Stripe",
            CallbackType = "payment_intent.created",
            IdempotencyKey = "evt_keep_payload",
            PayloadJson = "{ \"first\": true }"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var writer = new ProviderCallbackInboxWriter(db);
        var duplicate = await writer.AddIfNewAsync(
            provider: "Stripe",
            callbackType: "payment_intent.failed",
            idempotencyKey: "evt_keep_payload",
            rawPayload: "{ \"second\": true }",
            TestContext.Current.CancellationToken);

        duplicate.Should().BeTrue();

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Provider == "Stripe" && x.IdempotencyKey == "evt_keep_payload", TestContext.Current.CancellationToken);
        item.PayloadJson.Should().Be("{ \"first\": true }");
    }

    [Fact]
    public async Task AddIfNewAsync_Should_DetectDuplicate_IndependentlyFromCallbackType()
    {
        await using var db = ProviderCallbackInboxWriterTestDbContext.Create();
        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "Stripe",
            CallbackType = "payment_intent.created",
            IdempotencyKey = "evt_type_diff",
            PayloadJson = "{}"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var writer = new ProviderCallbackInboxWriter(db);
        var duplicate = await writer.AddIfNewAsync(
            provider: "Stripe",
            callbackType: "charge.refunded",
            idempotencyKey: "evt_type_diff",
            rawPayload: "{}",
            TestContext.Current.CancellationToken);

        duplicate.Should().BeTrue();
        var count = await db.Set<ProviderCallbackInboxMessage>()
            .CountAsync(x => x.Provider == "Stripe" && x.IdempotencyKey == "evt_type_diff", TestContext.Current.CancellationToken);
        count.Should().Be(1);
    }

    [Fact]
    public async Task AddIfNewAsync_Should_IgnoreDeletedMessages_WhenCheckingDuplicate()
    {
        await using var db = ProviderCallbackInboxWriterTestDbContext.Create();
        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "DHL",
            CallbackType = "delivered",
            IdempotencyKey = "dup_deleted",
            PayloadJson = "{}",
            IsDeleted = true
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var writer = new ProviderCallbackInboxWriter(db);
        var duplicate = await writer.AddIfNewAsync(
            provider: "DHL",
            callbackType: "delivered",
            idempotencyKey: "dup_deleted",
            rawPayload: "{}",
            TestContext.Current.CancellationToken);

        duplicate.Should().BeFalse();
        var count = await db.Set<ProviderCallbackInboxMessage>()
            .CountAsync(x => x.Provider == "DHL" && x.IdempotencyKey == "dup_deleted", TestContext.Current.CancellationToken);
        count.Should().Be(2);
    }

    [Fact]
    public async Task AddIfNewAsync_Should_Throw_WhenSaveFailsAndDuplicateDoesNotExist()
    {
        await using var db = ThrowingSaveDbContext.Create(throwOnSave: true);

        var writer = new ProviderCallbackInboxWriter(db);

        var act = async () => await writer.AddIfNewAsync(
            provider: "Stripe",
            callbackType: "payment_intent.succeeded",
            idempotencyKey: "evt_save_fail",
            rawPayload: "{}",
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task AddIfNewAsync_Should_ThrowOperationCanceled_WhenCancellationIsRequested()
    {
        await using var db = ProviderCallbackInboxWriterTestDbContext.Create();
        var writer = new ProviderCallbackInboxWriter(db);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        Func<Task> act = () => writer.AddIfNewAsync(
            provider: "Stripe",
            callbackType: "charge.failed",
            idempotencyKey: "evt_cancelled",
            rawPayload: "{}",
            cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task AddIfNewAsync_Should_PropagateSaveFailure_WhenSaveThrowsEvenAfterConcurrentInsertAttempt()
    {
        await using var db = ThrowingSaveDbContext.Create(throwOnSave: false);
        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "Stripe",
            CallbackType = "payment_intent.created",
            IdempotencyKey = "evt_concurrent",
            PayloadJson = "{}"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.EnableThrowOnSave();

        var writer = new ProviderCallbackInboxWriter(db);
        var act = async () => await writer.AddIfNewAsync(
            provider: "Stripe",
            callbackType: "payment_intent.succeeded",
            idempotencyKey: "evt_concurrent",
            rawPayload: "{}",
            TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task AddIfNewAsync_Should_Throw_WhenSaveThrowsAndDuplicateCheckForProviderSpecificKeyFails()
    {
        await using var db = ThrowingSaveDbContext.Create(throwOnSave: true);
        var writer = new ProviderCallbackInboxWriter(db);

        var act = async () => await writer.AddIfNewAsync(
            provider: "Stripe",
            callbackType: "payment_intent.succeeded",
            idempotencyKey: "evt_fallback",
            rawPayload: "{}",
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task AddIfNewAsync_Should_DetectDuplicate_WhenIdempotencyKeyIsEmpty()
    {
        await using var db = ProviderCallbackInboxWriterTestDbContext.Create();
        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "Stripe",
            CallbackType = "payment_intent.created",
            IdempotencyKey = string.Empty,
            PayloadJson = "{}",
            IsDeleted = false
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var writer = new ProviderCallbackInboxWriter(db);
        var duplicate = await writer.AddIfNewAsync(
            provider: "Stripe",
            callbackType: "charge.failed",
            idempotencyKey: string.Empty,
            rawPayload: "{}",
            TestContext.Current.CancellationToken);

        duplicate.Should().BeTrue();
        var count = await db.Set<ProviderCallbackInboxMessage>()
            .CountAsync(x => x.Provider == "Stripe" && x.IdempotencyKey == string.Empty, TestContext.Current.CancellationToken);
        count.Should().Be(1);
    }

    [Fact]
    public async Task AddIfNewAsync_Should_AllowSameIdempotencyForDifferentProvider()
    {
        await using var db = ProviderCallbackInboxWriterTestDbContext.Create();

        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "Stripe",
            CallbackType = "payment_intent.succeeded",
            IdempotencyKey = "evt_duplicate",
            PayloadJson = "{}"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var writer = new ProviderCallbackInboxWriter(db);
        var duplicate = await writer.AddIfNewAsync(
            provider: "DHL",
            callbackType: "delivered",
            idempotencyKey: "evt_duplicate",
            rawPayload: "{}",
            TestContext.Current.CancellationToken);

        duplicate.Should().BeFalse();

        var count = await db.Set<ProviderCallbackInboxMessage>()
            .CountAsync(x => x.IdempotencyKey == "evt_duplicate", TestContext.Current.CancellationToken);
        count.Should().Be(2);
    }

    [Fact]
    public async Task AddIfNewAsync_Should_SaveMessage_WhenIdempotencyKeyIsDifferent()
    {
        await using var db = ProviderCallbackInboxWriterTestDbContext.Create();

        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "Stripe",
            CallbackType = "payment_intent.created",
            IdempotencyKey = "evt_existing",
            PayloadJson = "{}"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var writer = new ProviderCallbackInboxWriter(db);
        var duplicate = await writer.AddIfNewAsync(
            provider: "Stripe",
            callbackType: "payment_intent.updated",
            idempotencyKey: "evt_new",
            rawPayload: "{}",
            TestContext.Current.CancellationToken);

        duplicate.Should().BeFalse();

        var count = await db.Set<ProviderCallbackInboxMessage>()
            .CountAsync(x => x.Provider == "Stripe", TestContext.Current.CancellationToken);
        count.Should().Be(2);
    }

    [Fact]
    public async Task AddIfNewAsync_Should_SaveMessage_WhenIdempotencyKeyIsNull()
    {
        await using var db = ProviderCallbackInboxWriterTestDbContext.Create();
        var writer = new ProviderCallbackInboxWriter(db);

        var duplicate = await writer.AddIfNewAsync(
            provider: "Stripe",
            callbackType: "payment_intent.succeeded",
            idempotencyKey: null!,
            rawPayload: "{ \"nullKey\": true }",
            TestContext.Current.CancellationToken);

        duplicate.Should().BeFalse();

        var count = await db.Set<ProviderCallbackInboxMessage>()
            .CountAsync(x => x.Provider == "Stripe" && x.IdempotencyKey == null, TestContext.Current.CancellationToken);
        count.Should().Be(1);
    }

    [Fact]
    public async Task AddIfNewAsync_Should_DetectDuplicate_WhenIdempotencyKeyIsNull()
    {
        await using var db = ProviderCallbackInboxWriterTestDbContext.Create();
        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "Stripe",
            CallbackType = "payment_intent.created",
            IdempotencyKey = null,
            PayloadJson = "{ \"first\": true }"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var writer = new ProviderCallbackInboxWriter(db);
        var duplicate = await writer.AddIfNewAsync(
            provider: "Stripe",
            callbackType: "payment_intent.updated",
            idempotencyKey: null!,
            rawPayload: "{ \"second\": true }",
            TestContext.Current.CancellationToken);

        duplicate.Should().BeTrue();

        var count = await db.Set<ProviderCallbackInboxMessage>()
            .CountAsync(x => x.Provider == "Stripe" && x.IdempotencyKey == null, TestContext.Current.CancellationToken);
        count.Should().Be(1);
    }

    [Fact]
    public async Task AddIfNewAsync_Should_NotTreatNullAndEmptyAsSameIdempotency()
    {
        await using var db = ProviderCallbackInboxWriterTestDbContext.Create();
        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "Stripe",
            CallbackType = "payment_intent.created",
            IdempotencyKey = null,
            PayloadJson = "{ \"first\": true }"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var writer = new ProviderCallbackInboxWriter(db);
        var duplicate = await writer.AddIfNewAsync(
            provider: "Stripe",
            callbackType: "payment_intent.failed",
            idempotencyKey: string.Empty,
            rawPayload: "{ \"second\": true }",
            TestContext.Current.CancellationToken);

        duplicate.Should().BeFalse();

        var count = await db.Set<ProviderCallbackInboxMessage>()
            .CountAsync(x => x.Provider == "Stripe" && x.IdempotencyKey == null, TestContext.Current.CancellationToken);
        count.Should().Be(1);
        var countWithEmpty = await db.Set<ProviderCallbackInboxMessage>()
            .CountAsync(x => x.Provider == "Stripe" && x.IdempotencyKey == string.Empty, TestContext.Current.CancellationToken);
        countWithEmpty.Should().Be(1);
    }

    [Fact]
    public async Task AddIfNewAsync_Should_DetectDuplicate_WhenWhitespaceIdempotencyMatches()
    {
        await using var db = ProviderCallbackInboxWriterTestDbContext.Create();
        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "Stripe",
            CallbackType = "payment_intent.created",
            IdempotencyKey = "  ",
            PayloadJson = "{ \"first\": true }"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var writer = new ProviderCallbackInboxWriter(db);
        var duplicate = await writer.AddIfNewAsync(
            provider: "Stripe",
            callbackType: "payment_intent.updated",
            idempotencyKey: "  ",
            rawPayload: "{ \"second\": true }",
            TestContext.Current.CancellationToken);

        duplicate.Should().BeTrue();

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Provider == "Stripe" && x.IdempotencyKey == "  ", TestContext.Current.CancellationToken);
        item.PayloadJson.Should().Be("{ \"first\": true }");
        item.CallbackType.Should().Be("payment_intent.created");
    }

    [Fact]
    public async Task AddIfNewAsync_Should_NotIncreaseCount_WhenConcurrentInsertCausesDbUpdateException()
    {
        await using var db = ThrowingSaveDbContext.Create(throwOnSave: false);
        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "Stripe",
            CallbackType = "payment_intent.created",
            IdempotencyKey = "evt_concurrent",
            PayloadJson = "{}"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.EnableThrowOnSave();

        var writer = new ProviderCallbackInboxWriter(db);
        var duplicate = await writer.AddIfNewAsync(
            provider: "Stripe",
            callbackType: "payment_intent.succeeded",
            idempotencyKey: "evt_concurrent",
            rawPayload: "{}",
            TestContext.Current.CancellationToken);

        duplicate.Should().BeTrue();

        var count = await db.Set<ProviderCallbackInboxMessage>()
            .CountAsync(x => x.Provider == "Stripe" && x.IdempotencyKey == "evt_concurrent", TestContext.Current.CancellationToken);
        count.Should().Be(1);
    }

    private sealed class ProviderCallbackInboxWriterTestDbContext : DbContext, IAppDbContext
    {
        public DbSet<ProviderCallbackInboxMessage> ProviderCallbackInboxMessages { get; set; }

        private ProviderCallbackInboxWriterTestDbContext(DbContextOptions<ProviderCallbackInboxWriterTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static ProviderCallbackInboxWriterTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<ProviderCallbackInboxWriterTestDbContext>()
                .UseInMemoryDatabase($"darwin_provider_callback_inbox_writer_tests_{Guid.NewGuid()}")
                .Options;

            return new ProviderCallbackInboxWriterTestDbContext(options);
        }
    }

    private sealed class ThrowingSaveDbContext : DbContext, IAppDbContext
    {
        private bool _throwOnSave;

        public DbSet<ProviderCallbackInboxMessage> ProviderCallbackInboxMessages { get; set; } = null!;

        private ThrowingSaveDbContext(DbContextOptions<ThrowingSaveDbContext> options, bool throwOnSave)
            : base(options)
        {
            _throwOnSave = throwOnSave;
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static ThrowingSaveDbContext Create(bool throwOnSave)
        {
            var options = new DbContextOptionsBuilder<ThrowingSaveDbContext>()
                .UseInMemoryDatabase($"darwin_provider_callback_inbox_writer_throw_tests_{Guid.NewGuid()}")
                .Options;

            return new ThrowingSaveDbContext(options, throwOnSave);
        }

        public void EnableThrowOnSave()
        {
            _throwOnSave = true;
        }

        public override int SaveChanges()
        {
            if (_throwOnSave)
            {
                throw new DbUpdateException("simulated db failure");
            }

            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (_throwOnSave)
            {
                return Task.FromException<int>(new DbUpdateException("simulated db failure"));
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
