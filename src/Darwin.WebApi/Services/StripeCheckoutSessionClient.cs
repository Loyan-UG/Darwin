using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using Darwin.Application.Abstractions.Payments;

namespace Darwin.WebApi.Services;

/// <summary>
/// Creates Stripe-hosted Checkout Sessions for storefront order payments.
/// </summary>
public sealed class StripeCheckoutSessionClient : IStorefrontPaymentSessionClient
{
    private const string CheckoutSessionsPath = "v1/checkout/sessions";
    private static readonly Uri DefaultStripeApiBaseUri = new("https://api.stripe.com/");

    private readonly HttpClient _httpClient;

    public StripeCheckoutSessionClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _httpClient.BaseAddress ??= DefaultStripeApiBaseUri;
        _httpClient.Timeout = _httpClient.Timeout == Timeout.InfiniteTimeSpan
            ? TimeSpan.FromSeconds(30)
            : _httpClient.Timeout;
    }

    public async Task<StorefrontPaymentSessionResult> CreateSessionAsync(StorefrontPaymentSessionRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!string.Equals(request.Provider, "Stripe", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Unsupported storefront payment provider.");
        }

        if (string.IsNullOrWhiteSpace(request.SecretKey))
        {
            throw new InvalidOperationException("Stripe secret key is not configured.");
        }

        if (request.AmountMinor <= 0)
        {
            throw new InvalidOperationException("Stripe checkout amount must be greater than zero.");
        }

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, CheckoutSessionsPath)
        {
            Content = new FormUrlEncodedContent(BuildFormFields(request))
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.SecretKey.Trim());

        using var response = await _httpClient.SendAsync(httpRequest, ct).ConfigureAwait(false);
        var responseBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Stripe checkout session creation failed with HTTP {(int)response.StatusCode}.");
        }

        using var document = JsonDocument.Parse(responseBody);
        var root = document.RootElement;
        var sessionId = GetString(root, "id");
        var checkoutUrl = GetString(root, "url");
        if (string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(checkoutUrl))
        {
            throw new InvalidOperationException("Stripe checkout session response was incomplete.");
        }

        var paymentIntentId = GetString(root, "payment_intent");
        return new StorefrontPaymentSessionResult
        {
            ProviderReference = sessionId.Trim(),
            ProviderCheckoutSessionReference = sessionId.Trim(),
            ProviderPaymentIntentReference = string.IsNullOrWhiteSpace(paymentIntentId) ? null : paymentIntentId.Trim(),
            CheckoutUrl = checkoutUrl.Trim()
        };
    }

    private static IEnumerable<KeyValuePair<string, string>> BuildFormFields(StorefrontPaymentSessionRequest request)
    {
        var currency = request.Currency.Trim().ToLowerInvariant();
        var orderNumber = request.OrderNumber.Trim();
        var productName = string.IsNullOrWhiteSpace(request.MerchantDisplayName)
            ? $"Order {orderNumber}"
            : $"{request.MerchantDisplayName.Trim()} order {orderNumber}";

        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["mode"] = "payment",
            ["success_url"] = request.ReturnUrl.Trim(),
            ["cancel_url"] = request.CancelUrl.Trim(),
            ["client_reference_id"] = request.PaymentId.ToString("D"),
            ["line_items[0][quantity]"] = "1",
            ["line_items[0][price_data][currency]"] = currency,
            ["line_items[0][price_data][unit_amount]"] = request.AmountMinor.ToString(CultureInfo.InvariantCulture),
            ["line_items[0][price_data][product_data][name]"] = productName,
            ["metadata[orderId]"] = request.OrderId.ToString("D"),
            ["metadata[orderNumber]"] = orderNumber,
            ["metadata[paymentId]"] = request.PaymentId.ToString("D"),
            ["payment_intent_data[metadata][orderId]"] = request.OrderId.ToString("D"),
            ["payment_intent_data[metadata][orderNumber]"] = orderNumber,
            ["payment_intent_data[metadata][paymentId]"] = request.PaymentId.ToString("D"),
            ["expand[0]"] = "payment_intent"
        };
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.String)
        {
            return property.GetString();
        }

        if (property.ValueKind == JsonValueKind.Object &&
            property.TryGetProperty("id", out var idProperty) &&
            idProperty.ValueKind == JsonValueKind.String)
        {
            return idProperty.GetString();
        }

        return null;
    }
}
