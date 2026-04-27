using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Orders.DTOs;
using Darwin.Domain.Entities.Integration;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Orders.Commands
{
    public sealed class UpdateShipmentProviderOperationHandler
    {
        private readonly IAppDbContext _db;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public UpdateShipmentProviderOperationHandler(
            IAppDbContext db,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task<Result> HandleAsync(UpdateShipmentProviderOperationDto dto, CancellationToken ct = default)
        {
            if (dto.Id == Guid.Empty || dto.RowVersion.Length == 0)
            {
                return Result.Fail(_localizer["InvalidDeleteRequest"]);
            }

            var operation = await _db.Set<ShipmentProviderOperation>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct)
                .ConfigureAwait(false);

            if (operation is null)
            {
                return Result.Fail(_localizer["ShipmentProviderOperationNotFound"]);
            }

            if (!operation.RowVersion.SequenceEqual(dto.RowVersion))
            {
                return Result.Fail(_localizer["ItemConcurrencyConflict"]);
            }

            if (!TryApplyAction(operation, dto))
            {
                return Result.Fail(_localizer["ShipmentProviderOperationUnsupportedAction"]);
            }

            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return Result.Ok();
        }

        private static bool TryApplyAction(ShipmentProviderOperation operation, UpdateShipmentProviderOperationDto dto)
        {
            var now = DateTime.UtcNow;
            switch (dto.Action)
            {
                case "MarkProcessed":
                    operation.Status = "Processed";
                    operation.ProcessedAtUtc = now;
                    operation.FailureReason = null;
                    break;
                case "MarkFailed":
                    operation.Status = "Failed";
                    operation.LastAttemptAtUtc = now;
                    operation.FailureReason = string.IsNullOrWhiteSpace(dto.FailureReason)
                        ? "Marked failed by WebAdmin operator."
                        : dto.FailureReason.Trim();
                    break;
                case "Requeue":
                    operation.IsDeleted = false;
                    operation.Status = "Pending";
                    operation.AttemptCount = 0;
                    operation.LastAttemptAtUtc = null;
                    operation.ProcessedAtUtc = null;
                    operation.FailureReason = null;
                    break;
                case "Cancel":
                    operation.IsDeleted = true;
                    operation.Status = "Cancelled";
                    operation.LastAttemptAtUtc = now;
                    operation.FailureReason = string.IsNullOrWhiteSpace(dto.FailureReason)
                        ? "Cancelled by WebAdmin operator."
                        : dto.FailureReason.Trim();
                    break;
                default:
                    return false;
            }

            return true;
        }
    }
}
