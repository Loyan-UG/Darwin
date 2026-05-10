using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using Darwin.Application.Abstractions.Payments;

namespace Darwin.WebApi.Services;

/// <summary>
/// Creates Stripe-hosted Checkout Sessions for business subscriptions.
/// </summary>
public sealed class StripeSubscriptionCheckoutSessionClient : ISubscriptionCheckoutSessionClient
{
    private const string CheckoutSessionsPath = "v1/checkout/sessions";
    private static readonly Uri DefaultStripeApiBaseUri = new("https://api.stripe.com/");

    private readonly HttpClient _httpClient;

    public StripeSubscriptionCheckoutSessionClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _httpClient.BaseAddress ??= DefaultStripeApiBaseUri;
        _httpClient.Timeout = _httpClient.Timeout == Timeout.InfiniteTimeSpan
            ? TimeSpan.FromSeconds(30)
            : _httpClient.Timeout;
    }

    public async Task<SubscriptionCheckoutSessionResult> CreateSessionAsync(SubscriptionCheckoutSessionRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!string.Equals(request.Provider, "Stripe", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Unsupported subscription checkout provider.");
        }

        if (string.IsNullOrWhiteSpace(request.SecretKey))
        {
            throw new InvalidOperationException("Stripe secret key is not configured.");
        }

        if (request.AmountMinor <= 0)
        {
            throw new InvalidOperationException("Stripe subscription amount must be greater than zero.");
        }

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, CheckoutSessionsPath)
        {
            Content = new FormUrlEncodedContent(BuildFormFields(request))
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.SecretKey.Trim());
        httpRequest.Headers.TryAddWithoutValidation("Idempotency-Key", $"subscription-checkout-{request.BusinessId:D}-{request.PlanId:D}");

        using var response = await _httpClient.SendAsync(httpRequest, ct).ConfigureAwait(false);
        var responseBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Stripe subscription checkout session creation failed with HTTP {(int)response.StatusCode}.");
        }

        using var document = JsonDocument.Parse(responseBody);
        var root = document.RootElement;
        var sessionId = GetString(root, "id");
        var checkoutUrl = GetString(root, "url");
        if (string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(checkoutUrl))
        {
            throw new InvalidOperationException("Stripe subscription checkout session response was incomplete.");
        }

        return new SubscriptionCheckoutSessionResult
        {
            ProviderReference = sessionId.Trim(),
            ProviderCheckoutSessionReference = sessionId.Trim(),
            ProviderSubscriptionReference = GetString(root, "subscription")?.Trim(),
            ProviderCustomerReference = GetString(root, "customer")?.Trim(),
            CheckoutUrl = checkoutUrl.Trim(),
            ExpiresAtUtc = GetUnixDateTimeUtc(root, "expires_at")
        };
    }

    private static IEnumerable<KeyValuePair<string, string>> BuildFormFields(SubscriptionCheckoutSessionRequest request)
    {
        var currency = request.Currency.Trim().ToLowerInvariant();
        var planName = string.IsNullOrWhiteSpace(request.PlanName) ? request.PlanCode.Trim() : request.PlanName.Trim();
        var businessName = string.IsNullOrWhiteSpace(request.BusinessName) ? "Business subscription" : request.BusinessName.Trim();
        var interval = NormalizeInterval(request.Interval);
        var intervalCount = Math.Clamp(request.IntervalCount, 1, 12);

        var fields = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["mode"] = "subscription",
            ["success_url"] = request.SuccessUrl.Trim(),
            ["cancel_url"] = request.CancelUrl.Trim(),
            ["client_reference_id"] = request.BusinessId.ToString("D"),
            ["line_items[0][quantity]"] = "1",
            ["line_items[0][price_data][currency]"] = currency,
            ["line_items[0][price_data][unit_amount]"] = request.AmountMinor.ToString(CultureInfo.InvariantCulture),
            ["line_items[0][price_data][recurring][interval]"] = interval,
            ["line_items[0][price_data][recurring][interval_count]"] = intervalCount.ToString(CultureInfo.InvariantCulture),
            ["line_items[0][price_data][product_data][name]"] = planName,
            ["line_items[0][price_data][product_data][metadata][businessId]"] = request.BusinessId.ToString("D"),
            ["line_items[0][price_data][product_data][metadata][planId]"] = request.PlanId.ToString("D"),
            ["line_items[0][price_data][product_data][metadata][planCode]"] = request.PlanCode.Trim(),
            ["metadata[businessId]"] = request.BusinessId.ToString("D"),
            ["metadata[businessName]"] = businessName,
            ["metadata[planId]"] = request.PlanId.ToString("D"),
            ["metadata[planCode]"] = request.PlanCode.Trim(),
            ["subscription_data[metadata][businessId]"] = request.BusinessId.ToString("D"),
            ["subscription_data[metadata][planId]"] = request.PlanId.ToString("D"),
            ["subscription_data[metadata][planCode]"] = request.PlanCode.Trim(),
            ["expand[0]"] = "subscription",
            ["expand[1]"] = "customer"
        };

        if (!string.IsNullOrWhiteSpace(request.CustomerEmail))
        {
            fields["customer_email"] = request.CustomerEmail.Trim();
        }

        if (request.TrialDays is > 0)
        {
            fields["subscription_data[trial_period_days]"] = request.TrialDays.Value.ToString(CultureInfo.InvariantCulture);
        }

        return fields;
    }

    private static string NormalizeInterval(string interval)
        => interval.Trim().ToLowerInvariant() switch
        {
            "day" => "day",
            "week" => "week",
            "year" => "year",
            _ => "month"
        };

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

    private static DateTime? GetUnixDateTimeUtc(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind != JsonValueKind.Number || !property.TryGetInt64(out var seconds))
        {
            return null;
        }

        return DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;
    }
}
