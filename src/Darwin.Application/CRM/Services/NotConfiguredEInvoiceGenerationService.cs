using Darwin.Application.Abstractions.Invoicing;
using Darwin.Domain.Entities.CRM;

namespace Darwin.Application.CRM.Services;

public sealed class NotConfiguredEInvoiceGenerationService : IEInvoiceGenerationService
{
    public Task<EInvoiceGenerationResult> GenerateAsync(
        Invoice invoice,
        EInvoiceGenerationRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        ArgumentNullException.ThrowIfNull(request);

        if (!Enum.IsDefined(request.Format))
        {
            return Task.FromResult(new EInvoiceGenerationResult(
                EInvoiceGenerationStatus.UnsupportedFormat,
                "The requested e-invoice format is not supported by the current deployment."));
        }

        return Task.FromResult(new EInvoiceGenerationResult(
            EInvoiceGenerationStatus.NotConfigured,
            "A compliant e-invoice generator is not configured. Current JSON, HTML, CSV, and structured source-model exports are not legal e-invoice artifacts."));
    }
}
