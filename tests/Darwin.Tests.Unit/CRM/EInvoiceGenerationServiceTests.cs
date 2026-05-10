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
