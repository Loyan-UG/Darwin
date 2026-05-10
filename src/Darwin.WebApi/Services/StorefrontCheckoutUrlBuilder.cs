using Darwin.Application;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Localization;

namespace Darwin.WebApi.Services;

/// <summary>
/// Builds storefront checkout return and cancellation URLs from configuration.
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

}
