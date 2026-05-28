using Darwin.Application.CRM.Commands;
using Darwin.Application.Abstractions.Notifications;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text;

namespace Darwin.Worker;

public sealed class VatValidationRetryBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<VatValidationRetryWorkerOptions> _optionsMonitor;
    private readonly ILogger<VatValidationRetryBackgroundService> _logger;

    public VatValidationRetryBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<VatValidationRetryWorkerOptions> optionsMonitor,
        ILogger<VatValidationRetryBackgroundService> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var loggedDisabled = false;

        while (!stoppingToken.IsCancellationRequested)
        {
            var options = Normalize(_optionsMonitor.CurrentValue);
            if (!options.Enabled)
            {
                if (!loggedDisabled)
                {
                    _logger.LogInformation("VAT validation retry worker is disabled.");
                    loggedDisabled = true;
                }

                await Task.Delay(TimeSpan.FromMinutes(options.PollIntervalMinutes), stoppingToken).ConfigureAwait(false);
                continue;
            }

            if (loggedDisabled)
            {
                _logger.LogInformation("VAT validation retry worker enabled.");
                loggedDisabled = false;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<RetryUnknownCustomerVatValidationBatchHandler>();
                var result = await handler.HandleAsync(options.BatchSize, options.MinRetryAgeMinutes, stoppingToken).ConfigureAwait(false);

                if (result.RetriedCount > 0)
                {
                    _logger.LogInformation(
                        "VAT validation retry processed {RetriedCount} customers: {ValidCount} valid, {InvalidCount} invalid, {UnknownCount} still unknown.",
                        result.RetriedCount,
                        result.ValidCount,
                        result.InvalidCount,
                        result.UnknownCount);
                }

                if (result.UnknownCount >= options.CriticalUnknownCountAlertThreshold)
                {
                    await SendCriticalUnknownAlertAsync(scope.ServiceProvider, result, options, stoppingToken)
                        .ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "VAT validation retry iteration failed.");
            }

            await Task.Delay(TimeSpan.FromMinutes(options.PollIntervalMinutes), stoppingToken).ConfigureAwait(false);
        }
    }

    private static VatValidationRetryWorkerOptions Normalize(VatValidationRetryWorkerOptions options)
        => new()
        {
            Enabled = options.Enabled,
            PollIntervalMinutes = Math.Clamp(options.PollIntervalMinutes, 15, 10080),
            BatchSize = Math.Clamp(options.BatchSize, 1, 100),
            MinRetryAgeMinutes = Math.Clamp(options.MinRetryAgeMinutes, 1, 10080),
            CriticalUnknownCountAlertThreshold = Math.Clamp(options.CriticalUnknownCountAlertThreshold, 1, 1000),
            CriticalAlertCooldownHours = Math.Clamp(options.CriticalAlertCooldownHours, 1, 168)
        };

    private async Task SendCriticalUnknownAlertAsync(
        IServiceProvider services,
        RetryUnknownCustomerVatValidationBatchResult result,
        VatValidationRetryWorkerOptions options,
        CancellationToken ct)
    {
        try
        {
            var db = services.GetRequiredService<IAppDbContext>();
            var settings = await db.Set<SiteSetting>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => !x.IsDeleted, ct)
                .ConfigureAwait(false);
            var recipient = ResolveAdminAlertRecipient(settings);
            if (string.IsNullOrWhiteSpace(recipient))
            {
                _logger.LogWarning(
                    "VAT validation retry left {UnknownCount} customers unknown, but no admin alert recipient is configured.",
                    result.UnknownCount);
                return;
            }

            var sender = services.GetRequiredService<IEmailSender>();
            var cooldownBucket = (DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 3600) / options.CriticalAlertCooldownHours;
            var correlationKey = $"vat-validation-retry-critical-{cooldownBucket}";
            var subject = "Darwin VAT validation retry requires attention";
            var htmlBody = BuildCriticalUnknownAlertHtml(result, options);
            await sender.SendAsync(
                    recipient,
                    subject,
                    htmlBody,
                    ct,
                    new EmailDispatchContext
                    {
                        FlowKey = "VatValidationRetryCriticalAlert",
                        TemplateKey = "VatValidationRetryCriticalAlertDefault",
                        SenderRole = EmailSenderRole.Admin,
                        CorrelationKey = correlationKey,
                        IntendedRecipientEmail = recipient
                    })
                .ConfigureAwait(false);

            _logger.LogWarning(
                "VAT validation retry alert queued for {UnknownCount} unknown customers after {RetriedCount} retries.",
                result.UnknownCount,
                result.RetriedCount);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VAT validation retry critical alert failed.");
        }
    }

    private static string? ResolveAdminAlertRecipient(SiteSetting? settings)
    {
        if (!string.IsNullOrWhiteSpace(settings?.SystemAdminEmail))
        {
            return settings.SystemAdminEmail.Trim();
        }

        return settings?.AdminAlertEmailsCsv?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();
    }

    private static string BuildCriticalUnknownAlertHtml(
        RetryUnknownCustomerVatValidationBatchResult result,
        VatValidationRetryWorkerOptions options)
    {
        var builder = new StringBuilder();
        builder.Append("<h1>VAT validation retry requires attention</h1>");
        builder.Append("<p>The scheduled VIES retry worker completed a batch, but too many VAT IDs still require manual review.</p>");
        builder.Append("<ul>");
        builder.Append(CultureInvariantListItem("Retried customers", result.RetriedCount));
        builder.Append(CultureInvariantListItem("Confirmed valid", result.ValidCount));
        builder.Append(CultureInvariantListItem("Confirmed invalid", result.InvalidCount));
        builder.Append(CultureInvariantListItem("Still unknown", result.UnknownCount));
        builder.Append(CultureInvariantListItem("Alert threshold", options.CriticalUnknownCountAlertThreshold));
        builder.Append("</ul>");
        builder.Append("<p>Open WebAdmin Billing / Tax Compliance and review the VAT validation queue. Provider failures must remain Unknown until an operator verifies official evidence.</p>");
        return builder.ToString();
    }

    private static string CultureInvariantListItem(string label, int value)
    {
        return $"<li><strong>{WebUtility.HtmlEncode(label)}:</strong> {value}</li>";
    }
}
