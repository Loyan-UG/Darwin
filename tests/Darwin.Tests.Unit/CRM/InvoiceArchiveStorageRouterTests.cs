using Darwin.Application.Abstractions.Invoicing;
using Darwin.Application.CRM.Services;
using Darwin.Domain.Entities.CRM;
using FluentAssertions;

namespace Darwin.Tests.Unit.CRM;

public sealed class InvoiceArchiveStorageRouterTests
{
    [Fact]
    public async Task SaveAsync_Should_Route_To_Selected_Internal_Database_Provider_By_Default()
    {
        var provider = new RecordingInvoiceArchiveStorageProvider(InvoiceArchiveStorageProviderNames.InternalDatabase);
        var router = new InvoiceArchiveStorageRouter(
            new IInvoiceArchiveStorageProvider[] { provider },
            new InvoiceArchiveStorageSelection());
        var invoice = new Invoice { Id = Guid.NewGuid() };
        var artifact = new InvoiceArchiveStorageArtifact(
            invoice.Id,
            new DateTime(2026, 5, 10, 10, 0, 0, DateTimeKind.Utc),
            "application/json",
            "invoice.json",
            "{\"invoiceNumber\":\"INV-ROUTED\"}");

        var result = await router.SaveAsync(invoice, artifact, TestContext.Current.CancellationToken);

        provider.SaveCallCount.Should().Be(1);
        provider.LastArtifact.Should().Be(artifact);
        result.HashSha256.Should().Be("hash-InternalDatabase");
    }

    [Fact]
    public async Task ExistsAsync_Should_Reject_Unregistered_Selected_Provider()
    {
        var router = new InvoiceArchiveStorageRouter(
            new IInvoiceArchiveStorageProvider[]
            {
                new RecordingInvoiceArchiveStorageProvider(InvoiceArchiveStorageProviderNames.InternalDatabase)
            },
            new InvoiceArchiveStorageSelection { ProviderName = InvoiceArchiveStorageProviderNames.AzureBlob });

        var action = () => router.ExistsAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        await action.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Invoice archive storage provider 'AzureBlob' is not registered.");
    }

    private sealed class RecordingInvoiceArchiveStorageProvider : IInvoiceArchiveStorageProvider
    {
        public RecordingInvoiceArchiveStorageProvider(string providerName)
        {
            ProviderName = providerName;
        }

        public string ProviderName { get; }

        public int SaveCallCount { get; private set; }

        public InvoiceArchiveStorageArtifact? LastArtifact { get; private set; }

        public Task<InvoiceArchiveStorageResult> SaveAsync(Invoice invoice, InvoiceArchiveStorageArtifact artifact, CancellationToken ct = default)
        {
            SaveCallCount++;
            LastArtifact = artifact;

            return Task.FromResult(new InvoiceArchiveStorageResult(
                $"hash-{ProviderName}",
                artifact.IssuedAtUtc,
                artifact.IssuedAtUtc.AddYears(10),
                "test-policy"));
        }

        public Task<InvoiceArchiveStorageArtifact?> ReadAsync(Guid invoiceId, CancellationToken ct = default)
            => Task.FromResult<InvoiceArchiveStorageArtifact?>(null);

        public Task<bool> ExistsAsync(Guid invoiceId, CancellationToken ct = default)
            => Task.FromResult(true);

        public Task PurgePayloadAsync(Invoice invoice, string reason, DateTime purgedAtUtc, CancellationToken ct = default)
            => Task.CompletedTask;
    }
}
