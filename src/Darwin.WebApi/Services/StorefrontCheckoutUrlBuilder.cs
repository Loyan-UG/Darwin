using Darwin.Application;
using Darwin.Application.Orders.DTOs;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Localization;

namespace Darwin.WebApi.Services;

/// <summary>
/// Builds storefront checkout return, cancellation, and Stripe handoff URLs from configuration.
/// </summary>
public sealed class StorefrontCheckoutUrlBuilder
{
    private readonly IConfiguration _configuration;
    private readonly IStringLocalizer<ValidationResource> _validationLocalizer;

    /// <summary>
    /// Initializes a new instance of the <see cref="StorefrontCheckoutUrlBuilder"/> class.
    /// </summary>
    public StorefrontCheckoutUrlBuilder(
        IConfiguration configuration,
        IStringLocalizer<ValidationResource> validationLocalizer)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _validationLocalizer = validationLocalizer ?? throw new ArgumentNullException(nameof(validationLocalizer));
    }

    /// <summary>
    /// Builds the front-office confirmation URL for a storefront order.
    /// </summary>
    public string BuildFrontOfficeConfirmationUrl(Guid orderId, string? orderNumber, bool cancelled)
    {
        var baseUrl = _configuration["StorefrontCheckout:FrontOfficeBaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl) || !Uri.TryCreate(baseUrl, UriKind.Absolute, out var frontOfficeBaseUri))
        {
            throw new InvalidOperationException(_validationLocalizer["StorefrontFrontOfficeBaseUrlNotConfigured"]);
        }

        var queryBuilder = new QueryBuilder();
        if (!string.IsNullOrWhiteSpace(orderNumber))
        {
            queryBuilder.Add("orderNumber", orderNumber.Trim());
        }

        if (cancelled)
        {
            queryBuilder.Add("cancelled", "true");
        }

        return new UriBuilder(frontOfficeBaseUri)
        {
            Path = $"/checkout/orders/{orderId:D}/confirmation",
            Query = queryBuilder.ToQueryString().Value?.TrimStart('?')
        }.Uri.AbsoluteUri;
    }

    /// <summary>
    /// Builds the configured Stripe checkout handoff URL for a storefront payment intent.
    /// </summary>
    public string BuildStripeCheckoutUrl(StorefrontPaymentIntentResultDto result, string returnUrl, string cancelUrl)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (!string.Equals(result.Provider, "Stripe", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(_validationLocalizer["StorefrontPaymentProviderNotSupported"]);
        }

        var stripeCheckoutBaseUrl = _configuration["StorefrontCheckout:StripeCheckoutBaseUrl"];
        if (string.IsNullOrWhiteSpace(stripeCheckoutBaseUrl) || !Uri.TryCreate(stripeCheckoutBaseUrl, UriKind.Absolute, out var stripeCheckoutBaseUri))
        {
            throw new InvalidOperationException(_validationLocalizer["StorefrontStripeCheckoutBaseUrlNotConfigured"]);
        }

        var queryBuilder = new QueryBuilder
        {
            { "orderId", result.OrderId.ToString("D") },
            { "paymentId", result.PaymentId.ToString("D") },
            { "provider", "Stripe" },
            { "checkoutSessionId", result.ProviderCheckoutSessionReference ?? result.ProviderReference },
            { "returnUrl", returnUrl },
            { "cancelUrl", cancelUrl }
        };

        if (!string.IsNullOrWhiteSpace(result.ProviderPaymentIntentReference))
        {
            queryBuilder.Add("paymentIntentId", result.ProviderPaymentIntentReference);
        }

        return new UriBuilder(stripeCheckoutBaseUri)
        {
            Query = queryBuilder.ToQueryString().Value?.TrimStart('?')
        }.Uri.AbsoluteUri;
    }
}
