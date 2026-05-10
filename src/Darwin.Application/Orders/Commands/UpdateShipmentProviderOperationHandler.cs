using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Orders.DTOs;
using Darwin.Domain.Entities.Integration;
using Darwin.Domain.Entities.Orders;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Orders.Commands
{
    public sealed class UpdateShipmentProviderOperationHandler
    {
        private const int MaxActionLength = 64;
        private const int MaxFailureReasonLength = 1024;

        private readonly IAppDbContext _db;
        private readonly IClock _clock;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public UpdateShipmentProviderOperationHandler(
            IAppDbContext db,
            IClock clock,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task<Result> HandleAsync(UpdateShipmentProviderOperationDto dto, CancellationToken ct = default)
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

            var operation = await _db.Set<ShipmentProviderOperation>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct)
                .ConfigureAwait(false);

            if (operation is null)
            {
                return Result.Fail(_localizer["ShipmentProviderOperationNotFound"]);
            }

            var currentRowVersion = operation.RowVersion ?? Array.Empty<byte>();
            if (!currentRowVersion.SequenceEqual(rowVersion))
            {
                return Result.Fail(_localizer["ItemConcurrencyConflict"]);
            }

            var nowUtc = _clock.UtcNow;
            if (!TryApplyAction(operation, dto, nowUtc))
            {
                return Result.Fail(_localizer["ShipmentProviderOperationUnsupportedAction"]);
            }

            var requeueRequested = IsRequeueAction(dto.Action);
            if (requeueRequested && await HasPendingSiblingAsync(operation, ct).ConfigureAwait(false))
            {
                return Result.Fail(_localizer["ShipmentProviderOperationPendingAlreadyExists"]);
            }

            if (requeueRequested)
            {
                await MarkShipmentProviderOperationRequeuedAsync(operation, nowUtc, ct).ConfigureAwait(false);
            }

            try
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Fail(_localizer["ItemConcurrencyConflict"]);
            }
            catch (DbUpdateException)
            {
                if (requeueRequested && await HasPendingSiblingAsync(operation, ct).ConfigureAwait(false))
                {
                    return Result.Fail(_localizer["ShipmentProviderOperationPendingAlreadyExists"]);
                }

                throw;
            }

            return Result.Ok();
        }

        private Task<bool> HasPendingSiblingAsync(ShipmentProviderOperation operation, CancellationToken ct)
        {
            return _db.Set<ShipmentProviderOperation>()
                .AsNoTracking()
                .AnyAsync(
                    x => x.Id != operation.Id &&
                         x.ShipmentId == operation.ShipmentId &&
                         !x.IsDeleted &&
                         x.Provider == operation.Provider &&
                         x.OperationType == operation.OperationType &&
                         x.Status == "Pending",
                    ct);
        }

        private async Task MarkShipmentProviderOperationRequeuedAsync(
            ShipmentProviderOperation operation,
            DateTime nowUtc,
            CancellationToken ct)
        {
            if (!string.Equals(operation.Provider, "DHL", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var eventKey = ResolveDhlRequeuedEventKey(operation.OperationType);
            if (eventKey is null)
            {
                return;
            }

            var shipment = await _db.Set<Shipment>()
                .FirstOrDefaultAsync(x => x.Id == operation.ShipmentId && !x.IsDeleted, ct)
                .ConfigureAwait(false);

            if (shipment is null)
            {
                return;
            }

            shipment.LastCarrierEventKey = eventKey;
            await ShipmentCarrierEventRecorder.AddIfMissingAsync(
                _db,
                shipment,
                eventKey,
                nowUtc,
                "Requeued",
                ct: ct).ConfigureAwait(false);
        }

        private static bool TryApplyAction(ShipmentProviderOperation operation, UpdateShipmentProviderOperationDto dto, DateTime now)
        {
            switch (NormalizeAction(dto.Action))
            {
                case "MARKPROCESSED":
                    operation.Status = "Processed";
                    operation.ProcessedAtUtc = now;
                    operation.FailureReason = null;
                    break;
                case "MARKFAILED":
                    operation.Status = "Failed";
                    operation.LastAttemptAtUtc = now;
                    operation.ProcessedAtUtc = null;
                    var failedReason = Truncate(dto.FailureReason?.Trim(), MaxFailureReasonLength);
                    operation.FailureReason = string.IsNullOrWhiteSpace(failedReason)
                        ? "Marked failed by WebAdmin operator."
                        : failedReason;
                    break;
                case "REQUEUE":
                    operation.IsDeleted = false;
                    operation.Status = "Pending";
                    operation.AttemptCount = 0;
                    operation.LastAttemptAtUtc = null;
                    operation.ProcessedAtUtc = null;
                    operation.FailureReason = null;
                    break;
                case "CANCEL":
                    operation.IsDeleted = true;
                    operation.Status = "Cancelled";
                    operation.LastAttemptAtUtc = now;
                    operation.ProcessedAtUtc = null;
                    var cancelledReason = Truncate(dto.FailureReason?.Trim(), MaxFailureReasonLength);
                    operation.FailureReason = string.IsNullOrWhiteSpace(cancelledReason)
                        ? "Cancelled by WebAdmin operator."
                        : cancelledReason;
                    break;
                default:
                    return false;
            }

            return true;
        }

        private static bool IsRequeueAction(string? action)
        {
            return string.Equals((action ?? string.Empty).Trim(), "Requeue", StringComparison.OrdinalIgnoreCase);
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

        private static string? ResolveDhlRequeuedEventKey(string operationType)
        {
            return operationType switch
            {
                "CreateShipment" => "shipment.provider_create_queued",
                "GenerateLabel" => "shipment.label_queued",
                "CreateReturnShipment" => "return.provider_create_queued",
                _ => null
            };
        }

        private static string? Truncate(string? value, int maxLength)
        {
            return value is null || value.Length <= maxLength
                ? value
                : value[..maxLength];
        }
    }
}
