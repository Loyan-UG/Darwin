using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Abstractions.Storage;
using Darwin.Application.Billing.Services;
using Darwin.Application.Foundation;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.Foundation;
using Darwin.Domain.Entities.Integration;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Tests.Unit.Billing;

public sealed class FinanceExportPackageBuilderServiceTests
{
    private static readonly DateTime FixedNow = new(2033, 5, 6, 7, 8, 9, DateTimeKind.Utc);
    private static readonly DateTime PeriodStart = new(2033, 5, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime PeriodEnd = new(2033, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task BuildAsync_Should_CreateDeterministicJsonPackage_ForEligiblePostedEntries()
    {
        await using var db = FinanceExportPackageTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var targetId = SeedExternalSystem(db);
        var receivable = SeedAccount(db, businessId, AccountType.Asset, "Receivables", "1100");
        var revenue = SeedAccount(db, businessId, AccountType.Revenue, "Revenue", "4000");
        AddEntry(db, businessId, PeriodStart.AddDays(1), JournalEntryPostingStatus.Posted, "posting-b", "INV-B", (receivable, 1000, 0, "Debit"), (revenue, 0, 1000, "Credit"));
        AddEntry(db, businessId, PeriodStart.AddDays(2), JournalEntryPostingStatus.Draft, "posting-draft", "INV-DRAFT", (receivable, 200, 0, null), (revenue, 0, 200, null));
        AddEntry(db, businessId, PeriodStart.AddDays(3), JournalEntryPostingStatus.Voided, "posting-void", "INV-VOID", (receivable, 300, 0, null), (revenue, 0, 300, null));
        AddEntry(db, Guid.NewGuid(), PeriodStart.AddDays(1), JournalEntryPostingStatus.Posted, "posting-other", "INV-OTHER", (receivable, 400, 0, null), (revenue, 0, 400, null));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var batchId = await CreateBatchAsync(db, businessId, targetId, FinanceExportPostingStatusMode.PostedOnly);
        var service = CreateBuilder(db);

        var result = await service.BuildAsync(new FinanceExportPackageBuildCommand(batchId), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.EntryCount.Should().Be(1);
        result.Value.LineCount.Should().Be(2);
        result.Value.PackageHashSha256.Should().Be(Sha256(result.Value.PackageJson));
        using var json = JsonDocument.Parse(result.Value.PackageJson);
        json.RootElement.GetProperty("header").GetProperty("entryCount").GetInt32().Should().Be(1);
        json.RootElement.GetProperty("header").GetProperty("lineCount").GetInt32().Should().Be(2);
        json.RootElement.GetProperty("header").GetProperty("totalDebitMinor").GetInt64().Should().Be(1000);
        json.RootElement.GetProperty("entries")[0].GetProperty("postingKey").GetString().Should().Be("posting-b");
        json.RootElement.GetProperty("entries")[0].GetProperty("lines")[0].GetProperty("accountCode").GetString().Should().Be("1100");
        json.RootElement.ToString().Contains("archive", StringComparison.OrdinalIgnoreCase).Should().BeFalse();

        var batch = await db.Set<FinanceExportBatch>().Include(x => x.Attempts).SingleAsync(x => x.Id == batchId, TestContext.Current.CancellationToken);
        batch.Status.Should().Be(FinanceExportBatchStatus.Generated);
        batch.PackageHashSha256.Should().Be(result.Value.PackageHashSha256);
        batch.PackageContentType.Should().Be(FinanceExportPackageBuilderService.PackageContentType);
        batch.Attempts.Should().ContainSingle(x => x.Status == FinanceExportAttemptStatus.Succeeded);
    }

    [Fact]
    public async Task BuildAsync_Should_IncludeReversedEntries_Only_WhenModeAllows()
    {
        await using var db = FinanceExportPackageTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var targetId = SeedExternalSystem(db);
        var asset = SeedAccount(db, businessId, AccountType.Asset, "Receivables");
        var revenue = SeedAccount(db, businessId, AccountType.Revenue, "Revenue");
        AddEntry(db, businessId, PeriodStart.AddDays(1), JournalEntryPostingStatus.Posted, "posting-posted", "INV-1", (asset, 100, 0, null), (revenue, 0, 100, null));
        AddEntry(db, businessId, PeriodStart.AddDays(2), JournalEntryPostingStatus.Reversed, "posting-reversed", "INV-1", (revenue, 100, 0, null), (asset, 0, 100, null));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var postedOnlyBatch = await CreateBatchAsync(db, businessId, targetId, FinanceExportPostingStatusMode.PostedOnly);
        var postedAndReversedBatch = await CreateBatchAsync(db, businessId, targetId, FinanceExportPostingStatusMode.PostedAndReversed);
        var service = CreateBuilder(db);

        var postedOnly = await service.BuildAsync(new FinanceExportPackageBuildCommand(postedOnlyBatch), TestContext.Current.CancellationToken);
        var postedAndReversed = await service.BuildAsync(new FinanceExportPackageBuildCommand(postedAndReversedBatch), TestContext.Current.CancellationToken);

        postedOnly.Value!.EntryCount.Should().Be(1);
        postedAndReversed.Value!.EntryCount.Should().Be(2);
        postedAndReversed.Value.PackageJson.Should().Contain("posting-reversed");
    }

    [Fact]
    public async Task BuildAsync_Should_GenerateZeroEntryPackage_ForEmptyPeriod()
    {
        await using var db = FinanceExportPackageTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var targetId = SeedExternalSystem(db);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var batchId = await CreateBatchAsync(db, businessId, targetId, FinanceExportPostingStatusMode.PostedOnly);

        var result = await CreateBuilder(db).BuildAsync(new FinanceExportPackageBuildCommand(batchId), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.EntryCount.Should().Be(0);
        result.Value.LineCount.Should().Be(0);
        result.Value.PackageJson.Should().Contain("\"entries\":[]");
    }

    [Fact]
    public async Task BuildAsync_Should_RejectAlreadyGeneratedBatch()
    {
        await using var db = FinanceExportPackageTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var targetId = SeedExternalSystem(db);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var batchId = await CreateBatchAsync(db, businessId, targetId, FinanceExportPostingStatusMode.PostedOnly);
        var batch = await db.Set<FinanceExportBatch>().SingleAsync(x => x.Id == batchId, TestContext.Current.CancellationToken);
        batch.Status = FinanceExportBatchStatus.Generated;
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await CreateBuilder(db).BuildAsync(new FinanceExportPackageBuildCommand(batchId), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("cannot be regenerated");
        db.Set<FinanceExportAttempt>().Should().BeEmpty();
    }

    [Fact]
    public async Task BuildAsync_Should_RecordFailedAttempt_WhenSensitivePostingTextIsFound()
    {
        await using var db = FinanceExportPackageTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var targetId = SeedExternalSystem(db);
        var asset = SeedAccount(db, businessId, AccountType.Asset, "Receivables");
        var revenue = SeedAccount(db, businessId, AccountType.Revenue, "Revenue");
        AddEntry(db, businessId, PeriodStart.AddDays(1), JournalEntryPostingStatus.Posted, "posting-sensitive", "access token", (asset, 100, 0, null), (revenue, 0, 100, null));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var batchId = await CreateBatchAsync(db, businessId, targetId, FinanceExportPostingStatusMode.PostedOnly);

        var result = await CreateBuilder(db).BuildAsync(new FinanceExportPackageBuildCommand(batchId), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        var batch = await db.Set<FinanceExportBatch>().Include(x => x.Attempts).SingleAsync(x => x.Id == batchId, TestContext.Current.CancellationToken);
        batch.Status.Should().Be(FinanceExportBatchStatus.Failed);
        batch.Attempts.Should().ContainSingle(x => x.Status == FinanceExportAttemptStatus.Failed);
    }

    [Fact]
    public async Task GenerateAndStoreAsync_Should_StorePackage_RegisterDocument_AndCompleteAttempt()
    {
        await using var db = FinanceExportPackageTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var targetId = SeedExternalSystem(db);
        var receivable = SeedAccount(db, businessId, AccountType.Asset, "Receivables", "1100");
        var revenue = SeedAccount(db, businessId, AccountType.Revenue, "Revenue", "4000");
        AddEntry(db, businessId, PeriodStart.AddDays(1), JournalEntryPostingStatus.Posted, "posting-store", "INV-STORE", (receivable, 1000, 0, null), (revenue, 0, 1000, null));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var batchId = await CreateBatchAsync(db, businessId, targetId, FinanceExportPostingStatusMode.PostedOnly);
        var storage = new RecordingObjectStorageService();
        var service = CreateStorageService(db, storage);

        var result = await service.GenerateAndStoreAsync(batchId, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.PackageHashSha256.Should().NotBeEmpty();
        storage.Writes.Should().ContainSingle();
        storage.Writes[0].ObjectKey.Should().Contain(batchId.ToString("N"));
        var batch = await db.Set<FinanceExportBatch>().Include(x => x.Attempts).SingleAsync(x => x.Id == batchId, TestContext.Current.CancellationToken);
        batch.Status.Should().Be(FinanceExportBatchStatus.Generated);
        batch.Attempts.Should().ContainSingle(x => x.Status == FinanceExportAttemptStatus.Succeeded);
        var document = await db.Set<DocumentRecord>().SingleAsync(x => x.EntityId == batchId, TestContext.Current.CancellationToken);
        document.EntityType.Should().Be(FinanceExportPackageStorageService.EntityType);
        document.ContentHash.Should().Be(result.Value.PackageHashSha256);
    }

    [Fact]
    public async Task GenerateAndStoreAsync_Should_FailAttempt_WhenStorageFails()
    {
        await using var db = FinanceExportPackageTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var targetId = SeedExternalSystem(db);
        var receivable = SeedAccount(db, businessId, AccountType.Asset, "Receivables", "1100");
        var revenue = SeedAccount(db, businessId, AccountType.Revenue, "Revenue", "4000");
        AddEntry(db, businessId, PeriodStart.AddDays(1), JournalEntryPostingStatus.Posted, "posting-fail", "INV-FAIL", (receivable, 1000, 0, null), (revenue, 0, 1000, null));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var batchId = await CreateBatchAsync(db, businessId, targetId, FinanceExportPostingStatusMode.PostedOnly);
        var service = CreateStorageService(db, new RecordingObjectStorageService { ThrowOnSave = true });

        var result = await service.GenerateAndStoreAsync(batchId, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        var batch = await db.Set<FinanceExportBatch>().Include(x => x.Attempts).SingleAsync(x => x.Id == batchId, TestContext.Current.CancellationToken);
        batch.Status.Should().Be(FinanceExportBatchStatus.Failed);
        batch.Attempts.Should().ContainSingle(x => x.Status == FinanceExportAttemptStatus.Failed);
        (await db.Set<DocumentRecord>().CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
    }

    [Fact]
    public async Task GetStoredPackageAsync_Should_ReadExistingDocumentObject_WithoutRegeneration()
    {
        await using var db = FinanceExportPackageTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var targetId = SeedExternalSystem(db);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var batchId = await CreateBatchAsync(db, businessId, targetId, FinanceExportPostingStatusMode.PostedOnly);
        var storage = new RecordingObjectStorageService();
        var packageJson = "{\"header\":{\"entryCount\":0},\"entries\":[]}";
        var hash = Sha256(packageJson);
        await using (var content = new MemoryStream(Encoding.UTF8.GetBytes(packageJson), writable: false))
        {
            await storage.SaveAsync(new ObjectStorageWriteRequest(
                    FinanceExportPackageStorageService.ContainerName,
                    "finance-exports/test/package.json",
                    FinanceExportPackageBuilderService.PackageContentType,
                    "finance-export-test.json",
                    content,
                    Encoding.UTF8.GetByteCount(packageJson),
                    hash,
                    ProfileName: FinanceExportPackageStorageService.ProfileName),
                TestContext.Current.CancellationToken);
        }

        db.Set<DocumentRecord>().Add(new DocumentRecord
        {
            EntityType = FinanceExportPackageStorageService.EntityType,
            EntityId = batchId,
            DocumentKind = DocumentRecordKind.Evidence,
            Title = "Finance export package",
            FileName = "finance-export-test.json",
            ContentType = FinanceExportPackageBuilderService.PackageContentType,
            SizeBytes = Encoding.UTF8.GetByteCount(packageJson),
            ContentHash = hash,
            StorageProvider = FinanceExportPackageStorageService.ProfileName,
            StorageContainer = FinanceExportPackageStorageService.ContainerName,
            StorageKey = "finance-exports/test/package.json",
            Visibility = FoundationVisibility.Internal
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await CreateStorageService(db, storage).GetStoredPackageAsync(batchId, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        await using var read = result.Value!.Content;
        using var reader = new StreamReader(read, Encoding.UTF8);
        (await reader.ReadToEndAsync(TestContext.Current.CancellationToken)).Should().Be(packageJson);
        storage.Writes.Should().ContainSingle();
    }

    private static async Task<Guid> CreateBatchAsync(
        FinanceExportPackageTestDbContext db,
        Guid businessId,
        Guid targetId,
        FinanceExportPostingStatusMode mode)
    {
        var batch = await CreateBatchService(db).GetOrCreateBatchAsync(
            new FinanceExportBatchCommand(businessId, targetId, PeriodStart, PeriodEnd, mode),
            TestContext.Current.CancellationToken);
        batch.Succeeded.Should().BeTrue();
        return batch.Value!.BatchId;
    }

    private static Guid SeedExternalSystem(FinanceExportPackageTestDbContext db)
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
        return system.Id;
    }

    private static Guid SeedAccount(FinanceExportPackageTestDbContext db, Guid businessId, AccountType type, string name, string? code = null)
    {
        var account = new FinancialAccount
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            Type = type,
            Name = name,
            Code = code
        };
        db.Set<FinancialAccount>().Add(account);
        return account.Id;
    }

    private static void AddEntry(
        FinanceExportPackageTestDbContext db,
        Guid businessId,
        DateTime entryDateUtc,
        JournalEntryPostingStatus status,
        string postingKey,
        string sourceDocumentNumber,
        params (Guid AccountId, long Debit, long Credit, string? Memo)[] lines)
    {
        var entry = new JournalEntry
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            EntryDateUtc = entryDateUtc,
            Description = $"Entry {postingKey}",
            PostingStatus = status,
            PostingKind = JournalEntryPostingKind.InvoiceIssued,
            PostingKey = postingKey,
            SourceEntityType = "Invoice",
            SourceEntityId = Guid.NewGuid(),
            SourceDocumentNumber = sourceDocumentNumber,
            PostedAtUtc = entryDateUtc,
            Lines = lines.Select((line, index) => new JournalEntryLine
            {
                Id = Guid.NewGuid(),
                AccountId = line.AccountId,
                DebitMinor = line.Debit,
                CreditMinor = line.Credit,
                Memo = line.Memo,
                CreatedAtUtc = entryDateUtc.AddSeconds(index)
            }).ToList()
        };
        db.Set<JournalEntry>().Add(entry);
    }

    private static FinanceExportPackageBuilderService CreateBuilder(IAppDbContext db)
        => new(db, new FixedClock(FixedNow), CreateBatchService(db));

    private static FinanceExportPackageStorageService CreateStorageService(IAppDbContext db, IObjectStorageService storage)
        => new(db, storage, new DocumentRecordService(db), CreateBatchService(db), CreateBuilder(db));

    private static FinanceExportBatchService CreateBatchService(IAppDbContext db)
        => new(db, new FixedClock(FixedNow));

    private static string Sha256(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private sealed class FixedClock : IClock
    {
        private readonly DateTime _utcNow;

        public FixedClock(DateTime utcNow) => _utcNow = utcNow;

        public DateTime UtcNow => _utcNow;
    }

    private sealed class FinanceExportPackageTestDbContext : DbContext, IAppDbContext
    {
        private FinanceExportPackageTestDbContext(DbContextOptions<FinanceExportPackageTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static FinanceExportPackageTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<FinanceExportPackageTestDbContext>()
                .UseInMemoryDatabase($"darwin_finance_export_package_tests_{Guid.NewGuid()}")
                .Options;
            return new FinanceExportPackageTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<ExternalSystem>().HasKey(x => x.Id);
            modelBuilder.Entity<FinancialAccount>().HasKey(x => x.Id);
            modelBuilder.Entity<FinanceExportBatch>(b =>
            {
                b.HasKey(x => x.Id);
                b.HasMany(x => x.Attempts).WithOne().HasForeignKey(x => x.FinanceExportBatchId);
            });
            modelBuilder.Entity<FinanceExportAttempt>().HasKey(x => x.Id);
            modelBuilder.Entity<DocumentRecord>().HasKey(x => x.Id);
            modelBuilder.Entity<JournalEntry>(b =>
            {
                b.HasKey(x => x.Id);
                b.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.JournalEntryId);
            });
            modelBuilder.Entity<JournalEntryLine>().HasKey(x => x.Id);
        }
    }

    private sealed class RecordingObjectStorageService : IObjectStorageService
    {
        private readonly Dictionary<string, StoredObject> _objects = new(StringComparer.OrdinalIgnoreCase);

        public List<ObjectStorageWriteRequest> Writes { get; } = new();

        public bool ThrowOnSave { get; init; }

        public async Task<ObjectStorageWriteResult> SaveAsync(ObjectStorageWriteRequest request, CancellationToken ct = default)
        {
            if (ThrowOnSave)
            {
                throw new InvalidOperationException("storage unavailable");
            }

            await using var buffer = new MemoryStream();
            await request.Content.CopyToAsync(buffer, ct);
            var bytes = buffer.ToArray();
            var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(request.ExpectedSha256Hash))
            {
                hash.Should().Be(request.ExpectedSha256Hash);
            }

            var key = StorageKey(request.ContainerName, request.ObjectKey);
            _objects[key] = new StoredObject(bytes, request.ContentType, request.FileName, hash);
            Writes.Add(request);
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
}
