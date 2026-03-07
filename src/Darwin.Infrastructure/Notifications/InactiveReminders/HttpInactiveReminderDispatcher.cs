using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Notifications;
using Darwin.Shared.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Darwin.Infrastructure.Notifications.InactiveReminders;

/// <summary>
/// Dispatches inactive reminders through a configurable HTTP push gateway endpoint.
/// </summary>
public sealed class HttpInactiveReminderDispatcher : IInactiveReminderDispatcher
{
    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<InactiveReminderPushGatewayOptions> _optionsMonitor;
    private readonly ILogger<HttpInactiveReminderDispatcher> _logger;

    public HttpInactiveReminderDispatcher(
        HttpClient httpClient,
        IOptionsMonitor<InactiveReminderPushGatewayOptions> optionsMonitor,
        ILogger<HttpInactiveReminderDispatcher> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Result> DispatchAsync(
        Guid userId,
        string destinationDeviceId,
        string pushToken,
        string platform,
        int inactiveDays,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(destinationDeviceId))
        {
            return Result.Fail("Destination device id is required.");
        }

        if (string.IsNullOrWhiteSpace(pushToken))
        {
            return Result.Fail("Push token is required.");
        }

        var options = _optionsMonitor.CurrentValue;
        if (!options.Enabled)
        {
            return Result.Fail("Inactive reminder gateway dispatch is disabled.");
        }

        if (string.IsNullOrWhiteSpace(options.Endpoint))
        {
            return Result.Fail("Inactive reminder gateway endpoint is not configured.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, options.Endpoint)
        {
            Content = JsonContent.Create(new InactiveReminderPushGatewayRequest
            {
                UserId = userId,
                DeviceId = destinationDeviceId,
                PushToken = pushToken,
                Platform = string.IsNullOrWhiteSpace(platform) ? "Unknown" : platform,
                InactiveDays = Math.Max(0, inactiveDays),
                Title = ApplyTemplate(options.TitleTemplate, inactiveDays),
                Body = ApplyTemplate(options.BodyTemplate, inactiveDays)
            })
        };

        if (!string.IsNullOrWhiteSpace(options.BearerToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.BearerToken.Trim());
        }

        try
        {
            using var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return Result.Ok();
            }

            var responseBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogWarning(
                "Inactive reminder gateway rejected dispatch. StatusCode={StatusCode}, UserId={UserId}, Body={Body}",
                (int)response.StatusCode,
                userId,
                responseBody);

            return Result.Fail($"Inactive reminder gateway rejected dispatch with status {(int)response.StatusCode}.");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Inactive reminder gateway dispatch failed for UserId={UserId}.", userId);
            return Result.Fail("Inactive reminder gateway request failed.");
        }
    }

    /// <summary>
    /// Applies simple placeholder replacement in templates.
    /// </summary>
    private static string ApplyTemplate(string template, int inactiveDays)
    {
        var safeTemplate = string.IsNullOrWhiteSpace(template)
            ? string.Empty
            : template;

        return safeTemplate.Replace("{inactiveDays}", Math.Max(0, inactiveDays).ToString(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Outbound gateway payload for one push dispatch request.
    /// </summary>
    private sealed class InactiveReminderPushGatewayRequest
    {
        public Guid UserId { get; init; }
        public string DeviceId { get; init; } = string.Empty;
        public string PushToken { get; init; } = string.Empty;
        public string Platform { get; init; } = "Unknown";
        public int InactiveDays { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Body { get; init; } = string.Empty;
    }
}
