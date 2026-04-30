using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Billing.DTOs;
using Darwin.Domain.Entities.Integration;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Billing.Commands
{
    public sealed class UpdateBillingWebhookDeliveryHandler
    {
        private const int MaxActionLength = 64;

        private readonly IAppDbContext _db;
        private readonly IClock _clock;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public UpdateBillingWebhookDeliveryHandler(
            IAppDbContext db,
            IClock clock,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task<Result> HandleAsync(UpdateBillingWebhookDeliveryDto dto, CancellationToken ct = default)
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

            var delivery = await _db.Set<WebhookDelivery>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct)
                .ConfigureAwait(false);

            if (delivery is null)
            {
                return Result.Fail(_localizer["WebhookDeliveryNotFound"]);
            }

            var currentVersion = delivery.RowVersion ?? Array.Empty<byte>();
            if (!currentVersion.SequenceEqual(rowVersion))
            {
                return Result.Fail(_localizer["ItemConcurrencyConflict"]);
            }

            if (!TryApplyAction(delivery, dto.Action, _clock.UtcNow))
            {
                return Result.Fail(_localizer["WebhookDeliveryUnsupportedAction"]);
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

        private static bool TryApplyAction(WebhookDelivery delivery, string action, DateTime now)
        {
            switch (NormalizeAction(action))
            {
                case "MARKSUCCEEDED":
                    delivery.IsDeleted = false;
                    delivery.Status = "Succeeded";
                    delivery.ResponseCode ??= 200;
                    delivery.LastAttemptAtUtc = now;
                    break;
                case "REQUEUE":
                    delivery.IsDeleted = false;
                    delivery.Status = "Pending";
                    delivery.RetryCount = 0;
                    delivery.ResponseCode = null;
                    delivery.LastAttemptAtUtc = null;
                    break;
                case "SUPPRESS":
                    delivery.Status = "Suppressed";
                    delivery.IsDeleted = true;
                    delivery.LastAttemptAtUtc = now;
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
    }
}
