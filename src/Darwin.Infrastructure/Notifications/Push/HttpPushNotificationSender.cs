using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Notifications;
using Darwin.Application.Abstractions.Services;
using Darwin.Shared.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Darwin.Infrastructure.Notifications.Push;

public sealed class HttpPushNotificationSender : IPushNotificationSender
{
    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<PushGatewayOptions> _optionsMonitor;
    private readonly IClock _clock;
    private readonly ILogger<HttpPushNotificationSender> _logger;

    public HttpPushNotificationSender(
        HttpClient httpClient,
        IOptionsMonitor<PushGatewayOptions> optionsMonitor,
        IClock clock,
        ILogger<HttpPushNotificationSender> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<PushNotificationSendResult>> SendAsync(PushNotificationSendRequest request, CancellationToken ct)
    {
        if (request is null)
        {
            return Result<PushNotificationSendResult>.Fail("Gateway.RequestRequired");
        }

        if (string.IsNullOrWhiteSpace(request.PushToken))
        {
            return Result<PushNotificationSendResult>.Ok(new PushNotificationSendResult
            {
                FailureCode = "Gateway.PushTokenRequired",
                IsInvalidTokenFailure = true
            });
        }

        var options = _optionsMonitor.CurrentValue;
        if (!options.Enabled)
        {
            return Result<PushNotificationSendResult>.Ok(new PushNotificationSendResult
            {
                FailureCode = "Gateway.Disabled"
            });
        }

        var endpoint = ResolveEndpoint(options);
        if (endpoint is null)
        {
            return Result<PushNotificationSendResult>.Ok(new PushNotificationSendResult
            {
                FailureCode = "Gateway.EndpointNotConfigured"
            });
        }

        var payload = new PushGatewayRequest
        {
            NotificationId = request.NotificationId,
            UserId = request.UserId,
            DeviceId = request.DeviceId,
            PushToken = request.PushToken,
            Platform = string.IsNullOrWhiteSpace(request.Platform) ? "Unknown" : request.Platform,
            Provider = string.IsNullOrWhiteSpace(options.Provider) ? NormalizeProviderFromPlatform(request.Platform) : options.Provider.Trim(),
            TargetApp = request.TargetApp,
            Title = request.Title,
            Body = request.Body,
            DeepLink = NormalizeInternalDeepLink(request.DeepLink),
            Data = BuildDataPayload(request),
            CollapseKey = request.CollapseKey,
            AnalyticsLabel = request.AnalyticsLabel,
            IdempotencyKey = request.IdempotencyKey,
            AndroidChannelId = options.AndroidChannelId,
            ApnsTopic = options.ApnsTopic
        };

        var maxAttempts = Math.Clamp(options.MaxAttempts, 1, 5);
        var initialBackoffMs = Math.Clamp(options.InitialBackoffMilliseconds, 100, 10_000);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = JsonContent.Create(payload)
                };

                if (!string.IsNullOrWhiteSpace(options.ApiKey))
                {
                    httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey.Trim());
                }

                using var response = await _httpClient.SendAsync(httpRequest, ct).ConfigureAwait(false);
                var responseCode = (int)response.StatusCode;
                var responseBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                var providerMessageId = TryReadProviderMessageId(responseBody);

                if (response.IsSuccessStatusCode)
                {
                    return Result<PushNotificationSendResult>.Ok(new PushNotificationSendResult
                    {
                        ResponseCode = responseCode,
                        ProviderMessageId = providerMessageId
                    });
                }

                var failureCode = MapProviderFailureCodeFromBody(responseBody) ?? MapGatewayFailureCode(responseCode);
                var transient = IsTransientGatewayStatusCode(responseCode) || IsTransientProviderFailure(failureCode);
                var invalidToken = IsInvalidTokenFailure(failureCode);

                _logger.LogWarning(
                    "Push gateway rejected dispatch. Attempt={Attempt}/{MaxAttempts}, StatusCode={StatusCode}, UserId={UserId}, DeviceId={DeviceId}, FailureCode={FailureCode}",
                    attempt,
                    maxAttempts,
                    responseCode,
                    request.UserId,
                    request.DeviceId,
                    failureCode);

                if (!transient || invalidToken || attempt >= maxAttempts)
                {
                    return Result<PushNotificationSendResult>.Ok(new PushNotificationSendResult
                    {
                        ResponseCode = responseCode,
                        ProviderMessageId = providerMessageId,
                        FailureCode = failureCode,
                        IsTransientFailure = transient && !invalidToken,
                        IsInvalidTokenFailure = invalidToken
                    });
                }

