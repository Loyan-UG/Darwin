using System.Security.Cryptography;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Abstractions.Storage;
using Darwin.Application.Foundation;
using Darwin.Application.HumanResources.Queries;
using Darwin.Application.HumanResources.Services;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Foundation;
using Darwin.Domain.Entities.HumanResources;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Tests.Unit.HumanResources;

public sealed class MemberPayrollPayslipQueriesTests
{
    private static readonly DateTime Now = new(2035, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetMyPayslipsPage_Should_Return_Only_LinkedEmployeePayslips_With_HighLevelPaymentStatus()
    {
        await using var db = MemberPayrollTestDbContext.Create();
        var seed = SeedPayslipScope(db);
        SeedPostedPayrollPayment(db, seed.BusinessId, seed.PayrollRunId, seed.LineId, seed.EmployeeId, 295000);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetMyPayslipsPageHandler(db, new FixedCurrentUser(seed.UserId));

        var (items, total) = await handler.HandleAsync(1, 20, TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Should().ContainSingle();
        items[0].Id.Should().Be(seed.PayslipId);
        items[0].BusinessId.Should().Be(seed.BusinessId);
        items[0].EmployeeId.Should().Be(seed.EmployeeId);
        items[0].PaymentStatus.Should().Be("Paid");
        items[0].DocumentPath.Should().Be($"api/v1/member/payroll/payslips/{seed.PayslipId:D}/document");

        var otherHandler = new GetMyPayslipsPageHandler(db, new FixedCurrentUser(Guid.NewGuid()));
        var (otherItems, otherTotal) = await otherHandler.HandleAsync(1, 20, TestContext.Current.CancellationToken);
        otherTotal.Should().Be(0);
        otherItems.Should().BeEmpty();
    }

    [Fact]
    public async Task DownloadMyPayslipDocument_Should_Read_OfficialPdf_And_Record_DownloadAudit()
    {
        await using var db = MemberPayrollTestDbContext.Create();
        var seed = SeedPayslipScope(db);
        var storage = new RecordingObjectStorageService();
        storage.Store(PayrollPayslipArtifactService.ContainerName, seed.StorageKey, "%PDF-1.4"u8.ToArray(), PayrollPayslipArtifactService.PdfContentType, "payslip.pdf");
        var events = new BusinessEventService(db, new FixedClock(Now));
        var handler = new DownloadMyPayslipDocumentHandler(db, new FixedCurrentUser(seed.UserId), storage, new FixedClock(Now), events);

        var result = await handler.HandleAsync(seed.PayslipId, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue(result.Error);
        result.Value!.ContentType.Should().Be(PayrollPayslipArtifactService.PdfContentType);
        result.Value.FileName.Should().Be("payslip.pdf");
        db.Set<BusinessEvent>().Should().ContainSingle(x => x.EventType == "hr.employee_payslip.downloaded" && x.EntityId == seed.PayslipId);
        db.Set<AuditTrail>().Should().ContainSingle(x => x.Action == AuditTrailAction.Exported && x.EntityId == seed.PayslipId);
    }

    [Fact]
    public async Task DownloadMyPayslipDocument_Should_Not_Regenerate_When_Object_Is_Missing()
    {
        await using var db = MemberPayrollTestDbContext.Create();
        var seed = SeedPayslipScope(db);
        var handler = new DownloadMyPayslipDocumentHandler(db, new FixedCurrentUser(seed.UserId), new RecordingObjectStorageService(), new FixedClock(Now), new BusinessEventService(db, new FixedClock(Now)));

        var result = await handler.HandleAsync(seed.PayslipId, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("PayrollPayslipObjectNotFound");
        db.Set<BusinessEvent>().Should().BeEmpty();
        db.Set<AuditTrail>().Should().BeEmpty();
    }

    private static SeedIds SeedPayslipScope(MemberPayrollTestDbContext db)
    {
        var businessId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var runId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var payslipId = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        var storageKey = $"payroll-payslips/{businessId:N}/{runId:N}/{payslipId:N}/payslip.pdf";

        db.Set<Business>().Add(new Business { Id = businessId, Name = "Darwin Demo", DefaultCurrency = "EUR" });
        db.Set<BusinessMember>().Add(new BusinessMember { Id = memberId, BusinessId = businessId, UserId = userId, IsActive = true });
        db.Set<Employee>().Add(new Employee
        {
            Id = employeeId,
            BusinessId = businessId,
            BusinessMemberId = memberId,
            EmployeeNumber = "EMP-001",
            FirstName = "Ada",
            LastName = "Lovelace",
            Status = EmployeeStatus.Active
        });
        db.Set<PayrollRun>().Add(new PayrollRun
        {
            Id = runId,
            BusinessId = businessId,
            PayrollPeriodId = Guid.NewGuid(),
            PayrollRuleSetId = Guid.NewGuid(),
            RunNumber = "PAY-2035-01",
            Status = PayrollRunStatus.Posted,
            Currency = "EUR",
            PeriodStartUtc = new DateTime(2035, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            PeriodEndUtc = new DateTime(2035, 1, 31, 0, 0, 0, DateTimeKind.Utc)
        });
        db.Set<PayrollRunLine>().Add(new PayrollRunLine
        {
            Id = lineId,
            BusinessId = businessId,
            PayrollRunId = runId,
            EmployeeId = employeeId,
            EmployeeNumber = "EMP-001",
            EmployeeName = "Ada Lovelace",
            GrossPayMinor = 420000,
            EmployeeDeductionMinor = 125000,
            EmployerCostMinor = 50000,
            NetPayMinor = 295000
        });
        db.Set<DocumentRecord>().Add(new DocumentRecord
        {
            Id = documentId,
            EntityType = PayrollPayslipArtifactService.EntityType,
            EntityId = payslipId,
            DocumentKind = DocumentRecordKind.StaffDocument,
            Title = "Payslip PS-2035-01-001",
            FileName = "payslip.pdf",
            ContentType = PayrollPayslipArtifactService.PdfContentType,
            SizeBytes = 8,
            StorageProvider = PayrollPayslipArtifactService.ProfileName,
            StorageContainer = PayrollPayslipArtifactService.ContainerName,
            StorageKey = storageKey,
            Visibility = FoundationVisibility.Internal
        });
        db.Set<PayrollPayslip>().Add(new PayrollPayslip
        {
            Id = payslipId,
            BusinessId = businessId,
            PayrollRunId = runId,
            PayrollRunLineId = lineId,
            EmployeeId = employeeId,
            DocumentRecordId = documentId,
            PayslipNumber = "PS-2035-01-001",
            Status = PayrollPayslipStatus.Generated,
            Currency = "EUR",
            PeriodStartUtc = new DateTime(2035, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            PeriodEndUtc = new DateTime(2035, 1, 31, 0, 0, 0, DateTimeKind.Utc),
            GrossPayMinor = 420000,
            EmployeeDeductionMinor = 125000,
            EmployerCostMinor = 50000,
            NetPayMinor = 295000,
            GeneratedAtUtc = Now
        });
        db.SaveChanges();
        return new SeedIds(businessId, userId, employeeId, runId, lineId, payslipId, storageKey);
    }

    private static void SeedPostedPayrollPayment(MemberPayrollTestDbContext db, Guid businessId, Guid runId, Guid lineId, Guid employeeId, long amountMinor)
    {
        var paymentId = Guid.NewGuid();
        db.Set<PayrollPayment>().Add(new PayrollPayment
        {
            Id = paymentId,
            BusinessId = businessId,
            PayrollRunId = runId,
            Status = PayrollPaymentStatus.Posted,
            PaymentDateUtc = Now,
            Currency = "EUR",
            TotalAmountMinor = amountMinor,
            PostedAtUtc = Now,
            PostingJournalEntryId = Guid.NewGuid()
        });
        db.Set<PayrollPaymentAllocation>().Add(new PayrollPaymentAllocation
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            PayrollPaymentId = paymentId,
            PayrollRunId = runId,
            PayrollRunLineId = lineId,
            EmployeeId = employeeId,
            AmountMinor = amountMinor
        });
    }

    private sealed class FixedCurrentUser(Guid userId) : ICurrentUserService
    {
        public Guid GetCurrentUserId() => userId;
    }

    private sealed class FixedClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }

    private sealed class RecordingObjectStorageService : IObjectStorageService
    {
        private readonly Dictionary<string, StoredObject> _objects = new(StringComparer.OrdinalIgnoreCase);

        public void Store(string container, string key, byte[] bytes, string contentType, string fileName)
            => _objects[StorageKey(container, key)] = new StoredObject(bytes, contentType, fileName, Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant());

        public Task<ObjectStorageWriteResult> SaveAsync(ObjectStorageWriteRequest request, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<ObjectStorageReadResult?> ReadAsync(ObjectStorageObjectReference reference, CancellationToken ct = default)
            => Task.FromResult(_objects.TryGetValue(StorageKey(reference.ContainerName, reference.ObjectKey), out var value)
                ? new ObjectStorageReadResult(new MemoryStream(value.Bytes, writable: false), value.ContentType, value.FileName, value.Bytes.LongLength, value.Sha256Hash)
                : null);

        public Task<bool> ExistsAsync(ObjectStorageObjectReference reference, CancellationToken ct = default)
            => Task.FromResult(_objects.ContainsKey(StorageKey(reference.ContainerName, reference.ObjectKey)));

        public Task<ObjectStorageObjectMetadata?> GetMetadataAsync(ObjectStorageObjectReference reference, CancellationToken ct = default)
            => Task.FromResult<ObjectStorageObjectMetadata?>(null);

        public Task DeleteAsync(ObjectStorageDeleteRequest request, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<Uri?> GetTemporaryReadUrlAsync(ObjectStorageTemporaryUrlRequest request, CancellationToken ct = default)
            => Task.FromResult<Uri?>(null);

        public ObjectStorageCapabilities GetCapabilities(ObjectStorageContainerSelection selection)
            => new(ObjectStorageProviderKind.FileSystem, false, false, false, false, false, false, true, false);

        private static string StorageKey(string container, string key) => container + "/" + key;

        private sealed record StoredObject(byte[] Bytes, string ContentType, string FileName, string Sha256Hash);
    }

    private sealed class MemberPayrollTestDbContext : DbContext, IAppDbContext
    {
        private MemberPayrollTestDbContext(DbContextOptions<MemberPayrollTestDbContext> options) : base(options) { }

        public DbSet<Business> Businesses => Set<Business>();
        public DbSet<BusinessMember> BusinessMembers => Set<BusinessMember>();
        public DbSet<Employee> Employees => Set<Employee>();
        public DbSet<PayrollRun> PayrollRuns => Set<PayrollRun>();
        public DbSet<PayrollRunLine> PayrollRunLines => Set<PayrollRunLine>();
        public DbSet<PayrollPayslip> PayrollPayslips => Set<PayrollPayslip>();
        public DbSet<PayrollPayment> PayrollPayments => Set<PayrollPayment>();
        public DbSet<PayrollPaymentAllocation> PayrollPaymentAllocations => Set<PayrollPaymentAllocation>();
        public DbSet<PayrollPaymentBankCorrection> PayrollPaymentBankCorrections => Set<PayrollPaymentBankCorrection>();
        public DbSet<DocumentRecord> DocumentRecords => Set<DocumentRecord>();
        public DbSet<BusinessEvent> BusinessEvents => Set<BusinessEvent>();
        public DbSet<AuditTrail> AuditTrails => Set<AuditTrail>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Business>(builder =>
            {
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
        }

        public static MemberPayrollTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<MemberPayrollTestDbContext>()
                .UseInMemoryDatabase($"darwin_member_payroll_tests_{Guid.NewGuid()}")
                .Options;
            return new MemberPayrollTestDbContext(options);
        }
    }

    private sealed record SeedIds(Guid BusinessId, Guid UserId, Guid EmployeeId, Guid PayrollRunId, Guid LineId, Guid PayslipId, string StorageKey);
}
