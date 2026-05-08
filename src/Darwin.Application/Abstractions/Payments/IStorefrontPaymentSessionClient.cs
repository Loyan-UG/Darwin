namespace Darwin.Application.Abstractions.Payments;

/// <summary>
/// Creates hosted payment sessions for storefront checkout providers.
/// </summary>
public interface IStorefrontPaymentSessionClient
{
    /// <summary>
    /// Creates a hosted checkout session for a pending storefront payment.
    /// </summary>
    Task<StorefrontPaymentSessionResult> CreateSessionAsync(StorefrontPaymentSessionRequest request, CancellationToken ct = default);
}

/// <summary>
/// Provider-neutral request used to create a hosted checkout session.
/// </summary>
public sealed class StorefrontPaymentSessionRequest
{
    public string Provider { get; init; } = "Stripe";

    public string SecretKey { get; init; } = string.Empty;

    public string MerchantDisplayName { get; init; } = string.Empty;

    public Guid OrderId { get; init; }

    public string OrderNumber { get; init; } = string.Empty;

    public Guid PaymentId { get; init; }

    public long AmountMinor { get; init; }

    public string Currency { get; init; } = string.Empty;

    public string ReturnUrl { get; init; } = string.Empty;

    public string CancelUrl { get; init; } = string.Empty;
}

/// <summary>
/// Provider references returned after creating a hosted checkout session.
/// </summary>
public sealed class StorefrontPaymentSessionResult
{
    public string ProviderReference { get; init; } = string.Empty;

    public string? ProviderPaymentIntentReference { get; init; }

    public string? ProviderCheckoutSessionReference { get; init; }

    public string CheckoutUrl { get; init; } = string.Empty;
}
