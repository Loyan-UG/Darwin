using System.Security.Cryptography;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Abstractions.Storage;
using Darwin.Application.Foundation;
using Darwin.Application.HumanResources.DTOs;
using Darwin.Application.HumanResources.Services;
using Darwin.Domain.Entities.Foundation;
using Darwin.Domain.Entities.HumanResources;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Tests.Unit.HumanResources;

public sealed class PayrollPayslipArtifactServiceTests
{
    private static readonly DateTime Now = new(2034, 5, 10, 8, 30, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GenerateForRunAsync_Should_Create_PdfOfficialDocument_With_VersionedTemplate_And_SourceHtml()
    {
        await using var db = PayrollPayslipTestDbContext.Create();
        var run = SeedApprovedPayrollRun(db);
        var storage = new RecordingObjectStorageService(ObjectStorageProviderKind.FileSystem);
        var service = CreateService(db, storage);

        var result = await service.GenerateForRunAsync(new GeneratePayrollPayslipsDto
        {
            PayrollRunId = run.Id,
            RowVersion = [7]
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue(result.Error);
        result.Value.Should().Be(1);

        var payslip = await db.Set<PayrollPayslip>().SingleAsync(TestContext.Current.CancellationToken);
        payslip.MetadataJson.Should().Contain("\"format\":\"pdf\"");
        payslip.MetadataJson.Should().Contain(PayrollPayslipArtifactService.TemplateCode);
        payslip.SnapshotJson.Should().Contain(PayrollPayslipArtifactService.TemplateVersion);

        var documents = await db.Set<DocumentRecord>()
            .Where(x => x.EntityType == PayrollPayslipArtifactService.EntityType && x.EntityId == payslip.Id)
            .OrderBy(x => x.FileName)
            .ToListAsync(TestContext.Current.CancellationToken);
        documents.Should().HaveCount(2);
        documents.Should().Contain(x => x.ContentType == PayrollPayslipArtifactService.HtmlContentType && x.FileName.EndsWith("-source.html", StringComparison.Ordinal));

        var official = documents.Single(x => x.Id == payslip.DocumentRecordId);
        official.ContentType.Should().Be(PayrollPayslipArtifactService.PdfContentType);
        official.FileName.Should().EndWith(".pdf");
        official.MetadataJson.Should().Contain("\"format\":\"pdf\"");
        official.MetadataJson.Should().Contain(PayrollPayslipArtifactService.TemplateVersion);

        storage.Writes.Should().HaveCount(2);
        storage.Writes.Should().Contain(x => x.ContentType == PayrollPayslipArtifactService.HtmlContentType && x.Metadata!["artifact-format"] == "html-source");
        storage.Writes.Should().Contain(x => x.ContentType == PayrollPayslipArtifactService.PdfContentType && x.Metadata!["template-version"] == PayrollPayslipArtifactService.TemplateVersion);

        var download = await service.DownloadAsync(run.BusinessId, payslip.Id, TestContext.Current.CancellationToken);
        download.Succeeded.Should().BeTrue(download.Error);
        download.Value!.ContentType.Should().Be(PayrollPayslipArtifactService.PdfContentType);
        download.Value.FileName.Should().Be(official.FileName);
        await using var buffer = new MemoryStream();
        await download.Value.Content.CopyToAsync(buffer, TestContext.Current.CancellationToken);
        buffer.ToArray()[..8].Should().Equal("%PDF-1.4"u8.ToArray());
    }

    [Fact]
    public async Task GenerateForRunAsync_Should_Block_When_ObjectStorage_Is_DatabaseFallback()
    {
        await using var db = PayrollPayslipTestDbContext.Create();
        var run = SeedApprovedPayrollRun(db);
        var service = CreateService(db, new RecordingObjectStorageService(ObjectStorageProviderKind.Database));

        var result = await service.GenerateForRunAsync(new GeneratePayrollPayslipsDto
        {
            PayrollRunId = run.Id,
            RowVersion = [7]
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("PayrollPayslipStorageNotReady");
        db.Set<PayrollPayslip>().Should().BeEmpty();
    }

    private static PayrollPayslipArtifactService CreateService(PayrollPayslipTestDbContext db, IObjectStorageService storage)
        => new(db, storage, new DocumentRecordService(db), new FixedClock(Now));

    private static PayrollRun SeedApprovedPayrollRun(PayrollPayslipTestDbContext db)
    {
        var businessId = Guid.NewGuid();
        var run = new PayrollRun
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            PayrollPeriodId = Guid.NewGuid(),
            PayrollRuleSetId = Guid.NewGuid(),
            RunNumber = "PAY-2034-05",
            Status = PayrollRunStatus.Approved,
            JurisdictionCode = "DE",
            RuleSetCode = "DE-PAYROLL",
            RuleVersion = "2034.05",
            Currency = "EUR",
            PeriodStartUtc = new DateTime(2034, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            PeriodEndUtc = new DateTime(2034, 5, 31, 0, 0, 0, DateTimeKind.Utc),
            EmployeeCount = 1,
            GrossPayMinor = 500000,
            EmployeeDeductionMinor = 120000,
            EmployerCostMinor = 90000,
            NetPayMinor = 380000,
            ApprovedAtUtc = Now,
            RowVersion = [7]
        };

        var line = new PayrollRunLine
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            PayrollRunId = run.Id,
            EmployeeId = Guid.NewGuid(),
            EmployeeNumber = "E-100",
            EmployeeName = "Ada Lovelace",
            WorkMinutes = 9600,
            GrossPayMinor = 500000,
            EmployeeDeductionMinor = 120000,
            EmployerCostMinor = 90000,
            NetPayMinor = 380000
        };
        line.Components.Add(new PayrollRunLineComponent
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            PayrollRunId = run.Id,
            PayrollRunLineId = line.Id,
            PayrollRuleComponentId = Guid.NewGuid(),
            ComponentCode = "BASE",
            DisplayName = "Base salary",
            ComponentType = PayrollRuleComponentType.GrossPay,
            CalculationMethod = PayrollRuleCalculationMethod.FixedAmount,
            Basis = PayrollRuleBasis.ContractRate,
            AmountMinor = 500000,
            SortOrder = 1
        });
        line.Components.Add(new PayrollRunLineComponent
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            PayrollRunId = run.Id,
            PayrollRunLineId = line.Id,
            PayrollRuleComponentId = Guid.NewGuid(),
            ComponentCode = "TAX",
            DisplayName = "Income tax",
            ComponentType = PayrollRuleComponentType.TaxWithholding,
            CalculationMethod = PayrollRuleCalculationMethod.Percentage,
            Basis = PayrollRuleBasis.TaxableIncome,
            AmountMinor = -120000,
            SortOrder = 2
        });

        run.Lines.Add(line);
        db.Set<PayrollRun>().Add(run);
        db.SaveChanges();
        return run;
    }

    private sealed class FixedClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }

    private sealed class RecordingObjectStorageService(ObjectStorageProviderKind providerKind) : IObjectStorageService
    {
        private readonly Dictionary<string, StoredObject> _objects = new(StringComparer.OrdinalIgnoreCase);

        public List<ObjectStorageWriteRequest> Writes { get; } = new();

        public async Task<ObjectStorageWriteResult> SaveAsync(ObjectStorageWriteRequest request, CancellationToken ct = default)
        {
            await using var buffer = new MemoryStream();
            await request.Content.CopyToAsync(buffer, ct);
            var bytes = buffer.ToArray();
            var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
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
                Now,
                null,
                ObjectRetentionMode.None,
                false,
                null,
                false);
        }

        public Task<ObjectStorageReadResult?> ReadAsync(ObjectStorageObjectReference reference, CancellationToken ct = default)
            => Task.FromResult(_objects.TryGetValue(StorageKey(reference.ContainerName, reference.ObjectKey), out var value)
                ? new ObjectStorageReadResult(new MemoryStream(value.Bytes, writable: false), value.ContentType, value.FileName, value.Bytes.LongLength, value.Sha256Hash)
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
                providerKind,
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

    private sealed class PayrollPayslipTestDbContext : DbContext, IAppDbContext
    {
        private PayrollPayslipTestDbContext(DbContextOptions<PayrollPayslipTestDbContext> options) : base(options) { }
        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static PayrollPayslipTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<PayrollPayslipTestDbContext>()
                .UseInMemoryDatabase($"darwin_payroll_payslip_tests_{Guid.NewGuid()}")
                .Options;
            return new PayrollPayslipTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DocumentRecord>().HasKey(x => x.Id);
            modelBuilder.Entity<PayrollPayslip>().HasKey(x => x.Id);
            modelBuilder.Entity<PayrollRun>(b =>
            {
                b.HasKey(x => x.Id);
                b.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.PayrollRunId);
            });
            modelBuilder.Entity<PayrollRunLine>(b =>
            {
                b.HasKey(x => x.Id);
                b.HasMany(x => x.Components).WithOne().HasForeignKey(x => x.PayrollRunLineId);
            });
            modelBuilder.Entity<PayrollRunLineComponent>().HasKey(x => x.Id);
        }
    }
}
