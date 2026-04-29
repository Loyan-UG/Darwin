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

        public async Task HandleAsync(Guid shipmentId, CancellationToken ct = default)
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
                    ShipmentId = shipment.Id,
                    Provider = "DHL",
                    OperationType = "GenerateLabel",
                    Status = "Pending"
                });
            }

            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }
}
