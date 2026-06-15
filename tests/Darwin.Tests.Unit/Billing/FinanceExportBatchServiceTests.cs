using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Billing.Services;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.Integration;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Tests.Unit.Billing;

public sealed class FinanceExportBatchServiceTests
{
    private static readonly DateTime FixedNow = new(2033, 4, 5, 9, 30, 0, DateTimeKind.Utc);
    private static readonly DateTime PeriodStart = new(2033, 4, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime PeriodEnd = new(2033, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetOrCreateBatchAsync_Should_CreateIdempotentBusinessTargetPeriodBatch()
    {
        await using var db = FinanceExportBatchTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var targetId = SeedExternalSystem(db, ExternalSystemKind.Accounting);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = CreateService(db);
        var command = new FinanceExportBatchCommand(businessId, targetId, PeriodStart, PeriodEnd);

        var created = await service.GetOrCreateBatchAsync(command, TestContext.Current.CancellationToken);
        var duplicate = await service.GetOrCreateBatchAsync(command, TestContext.Current.CancellationToken);

        created.Succeeded.Should().BeTrue();
        created.Value!.Created.Should().BeTrue();
        duplicate.Succeeded.Should().BeTrue();
        duplicate.Value!.Created.Should().BeFalse();
        duplicate.Value.BatchId.Should().Be(created.Value.BatchId);
        db.Set<FinanceExportBatch>().Should().ContainSingle();
        var batch = await db.Set<FinanceExportBatch>().SingleAsync(TestContext.Current.CancellationToken);
        batch.ExportKey.Should().Be(FinanceExportBatchService.BuildExportKey(command));
        batch.Status.Should().Be(FinanceExportBatchStatus.Draft);
    }

    [Fact]
    public async Task GetOrCreateBatchAsync_Should_RejectInactiveOrNonAccountingTargets_AndSensitiveMetadata()
    {
        await using var db = FinanceExportBatchTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var crmTargetId = SeedExternalSystem(db, ExternalSystemKind.Crm);
        var inactiveTargetId = SeedExternalSystem(db, ExternalSystemKind.Accounting, isActive: false);
        var accountingTargetId = SeedExternalSystem(db, ExternalSystemKind.Accounting);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = CreateService(db);

        var nonAccounting = await service.GetOrCreateBatchAsync(new FinanceExportBatchCommand(businessId, crmTargetId, PeriodStart, PeriodEnd), TestContext.Current.CancellationToken);
        var inactive = await service.GetOrCreateBatchAsync(new FinanceExportBatchCommand(businessId, inactiveTargetId, PeriodStart, PeriodEnd), TestContext.Current.CancellationToken);
        var sensitive = await service.GetOrCreateBatchAsync(new FinanceExportBatchCommand(
            businessId,
            accountingTargetId,
            PeriodStart,
            PeriodEnd,
            MetadataJson: "{\"accessToken\":\"secret\"}"), TestContext.Current.CancellationToken);

        nonAccounting.Succeeded.Should().BeFalse();
        inactive.Succeeded.Should().BeFalse();
        sensitive.Succeeded.Should().BeFalse();
        sensitive.Error.Should().Contain("Sensitive");
        db.Set<FinanceExportBatch>().Should().BeEmpty();
    }

    [Fact]
    public async Task Attempts_Should_StartCompleteAndUpdateBatchGeneratedState()
    {
        await using var db = FinanceExportBatchTestDbContext.Create();
        var batchId = await CreateBatchAsync(db);
        var service = CreateService(db);

        var attempt = await service.StartAttemptAsync(batchId, "{\"source\":\"unit\"}", TestContext.Current.CancellationToken);
        var completed = await service.CompleteAttemptAsync(
            attempt.Value!.AttemptId,
            "abc123",
            "text/csv",
            "finance-export.csv",
            TestContext.Current.CancellationToken);

        attempt.Succeeded.Should().BeTrue();
        completed.Succeeded.Should().BeTrue();
        var batch = await db.Set<FinanceExportBatch>()
            .Include(x => x.Attempts)
            .SingleAsync(TestContext.Current.CancellationToken);
        batch.Status.Should().Be(FinanceExportBatchStatus.Generated);
        batch.GeneratedAtUtc.Should().Be(FixedNow);
        batch.PackageHashSha256.Should().Be("abc123");
        batch.Attempts.Should().ContainSingle(x =>
            x.AttemptNumber == 1 &&
            x.Status == FinanceExportAttemptStatus.Succeeded &&
            x.CompletedAtUtc == FixedNow);
    }

    [Fact]
    public async Task Attempts_Should_RecordSafeFailureAndAllowNewAttemptNumber()
    {
        await using var db = FinanceExportBatchTestDbContext.Create();
        var batchId = await CreateBatchAsync(db);
        var service = CreateService(db);
        var first = await service.StartAttemptAsync(batchId, ct: TestContext.Current.CancellationToken);

        var failed = await service.FailAttemptAsync(first.Value!.AttemptId, "Package validation failed", TestContext.Current.CancellationToken);
        var second = await service.StartAttemptAsync(batchId, ct: TestContext.Current.CancellationToken);
        var sensitiveFailure = await service.FailAttemptAsync(second.Value!.AttemptId, "provider token leaked", TestContext.Current.CancellationToken);

        failed.Succeeded.Should().BeTrue();
        second.Succeeded.Should().BeTrue();
        second.Value!.AttemptNumber.Should().Be(2);
        sensitiveFailure.Succeeded.Should().BeFalse();
        var batch = await db.Set<FinanceExportBatch>()
            .Include(x => x.Attempts)
            .SingleAsync(TestContext.Current.CancellationToken);
        batch.Status.Should().Be(FinanceExportBatchStatus.Failed);
        batch.ErrorSummary.Should().Be("Package validation failed");
        batch.Attempts.Should().ContainSingle(x => x.Status == FinanceExportAttemptStatus.Failed);
        batch.Attempts.Should().ContainSingle(x => x.Status == FinanceExportAttemptStatus.Started && x.AttemptNumber == 2);
    }

    private static async Task<Guid> CreateBatchAsync(FinanceExportBatchTestDbContext db)
    {
        var businessId = Guid.NewGuid();
        var targetId = SeedExternalSystem(db, ExternalSystemKind.Accounting);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var created = await CreateService(db).GetOrCreateBatchAsync(
            new FinanceExportBatchCommand(businessId, targetId, PeriodStart, PeriodEnd),
            TestContext.Current.CancellationToken);
        return created.Value!.BatchId;
    }

    private static FinanceExportBatchService CreateService(IAppDbContext db)
        => new(db, new FixedClock(FixedNow));

    private static Guid SeedExternalSystem(FinanceExportBatchTestDbContext db, ExternalSystemKind kind, bool isActive = true)
    {
        var system = new ExternalSystem
        {
            Id = Guid.NewGuid(),
            Code = kind.ToString().ToLowerInvariant(),
            Name = kind.ToString(),
            Kind = kind,
            IsActive = isActive
        };
        db.Set<ExternalSystem>().Add(system);
        return system.Id;
    }

    private sealed class FixedClock : IClock
    {
        private readonly DateTime _utcNow;

        public FixedClock(DateTime utcNow) => _utcNow = utcNow;

        public DateTime UtcNow => _utcNow;
    }

    private sealed class FinanceExportBatchTestDbContext : DbContext, IAppDbContext
    {
        private FinanceExportBatchTestDbContext(DbContextOptions<FinanceExportBatchTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static FinanceExportBatchTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<FinanceExportBatchTestDbContext>()
                .UseInMemoryDatabase($"darwin_finance_export_batch_tests_{Guid.NewGuid()}")
                .Options;
            return new FinanceExportBatchTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ExternalSystem>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Code).IsRequired();
                b.Property(x => x.Name).IsRequired();
            });

            modelBuilder.Entity<FinanceExportBatch>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ExportKey).IsRequired();
                b.Property(x => x.MetadataJson).IsRequired();
                b.HasMany(x => x.Attempts).WithOne().HasForeignKey(x => x.FinanceExportBatchId);
            });

            modelBuilder.Entity<FinanceExportAttempt>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.MetadataJson).IsRequired();
            });
        }
    }
}
