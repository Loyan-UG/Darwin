using Darwin.Application.Abstractions.Invoicing;
using Darwin.Domain.Entities.CRM;

namespace Darwin.Application.CRM.Services;

/// <summary>
/// Routes invoice archive operations to the configured named storage provider.
/// </summary>
public sealed class InvoiceArchiveStorageRouter : IInvoiceArchiveStorage
{
    private readonly IReadOnlyDictionary<string, IInvoiceArchiveStorageProvider> _providers;
    private readonly InvoiceArchiveStorageSelection _selection;

    public InvoiceArchiveStorageRouter(
        IEnumerable<IInvoiceArchiveStorageProvider> providers,
        InvoiceArchiveStorageSelection selection)
    {
        ArgumentNullException.ThrowIfNull(providers);
        _selection = selection ?? throw new ArgumentNullException(nameof(selection));

        _providers = providers
            .GroupBy(provider => provider.ProviderName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.First(),
                StringComparer.OrdinalIgnoreCase);
    }

    public Task<InvoiceArchiveStorageResult> SaveAsync(Invoice invoice, InvoiceArchiveStorageArtifact artifact, CancellationToken ct = default)
        => ActiveProvider.SaveAsync(invoice, artifact, ct);

    public Task<InvoiceArchiveStorageArtifact?> ReadAsync(Guid invoiceId, CancellationToken ct = default)
        => ActiveProvider.ReadAsync(invoiceId, ct);

    public Task<bool> ExistsAsync(Guid invoiceId, CancellationToken ct = default)
        => ActiveProvider.ExistsAsync(invoiceId, ct);

    public Task PurgePayloadAsync(Invoice invoice, string reason, DateTime purgedAtUtc, CancellationToken ct = default)
        => ActiveProvider.PurgePayloadAsync(invoice, reason, purgedAtUtc, ct);

    private IInvoiceArchiveStorageProvider ActiveProvider
    {
        get
        {
            var providerName = string.IsNullOrWhiteSpace(_selection.ProviderName)
                ? InvoiceArchiveStorageProviderNames.InternalDatabase
                : _selection.ProviderName.Trim();
            providerName = NormalizeProviderName(providerName);

            if (_providers.TryGetValue(providerName, out var provider))
            {
                return provider;
            }

            throw new InvalidOperationException($"Invoice archive storage provider '{providerName}' is not registered.");
        }
    }

    private static string NormalizeProviderName(string providerName)
        => string.Equals(providerName, InvoiceArchiveStorageProviderNames.Minio, StringComparison.OrdinalIgnoreCase) ||
           string.Equals(providerName, InvoiceArchiveStorageProviderNames.AwsS3, StringComparison.OrdinalIgnoreCase)
            ? InvoiceArchiveStorageProviderNames.S3Compatible
            : providerName;
}
