using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Domain.Entities.Integration;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Entities.Settings;
using Darwin.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Orders.Commands;

/// <summary>
/// Queues a DHL return-shipment operation without overwriting the original outbound shipment.
/// </summary>
public sealed class QueueDhlReturnShipmentHandler
{
    private const string Provider = "DHL";
    private const string OperationType = "CreateReturnShipment";
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly IStringLocalizer<ValidationResource> _localizer;

    public QueueDhlReturnShipmentHandler(
        IAppDbContext db,
        IStringLocalizer<ValidationResource> localizer,
        IClock? clock = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        _clock = clock ?? DefaultHandlerDependencies.DefaultClock;
    }

    public async Task<Guid> HandleAsync(Guid outboundShipmentId, byte[] rowVersion, CancellationToken ct = default)
    {
        if (outboundShipmentId == Guid.Empty)
        {
            throw new InvalidOperationException(_localizer["ShipmentNotFoundForLabelGeneration"]);
        }

        if (rowVersion.Length == 0)
        {
            throw new DbUpdateConcurrencyException(_localizer["ItemConcurrencyConflict"]);
        }

        var outbound = await _db.Set<Shipment>()
            .Include(x => x.Lines.Where(line => !line.IsDeleted))
            .FirstOrDefaultAsync(x => x.Id == outboundShipmentId && !x.IsDeleted, ct)
            .ConfigureAwait(false);

        if (outbound is null)
        {
            throw new InvalidOperationException(_localizer["ShipmentNotFoundForLabelGeneration"]);
        }

        if (!DhlShipmentPhaseOneMetadata.IsDhlCarrier(outbound.Carrier))
        {
            throw new ValidationException(_localizer["ShipmentCarrierMustBeDhlForLabelGeneration"]);
        }

        if (!outbound.RowVersion.SequenceEqual(rowVersion))
        {
            throw new DbUpdateConcurrencyException(_localizer["ItemConcurrencyConflict"]);
        }

        var settings = await _db.Set<SiteSetting>()
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (settings is null || !settings.DhlEnabled)
        {
            throw new InvalidOperationException(_localizer["DhlLabelGenerationNotEnabled"]);
        }

        if (!DhlShipmentPhaseOneMetadata.HasLabelGenerationReadiness(settings))
        {
            throw new InvalidOperationException(_localizer["DhlLabelGenerationNotConfigured"]);
        }

        var returnEventKeys = new[]
        {
            "return.provider_create_queued",
            "return.provider_create_failed",
            "return.label_created"
        };

        var existingReturnShipment = await _db.Set<Shipment>()
            .Where(x => x.OrderId == outbound.OrderId &&
                        !x.IsDeleted &&
                        x.Carrier == Provider &&
                        x.LastCarrierEventKey != null &&
                        returnEventKeys.Contains(x.LastCarrierEventKey))
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (existingReturnShipment is not null)
        {
            if (!string.IsNullOrWhiteSpace(existingReturnShipment.LabelUrl))
            {
                return existingReturnShipment.Id;
            }

            await EnsureReturnOperationQueuedAsync(existingReturnShipment.Id, ct).ConfigureAwait(false);
            return existingReturnShipment.Id;
        }

        var nowUtc = _clock.UtcNow;
        var returnShipment = new Shipment
        {
            OrderId = outbound.OrderId,
            MethodId = outbound.MethodId,
            Status = ShipmentStatus.Pending,
            Carrier = Provider,
            Service = outbound.Service,
            TotalWeight = outbound.TotalWeight,
            LastCarrierEventKey = "return.provider_create_queued",
            CreatedAtUtc = nowUtc,
            Lines = outbound.Lines
                .Where(x => !x.IsDeleted && x.Quantity > 0)
                .Select(x => new ShipmentLine
                {
                    OrderLineId = x.OrderLineId,
                    Quantity = x.Quantity
                })
                .ToList()
        };

        if (returnShipment.Lines.Count == 0)
        {
            throw new ValidationException(_localizer["DhlReturnShipmentRequiresLines"]);
        }

        _db.Set<Shipment>().Add(returnShipment);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        _db.Set<ShipmentProviderOperation>().Add(new ShipmentProviderOperation
        {
            ShipmentId = returnShipment.Id,
            Provider = Provider,
            OperationType = OperationType,
            Status = "Pending"
        });

        await ShipmentCarrierEventRecorder.AddIfMissingAsync(
            _db,
            returnShipment,
            "return.provider_create_queued",
            nowUtc,
            "ReturnQueued",
            ct: ct).ConfigureAwait(false);

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return returnShipment.Id;
    }

    private async Task EnsureReturnOperationQueuedAsync(Guid returnShipmentId, CancellationToken ct)
    {
        var pendingOperation = await _db.Set<ShipmentProviderOperation>()
            .FirstOrDefaultAsync(
                x => x.ShipmentId == returnShipmentId &&
                     !x.IsDeleted &&
                     x.Provider == Provider &&
                     x.OperationType == OperationType &&
                     x.Status == "Pending",
                ct)
            .ConfigureAwait(false);

        if (pendingOperation is not null)
        {
            return;
        }

        var failedOperation = await _db.Set<ShipmentProviderOperation>()
            .OrderByDescending(x => x.LastAttemptAtUtc ?? x.CreatedAtUtc)
            .FirstOrDefaultAsync(
                x => x.ShipmentId == returnShipmentId &&
                     !x.IsDeleted &&
                     x.Provider == Provider &&
                     x.OperationType == OperationType &&
                     x.Status == "Failed",
                ct)
            .ConfigureAwait(false);

        if (failedOperation is not null)
        {
            failedOperation.Status = "Pending";
            failedOperation.AttemptCount = 0;
            failedOperation.LastAttemptAtUtc = null;
            failedOperation.ProcessedAtUtc = null;
            failedOperation.FailureReason = null;
        }
        else
        {
            _db.Set<ShipmentProviderOperation>().Add(new ShipmentProviderOperation
            {
                ShipmentId = returnShipmentId,
                Provider = Provider,
                OperationType = OperationType,
                Status = "Pending"
            });
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
