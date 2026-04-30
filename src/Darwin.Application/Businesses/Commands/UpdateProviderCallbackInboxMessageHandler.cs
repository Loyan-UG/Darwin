using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Businesses.DTOs;
using Darwin.Domain.Entities.Integration;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Businesses.Commands
{
    public sealed class UpdateProviderCallbackInboxMessageHandler
    {
        private const int MaxActionLength = 64;
        private const int MaxFailureReasonLength = 1024;

        private readonly IAppDbContext _db;
        private readonly IClock _clock;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public UpdateProviderCallbackInboxMessageHandler(
            IAppDbContext db,
            IClock clock,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task<Result> HandleAsync(UpdateProviderCallbackInboxMessageDto dto, CancellationToken ct = default)
        {
            if (dto.Id == Guid.Empty)
            {
                return Result.Fail(_localizer["InvalidDeleteRequest"]);
            }

            var rowVersion = dto.RowVersion ?? Array.Empty<byte>();
            if (rowVersion.Length == 0)
            {
                return Result.Fail(_localizer["RowVersionRequired"]);
            }

            var message = await _db.Set<ProviderCallbackInboxMessage>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct)
                .ConfigureAwait(false);

            if (message is null)
            {
                return Result.Fail(_localizer["ProviderCallbackInboxMessageNotFound"]);
            }

            var currentVersion = message.RowVersion ?? Array.Empty<byte>();
            if (!currentVersion.SequenceEqual(rowVersion))
            {
                return Result.Fail(_localizer["ItemConcurrencyConflict"]);
            }

            if (!TryApplyAction(message, dto, _clock.UtcNow))
            {
                return Result.Fail(_localizer["ProviderCallbackInboxUnsupportedAction"]);
            }

            try
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Fail(_localizer["ItemConcurrencyConflict"]);
            }

            return Result.Ok();
        }

        private static bool TryApplyAction(ProviderCallbackInboxMessage message, UpdateProviderCallbackInboxMessageDto dto, DateTime now)
        {
            switch (NormalizeAction(dto.Action))
            {
                case "MARKPROCESSED":
                    message.Status = "Processed";
                    message.ProcessedAtUtc = now;
                    message.FailureReason = null;
                    break;
                case "MARKFAILED":
                    message.Status = "Failed";
                    message.LastAttemptAtUtc = now;
                    message.ProcessedAtUtc = null;
                    var failureReason = Truncate(dto.FailureReason?.Trim(), MaxFailureReasonLength);
                    message.FailureReason = string.IsNullOrWhiteSpace(failureReason)
                        ? "Marked failed by WebAdmin operator."
                        : failureReason;
                    break;
                case "REQUEUE":
                    message.Status = "Pending";
                    message.AttemptCount = 0;
                    message.LastAttemptAtUtc = null;
                    message.ProcessedAtUtc = null;
                    message.FailureReason = null;
                    break;
                default:
                    return false;
            }

            return true;
        }

        private static string NormalizeAction(string? action)
        {
            if (string.IsNullOrWhiteSpace(action))
            {
                return string.Empty;
            }

            var trimmed = action.Trim();
            return trimmed.Length > MaxActionLength
                ? string.Empty
                : trimmed.ToUpperInvariant();
        }

        private static string? Truncate(string? value, int maxLength)
        {
            return value is null || value.Length <= maxLength
                ? value
                : value[..maxLength];
        }
    }
}
