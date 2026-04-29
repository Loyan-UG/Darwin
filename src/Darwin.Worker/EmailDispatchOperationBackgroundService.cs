using Darwin.Application.Abstractions.Notifications;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Integration;
using Darwin.Infrastructure.Notifications.Smtp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Darwin.Worker;

public sealed class EmailDispatchOperationBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<EmailDispatchOperationWorkerOptions> _options;
    private readonly ILogger<EmailDispatchOperationBackgroundService> _logger;

    public EmailDispatchOperationBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<EmailDispatchOperationWorkerOptions> options,
        ILogger<EmailDispatchOperationBackgroundService> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var options = Normalize(_options.Value);
            if (!options.Enabled)
            {
                await Task.Delay(TimeSpan.FromSeconds(options.PollIntervalSeconds), stoppingToken).ConfigureAwait(false);
                continue;
            }

            try
            {
                await ProcessAsync(options, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email dispatch operation iteration failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(options.PollIntervalSeconds), stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task ProcessAsync(EmailDispatchOperationWorkerOptions options, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var nowUtc = DateTime.UtcNow;
        var retryCutoffUtc = nowUtc.AddSeconds(-options.RetryCooldownSeconds);

        var items = await db.Set<EmailDispatchOperation>()
            .Where(x => !x.IsDeleted)
            .Where(x => !x.BusinessId.HasValue || db.Set<Business>().Any(b => b.Id == x.BusinessId.Value && !b.IsDeleted))
            .Where(x => x.Status == "Pending" || x.Status == "Failed")
            .Where(x => x.AttemptCount < options.MaxAttempts)
            .Where(x => !x.LastAttemptAtUtc.HasValue || x.LastAttemptAtUtc <= retryCutoffUtc)
            .OrderBy(x => x.LastAttemptAtUtc ?? x.CreatedAtUtc)
            .Take(options.BatchSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var item in items)
        {
            item.AttemptCount += 1;
            item.LastAttemptAtUtc = nowUtc;
            if (!await QueueSaveResilience.TrySaveClaimAsync(db, _logger, "email dispatch operation", item.Id, ct).ConfigureAwait(false))
            {
                continue;
            }

            try
            {
                await ProcessOneAsync(scope.ServiceProvider, item, ct).ConfigureAwait(false);
                item.Status = "Succeeded";
                item.ProcessedAtUtc = nowUtc;
                item.FailureReason = null;
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
            {
                item.Status = "Failed";
                item.FailureReason = WorkerFailureText.Truncate(ex.Message);
                _logger.LogWarning(ex, "Email dispatch operation {OperationId} failed.", item.Id);
            }

            await QueueSaveResilience.TrySaveCompletionAsync(db, _logger, "email dispatch operation", item.Id, ct).ConfigureAwait(false);
        }
    }

    private static async Task ProcessOneAsync(IServiceProvider services, EmailDispatchOperation item, CancellationToken ct)
    {
        if (!string.Equals(item.Provider, "SMTP", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Unsupported email provider '{item.Provider}'.");
        }

        var sender = services.GetRequiredService<SmtpEmailSender>();
        await sender.SendAsync(
            item.RecipientEmail,
            item.Subject,
            item.HtmlBody,
            ct,
            new EmailDispatchContext
            {
                FlowKey = item.FlowKey,
                TemplateKey = item.TemplateKey,
                CorrelationKey = item.CorrelationKey,
                BusinessId = item.BusinessId,
                IntendedRecipientEmail = item.IntendedRecipientEmail
            }).ConfigureAwait(false);
    }

    private static EmailDispatchOperationWorkerOptions Normalize(EmailDispatchOperationWorkerOptions options)
    {
        return new EmailDispatchOperationWorkerOptions
        {
            Enabled = options.Enabled,
            PollIntervalSeconds = Math.Max(5, options.PollIntervalSeconds),
            BatchSize = Math.Clamp(options.BatchSize, 1, 100),
            RetryCooldownSeconds = Math.Clamp(options.RetryCooldownSeconds, 5, 3600),
            MaxAttempts = Math.Clamp(options.MaxAttempts, 1, 25)
        };
    }
}
