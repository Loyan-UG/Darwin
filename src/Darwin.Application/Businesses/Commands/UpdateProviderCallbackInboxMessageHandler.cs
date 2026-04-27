using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Businesses.DTOs;
using Darwin.Domain.Entities.Integration;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Businesses.Commands
{
    public sealed class UpdateProviderCallbackInboxMessageHandler
    {
        private readonly IAppDbContext _db;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public UpdateProviderCallbackInboxMessageHandler(
            IAppDbContext db,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task<Result> HandleAsync(UpdateProviderCallbackInboxMessageDto dto, CancellationToken ct = default)
        {
            if (dto.Id == Guid.Empty || dto.RowVersion.Length == 0)
            {
                return Result.Fail(_localizer["InvalidDeleteRequest"]);
            }

            var message = await _db.Set<ProviderCallbackInboxMessage>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct)
                .ConfigureAwait(false);

            if (message is null)
            {
                return Result.Fail(_localizer["ProviderCallbackInboxMessageNotFound"]);
            }

            if (!message.RowVersion.SequenceEqual(dto.RowVersion))
            {
                return Result.Fail(_localizer["ItemConcurrencyConflict"]);
            }

            if (!TryApplyAction(message, dto))
            {
                return Result.Fail(_localizer["ProviderCallbackInboxUnsupportedAction"]);
            }

            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return Result.Ok();
        }

        private static bool TryApplyAction(ProviderCallbackInboxMessage message, UpdateProviderCallbackInboxMessageDto dto)
        {
            var now = DateTime.UtcNow;
            switch (dto.Action)
            {
                case "MarkProcessed":
                    message.Status = "Processed";
                    message.ProcessedAtUtc = now;
                    message.FailureReason = null;
                    break;
                case "MarkFailed":
                    message.Status = "Failed";
                    message.LastAttemptAtUtc = now;
                    message.FailureReason = string.IsNullOrWhiteSpace(dto.FailureReason)
                        ? "Marked failed by WebAdmin operator."
                        : dto.FailureReason.Trim();
                    break;
                case "Requeue":
                    message.Status = "Pending";
                    message.ProcessedAtUtc = null;
                    message.FailureReason = null;
                    break;
                default:
                    return false;
            }

            return true;
        }
    }
}
