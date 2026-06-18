using System.Globalization;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Domain.Entities.Foundation;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Foundation;

public sealed class NumberSequenceService
{
    public const string GlobalScopeKey = "GLOBAL";
    private const int MaxReservationRetries = 3;

    private readonly IAppDbContext _db;
    private readonly IClock _clock;

    public NumberSequenceService(IAppDbContext db, IClock clock)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async Task<Result<Guid>> CreateSequenceAsync(CreateNumberSequenceCommand command, CancellationToken ct = default)
    {
        var scopeKey = NormalizeScopeKey(command.ScopeKey);
        var pattern = FoundationInputNormalizer.Required(command.PrefixPattern);
        var validation = ValidateSequenceInput(command.DocumentType, scopeKey, pattern, command.NextValue, command.PaddingLength);
        if (!validation.Succeeded)
        {
            return Result<Guid>.Fail(validation.Error!);
        }

        if (FoundationInputNormalizer.LooksSensitive(command.Description) ||
            FoundationInputNormalizer.LooksSensitive(command.MetadataJson))
        {
            return Result<Guid>.Fail("Sensitive secrets must not be stored in number sequence metadata.");
        }

        var duplicate = await _db.Set<NumberSequence>()
            .AnyAsync(x =>
                x.BusinessId == command.BusinessId &&
                x.DocumentType == command.DocumentType &&
                x.ScopeKey == scopeKey &&
                !x.IsDeleted,
                ct)
            .ConfigureAwait(false);
        if (duplicate)
        {
            return Result<Guid>.Fail("Number sequence already exists.");
        }

        var nowUtc = _clock.UtcNow;
        var sequence = new NumberSequence
        {
            BusinessId = command.BusinessId,
            DocumentType = command.DocumentType,
            ScopeKey = scopeKey!,
            PrefixPattern = pattern!,
            NextValue = command.NextValue,
            PaddingLength = command.PaddingLength,
            ResetPolicy = command.ResetPolicy,
            CurrentPeriodKey = ResolvePeriodKey(command.ResetPolicy, nowUtc),
            IsActive = command.IsActive,
            Description = FoundationInputNormalizer.Optional(command.Description),
            MetadataJson = FoundationInputNormalizer.Json(command.MetadataJson)
        };

        _db.Set<NumberSequence>().Add(sequence);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result<Guid>.Ok(sequence.Id);
    }

    public async Task<Result<string>> PreviewAsync(NumberSequenceRequest request, CancellationToken ct = default)
    {
        var sequence = await FindActiveSequenceAsync(request, tracking: false, ct).ConfigureAwait(false);
        if (sequence is null)
        {
            return Result<string>.Fail("Active number sequence was not found.");
        }

        var nowUtc = _clock.UtcNow;
        var periodKey = ResolvePeriodKey(sequence.ResetPolicy, nowUtc);
        var nextValue = ShouldReset(sequence, periodKey) ? 1 : sequence.NextValue;
        return Result<string>.Ok(Format(sequence.PrefixPattern, nextValue, sequence.PaddingLength, nowUtc));
    }

    public async Task<Result<string>> ReserveNextAsync(NumberSequenceRequest request, CancellationToken ct = default)
    {
        for (var attempt = 0; attempt < MaxReservationRetries; attempt++)
        {
            var sequence = await FindActiveSequenceAsync(request, tracking: true, ct).ConfigureAwait(false);
            if (sequence is null)
            {
                return Result<string>.Fail("Active number sequence was not found.");
            }

            var nowUtc = _clock.UtcNow;
            var periodKey = ResolvePeriodKey(sequence.ResetPolicy, nowUtc);
            if (ShouldReset(sequence, periodKey))
            {
                sequence.CurrentPeriodKey = periodKey;
                sequence.NextValue = 1;
            }

            var reserved = sequence.NextValue;
            sequence.NextValue++;
            var formatted = Format(sequence.PrefixPattern, reserved, sequence.PaddingLength, nowUtc);

            try
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
                return Result<string>.Ok(formatted);
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxReservationRetries - 1)
            {
                continue;
            }
        }

        return Result<string>.Fail("Number sequence reservation failed due to concurrent updates.");
    }

    public static string Format(string pattern, long sequenceValue, int paddingLength, DateTime nowUtc)
    {
        var seq = sequenceValue.ToString(CultureInfo.InvariantCulture).PadLeft(paddingLength, '0');
        return pattern
            .Replace("{yyyy}", nowUtc.ToString("yyyy", CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{yy}", nowUtc.ToString("yy", CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{MM}", nowUtc.ToString("MM", CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{dd}", nowUtc.ToString("dd", CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{seq}", seq, StringComparison.Ordinal);
    }

    private async Task<NumberSequence?> FindActiveSequenceAsync(NumberSequenceRequest request, bool tracking, CancellationToken ct)
    {
        var scopeKey = NormalizeScopeKey(request.ScopeKey);
        if (scopeKey is null)
        {
            return null;
        }

        var query = _db.Set<NumberSequence>()
            .Where(x =>
                x.BusinessId == request.BusinessId &&
                x.DocumentType == request.DocumentType &&
                x.ScopeKey == scopeKey &&
                x.IsActive &&
                !x.IsDeleted);

        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(ct).ConfigureAwait(false);
    }

    private static Result ValidateSequenceInput(
        NumberSequenceDocumentType documentType,
        string? scopeKey,
        string? pattern,
        long nextValue,
        int paddingLength)
    {
        if (!Enum.IsDefined(typeof(NumberSequenceDocumentType), documentType))
        {
            return Result.Fail("Document type is required.");
        }

        if (scopeKey is null)
        {
            return Result.Fail("Scope key is required.");
        }

        if (pattern is null)
        {
            return Result.Fail("Prefix pattern is required.");
        }

        if (!pattern.Contains("{seq}", StringComparison.Ordinal))
        {
            return Result.Fail("Prefix pattern must contain {seq}.");
        }

        if (nextValue < 1)
        {
            return Result.Fail("Next value must be greater than zero.");
        }

        if (paddingLength is < 1 or > 12)
        {
            return Result.Fail("Padding length must be between 1 and 12.");
        }

        return Result.Ok();
    }

    private static string? NormalizeScopeKey(string? value)
        => FoundationInputNormalizer.Key(value)?.ToUpperInvariant();

    private static string? ResolvePeriodKey(NumberSequenceResetPolicy resetPolicy, DateTime nowUtc)
        => resetPolicy switch
        {
            NumberSequenceResetPolicy.Never => null,
            NumberSequenceResetPolicy.Daily => nowUtc.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
            NumberSequenceResetPolicy.Monthly => nowUtc.ToString("yyyyMM", CultureInfo.InvariantCulture),
            NumberSequenceResetPolicy.Yearly => nowUtc.ToString("yyyy", CultureInfo.InvariantCulture),
            _ => null
        };

    private static bool ShouldReset(NumberSequence sequence, string? periodKey)
        => sequence.ResetPolicy != NumberSequenceResetPolicy.Never &&
           !string.Equals(sequence.CurrentPeriodKey, periodKey, StringComparison.Ordinal);
}

public sealed record CreateNumberSequenceCommand(
    Guid? BusinessId,
    NumberSequenceDocumentType DocumentType,
    string? ScopeKey,
    string? PrefixPattern,
    long NextValue,
    int PaddingLength,
    NumberSequenceResetPolicy ResetPolicy,
    bool IsActive = true,
    string? Description = null,
    string? MetadataJson = null);

public sealed record NumberSequenceRequest(
    Guid? BusinessId,
    NumberSequenceDocumentType DocumentType,
    string? ScopeKey);
