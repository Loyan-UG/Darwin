namespace Darwin.Application.Abstractions.Payments;

/// <summary>
/// Creates provider-side refunds for captured storefront payments.
/// </summary>
public interface IRefundProviderClient
{
    /// <summary>
    /// Creates a provider refund using an idempotent refund request.
    /// </summary>
    Task<RefundProviderResult> CreateRefundAsync(RefundProviderRequest request, CancellationToken ct = default);
}

/// <summary>
/// Provider-neutral refund request.
/// </summary>
public sealed class RefundProviderRequest
{
    public string Provider { get; init; } = "Stripe";

    public string SecretKey { get; init; } = string.Empty;

    public Guid RefundId { get; init; }

    public Guid PaymentId { get; init; }

    public Guid? OrderId { get; init; }

    public string? ProviderPaymentIntentReference { get; init; }

    public string? ProviderTransactionReference { get; init; }

    public long AmountMinor { get; init; }

    public string Currency { get; init; } = string.Empty;

    public string Reason { get; init; } = string.Empty;
}

/// <summary>
/// Provider-neutral refund result.
/// </summary>
public sealed class RefundProviderResult
{
    public string ProviderRefundReference { get; init; } = string.Empty;

    public string? ProviderPaymentReference { get; init; }

    public string ProviderStatus { get; init; } = string.Empty;

    public bool IsCompleted { get; init; }

    public bool IsFailed { get; init; }

    public string? FailureReason { get; init; }
}
