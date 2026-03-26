using Darwin.Application.Orders.DTOs;
using Microsoft.AspNetCore.Http.Extensions;

namespace Darwin.WebApi.Services;

/// <summary>
/// Builds storefront checkout return, cancellation, and hosted-checkout handoff URLs from configuration.
/// </summary>
public sealed class StorefrontCheckoutUrlBuilder
{
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="StorefrontCheckoutUrlBuilder"/> class.
    /// </summary>
    public StorefrontCheckoutUrlBuilder(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// Builds the front-office confirmation URL for a storefront order.
    /// </summary>
    public string BuildFrontOfficeConfirmationUrl(Guid orderId, string? orderNumber, bool cancelled)
    {
        var baseUrl = _configuration["StorefrontCheckout:FrontOfficeBaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl) || !Uri.TryCreate(baseUrl, UriKind.Absolute, out var frontOfficeBaseUri))
        {
            throw new InvalidOperationException("Storefront front-office base URL is not configured.");
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
    /// Builds the configured hosted-checkout handoff URL for a storefront payment intent.
    /// </summary>
    public string BuildGatewayUrl(StorefrontPaymentIntentResultDto result, string returnUrl, string cancelUrl)
    {
        ArgumentNullException.ThrowIfNull(result);

        var gatewayBaseUrl = _configuration["StorefrontCheckout:PaymentGatewayBaseUrl"];
        if (string.IsNullOrWhiteSpace(gatewayBaseUrl) || !Uri.TryCreate(gatewayBaseUrl, UriKind.Absolute, out var gatewayBaseUri))
        {
            throw new InvalidOperationException("Storefront payment gateway base URL is not configured.");
        }

        var queryBuilder = new QueryBuilder
        {
            { "orderId", result.OrderId.ToString("D") },
            { "paymentId", result.PaymentId.ToString("D") },
            { "provider", result.Provider },
            { "sessionToken", result.ProviderReference },
            { "returnUrl", returnUrl },
            { "cancelUrl", cancelUrl }
        };

        return new UriBuilder(gatewayBaseUri)
        {
            Query = queryBuilder.ToQueryString().Value?.TrimStart('?')
        }.Uri.AbsoluteUri;
    }
}