                await DelayBeforeRetryAsync(attempt, initialBackoffMs, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(
                    ex,
                    "Push gateway transport failure. Attempt={Attempt}/{MaxAttempts}, UserId={UserId}, DeviceId={DeviceId}",
                    attempt,
                    maxAttempts,
                    request.UserId,
                    request.DeviceId);

                if (attempt >= maxAttempts)
                {
                    return Result<PushNotificationSendResult>.Ok(new PushNotificationSendResult
                    {
                        FailureCode = ex is TaskCanceledException ? "Gateway.Timeout" : "Gateway.TransportError",
                        IsTransientFailure = true
                    });
                }

                await DelayBeforeRetryAsync(attempt, initialBackoffMs, ct).ConfigureAwait(false);
            }
        }

        return Result<PushNotificationSendResult>.Ok(new PushNotificationSendResult
        {
            FailureCode = "Gateway.TransportError",
            IsTransientFailure = true
        });
    }

    private async Task DelayBeforeRetryAsync(int attempt, int initialBackoffMs, CancellationToken ct)
    {
        var exponential = initialBackoffMs * Math.Pow(2, Math.Max(0, attempt - 1));
        var bounded = Math.Min(15_000d, exponential);
        var jitter = Math.Abs(_clock.UtcNow.Ticks % 200);
        await Task.Delay(TimeSpan.FromMilliseconds(bounded + jitter), ct).ConfigureAwait(false);
    }

    private static Dictionary<string, string> BuildDataPayload(PushNotificationSendRequest request)
    {
        var data = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["notificationId"] = request.NotificationId == Guid.Empty ? string.Empty : request.NotificationId.ToString("D"),
            ["targetApp"] = request.TargetApp ?? string.Empty,
            ["deepLink"] = NormalizeInternalDeepLink(request.DeepLink) ?? string.Empty,
            ["sourceType"] = request.SourceType ?? string.Empty,
            ["sourceId"] = request.SourceId?.ToString("D") ?? string.Empty,
            ["collapseKey"] = request.CollapseKey ?? string.Empty
        };

        return data;
    }

    private static string? NormalizeInternalDeepLink(string? deepLink)
    {
        if (string.IsNullOrWhiteSpace(deepLink))
        {
            return null;
        }

        var trimmed = deepLink.Trim();
        return trimmed.StartsWith("loyan://", StringComparison.OrdinalIgnoreCase)
            ? trimmed
            : null;
    }

    private static Uri? ResolveEndpoint(PushGatewayOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.Endpoint) &&
            Uri.TryCreate(options.Endpoint.Trim(), UriKind.Absolute, out var absolute))
        {
            return absolute;
        }

        if (string.IsNullOrWhiteSpace(options.BaseUrl) || string.IsNullOrWhiteSpace(options.Endpoint))
        {
            return null;
        }

        var baseUrl = options.BaseUrl.Trim();
        if (!baseUrl.EndsWith("/", StringComparison.Ordinal))
        {
            baseUrl += "/";
        }

        return Uri.TryCreate(new Uri(baseUrl), options.Endpoint.TrimStart('/'), out var combined)
            ? combined
            : null;
    }

    private static string NormalizeProviderFromPlatform(string? platform)
    {
        if (string.IsNullOrWhiteSpace(platform))
        {
            return "Unknown";
        }

        if (platform.Equals("Android", StringComparison.OrdinalIgnoreCase))
        {
            return "Fcm";
        }

        if (platform.Equals("iOS", StringComparison.OrdinalIgnoreCase) ||
            platform.Equals("MacCatalyst", StringComparison.OrdinalIgnoreCase))
        {
            return "Apns";
        }

        return platform.Trim();
    }

    private static bool IsTransientGatewayStatusCode(int statusCode)
        => statusCode == 408 || statusCode == 429 || (statusCode >= 500 && statusCode <= 599);

    private static bool IsTransientProviderFailure(string? failureCode)
        => failureCode?.Contains("RateLimited", StringComparison.OrdinalIgnoreCase) == true ||
           failureCode?.Contains("QuotaExceeded", StringComparison.OrdinalIgnoreCase) == true ||
           failureCode?.Contains("ServiceUnavailable", StringComparison.OrdinalIgnoreCase) == true ||
           failureCode?.Contains("ServerError", StringComparison.OrdinalIgnoreCase) == true ||
           failureCode?.Contains("Timeout", StringComparison.OrdinalIgnoreCase) == true ||
           failureCode?.Contains("TransportError", StringComparison.OrdinalIgnoreCase) == true;

    private static bool IsInvalidTokenFailure(string? failureCode)
        => failureCode?.Contains("TokenUnregistered", StringComparison.OrdinalIgnoreCase) == true ||
           failureCode?.Contains("InvalidArgument", StringComparison.OrdinalIgnoreCase) == true ||
           failureCode?.Contains("TokenInvalid", StringComparison.OrdinalIgnoreCase) == true ||
           failureCode?.Contains("BadDeviceToken", StringComparison.OrdinalIgnoreCase) == true;

    private static string MapGatewayFailureCode(int statusCode)
        => statusCode switch
        {
            400 => "Gateway.BadRequest",
            401 => "Gateway.Unauthorized",
            403 => "Gateway.Forbidden",
            404 => "Gateway.EndpointNotFound",
            408 => "Gateway.Timeout",
            409 => "Gateway.Conflict",
            429 => "Gateway.RateLimited",
            >= 500 and <= 599 => "Gateway.ServerError",
            _ => $"Gateway.Http{statusCode}"
        };

    private static string? TryReadProviderMessageId(string? responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(responseBody);
            var root = document.RootElement;
            return TryReadString(root, "providerMessageId")
                ?? TryReadString(root, "messageId")
                ?? TryReadString(root, "id")
                ?? TryReadString(root, "name");
        }
        catch
        {
            return null;
        }
    }

    private static string? MapProviderFailureCodeFromBody(string? responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(responseBody);
            var root = document.RootElement;
            var rawCode = TryReadString(root, "providerCode")
                ?? TryReadString(root, "providerReason")
                ?? TryReadString(root, "code")
                ?? TryReadString(root, "reason");

            if (string.IsNullOrWhiteSpace(rawCode))
            {
                return null;
            }

            var rawProvider = TryReadString(root, "provider") ?? TryReadString(root, "vendor") ?? string.Empty;
            var provider = NormalizeProviderName(rawProvider);
            var mapped = MapKnownProviderFailure(provider, rawCode);
            if (!string.IsNullOrWhiteSpace(mapped))
            {
                return mapped;
            }

            var code = NormalizeProviderCode(rawCode);
            return string.IsNullOrWhiteSpace(provider) ? $"Gateway.Provider.{code}" : $"Gateway.Provider.{provider}.{code}";
        }
        catch
        {
            return null;
        }
    }

    private static string? TryReadString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var element))
        {
            return null;
        }

        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null
        };
    }

    private static string NormalizeProviderName(string? rawProvider)
    {
        if (string.IsNullOrWhiteSpace(rawProvider))
        {
            return string.Empty;
        }

        var provider = rawProvider.Trim();
        if (provider.Equals("fcm", StringComparison.OrdinalIgnoreCase) ||
            provider.Equals("firebase", StringComparison.OrdinalIgnoreCase))
        {
            return "Fcm";
        }

        if (provider.Equals("apns", StringComparison.OrdinalIgnoreCase) ||
            provider.Equals("apple", StringComparison.OrdinalIgnoreCase))
        {
            return "Apns";
        }

        return NormalizeProviderCode(provider);
    }

    private static string? MapKnownProviderFailure(string provider, string rawCode)
    {
        var code = rawCode.Trim();
        if (provider.Equals("Fcm", StringComparison.Ordinal))
        {
            if (code.Equals("UNREGISTERED", StringComparison.OrdinalIgnoreCase) ||
                code.Equals("registration-token-not-registered", StringComparison.OrdinalIgnoreCase))
            {
                return "Gateway.Provider.Fcm.TokenUnregistered";
            }

            if (code.Equals("INVALID_ARGUMENT", StringComparison.OrdinalIgnoreCase) ||
                code.Equals("invalid-registration-token", StringComparison.OrdinalIgnoreCase))
            {
                return "Gateway.Provider.Fcm.InvalidArgument";
            }

            if (code.Equals("QUOTA_EXCEEDED", StringComparison.OrdinalIgnoreCase))
            {
                return "Gateway.Provider.Fcm.QuotaExceeded";
            }

            if (code.Equals("UNAVAILABLE", StringComparison.OrdinalIgnoreCase) ||
                code.Equals("internal", StringComparison.OrdinalIgnoreCase))
            {
                return "Gateway.Provider.Fcm.ServiceUnavailable";
            }
        }

        if (provider.Equals("Apns", StringComparison.Ordinal))
        {
            if (code.Equals("BadDeviceToken", StringComparison.OrdinalIgnoreCase) ||
                code.Equals("DeviceTokenNotForTopic", StringComparison.OrdinalIgnoreCase) ||
                code.Equals("Unregistered", StringComparison.OrdinalIgnoreCase))
            {
                return "Gateway.Provider.Apns.TokenInvalid";
            }

            if (code.Equals("TooManyRequests", StringComparison.OrdinalIgnoreCase) ||
                code.Equals("ServiceUnavailable", StringComparison.OrdinalIgnoreCase))
            {
                return "Gateway.Provider.Apns.ServiceUnavailable";
            }
        }

        return null;
    }

    private static string NormalizeProviderCode(string rawCode)
    {
        if (string.IsNullOrWhiteSpace(rawCode))
        {
            return string.Empty;
        }

        var chars = rawCode.Trim().ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (!char.IsLetterOrDigit(chars[i]) && chars[i] != '_' && chars[i] != '-')
            {
                chars[i] = '_';
            }
        }

        return new string(chars);
    }

    private sealed class PushGatewayRequest
    {
        public Guid NotificationId { get; init; }
        public Guid UserId { get; init; }
        public string DeviceId { get; init; } = string.Empty;
        public string PushToken { get; init; } = string.Empty;
        public string Platform { get; init; } = "Unknown";
        public string Provider { get; init; } = "Unknown";
        public string TargetApp { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string Body { get; init; } = string.Empty;
        public string? DeepLink { get; init; }
        public IReadOnlyDictionary<string, string> Data { get; init; } = new Dictionary<string, string>(StringComparer.Ordinal);
        public string? CollapseKey { get; init; }
        public string? AnalyticsLabel { get; init; }
        public string IdempotencyKey { get; init; } = string.Empty;
        public string? AndroidChannelId { get; init; }
        public string? ApnsTopic { get; init; }
    }
}
