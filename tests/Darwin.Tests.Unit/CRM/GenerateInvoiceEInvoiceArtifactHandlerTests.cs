using Darwin.Application.Abstractions.Invoicing;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.CRM.Commands;
using Darwin.Application.CRM.Services;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Tests.Unit.CRM;

public sealed class GenerateInvoiceEInvoiceArtifactHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_InvoiceUnavailable_When_Invoice_Is_Missing()
    {
        await using var db = EInvoiceArtifactDbContext.Create();
        var generator = new RecordingGenerator();
        var handler = CreateHandler(db, generator);

        var result = await handler.HandleAsync(
            Guid.NewGuid(),
            EInvoiceArtifactFormat.ZugferdFacturX,
            TestContext.Current.CancellationToken);

        result.Status.Should().Be(EInvoiceGenerationStatus.InvoiceUnavailable);
        generator.Calls.Should().Be(0);
    }

    [Fact]
    public async Task HandleAsync_Should_Return_SourceSnapshotUnavailable_When_Issued_Snapshot_Is_Missing()
    {
        await using var db = EInvoiceArtifactDbContext.Create();
        var invoiceId = Guid.NewGuid();
        db.Set<Invoice>().Add(new Invoice
        {
            Id = invoiceId,
            Status = InvoiceStatus.Open,
            Currency = "EUR",
            DueDateUtc = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            RowVersion = new byte[] { 1 }
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var generator = new RecordingGenerator();
        var handler = CreateHandler(db, generator);

        var result = await handler.HandleAsync(
            invoiceId,
            EInvoiceArtifactFormat.ZugferdFacturX,
            TestContext.Current.CancellationToken);

        result.Status.Should().Be(EInvoiceGenerationStatus.SourceSnapshotUnavailable);
        generator.Calls.Should().Be(0);
    }

    [Fact]
    public async Task HandleAsync_Should_Call_Generator_When_Issued_Snapshot_Is_Available()
    {
        await using var db = EInvoiceArtifactDbContext.Create();
        var invoiceId = Guid.NewGuid();
        db.Set<Invoice>().Add(new Invoice
        {
            Id = invoiceId,
            Status = InvoiceStatus.Open,
            Currency = "EUR",
            DueDateUtc = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            IssuedAtUtc = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            IssuedSnapshotJson = BuildReadySnapshot(invoiceId),
            RowVersion = new byte[] { 1 }
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var generator = new RecordingGenerator(new EInvoiceGenerationResult(
            EInvoiceGenerationStatus.Generated,
            "Generated",
            new EInvoiceArtifact(
                invoiceId,
                EInvoiceArtifactFormat.ZugferdFacturX,
                "application/pdf",
                "invoice.pdf",
                new byte[] { 1, 2, 3 },
                "factur-x-test-profile",
                new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc))));
        var handler = CreateHandler(db, generator);

        var result = await handler.HandleAsync(
            invoiceId,
            EInvoiceArtifactFormat.ZugferdFacturX,
            TestContext.Current.CancellationToken);

        result.Status.Should().Be(EInvoiceGenerationStatus.Generated);
        result.Artifact!.InvoiceId.Should().Be(invoiceId);
        generator.Calls.Should().Be(1);
        generator.LastFormat.Should().Be(EInvoiceArtifactFormat.ZugferdFacturX);
    }

    [Fact]
    public async Task HandleAsync_Should_Reject_Generated_Artifact_For_Different_Invoice()
    {
        await using var db = EInvoiceArtifactDbContext.Create();
        var invoiceId = Guid.NewGuid();
        db.Set<Invoice>().Add(new Invoice
        {
            Id = invoiceId,
            Status = InvoiceStatus.Open,
            Currency = "EUR",
            DueDateUtc = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            IssuedSnapshotJson = BuildReadySnapshot(invoiceId),
            RowVersion = new byte[] { 1 }
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var generator = new RecordingGenerator(new EInvoiceGenerationResult(
            EInvoiceGenerationStatus.Generated,
            "Generated",
            new EInvoiceArtifact(
                Guid.NewGuid(),
                EInvoiceArtifactFormat.ZugferdFacturX,
                "application/pdf",
                "invoice.pdf",
                new byte[] { 1 },
                "factur-x-test-profile",
                new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc))));
        var handler = CreateHandler(db, generator);

        var action = () => handler.HandleAsync(
            invoiceId,
            EInvoiceArtifactFormat.ZugferdFacturX,
            TestContext.Current.CancellationToken);

        await action.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Generated e-invoice artifact invoice id does not match the requested invoice.");
    }

    [Fact]
    public async Task HandleAsync_Should_Return_ValidationFailed_When_Issued_Snapshot_Lacks_EInvoice_Source_Fields()
    {
        await using var db = EInvoiceArtifactDbContext.Create();
        var invoiceId = Guid.NewGuid();
        db.Set<Invoice>().Add(new Invoice
        {
            Id = invoiceId,
            Status = InvoiceStatus.Open,
            Currency = "EUR",
            DueDateUtc = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            IssuedAtUtc = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            IssuedSnapshotJson = $$"""{"invoiceId":"{{invoiceId}}"}""",
            RowVersion = new byte[] { 1 }
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var generator = new RecordingGenerator();
        var handler = CreateHandler(db, generator);

        var result = await handler.HandleAsync(
            invoiceId,
            EInvoiceArtifactFormat.ZugferdFacturX,
            TestContext.Current.CancellationToken);

        result.Status.Should().Be(EInvoiceGenerationStatus.ValidationFailed);
        result.Message.Should().Contain("Issued invoice snapshot is missing required e-invoice source fields");
        generator.Calls.Should().Be(0);
    }

    [Fact]
    public async Task HandleAsync_Should_Reject_Unsupported_Format()
    {
        await using var db = EInvoiceArtifactDbContext.Create();
        var invoiceId = Guid.NewGuid();
        db.Set<Invoice>().Add(new Invoice
        {
            Id = invoiceId,
            Status = InvoiceStatus.Open,
            Currency = "EUR",
            DueDateUtc = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            IssuedAtUtc = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            IssuedSnapshotJson = BuildReadySnapshot(invoiceId),
            RowVersion = new byte[] { 1 }
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var generator = new RecordingGenerator();
        var handler = CreateHandler(db, generator);

        var result = await handler.HandleAsync(
            invoiceId,
            (EInvoiceArtifactFormat)123,
            TestContext.Current.CancellationToken);

        result.Status.Should().Be(EInvoiceGenerationStatus.UnsupportedFormat);
        result.Message.Should().Be("The requested e-invoice format is not supported.");
        generator.Calls.Should().Be(0);
    }

    [Fact]
    public async Task HandleAsync_Should_Save_Generated_Artifact_To_Storage()
    {
        await using var db = EInvoiceArtifactDbContext.Create();
        var invoiceId = Guid.NewGuid();
        db.Set<Invoice>().Add(new Invoice
        {
            Id = invoiceId,
            Status = InvoiceStatus.Open,
            Currency = "EUR",
            DueDateUtc = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            IssuedAtUtc = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            IssuedSnapshotJson = BuildReadySnapshot(invoiceId),
            RowVersion = new byte[] { 1 }
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var expectedArtifact = new EInvoiceArtifact(
            invoiceId,
            EInvoiceArtifactFormat.ZugferdFacturX,
            "application/pdf",
            "invoice.pdf",
            new byte[] { 1, 2, 3, 4 },
            "factur-x-test-profile",
            new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc));
        var generator = new RecordingGenerator(new EInvoiceGenerationResult(
            EInvoiceGenerationStatus.Generated,
            "Generated",
            expectedArtifact));
        var storage = new RecordingArtifactStorage();
        var handler = CreateHandler(db, generator, storage);

        var result = await handler.HandleAsync(
            invoiceId,
            EInvoiceArtifactFormat.ZugferdFacturX,
            TestContext.Current.CancellationToken);

        result.Status.Should().Be(EInvoiceGenerationStatus.Generated);
        result.Artifact!.InvoiceId.Should().Be(invoiceId);
        storage.Calls.Should().Be(1);
        storage.Artifact.Should().Be(expectedArtifact);
        result.Storage.Should().NotBeNull();
        result.Storage!.Provider.Should().Be("InMemoryTest");
        result.Storage.Sha256Hash.Should().Be("test-hash");
        result.Storage.ObjectKey.Should().Be("tests/2026-05-10/invoice.pdf");
    }

    [Fact]
    public async Task HandleAsync_Should_Generate_XRechnung_When_Requested_Format_Is_XRechnung()
    {
        await using var db = EInvoiceArtifactDbContext.Create();
        var invoiceId = Guid.NewGuid();
        db.Set<Invoice>().Add(new Invoice
        {
            Id = invoiceId,
            Status = InvoiceStatus.Open,
            Currency = "EUR",
            DueDateUtc = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            IssuedAtUtc = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            IssuedSnapshotJson = BuildReadySnapshot(invoiceId),
            RowVersion = new byte[] { 1 }
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var expectedArtifact = new EInvoiceArtifact(
            invoiceId,
            EInvoiceArtifactFormat.XRechnung,
            "application/xml",
            "rechnung.xml",
            new byte[] { 11, 22, 33 },
            "xrechnung-test-profile",
            new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc));
        var generator = new RecordingGenerator(new EInvoiceGenerationResult(
            EInvoiceGenerationStatus.Generated,
            "Generated",
            expectedArtifact));
        var storage = new RecordingArtifactStorage();
        var handler = CreateHandler(db, generator, storage);

        var result = await handler.HandleAsync(
            invoiceId,
            EInvoiceArtifactFormat.XRechnung,
            TestContext.Current.CancellationToken);

        result.Status.Should().Be(EInvoiceGenerationStatus.Generated);
        result.Artifact!.Format.Should().Be(EInvoiceArtifactFormat.XRechnung);
        result.Artifact!.ContentType.Should().Be("application/xml");
        generator.Calls.Should().Be(1);
        generator.LastFormat.Should().Be(EInvoiceArtifactFormat.XRechnung);
        storage.Calls.Should().Be(1);
        storage.Artifact.Should().Be(expectedArtifact);
        result.Storage.Should().NotBeNull();
        result.Storage!.Provider.Should().Be("InMemoryTest");
    }

    private static GenerateInvoiceEInvoiceArtifactHandler CreateHandler(
        IAppDbContext db,
        IEInvoiceGenerationService generator,
        IEInvoiceArtifactStorage? artifactStorage = null)
        => new(db, generator, new EInvoiceSourceReadinessValidator(), artifactStorage);

    private static string BuildReadySnapshot(Guid invoiceId)
        => $$"""
        {
          "invoiceId": "{{invoiceId}}",
          "currency": "EUR",
          "issuedAtUtc": "2026-05-01T00:00:00Z",
          "totalGrossMinor": 11900,
          "issuer": {
            "legalName": "Darwin GmbH",
            "taxId": "DE123456789",
            "addressLine1": "Issuer Street 1",
            "postalCode": "10115",
            "city": "Berlin",
            "country": "DE"
          },
          "customer": {
            "companyName": "Customer GmbH",
            "addressLine1": "Customer Street 2",
            "postalCode": "10115",
            "city": "Berlin",
            "country": "DE"
          },
          "lines": [
            {
              "id": "11111111-1111-1111-1111-111111111111",
              "description": "Invoice line",
              "quantity": 1,
              "unitPriceNetMinor": 10000,
              "totalNetMinor": 10000,
              "totalGrossMinor": 11900
            }
          ]
        }
        """;

    private sealed class RecordingGenerator : IEInvoiceGenerationService
    {
        private readonly EInvoiceGenerationResult _result;

        public RecordingGenerator(EInvoiceGenerationResult? result = null)
        {
            _result = result ?? new EInvoiceGenerationResult(EInvoiceGenerationStatus.NotConfigured, "Not configured");
        }

        public int Calls { get; private set; }
        public EInvoiceArtifactFormat? LastFormat { get; private set; }

        public Task<EInvoiceGenerationResult> GenerateAsync(
            Invoice invoice,
            EInvoiceGenerationRequest request,
            CancellationToken ct = default)
        {
            Calls++;
            LastFormat = request.Format;
            return Task.FromResult(_result);
        }
    }

    private sealed class RecordingArtifactStorage : IEInvoiceArtifactStorage
    {
        private readonly EInvoiceArtifactStorageResult _result = new(
            Provider: "InMemoryTest",
            ContainerName: "tests",
            ObjectKey: "tests/2026-05-10/invoice.pdf",
            VersionId: null,
            Sha256Hash: "test-hash",
            ContentLength: 4,
            CreatedAtUtc: new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc),
            RetentionUntilUtc: null,
            IsImmutable: false);

        public int Calls { get; private set; }
        public EInvoiceArtifact? Artifact { get; private set; }

        public Task<EInvoiceArtifactStorageResult> SaveAsync(EInvoiceArtifact artifact, CancellationToken ct = default)
        {
            Calls++;
            Artifact = artifact;
            return Task.FromResult(_result);
        }
    }

    private sealed class EInvoiceArtifactDbContext : DbContext, IAppDbContext
    {
        private EInvoiceArtifactDbContext(DbContextOptions<EInvoiceArtifactDbContext> options) : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Invoice>();
        }

        public static EInvoiceArtifactDbContext Create()
        {
            var options = new DbContextOptionsBuilder<EInvoiceArtifactDbContext>()
                .UseInMemoryDatabase($"darwin_e_invoice_artifact_tests_{Guid.NewGuid()}")
                .Options;

            return new EInvoiceArtifactDbContext(options);
        }
    }
}
