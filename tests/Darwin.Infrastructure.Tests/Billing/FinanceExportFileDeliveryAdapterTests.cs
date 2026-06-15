using System.Security.Cryptography;
using System.Text;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Storage;
using Darwin.Application.Billing.Services;
using Darwin.Application.Foundation;
using Darwin.Application.Integration;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.Foundation;
using Darwin.Domain.Entities.Integration;
using Darwin.Domain.Enums;
using Darwin.Infrastructure.Billing;
using Darwin.Infrastructure.Extensions;
using Darwin.Infrastructure.Storage;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Darwin.Infrastructure.Tests.Billing;

public sealed class FinanceExportFileDeliveryAdapterTests
{
    private static readonly DateTime FixedNow = new(2034, 2, 3, 4, 5, 6, DateTimeKind.Utc);
    private static readonly DateTime PeriodStart = new(2034, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime PeriodEnd = new(2034, 2, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task DeliverAsync_Should_WriteOutboundObject_AndReturnStableReceipt()
    {
        var storage = new RecordingObjectStorageService();
        var adapter = CreateAdapter(storage);
        var request = CreateRequest();

        var result = await adapter.DeliverAsync(request, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.RemoteId.Should().Be(FinanceExportFileDeliveryAdapter.BuildObjectKey(request));
        result.Value.RemoteDisplayId.Should().Be(FinanceExportFileDeliveryAdapter.BuildFileName(request));
        result.Value.DeliveredAtUtc.Should().Be(FixedNow);
        storage.Writes.Should().ContainSingle();
        storage.Writes[0].ProfileName.Should().Be(FinanceExportFileDeliveryAdapter.ProfileName);
        storage.Writes[0].ContainerName.Should().Be(FinanceExportFileDeliveryAdapter.ContainerName);
        storage.Writes[0].ExpectedSha256Hash.Should().Be(PackageHash);
        storage.Writes[0].OverwritePolicy.Should().Be(ObjectOverwritePolicy.Disallow);
    }

    [Fact]
    public async Task DeliverAsync_Should_Be_Idempotent_WhenExistingObjectHashMatches()
    {
        var storage = new RecordingObjectStorageService();
        var request = CreateRequest();
        storage.Seed(FinanceExportFileDeliveryAdapter.ContainerName, FinanceExportFileDeliveryAdapter.BuildObjectKey(request), PackageBytes, PackageHash);
        var adapter = CreateAdapter(storage);

        var result = await adapter.DeliverAsync(request, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        storage.Writes.Should().BeEmpty();
        result.Value!.SafeSummary.Should().Contain("already stored");
    }

    [Fact]
    public async Task DeliverAsync_Should_ReturnResolvedExistingObjectKey_WhenProfilePrefixWasApplied()
    {
        var storage = new RecordingObjectStorageService
        {
            ExistingMetadataObjectKey = "outbound/" + FinanceExportFileDeliveryAdapter.BuildObjectKey(CreateRequest())
        };
        var request = CreateRequest();
        storage.Seed(FinanceExportFileDeliveryAdapter.ContainerName, FinanceExportFileDeliveryAdapter.BuildObjectKey(request), PackageBytes, PackageHash);
        var adapter = CreateAdapter(storage);

        var result = await adapter.DeliverAsync(request, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.RemoteId.Should().Be(storage.ExistingMetadataObjectKey);
        storage.Writes.Should().BeEmpty();
    }

    [Fact]
    public async Task DeliverAsync_Should_Fail_WhenExistingObjectHashDiffers()
    {
        var storage = new RecordingObjectStorageService();
        var request = CreateRequest();
        storage.Seed(
            FinanceExportFileDeliveryAdapter.ContainerName,
            FinanceExportFileDeliveryAdapter.BuildObjectKey(request),
            "different"u8.ToArray(),
            Hash("different"));
        var adapter = CreateAdapter(storage);

        var result = await adapter.DeliverAsync(request, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("hash does not match");
        storage.Writes.Should().BeEmpty();
    }

    [Fact]
    public async Task DeliverAsync_Should_Fail_WhenOutboundProfileIsMissingOrDatabaseBacked()
    {
        var adapter = CreateAdapter(new RecordingObjectStorageService(), provider: ObjectStorageProviderKind.Database);

        var result = await adapter.DeliverAsync(CreateRequest(), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("not configured");
        adapter.CanDeliver(CreateRequest().Target).Should().BeFalse();
    }

    [Fact]
    public async Task DeliverAsync_Should_Fail_WhenStorageWriteHashDiffers()
    {
        var storage = new RecordingObjectStorageService { OverrideWriteHash = Hash("wrong") };
        var adapter = CreateAdapter(storage);

        var result = await adapter.DeliverAsync(CreateRequest(), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("hash does not match");
    }

    [Fact]
    public void AddFinanceExportFileDeliveryAdapterIfConfigured_Should_Register_Only_For_NonDatabase_Profile()
    {
        var readyConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ObjectStorage:Profiles:{FinanceExportFileDeliveryAdapter.ProfileName}:Provider"] = "FileSystem"
            })
            .Build();
        var readyServices = new ServiceCollection();
        readyServices.AddFinanceExportFileDeliveryAdapterIfConfigured(readyConfiguration);

        readyServices.Count(x => x.ServiceType == typeof(IFinanceExportConnectorAdapter) &&
                                 x.ImplementationType == typeof(FinanceExportFileDeliveryAdapter))
            .Should().Be(1);

        var missingConfiguration = new ConfigurationBuilder().Build();
        var missingServices = new ServiceCollection();
        missingServices.AddFinanceExportFileDeliveryAdapterIfConfigured(missingConfiguration);

        missingServices.Should().NotContain(x => x.ServiceType == typeof(IFinanceExportConnectorAdapter));
    }

    [Fact]
    public async Task DeliveryService_Should_CopyStoredPackage_ToOutboundProfile_AndMarkBatchDelivered()
    {
        var root = Path.Combine(Path.GetTempPath(), "darwin-finance-export-delivery-" + Guid.NewGuid().ToString("N"));
        await using var db = FinanceExportDeliverySmokeDbContext.Create();
        var businessId = Guid.NewGuid();
        var targetId = SeedExternalSystem(db);
        var batchId = await SeedGeneratedBatchWithPackageDocumentAsync(db, businessId, targetId, "source/package.json");
        var provider = BuildProvider(root, configureOutbound: true);
        using var scope = provider.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IObjectStorageService>();
        await SaveSourcePackageAsync(storage, "source/package.json");
        var adapter = scope.ServiceProvider.GetServices<IFinanceExportConnectorAdapter>().Single();
        var service = CreateDeliveryService(db, storage, adapter);

        var result = await service.DeliverAsync(new FinanceExportConnectorDeliveryCommand(batchId), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var batch = await db.Set<FinanceExportBatch>().Include(x => x.Attempts).SingleAsync(x => x.Id == batchId, TestContext.Current.CancellationToken);
        batch.Status.Should().Be(FinanceExportBatchStatus.Delivered);
        batch.Attempts.Should().Contain(x => x.Status == FinanceExportAttemptStatus.Succeeded);
        var reference = await db.Set<ExternalReference>().SingleAsync(TestContext.Current.CancellationToken);
        reference.ReferenceKind.Should().Be(ExternalReferenceKind.Export);
        reference.ExternalId.Should().Contain("outbound/");
        File.Exists(Path.Combine(root, FinanceExportFileDeliveryAdapter.ContainerName, reference.ExternalId.Replace('/', Path.DirectorySeparatorChar))).Should().BeTrue();
    }

    [Fact]
    public async Task DeliveryService_Should_KeepBatchGenerated_WhenOutboundProfileIsMissing()
    {
        var root = Path.Combine(Path.GetTempPath(), "darwin-finance-export-missing-outbound-" + Guid.NewGuid().ToString("N"));
        await using var db = FinanceExportDeliverySmokeDbContext.Create();
        var businessId = Guid.NewGuid();
        var targetId = SeedExternalSystem(db);
        var batchId = await SeedGeneratedBatchWithPackageDocumentAsync(db, businessId, targetId, "source/package.json");
        var provider = BuildProvider(root, configureOutbound: false);
        using var scope = provider.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IObjectStorageService>();
        await SaveSourcePackageAsync(storage, "source/package.json");
        var service = CreateDeliveryService(db, storage, scope.ServiceProvider.GetServices<IFinanceExportConnectorAdapter>().ToArray());

        var result = await service.DeliverAsync(new FinanceExportConnectorDeliveryCommand(batchId), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("No finance export connector adapter");
        var batch = await db.Set<FinanceExportBatch>().SingleAsync(x => x.Id == batchId, TestContext.Current.CancellationToken);
        batch.Status.Should().Be(FinanceExportBatchStatus.Generated);
    }

    [Fact]
    public async Task DeliveryService_Should_KeepBatchGenerated_WhenDestinationObjectHashDiffers()
    {
        var root = Path.Combine(Path.GetTempPath(), "darwin-finance-export-conflict-" + Guid.NewGuid().ToString("N"));
        await using var db = FinanceExportDeliverySmokeDbContext.Create();
        var businessId = Guid.NewGuid();
        var targetId = SeedExternalSystem(db);
        var batchId = await SeedGeneratedBatchWithPackageDocumentAsync(db, businessId, targetId, "source/package.json");
        var provider = BuildProvider(root, configureOutbound: true);
        using var scope = provider.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IObjectStorageService>();
        await SaveSourcePackageAsync(storage, "source/package.json");
        var adapter = scope.ServiceProvider.GetServices<IFinanceExportConnectorAdapter>().Single();
        var request = CreateRequest(batchId, businessId, targetId);
        await using (var conflicting = new MemoryStream("conflict"u8.ToArray(), writable: false))
        {
            await storage.SaveAsync(
                new ObjectStorageWriteRequest(
                    FinanceExportFileDeliveryAdapter.ContainerName,
                    FinanceExportFileDeliveryAdapter.BuildObjectKey(request),
                    FinanceExportPackageBuilderService.PackageContentType,
                    "conflict.json",
                    conflicting,
                    "conflict"u8.ToArray().LongLength,
                    Hash("conflict"),
                    OverwritePolicy: ObjectOverwritePolicy.Disallow,
                    ProfileName: FinanceExportFileDeliveryAdapter.ProfileName),
                TestContext.Current.CancellationToken);
        }

        var service = CreateDeliveryService(db, storage, adapter);

        var result = await service.DeliverAsync(new FinanceExportConnectorDeliveryCommand(batchId), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("hash does not match");
        var batch = await db.Set<FinanceExportBatch>().Include(x => x.Attempts).SingleAsync(x => x.Id == batchId, TestContext.Current.CancellationToken);
        batch.Status.Should().Be(FinanceExportBatchStatus.Generated);
        batch.Attempts.Should().Contain(x => x.Status == FinanceExportAttemptStatus.Failed);
    }

    private static FinanceExportFileDeliveryAdapter CreateAdapter(
        IObjectStorageService storage,
        ObjectStorageProviderKind provider = ObjectStorageProviderKind.FileSystem)
        => new(
            storage,
            Options.Create(new ObjectStorageOptions
            {
                Profiles =
                {
                    [FinanceExportFileDeliveryAdapter.ProfileName] = new ObjectStorageProfileOptions
                    {
                        Provider = provider,
                        ContainerName = FinanceExportFileDeliveryAdapter.ContainerName
                    }
                }
            }),
            new FixedClock(FixedNow));

    private static FinanceExportConnectorAdapterDeliveryRequest CreateRequest()
        => new(
            Guid.Parse("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
            Guid.Parse("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"),
            "finance-export-test",
            PeriodStart,
            PeriodEnd,
            FinanceExportPostingStatusMode.PostedAndReversed,
            new FinanceExportConnectorTarget(
                Guid.Parse("cccccccccccccccccccccccccccccccc"),
                "accounting-target",
                "Accounting Target",
                ExternalSystemKind.Accounting,
                null,
                "{}"),
            new MemoryStream(PackageBytes, writable: false),
            FinanceExportPackageBuilderService.PackageContentType,
            "finance-export-test.json",
            PackageHash,
            PackageBytes.LongLength);

    private static FinanceExportConnectorAdapterDeliveryRequest CreateRequest(Guid batchId, Guid businessId, Guid targetId)
        => new(
            batchId,
            businessId,
            "finance-export-test",
            PeriodStart,
            PeriodEnd,
            FinanceExportPostingStatusMode.PostedAndReversed,
            new FinanceExportConnectorTarget(
                targetId,
                "accounting-target",
                "Accounting Target",
                ExternalSystemKind.Accounting,
                null,
                "{}"),
            new MemoryStream(PackageBytes, writable: false),
            FinanceExportPackageBuilderService.PackageContentType,
            "finance-export-test.json",
            PackageHash,
            PackageBytes.LongLength);

    private static readonly byte[] PackageBytes = Encoding.UTF8.GetBytes("{\"header\":{\"entryCount\":0},\"entries\":[]}");

    private static readonly string PackageHash = Hash(PackageBytes);

    private static string Hash(string value) => Hash(Encoding.UTF8.GetBytes(value));

    private static string Hash(byte[] value) => Convert.ToHexString(SHA256.HashData(value)).ToLowerInvariant();

    private sealed class FixedClock : IClock
    {
        private readonly DateTime _utcNow;

        public FixedClock(DateTime utcNow) => _utcNow = utcNow;

        public DateTime UtcNow => _utcNow;
    }

    private sealed class RecordingObjectStorageService : IObjectStorageService
    {
        private readonly Dictionary<string, StoredObject> _objects = new(StringComparer.OrdinalIgnoreCase);

        public string? OverrideWriteHash { get; set; }

        public string? ExistingMetadataObjectKey { get; set; }

        public List<ObjectStorageWriteRequest> Writes { get; } = new();

        public void Seed(string container, string objectKey, byte[] bytes, string hash)
            => _objects[Key(container, objectKey)] = new StoredObject(bytes, hash);

        public async Task<ObjectStorageWriteResult> SaveAsync(ObjectStorageWriteRequest request, CancellationToken ct = default)
        {
            Writes.Add(request);
            await using var buffer = new MemoryStream();
            await request.Content.CopyToAsync(buffer, ct).ConfigureAwait(false);
            var bytes = buffer.ToArray();
            var hash = OverrideWriteHash ?? Hash(bytes);
            _objects[Key(request.ContainerName, request.ObjectKey)] = new StoredObject(bytes, hash);
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
                false,
                request.Metadata);
        }

        public Task<ObjectStorageReadResult?> ReadAsync(ObjectStorageObjectReference reference, CancellationToken ct = default)
            => Task.FromResult(_objects.TryGetValue(Key(reference.ContainerName, reference.ObjectKey), out var value)
                ? new ObjectStorageReadResult(
                    new MemoryStream(value.Bytes, writable: false),
                    FinanceExportPackageBuilderService.PackageContentType,
                    "finance-export-test.json",
                    value.Bytes.LongLength,
                    value.Hash)
                : null);

        public Task<bool> ExistsAsync(ObjectStorageObjectReference reference, CancellationToken ct = default)
            => Task.FromResult(_objects.ContainsKey(Key(reference.ContainerName, reference.ObjectKey)));

        public Task<ObjectStorageObjectMetadata?> GetMetadataAsync(ObjectStorageObjectReference reference, CancellationToken ct = default)
            => Task.FromResult(_objects.TryGetValue(Key(reference.ContainerName, reference.ObjectKey), out var value)
                ? new ObjectStorageObjectMetadata(
                    ObjectStorageProviderKind.FileSystem,
                    reference.ContainerName,
                    ExistingMetadataObjectKey ?? reference.ObjectKey,
                    null,
                    null,
                    FinanceExportPackageBuilderService.PackageContentType,
                    "finance-export-test.json",
                    value.Bytes.LongLength,
                    value.Hash,
                    FixedNow,
                    null,
                    ObjectRetentionMode.None,
                    false,
                    false)
                : null);

        public Task DeleteAsync(ObjectStorageDeleteRequest request, CancellationToken ct = default) => Task.CompletedTask;

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

        private static string Key(string container, string objectKey) => container + "/" + objectKey;

        private sealed record StoredObject(byte[] Bytes, string Hash);
    }

    private static ServiceProvider BuildProvider(string root, bool configureOutbound)
    {
        var values = new Dictionary<string, string?>
        {
            ["ObjectStorage:Provider"] = "Database",
            ["ObjectStorage:FileSystem:RootPath"] = root,
            ["ObjectStorage:Profiles:FinanceExports:Provider"] = "FileSystem",
            ["ObjectStorage:Profiles:FinanceExports:ContainerName"] = FinanceExportPackageStorageService.ContainerName,
            ["ObjectStorage:Profiles:FinanceExports:Prefix"] = "packages"
        };

        if (configureOutbound)
        {
            values[$"ObjectStorage:Profiles:{FinanceExportFileDeliveryAdapter.ProfileName}:Provider"] = "FileSystem";
            values[$"ObjectStorage:Profiles:{FinanceExportFileDeliveryAdapter.ProfileName}:ContainerName"] = FinanceExportFileDeliveryAdapter.ContainerName;
            values[$"ObjectStorage:Profiles:{FinanceExportFileDeliveryAdapter.ProfileName}:Prefix"] = "outbound";
        }

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var services = new ServiceCollection();
        services.AddSingleton<IClock>(new FixedClock(FixedNow));
        services.AddObjectStorageInfrastructure(configuration);
        services.AddFinanceExportFileDeliveryAdapterIfConfigured(configuration);
        return services.BuildServiceProvider(validateScopes: true);
    }

    private static FinanceExportConnectorDeliveryService CreateDeliveryService(
        FinanceExportDeliverySmokeDbContext db,
        IObjectStorageService storage,
        params IFinanceExportConnectorAdapter[] adapters)
    {
        var batchService = new FinanceExportBatchService(db, new FixedClock(FixedNow));
        return new FinanceExportConnectorDeliveryService(
            db,
            batchService,
            new FinanceExportPackageStorageService(db, storage, new DocumentRecordService(db), batchService, new FinanceExportPackageBuilderService(db, new FixedClock(FixedNow), batchService)),
            new ExternalSystemReferenceService(db),
            adapters);
    }

    private static async Task SaveSourcePackageAsync(IObjectStorageService storage, string storageKey)
    {
        await using var content = new MemoryStream(PackageBytes, writable: false);
        await storage.SaveAsync(
            new ObjectStorageWriteRequest(
                FinanceExportPackageStorageService.ContainerName,
                storageKey,
                FinanceExportPackageBuilderService.PackageContentType,
                "finance-export-test.json",
                content,
                PackageBytes.LongLength,
                PackageHash,
                OverwritePolicy: ObjectOverwritePolicy.Disallow,
                ProfileName: FinanceExportPackageStorageService.ProfileName),
            TestContext.Current.CancellationToken);
    }

    private static async Task<Guid> SeedGeneratedBatchWithPackageDocumentAsync(
        FinanceExportDeliverySmokeDbContext db,
        Guid businessId,
        Guid targetId,
        string storageKey)
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
            SizeBytes = PackageBytes.LongLength,
            ContentHash = PackageHash,
            StorageProvider = FinanceExportPackageStorageService.ProfileName,
            StorageContainer = FinanceExportPackageStorageService.ContainerName,
            StorageKey = storageKey,
            Visibility = FoundationVisibility.Internal
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return batch.Id;
    }

    private static Guid SeedExternalSystem(FinanceExportDeliverySmokeDbContext db)
    {
        var system = new ExternalSystem
        {
            Id = Guid.NewGuid(),
            Code = "accounting",
            Name = "Accounting",
            Kind = ExternalSystemKind.Accounting,
            IsActive = true
        };
        db.Set<ExternalSystem>().Add(system);
        db.SaveChanges();
        return system.Id;
    }

    private sealed class FinanceExportDeliverySmokeDbContext : DbContext, IAppDbContext
    {
        private FinanceExportDeliverySmokeDbContext(DbContextOptions<FinanceExportDeliverySmokeDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static FinanceExportDeliverySmokeDbContext Create()
        {
            var options = new DbContextOptionsBuilder<FinanceExportDeliverySmokeDbContext>()
                .UseInMemoryDatabase($"darwin_finance_export_delivery_smoke_{Guid.NewGuid():N}")
                .Options;
            return new FinanceExportDeliverySmokeDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<ExternalSystem>().HasKey(x => x.Id);
            modelBuilder.Entity<ExternalReference>().HasKey(x => x.Id);
            modelBuilder.Entity<DocumentRecord>().HasKey(x => x.Id);
            modelBuilder.Entity<FinanceExportBatch>(b =>
            {
                b.HasKey(x => x.Id);
                b.HasMany(x => x.Attempts).WithOne().HasForeignKey(x => x.FinanceExportBatchId);
            });
            modelBuilder.Entity<FinanceExportAttempt>().HasKey(x => x.Id);
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
