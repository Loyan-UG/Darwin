using Darwin.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Darwin.Infrastructure.Notifications;

internal static class NotificationAuditSaveResilience
{
    private const int MaxAttempts = 3;

    public static async Task SaveAsync(
        IAppDbContext db,
        ILogger logger,
        string auditName,
        CancellationToken ct)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                await db.SaveChangesAsync(ct).ConfigureAwait(false);
                return;
            }
            catch (DbUpdateException ex) when (attempt < MaxAttempts && !ct.IsCancellationRequested)
            {
                logger.LogWarning(
                    ex,
                    "Transient save failure while persisting {AuditName}. Attempt {Attempt}/{MaxAttempts}.",
                    auditName,
                    attempt,
                    MaxAttempts);

                await Task.Delay(TimeSpan.FromMilliseconds(100 * attempt), ct).ConfigureAwait(false);
            }
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
