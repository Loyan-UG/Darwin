using System.Security.Cryptography;
using System.Text;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Abstractions.Storage;
using Darwin.Application.Billing.Queries;
using Darwin.Application.Billing.Services;
using Darwin.Application.Foundation;
using Darwin.Application.Integration;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.Foundation;
using Darwin.Domain.Entities.Integration;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Tests.Unit.Billing;

public sealed class FinanceExportConnectorDeliveryServiceTests
{
    private static readonly DateTime FixedNow = new(2033, 7, 8, 9, 10, 11, DateTimeKind.Utc);
    private static readonly DateTime PeriodStart = new(2033, 7, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime PeriodEnd = new(2033, 8, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task DeliverAsync_Should_DeliverStoredPackage_RecordReference_AndMarkBatchDelivered()
    {
        await using var db = FinanceExportConnectorTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var targetId = SeedExternalSystem(db, ExternalSystemKind.Accounting);
        var batchId = await SeedGeneratedBatchWithStoredPackageAsync(db, businessId, targetId);
        var storage = await CreateStorageWithPackageAsync(db, batchId);
        var adapter = new RecordingConnectorAdapter(new FinanceExportConnectorAdapterDeliveryResult("remote-batch-1", "Remote Batch 1", FixedNow, "Accepted"));
        var service = CreateDeliveryService(db, storage, adapter);

        var result = await service.DeliverAsync(new FinanceExportConnectorDeliveryCommand(batchId), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        adapter.Deliveries.Should().ContainSingle();
        adapter.Deliveries[0].PackageJson.Should().Contain("\"entries\":[]");
        var batch = await db.Set<FinanceExportBatch>().Include(x => x.Attempts).SingleAsync(x => x.Id == batchId, TestContext.Current.CancellationToken);
        batch.Status.Should().Be(FinanceExportBatchStatus.Delivered);
        batch.DeliveredAtUtc.Should().Be(FixedNow);
        batch.Attempts.Should().ContainSingle(x => x.Status == FinanceExportAttemptStatus.Succeeded);
        var reference = await db.Set<ExternalReference>().SingleAsync(TestContext.Current.CancellationToken);
        reference.EntityType.Should().Be(FinanceExportConnectorDeliveryService.EntityType);
        reference.EntityId.Should().Be(batchId);
        reference.ReferenceKind.Should().Be(ExternalReferenceKind.Export);
        reference.ExternalId.Should().Be("remote-batch-1");
    }

    [Fact]
    public async Task DeliverAsync_Should_FailAttempt_AndKeepBatchGenerated_WhenStoredPackageObjectIsMissing()
    {
        await using var db = FinanceExportConnectorTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var targetId = SeedExternalSystem(db, ExternalSystemKind.Accounting);
        var batchId = await SeedGeneratedBatchWithStoredPackageAsync(db, businessId, targetId, addDocumentOnly: true);
        var service = CreateDeliveryService(db, new RecordingObjectStorageService(), new RecordingConnectorAdapter());

        var result = await service.DeliverAsync(new FinanceExportConnectorDeliveryCommand(batchId), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        var batch = await db.Set<FinanceExportBatch>().Include(x => x.Attempts).SingleAsync(x => x.Id == batchId, TestContext.Current.CancellationToken);
        batch.Status.Should().Be(FinanceExportBatchStatus.Generated);
        batch.Attempts.Should().ContainSingle(x => x.Status == FinanceExportAttemptStatus.Failed);
        db.Set<ExternalReference>().Should().BeEmpty();
    }

    [Theory]
    [InlineData(FinanceExportBatchStatus.Draft)]
    [InlineData(FinanceExportBatchStatus.Failed)]
    [InlineData(FinanceExportBatchStatus.Cancelled)]
    [InlineData(FinanceExportBatchStatus.Delivered)]
    public async Task DeliverAsync_Should_RejectNonGeneratedBatches(FinanceExportBatchStatus status)
    {
        await using var db = FinanceExportConnectorTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var targetId = SeedExternalSystem(db, ExternalSystemKind.Accounting);
        var batchId = await SeedGeneratedBatchWithStoredPackageAsync(db, businessId, targetId);
        var batch = await db.Set<FinanceExportBatch>().SingleAsync(x => x.Id == batchId, TestContext.Current.CancellationToken);
        batch.Status = status;
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await CreateDeliveryService(db, new RecordingObjectStorageService(), new RecordingConnectorAdapter())
            .DeliverAsync(new FinanceExportConnectorDeliveryCommand(batchId), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        db.Set<FinanceExportAttempt>().Should().BeEmpty();
    }

    [Theory]
    [InlineData(ExternalSystemKind.Crm, true)]
    [InlineData(ExternalSystemKind.Accounting, false)]
    public async Task DeliverAsync_Should_RejectInactiveOrNonAccountingTargets(ExternalSystemKind kind, bool isActive)
    {
        await using var db = FinanceExportConnectorTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var targetId = SeedExternalSystem(db, kind, isActive);
        var batchId = await SeedGeneratedBatchWithStoredPackageAsync(db, businessId, targetId);

        var result = await CreateDeliveryService(db, new RecordingObjectStorageService(), new RecordingConnectorAdapter())
            .DeliverAsync(new FinanceExportConnectorDeliveryCommand(batchId), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        db.Set<FinanceExportAttempt>().Should().BeEmpty();
    }

    [Fact]
    public async Task DeliverAsync_Should_FailAttempt_AndKeepBatchGenerated_WhenAdapterFails()
    {
        await using var db = FinanceExportConnectorTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var targetId = SeedExternalSystem(db, ExternalSystemKind.Accounting);
        var batchId = await SeedGeneratedBatchWithStoredPackageAsync(db, businessId, targetId);
        var storage = await CreateStorageWithPackageAsync(db, batchId);
        var adapter = new RecordingConnectorAdapter(error: "remote unavailable");

        var result = await CreateDeliveryService(db, storage, adapter)
            .DeliverAsync(new FinanceExportConnectorDeliveryCommand(batchId), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        var batch = await db.Set<FinanceExportBatch>().Include(x => x.Attempts).SingleAsync(x => x.Id == batchId, TestContext.Current.CancellationToken);
        batch.Status.Should().Be(FinanceExportBatchStatus.Generated);
        batch.Attempts.Should().ContainSingle(x => x.Status == FinanceExportAttemptStatus.Failed);
        db.Set<ExternalReference>().Should().BeEmpty();
    }

    [Fact]
    public async Task DeliverAsync_Should_NotCreateDuplicateReference_WhenDeliveredBatchIsRetried()
    {
        await using var db = FinanceExportConnectorTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var targetId = SeedExternalSystem(db, ExternalSystemKind.Accounting);
        var batchId = await SeedGeneratedBatchWithStoredPackageAsync(db, businessId, targetId);
        var storage = await CreateStorageWithPackageAsync(db, batchId);
        var service = CreateDeliveryService(db, storage, new RecordingConnectorAdapter(new FinanceExportConnectorAdapterDeliveryResult("remote-batch-1")));

        var first = await service.DeliverAsync(new FinanceExportConnectorDeliveryCommand(batchId), TestContext.Current.CancellationToken);
        var second = await service.DeliverAsync(new FinanceExportConnectorDeliveryCommand(batchId), TestContext.Current.CancellationToken);

        first.Succeeded.Should().BeTrue();
        second.Succeeded.Should().BeFalse();
        (await db.Set<ExternalReference>().CountAsync(TestContext.Current.CancellationToken)).Should().Be(1);
    }

    [Fact]
    public async Task DeliverAsync_Should_RejectSensitiveAdapterResponse()
    {
        await using var db = FinanceExportConnectorTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var targetId = SeedExternalSystem(db, ExternalSystemKind.Accounting);
        var batchId = await SeedGeneratedBatchWithStoredPackageAsync(db, businessId, targetId);
        var storage = await CreateStorageWithPackageAsync(db, batchId);
        var adapter = new RecordingConnectorAdapter(new FinanceExportConnectorAdapterDeliveryResult("access token leaked"));

        var result = await CreateDeliveryService(db, storage, adapter)
            .DeliverAsync(new FinanceExportConnectorDeliveryCommand(batchId), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        var batch = await db.Set<FinanceExportBatch>().Include(x => x.Attempts).SingleAsync(x => x.Id == batchId, TestContext.Current.CancellationToken);
        batch.Status.Should().Be(FinanceExportBatchStatus.Generated);
        batch.Attempts.Should().ContainSingle(x => x.Status == FinanceExportAttemptStatus.Failed);
        db.Set<ExternalReference>().Should().BeEmpty();
    }

    [Fact]
    public async Task PushHandler_Should_DeliverStoredPackage_WhenAdapterIsAvailable()
    {
        await using var db = FinanceExportConnectorTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var targetId = SeedExternalSystem(db, ExternalSystemKind.Accounting);
        var batchId = await SeedGeneratedBatchWithStoredPackageAsync(db, businessId, targetId);
        var storage = await CreateStorageWithPackageAsync(db, batchId);
        var handler = new PushFinanceExportPackageHandler(CreateDeliveryService(
            db,
            storage,
            new RecordingConnectorAdapter(new FinanceExportConnectorAdapterDeliveryResult("remote-batch-1", "Remote Batch 1"))));

        var result = await handler.HandleAsync(batchId, TestContext.Current.CancellationToken);

        result.FinanceExportBatchId.Should().Be(batchId);
        result.RemoteId.Should().Be("remote-batch-1");
        var batch = await db.Set<FinanceExportBatch>().SingleAsync(x => x.Id == batchId, TestContext.Current.CancellationToken);
        batch.Status.Should().Be(FinanceExportBatchStatus.Delivered);
    }

    [Fact]
    public async Task PushHandler_Should_Throw_AndKeepBatchGenerated_WhenAdapterIsMissing()
    {
        await using var db = FinanceExportConnectorTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var targetId = SeedExternalSystem(db, ExternalSystemKind.Accounting);
        var batchId = await SeedGeneratedBatchWithStoredPackageAsync(db, businessId, targetId);
        var storage = await CreateStorageWithPackageAsync(db, batchId);
        var handler = new PushFinanceExportPackageHandler(CreateDeliveryService(db, storage));

        var act = async () => await handler.HandleAsync(batchId, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>();
        var batch = await db.Set<FinanceExportBatch>().SingleAsync(x => x.Id == batchId, TestContext.Current.CancellationToken);
        batch.Status.Should().Be(FinanceExportBatchStatus.Generated);
    }

    private static FinanceExportConnectorDeliveryService CreateDeliveryService(
        FinanceExportConnectorTestDbContext db,
        IObjectStorageService storage,
        params IFinanceExportConnectorAdapter[] adapters)
        => new(
            db,
            CreateBatchService(db),
            new FinanceExportPackageStorageService(db, storage, new DocumentRecordService(db), CreateBatchService(db), CreatePackageBuilder(db)),
            new ExternalSystemReferenceService(db),
            adapters);

    private static FinanceExportBatchService CreateBatchService(IAppDbContext db)
        => new(db, new FixedClock(FixedNow));

    private static FinanceExportPackageBuilderService CreatePackageBuilder(IAppDbContext db)
        => new(db, new FixedClock(FixedNow), CreateBatchService(db));

    private static async Task<RecordingObjectStorageService> CreateStorageWithPackageAsync(
        FinanceExportConnectorTestDbContext db,
        Guid batchId)
    {
        var storage = new RecordingObjectStorageService();
        var document = await db.Set<DocumentRecord>().SingleAsync(x => x.EntityId == batchId, TestContext.Current.CancellationToken);
        await using var content = new MemoryStream(Encoding.UTF8.GetBytes(PackageJson), writable: false);
        await storage.SaveAsync(new ObjectStorageWriteRequest(
                document.StorageContainer,
                document.StorageKey,
                document.ContentType,
                document.FileName,
                content,
                Encoding.UTF8.GetByteCount(PackageJson),
                document.ContentHash,
                ProfileName: FinanceExportPackageStorageService.ProfileName),
            TestContext.Current.CancellationToken);
        return storage;
    }

    private static async Task<Guid> SeedGeneratedBatchWithStoredPackageAsync(
        FinanceExportConnectorTestDbContext db,
        Guid businessId,
        Guid targetId,
        bool addDocumentOnly = false)
    {
        var batch = new FinanceExportBatch
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            ExternalSystemId = targetId,
            ExportKey = "finance-export-test",
            PeriodStartUtc = PeriodStart,
            PeriodEndUtc = PeriodEnd,
            PostingStatusMode = FinanceExportPostingStatusMode.PostedAndReversed,
            Status = FinanceExportBatchStatus.Generated,
            GeneratedAtUtc = FixedNow,
            PackageHashSha256 = PackageHash,
            PackageContentType = FinanceExportPackageBuilderService.PackageContentType,
            PackageFileName = "finance-export-test.json"
        };
        db.Set<FinanceExportBatch>().Add(batch);
        db.Set<DocumentRecord>().Add(new DocumentRecord
        {
            EntityType = FinanceExportPackageStorageService.EntityType,
            EntityId = batch.Id,
            DocumentKind = DocumentRecordKind.Evidence,
            Title = "Finance export package",
            FileName = "finance-export-test.json",
            ContentType = FinanceExportPackageBuilderService.PackageContentType,
            SizeBytes = Encoding.UTF8.GetByteCount(PackageJson),
            ContentHash = PackageHash,
            StorageProvider = FinanceExportPackageStorageService.ProfileName,
            StorageContainer = FinanceExportPackageStorageService.ContainerName,
            StorageKey = "finance-exports/test/package.json",
            Visibility = FoundationVisibility.Internal
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return batch.Id;
    }

    private static Guid SeedExternalSystem(FinanceExportConnectorTestDbContext db, ExternalSystemKind kind, bool isActive = true)
    {
        var system = new ExternalSystem
        {
            Id = Guid.NewGuid(),
            Code = kind.ToString().ToUpperInvariant(),
            Name = kind.ToString(),
            Kind = kind,
            IsActive = isActive
        };
        db.Set<ExternalSystem>().Add(system);
        return system.Id;
    }

    private const string PackageJson = "{\"header\":{\"entryCount\":0},\"entries\":[]}";

    private static readonly string PackageHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(PackageJson))).ToLowerInvariant();

    private sealed class FixedClock : IClock
    {
        private readonly DateTime _utcNow;

        public FixedClock(DateTime utcNow) => _utcNow = utcNow;

        public DateTime UtcNow => _utcNow;
    }

    private sealed class RecordingConnectorAdapter : IFinanceExportConnectorAdapter
    {
        private readonly FinanceExportConnectorAdapterDeliveryResult? _result;
        private readonly string? _error;

        public RecordingConnectorAdapter(
            FinanceExportConnectorAdapterDeliveryResult? result = null,
            string? error = null)
        {
            _result = result ?? new FinanceExportConnectorAdapterDeliveryResult("remote-batch");
            _error = error;
        }

        public string AdapterCode => "test-adapter";

        public List<RecordedDelivery> Deliveries { get; } = new();

        public bool CanDeliver(FinanceExportConnectorTarget target)
            => target.Kind == ExternalSystemKind.Accounting;

        public async Task<Result<FinanceExportConnectorAdapterDeliveryResult>> DeliverAsync(
            FinanceExportConnectorAdapterDeliveryRequest request,
            CancellationToken ct = default)
        {
            using var reader = new StreamReader(request.PackageContent, Encoding.UTF8, leaveOpen: true);
            Deliveries.Add(new RecordedDelivery(await reader.ReadToEndAsync(ct), request.PackageHashSha256));
            return _error is null
                ? Result<FinanceExportConnectorAdapterDeliveryResult>.Ok(_result!)
                : Result<FinanceExportConnectorAdapterDeliveryResult>.Fail(_error);
        }
    }

    private sealed record RecordedDelivery(string PackageJson, string PackageHash);

    private sealed class RecordingObjectStorageService : IObjectStorageService
    {
        private readonly Dictionary<string, StoredObject> _objects = new(StringComparer.OrdinalIgnoreCase);

        public async Task<ObjectStorageWriteResult> SaveAsync(ObjectStorageWriteRequest request, CancellationToken ct = default)
        {
            await using var buffer = new MemoryStream();
            await request.Content.CopyToAsync(buffer, ct);
            var bytes = buffer.ToArray();
            var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
            _objects[StorageKey(request.ContainerName, request.ObjectKey)] = new StoredObject(bytes, request.ContentType, request.FileName, hash);
            return new ObjectStorageWriteResult(
                ObjectStorageProviderKind.FileSystem,
                request.ContainerName,
                request.ObjectKey,
                null,
                null,
                hash,
                bytes.LongLength,
                FixedNow,
                null,
                ObjectRetentionMode.None,
                false,
                null,
                false);
        }

        public Task<ObjectStorageReadResult?> ReadAsync(ObjectStorageObjectReference reference, CancellationToken ct = default)
            => Task.FromResult(_objects.TryGetValue(StorageKey(reference.ContainerName, reference.ObjectKey), out var value)
                ? new ObjectStorageReadResult(
                    new MemoryStream(value.Bytes, writable: false),
                    value.ContentType,
                    value.FileName,
                    value.Bytes.LongLength,
                    value.Sha256Hash)
                : null);

        public Task<bool> ExistsAsync(ObjectStorageObjectReference reference, CancellationToken ct = default)
            => Task.FromResult(_objects.ContainsKey(StorageKey(reference.ContainerName, reference.ObjectKey)));

        public Task<ObjectStorageObjectMetadata?> GetMetadataAsync(ObjectStorageObjectReference reference, CancellationToken ct = default)
            => Task.FromResult<ObjectStorageObjectMetadata?>(null);

        public Task DeleteAsync(ObjectStorageDeleteRequest request, CancellationToken ct = default)
        {
            _objects.Remove(StorageKey(request.Reference.ContainerName, request.Reference.ObjectKey));
            return Task.CompletedTask;
        }

        public Task<Uri?> GetTemporaryReadUrlAsync(ObjectStorageTemporaryUrlRequest request, CancellationToken ct = default)
            => Task.FromResult<Uri?>(null);

        public ObjectStorageCapabilities GetCapabilities(ObjectStorageContainerSelection selection)
            => new(
                ObjectStorageProviderKind.FileSystem,
                SupportsVersioning: false,
                SupportsObjectLock: false,
                SupportsRetention: false,
                SupportsLegalHold: false,
                SupportsTemporaryUrls: false,
                SupportsServerSideEncryption: false,
                SupportsConditionalWrites: true,
                SupportsNativeImmutability: false);

        private static string StorageKey(string container, string key) => container + "/" + key;

        private sealed record StoredObject(byte[] Bytes, string ContentType, string? FileName, string Sha256Hash);
    }

    private sealed class FinanceExportConnectorTestDbContext : DbContext, IAppDbContext
    {
        private FinanceExportConnectorTestDbContext(DbContextOptions<FinanceExportConnectorTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static FinanceExportConnectorTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<FinanceExportConnectorTestDbContext>()
                .UseInMemoryDatabase($"darwin_finance_export_connector_tests_{Guid.NewGuid()}")
                .Options;
            return new FinanceExportConnectorTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<ExternalSystem>().HasKey(x => x.Id);
            modelBuilder.Entity<ExternalReference>().HasKey(x => x.Id);
            modelBuilder.Entity<FinanceExportBatch>(b =>
            {
                b.HasKey(x => x.Id);
                b.HasMany(x => x.Attempts).WithOne().HasForeignKey(x => x.FinanceExportBatchId);
            });
            modelBuilder.Entity<FinanceExportAttempt>().HasKey(x => x.Id);
            modelBuilder.Entity<DocumentRecord>().HasKey(x => x.Id);
            modelBuilder.Entity<FinancialAccount>().HasKey(x => x.Id);
            modelBuilder.Entity<JournalEntry>(b =>
            {
                b.HasKey(x => x.Id);
                b.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.JournalEntryId);
            });
            modelBuilder.Entity<JournalEntryLine>().HasKey(x => x.Id);
        }
    }
}
