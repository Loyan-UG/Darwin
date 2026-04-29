using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Integration;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Entities.Settings;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Orders.Commands
{
    /// <summary>
    /// Queues a DHL label-generation operation for async worker processing.
    /// </summary>
    public sealed class GenerateDhlShipmentLabelHandler
    {
        private readonly IAppDbContext _db;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public GenerateDhlShipmentLabelHandler(
            IAppDbContext db,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db;
            _localizer = localizer;
        }

        public async Task HandleAsync(Guid shipmentId, byte[]? rowVersion = null, CancellationToken ct = default)
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

            if (rowVersion is not null)
            {
                var currentVersion = shipment.RowVersion ?? Array.Empty<byte>();
                if (rowVersion.Length == 0 || !currentVersion.SequenceEqual(rowVersion))
                {
                    throw new DbUpdateConcurrencyException(_localizer["ItemConcurrencyConflict"]);
                }
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

            if (!string.IsNullOrWhiteSpace(shipment.LabelUrl))
            {
                return;
            }

            var pendingOperation = await _db.Set<ShipmentProviderOperation>()
                .FirstOrDefaultAsync(
                    x => x.ShipmentId == shipment.Id &&
                         !x.IsDeleted &&
                         x.Provider == "DHL" &&
                         x.OperationType == "GenerateLabel" &&
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
                    x => x.ShipmentId == shipment.Id &&
                         !x.IsDeleted &&
                         x.Provider == "DHL" &&
                         x.OperationType == "GenerateLabel" &&
                         x.Status == "Failed",
                    ct)
                .ConfigureAwait(false);

            var queuedPendingOperation = false;
            ShipmentProviderOperation? queuedPendingOperationEntry = null;
            if (failedOperation is not null)
            {
                failedOperation.Status = "Pending";
                failedOperation.AttemptCount = 0;
                failedOperation.LastAttemptAtUtc = null;
                failedOperation.ProcessedAtUtc = null;
                failedOperation.FailureReason = null;
                queuedPendingOperation = true;
                queuedPendingOperationEntry = failedOperation;
            }
            else
            {
                queuedPendingOperationEntry = new ShipmentProviderOperation
                {
                    ShipmentId = shipment.Id,
                    Provider = "DHL",
                    OperationType = "GenerateLabel",
                    Status = "Pending"
                };

                _db.Set<ShipmentProviderOperation>().Add(queuedPendingOperationEntry);
                queuedPendingOperation = true;
            }

            try
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            catch (DbUpdateException ex)
            {
                if (queuedPendingOperation && await HasPendingLabelOperationAsync(shipment.Id, ct).ConfigureAwait(false))
                {
                    DetachQueueConflictEntries(ex, queuedPendingOperationEntry);

                    // A concurrent request queued the same pending label operation first.
                    return;
                }

                throw;
            }
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
