using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Integration;
using Microsoft.EntityFrameworkCore;

namespace Darwin.WebApi.Services;

/// <summary>
/// Writes verified provider callbacks into the inbox with duplicate-safe semantics.
/// </summary>
public sealed class ProviderCallbackInboxWriter
{
    private readonly IAppDbContext _db;

    public ProviderCallbackInboxWriter(IAppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<bool> AddIfNewAsync(
        string provider,
        string callbackType,
        string idempotencyKey,
        string rawPayload,
        CancellationToken ct)
    {
        if (await InboxMessageExistsAsync(provider, idempotencyKey, ct).ConfigureAwait(false))
        {
            return true;
        }

        _db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = provider,
            CallbackType = callbackType,
            IdempotencyKey = idempotencyKey,
            PayloadJson = rawPayload,
            Status = "Pending"
        });

        try
        {
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return false;
        }
        catch (DbUpdateException)
        {
            if (await InboxMessageExistsAsync(provider, idempotencyKey, ct).ConfigureAwait(false))
            {
                return true;
            }

            throw;
        }
    }

    private Task<bool> InboxMessageExistsAsync(string provider, string idempotencyKey, CancellationToken ct)
    {
        return _db.Set<ProviderCallbackInboxMessage>()
            .AsNoTracking()
            .AnyAsync(x => !x.IsDeleted && x.Provider == provider && x.IdempotencyKey == idempotencyKey, ct);
    }
}
