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

/// <summary>
/// Creates hosted subscription checkout sessions for business billing providers.
/// </summary>
public interface ISubscriptionCheckoutSessionClient
{
    /// <summary>
    /// Creates a hosted checkout session for a business subscription plan.
    /// </summary>
    Task<SubscriptionCheckoutSessionResult> CreateSessionAsync(SubscriptionCheckoutSessionRequest request, CancellationToken ct = default);
}

/// <summary>
/// Provider-neutral request used to create a hosted business subscription checkout session.
/// </summary>
public sealed class SubscriptionCheckoutSessionRequest
{
    public string Provider { get; init; } = "Stripe";

    public string SecretKey { get; init; } = string.Empty;

    public Guid BusinessId { get; init; }

    public string BusinessName { get; init; } = string.Empty;

    public string? CustomerEmail { get; init; }

    public Guid PlanId { get; init; }

    public string PlanCode { get; init; } = string.Empty;

    public string PlanName { get; init; } = string.Empty;

    public long AmountMinor { get; init; }

    public string Currency { get; init; } = string.Empty;

    public string Interval { get; init; } = string.Empty;

    public int IntervalCount { get; init; }

    public int? TrialDays { get; init; }

    public string SuccessUrl { get; init; } = string.Empty;

    public string CancelUrl { get; init; } = string.Empty;
}

/// <summary>
/// Provider references returned after creating a hosted subscription checkout session.
/// </summary>
public sealed class SubscriptionCheckoutSessionResult
{
    public string ProviderReference { get; init; } = string.Empty;

    public string ProviderCheckoutSessionReference { get; init; } = string.Empty;

    public string? ProviderSubscriptionReference { get; init; }

    public string? ProviderCustomerReference { get; init; }

    public string CheckoutUrl { get; init; } = string.Empty;

    public DateTime? ExpiresAtUtc { get; init; }
}
