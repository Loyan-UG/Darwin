using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using Darwin.Application.Abstractions.Payments;

namespace Darwin.Infrastructure.Payments;

/// <summary>
/// Creates Stripe refunds through the provider API.
/// </summary>
public sealed class StripeRefundProviderClient : IRefundProviderClient
{
    private const string RefundsPath = "v1/refunds";
    private static readonly Uri DefaultStripeApiBaseUri = new("https://api.stripe.com/");
    private static readonly Regex StripeReferenceRegex = new(
        @"\b(?:pi|ch|re|cs_(?:test|live))_[A-Za-z0-9_]{8,}\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(100));

    private readonly HttpClient _httpClient;

    public StripeRefundProviderClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _httpClient.BaseAddress ??= DefaultStripeApiBaseUri;
        _httpClient.Timeout = _httpClient.Timeout == Timeout.InfiniteTimeSpan
            ? TimeSpan.FromSeconds(30)
            : _httpClient.Timeout;
    }

    public async Task<RefundProviderResult> CreateRefundAsync(RefundProviderRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!string.Equals(request.Provider, "Stripe", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Unsupported refund provider.");
        }

        if (string.IsNullOrWhiteSpace(request.SecretKey))
        {
            throw new InvalidOperationException("Stripe secret key is not configured.");
        }

        if (request.AmountMinor <= 0)
        {
            throw new InvalidOperationException("Stripe refund amount must be greater than zero.");
        }

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, RefundsPath)
        {
            Content = new FormUrlEncodedContent(BuildFormFields(request))
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.SecretKey.Trim());
        httpRequest.Headers.TryAddWithoutValidation("Idempotency-Key", $"refund-{request.RefundId:D}");

        using var response = await _httpClient.SendAsync(httpRequest, ct).ConfigureAwait(false);
        var responseBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Stripe refund creation failed with HTTP {(int)response.StatusCode}: {GetSafeStripeError(responseBody)}");
        }

        using var document = JsonDocument.Parse(responseBody);
        var root = document.RootElement;
        var refundId = GetString(root, "id");
        var status = GetString(root, "status") ?? "unknown";
        if (string.IsNullOrWhiteSpace(refundId))
        {
            throw new InvalidOperationException("Stripe refund response was incomplete.");
        }

        var failureReason = GetString(root, "failure_reason");
        return new RefundProviderResult
        {
            ProviderRefundReference = refundId.Trim(),
            ProviderPaymentReference = GetString(root, "payment_intent") ?? GetString(root, "charge"),
            ProviderStatus = status.Trim(),
            IsCompleted = string.Equals(status, "succeeded", StringComparison.OrdinalIgnoreCase),
            IsFailed = string.Equals(status, "failed", StringComparison.OrdinalIgnoreCase),
            FailureReason = string.IsNullOrWhiteSpace(failureReason) ? null : failureReason.Trim()
        };
    }

    private static IEnumerable<KeyValuePair<string, string>> BuildFormFields(RefundProviderRequest request)
    {
        var fields = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["amount"] = request.AmountMinor.ToString(CultureInfo.InvariantCulture),
            ["metadata[refundId]"] = request.RefundId.ToString("D"),
            ["metadata[paymentId]"] = request.PaymentId.ToString("D"),
            ["metadata[reason]"] = request.Reason.Trim()
        };

        if (request.OrderId.HasValue)
        {
            fields["metadata[orderId]"] = request.OrderId.Value.ToString("D");
        }

        var paymentIntentReference = NormalizeOptional(request.ProviderPaymentIntentReference);
        var transactionReference = NormalizeOptional(request.ProviderTransactionReference);
        if (IsStripePaymentIntent(paymentIntentReference))
        {
            fields["payment_intent"] = paymentIntentReference!;
        }
        else if (IsStripeCharge(transactionReference))
        {
            fields["charge"] = transactionReference!;
        }
        else if (IsStripePaymentIntent(transactionReference))
        {
            fields["payment_intent"] = transactionReference!;
        }
        else
        {
            throw new InvalidOperationException("Stripe refund requires a payment intent or charge reference.");
        }

        return fields;
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool IsStripePaymentIntent(string? value)
        => value?.StartsWith("pi_", StringComparison.OrdinalIgnoreCase) == true;

    private static bool IsStripeCharge(string? value)
        => value?.StartsWith("ch_", StringComparison.OrdinalIgnoreCase) == true;

    private static string GetSafeStripeError(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return "No provider error message was returned.";
        }

        try
        {
            using var document = JsonDocument.Parse(responseBody);
            if (document.RootElement.TryGetProperty("error", out var error) &&
                error.TryGetProperty("message", out var message) &&
                message.ValueKind == JsonValueKind.String)
            {
                var value = message.GetString();
                return string.IsNullOrWhiteSpace(value) ? "Provider returned an empty error message." : RedactProviderReferences(value.Trim());
            }
        }
        catch (JsonException)
        {
            return "Provider returned an unreadable error message.";
        }

        return "Provider returned an unexpected error message.";
    }

    private static string RedactProviderReferences(string value)
        => StripeReferenceRegex.Replace(value, "[provider-reference]");

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
