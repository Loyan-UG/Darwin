using Darwin.Application.Abstractions.Invoicing;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.CRM.Services;
using Darwin.Application.Extensions;
using Darwin.Domain.Entities.CRM;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Darwin.Tests.Unit.CRM;

public sealed class EInvoiceGenerationServiceTests
{
    [Fact]
    public async Task NotConfiguredService_Should_Not_Generate_Fake_Compliant_Artifact()
    {
        var service = new NotConfiguredEInvoiceGenerationService();
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            IssuedSnapshotJson = "{\"invoiceId\":\"00000000-0000-0000-0000-000000000001\"}",
            RowVersion = new byte[] { 1 }
        };

        var result = await service.GenerateAsync(
            invoice,
            new EInvoiceGenerationRequest(EInvoiceArtifactFormat.ZugferdFacturX),
            TestContext.Current.CancellationToken);

        result.Status.Should().Be(EInvoiceGenerationStatus.NotConfigured);
        result.IsGenerated.Should().BeFalse();
        result.Artifact.Should().BeNull();
        result.Message.Should().Contain("not legal e-invoice artifacts");
    }

    [Fact]
    public async Task NotConfiguredService_Should_Reject_Unknown_Format_Without_Artifact()
    {
        var service = new NotConfiguredEInvoiceGenerationService();
        var invoice = new Invoice { Id = Guid.NewGuid(), RowVersion = new byte[] { 1 } };

        var result = await service.GenerateAsync(
            invoice,
            new EInvoiceGenerationRequest((EInvoiceArtifactFormat)999),
            TestContext.Current.CancellationToken);

        result.Status.Should().Be(EInvoiceGenerationStatus.UnsupportedFormat);
        result.IsGenerated.Should().BeFalse();
        result.Artifact.Should().BeNull();
    }

    [Fact]
    public async Task AddApplication_Should_Register_EInvoiceGeneration_Boundary_As_NotConfigured_Default()
    {
        await using var db = EInvoiceGenerationDbContext.Create();
        var services = new ServiceCollection();
        services.AddSingleton<IAppDbContext>(db);
        services.AddApplication(new ConfigurationBuilder().Build());

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IEInvoiceGenerationService>();

        service.Should().BeOfType<NotConfiguredEInvoiceGenerationService>();
        var result = await service.GenerateAsync(
            new Invoice { Id = Guid.NewGuid(), RowVersion = new byte[] { 1 } },
            new EInvoiceGenerationRequest(EInvoiceArtifactFormat.XRechnung),
            TestContext.Current.CancellationToken);

        result.Status.Should().Be(EInvoiceGenerationStatus.NotConfigured);
        result.Artifact.Should().BeNull();
    }

    [Fact]
    public void EInvoiceSourceReadinessValidator_Should_Report_Missing_Fields_For_Minimal_Snapshot()
    {
        var validator = new EInvoiceSourceReadinessValidator();
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            IssuedSnapshotJson = "{\"invoiceId\":\"00000000-0000-0000-0000-000000000001\"}",
            RowVersion = new byte[] { 1 }
        };

        var result = validator.Validate(invoice);

        result.IsReady.Should().BeFalse();
        result.MissingFields.Should().Contain("currency");
        result.MissingFields.Should().Contain("issuedAtUtc");
        result.MissingFields.Should().Contain("issuer");
        result.MissingFields.Should().Contain("customer");
        result.MissingFields.Should().Contain("lines");
    }

    [Fact]
    public void EInvoiceSourceReadinessValidator_Should_Accept_Minimum_Ready_Source_Snapshot()
    {
        var validator = new EInvoiceSourceReadinessValidator();
        var invoiceId = Guid.NewGuid();
        var invoice = new Invoice
        {
            Id = invoiceId,
            IssuedSnapshotJson = $$"""
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
                  "description": "Invoice line",
                  "quantity": 1,
                  "unitPriceNetMinor": 10000,
                  "totalNetMinor": 10000,
                  "totalGrossMinor": 11900
                }
              ]
            }
            """,
            RowVersion = new byte[] { 1 }
        };

        var result = validator.Validate(invoice);

        result.IsReady.Should().BeTrue();
        result.MissingFields.Should().BeEmpty();
    }

    private sealed class EInvoiceGenerationDbContext : DbContext, IAppDbContext
    {
        private EInvoiceGenerationDbContext(DbContextOptions<EInvoiceGenerationDbContext> options) : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static EInvoiceGenerationDbContext Create()
        {
            var options = new DbContextOptionsBuilder<EInvoiceGenerationDbContext>()
                .UseInMemoryDatabase($"darwin_e_invoice_generation_tests_{Guid.NewGuid()}")
                .Options;

            return new EInvoiceGenerationDbContext(options);
        }
    }
}
