using Darwin.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Worker;

internal static class QueueSaveResilience
{
    private const int MaxCompletionSaveAttempts = 3;

    public static async Task<bool> TrySaveClaimAsync(
        IAppDbContext db,
        ILogger logger,
        string queueName,
        Guid itemId,
        CancellationToken ct)
    {
        for (var attempt = 1; attempt <= MaxCompletionSaveAttempts; attempt++)
        {
            try
            {
                await db.SaveChangesAsync(ct).ConfigureAwait(false);
                return true;
            }
            catch (DbUpdateConcurrencyException ex) when (!ct.IsCancellationRequested)
            {
                logger.LogDebug(ex, "Skipped {QueueName} item {ItemId} because another worker instance claimed it first.", queueName, itemId);

                foreach (var entry in ex.Entries)
                {
                    entry.State = EntityState.Detached;
                }

                return false;
            }
            catch (DbUpdateException ex) when (!ct.IsCancellationRequested)
            {
                if (attempt >= MaxCompletionSaveAttempts)
                {
                    logger.LogError(
                        ex,
                        "Could not claim {QueueName} item {ItemId} after {MaxAttempts} attempts.",
                        queueName,
                        itemId,
                        MaxCompletionSaveAttempts);

                    foreach (var entry in ex.Entries)
                    {
                        entry.State = EntityState.Detached;
                    }

                    return false;
                }

                logger.LogWarning(
                    ex,
                    "Transient save failure while claiming {QueueName} item {ItemId}. Attempt {Attempt}/{MaxAttempts}.",
                    queueName,
                    itemId,
                    attempt,
                    MaxCompletionSaveAttempts);

                await Task.Delay(TimeSpan.FromMilliseconds(100 * attempt), ct).ConfigureAwait(false);
            }
        }

        return false;
    }

    public static async Task<bool> TrySaveCompletionAsync(
        IAppDbContext db,
        ILogger logger,
        string queueName,
        Guid itemId,
        CancellationToken ct)
    {
        for (var attempt = 1; attempt <= MaxCompletionSaveAttempts; attempt++)
        {
            try
            {
                await db.SaveChangesAsync(ct).ConfigureAwait(false);
                return true;
            }
            catch (DbUpdateConcurrencyException ex) when (!ct.IsCancellationRequested)
            {
                logger.LogWarning(
                    ex,
                    "Could not save {QueueName} item {ItemId} completion because the row changed concurrently after it was claimed.",
                    queueName,
                    itemId);

                foreach (var entry in ex.Entries)
                {
                    entry.State = EntityState.Detached;
                }

                return false;
            }
            catch (DbUpdateException ex) when (!ct.IsCancellationRequested)
            {
                if (attempt >= MaxCompletionSaveAttempts)
                {
                    logger.LogError(
                        ex,
                        "Could not save {QueueName} item {ItemId} completion after {MaxAttempts} attempts.",
                        queueName,
                        itemId,
                        MaxCompletionSaveAttempts);

                    foreach (var entry in ex.Entries)
                    {
                        entry.State = EntityState.Detached;
                    }

                    return false;
                }

                logger.LogWarning(
                    ex,
                    "Transient save failure while saving {QueueName} item {ItemId} completion. Attempt {Attempt}/{MaxAttempts}.",
                    queueName,
                    itemId,
                    attempt,
                    MaxCompletionSaveAttempts);

                await Task.Delay(TimeSpan.FromMilliseconds(100 * attempt), ct).ConfigureAwait(false);
            }
        }

        return false;
    }

    public static async Task<bool> TrySaveBatchAsync(
        IAppDbContext db,
        ILogger logger,
        string queueName,
        CancellationToken ct)
    {
        for (var attempt = 1; attempt <= MaxCompletionSaveAttempts; attempt++)
        {
            try
            {
                await db.SaveChangesAsync(ct).ConfigureAwait(false);
                return true;
            }
            catch (DbUpdateConcurrencyException ex) when (!ct.IsCancellationRequested)
            {
                logger.LogWarning(ex, "Could not save {QueueName} batch because one or more rows changed concurrently.", queueName);

                foreach (var entry in ex.Entries)
                {
                    entry.State = EntityState.Detached;
                }

                return false;
            }
            catch (DbUpdateException ex) when (!ct.IsCancellationRequested)
            {
                if (attempt >= MaxCompletionSaveAttempts)
                {
                    logger.LogError(
                        ex,
                        "Could not save {QueueName} batch after {MaxAttempts} attempts.",
                        queueName,
                        MaxCompletionSaveAttempts);

                    foreach (var entry in ex.Entries)
                    {
                        entry.State = EntityState.Detached;
                    }

                    return false;
                }

                logger.LogWarning(
                    ex,
                    "Transient save failure while saving {QueueName} batch. Attempt {Attempt}/{MaxAttempts}.",
                    queueName,
                    attempt,
                    MaxCompletionSaveAttempts);

                await Task.Delay(TimeSpan.FromMilliseconds(100 * attempt), ct).ConfigureAwait(false);
            }
        }

        return false;
    }
}
