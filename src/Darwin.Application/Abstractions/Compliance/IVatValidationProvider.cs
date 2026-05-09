using Darwin.Domain.Enums;

namespace Darwin.Application.Abstractions.Compliance;

public interface IVatValidationProvider
{
    Task<VatValidationProviderResult> ValidateAsync(string vatId, CancellationToken ct = default);
}

public sealed class VatValidationProviderResult
{
    public CustomerVatValidationStatus Status { get; init; } = CustomerVatValidationStatus.Unknown;
    public string Source { get; init; } = "provider";
    public string? Message { get; init; }
}
