namespace Darwin.Contracts.Shipping;

/// <summary>
/// Carrier callback payload used to apply DHL shipment lifecycle updates.
/// </summary>
public sealed class DhlShipmentCallbackRequest
{
    public string ProviderShipmentReference { get; set; } = string.Empty;

    public string CarrierEventKey { get; set; } = string.Empty;

    public DateTime OccurredAtUtc { get; set; }

    public string? ProviderStatus { get; set; }

    public string? ExceptionCode { get; set; }

    public string? ExceptionMessage { get; set; }

    public string? TrackingNumber { get; set; }

    public string? LabelUrl { get; set; }

    public string? Service { get; set; }
}
