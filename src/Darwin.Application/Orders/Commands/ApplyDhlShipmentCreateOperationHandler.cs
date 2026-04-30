using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Orders.DTOs;
using Darwin.Domain.Entities.Integration;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Entities.Settings;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Orders.Commands
{
    /// <summary>
    /// Applies a queued DHL shipment-creation operation to an existing shipment.
    /// </summary>
    public sealed class ApplyDhlShipmentCreateOperationHandler
    {
        private readonly IAppDbContext _db;
        private readonly IClock _clock;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public ApplyDhlShipmentCreateOperationHandler(
            IAppDbContext db,
            IClock clock,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db;
            _clock = clock;
            _localizer = localizer;
        }

        public async Task<ShipmentDetailDto> HandleAsync(Guid shipmentId, CancellationToken ct = default)
        {
            var shipment = await _db.Set<Shipment>()
                .FirstOrDefaultAsync(x => x.Id == shipmentId && !x.IsDeleted, ct)
                .ConfigureAwait(false);

            if (shipment is null)
            {
                throw new InvalidOperationException(_localizer["ShipmentNotFoundForLabelGeneration"]);
            }

            if (!DhlShipmentPhaseOneMetadata.IsDhlCarrier(shipment.Carrier))
            {
                throw new ValidationException(_localizer["ShipmentCarrierMustBeDhlForLabelGeneration"]);
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

            shipment.ProviderShipmentReference = string.IsNullOrWhiteSpace(shipment.ProviderShipmentReference)
                ? DhlShipmentPhaseOneMetadata.BuildProviderShipmentReference(shipment)
                : shipment.ProviderShipmentReference.Trim();

            shipment.TrackingNumber = string.IsNullOrWhiteSpace(shipment.TrackingNumber)
                ? DhlShipmentPhaseOneMetadata.BuildTrackingNumber(settings, shipment)
                : shipment.TrackingNumber.Trim();

            shipment.LastCarrierEventKey = "shipment.provider_created";

            var nowUtc = _clock.UtcNow;
            await ShipmentCarrierEventRecorder.AddIfMissingAsync(
                _db,
                shipment,
                "shipment.provider_created",
                nowUtc,
                "Created",
                ct: ct).ConfigureAwait(false);

            await _db.SaveChangesAsync(ct).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(shipment.LabelUrl))
            {
                var pendingLabelOperation = await _db.Set<ShipmentProviderOperation>()
                    .FirstOrDefaultAsync(
                        x => x.ShipmentId == shipment.Id &&
                             !x.IsDeleted &&
                             x.Provider == "DHL" &&
                             x.OperationType == "GenerateLabel" &&
                             x.Status == "Pending",
                        ct)
                    .ConfigureAwait(false);

                if (pendingLabelOperation is null)
                {
                    var queuedLabelOperation = false;
                    ShipmentProviderOperation? queuedLabelOperationEntry = null;
                    var failedLabelOperation = await _db.Set<ShipmentProviderOperation>()
                        .OrderByDescending(x => x.LastAttemptAtUtc ?? x.CreatedAtUtc)
                        .FirstOrDefaultAsync(
                            x => x.ShipmentId == shipment.Id &&
                                 !x.IsDeleted &&
                                 x.Provider == "DHL" &&
                                 x.OperationType == "GenerateLabel" &&
                                 x.Status == "Failed",
                            ct)
                        .ConfigureAwait(false);

                    if (failedLabelOperation is not null)
                    {
                        failedLabelOperation.Status = "Pending";
                        failedLabelOperation.AttemptCount = 0;
                        failedLabelOperation.LastAttemptAtUtc = null;
                        failedLabelOperation.ProcessedAtUtc = null;
                        failedLabelOperation.FailureReason = null;
                        queuedLabelOperation = true;
                        queuedLabelOperationEntry = failedLabelOperation;
                    }
                    else
                    {
                        queuedLabelOperationEntry = new ShipmentProviderOperation
                        {
                            ShipmentId = shipment.Id,
                            Provider = "DHL",
                            OperationType = "GenerateLabel",
                            Status = "Pending"
                        };

                        _db.Set<ShipmentProviderOperation>().Add(queuedLabelOperationEntry);
                        queuedLabelOperation = true;
                    }

                    try
                    {
                        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
                    }
                    catch (DbUpdateException ex)
                    {
                        if (queuedLabelOperation && await HasPendingLabelOperationAsync(shipment.Id, ct).ConfigureAwait(false))
                        {
                            DetachQueueConflictEntries(ex, queuedLabelOperationEntry);

                            // A concurrent queue path already created the pending label operation.
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
            }

            return new ShipmentDetailDto
            {
                Id = shipment.Id,
                Carrier = shipment.Carrier ?? string.Empty,
                Service = shipment.Service ?? string.Empty,
                ProviderShipmentReference = shipment.ProviderShipmentReference,
                TrackingNumber = shipment.TrackingNumber,
                TrackingUrl = Queries.ShipmentTrackingPresentation.ResolveTrackingUrl(shipment.Carrier, shipment.TrackingNumber),
                LabelUrl = shipment.LabelUrl,
                TotalWeight = shipment.TotalWeight,
                Status = shipment.Status,
                ShippedAtUtc = shipment.ShippedAtUtc,
                DeliveredAtUtc = shipment.DeliveredAtUtc,
                LastCarrierEventKey = shipment.LastCarrierEventKey
            };
        }

        private Task<bool> HasPendingLabelOperationAsync(Guid shipmentId, CancellationToken ct)
        {
            return _db.Set<ShipmentProviderOperation>()
                .AsNoTracking()
                .AnyAsync(
                    x => x.ShipmentId == shipmentId &&
                         !x.IsDeleted &&
                         x.Provider == "DHL" &&
                         x.OperationType == "GenerateLabel" &&
                         x.Status == "Pending",
                    ct);
        }

        private void DetachQueueConflictEntries(DbUpdateException ex, ShipmentProviderOperation? operation)
        {
            foreach (var entry in ex.Entries)
            {
                entry.State = EntityState.Detached;
            }

            if (operation is not null && _db is DbContext dbContext)
            {
                dbContext.Entry(operation).State = EntityState.Detached;
            }
        }
    }
}
