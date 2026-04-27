using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Orders.DTOs;
using Darwin.Domain.Entities.Orders;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Orders.Commands;

public sealed class ResolveShipmentCarrierExceptionHandler
{
    private const string ResolutionEventKey = "shipment.exception_resolved";
    private readonly IAppDbContext _db;
    private readonly IStringLocalizer<ValidationResource> _localizer;

    public ResolveShipmentCarrierExceptionHandler(
        IAppDbContext db,
        IStringLocalizer<ValidationResource> localizer)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    }

    public async Task<Result> HandleAsync(ResolveShipmentCarrierExceptionDto dto, CancellationToken ct = default)
    {
        if (dto.ShipmentId == Guid.Empty || dto.RowVersion.Length == 0)
        {
            return Result.Fail(_localizer["InvalidDeleteRequest"]);
        }

        var normalizedNote = dto.ResolutionNote?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedNote))
        {
            return Result.Fail(_localizer["ShipmentCarrierExceptionResolutionNoteRequired"]);
        }

        var shipment = await _db.Set<Shipment>()
            .FirstOrDefaultAsync(x => x.Id == dto.ShipmentId, ct)
            .ConfigureAwait(false);

        if (shipment is null)
        {
            return Result.Fail(_localizer["ShipmentNotFoundForCarrierResolution"]);
        }

        if (!shipment.RowVersion.SequenceEqual(dto.RowVersion))
        {
            return Result.Fail(_localizer["ItemConcurrencyConflict"]);
        }

        var now = DateTime.UtcNow;
        shipment.LastCarrierEventKey = ResolutionEventKey;

        await ShipmentCarrierEventRecorder.AddIfMissingAsync(
            _db,
            shipment,
            ResolutionEventKey,
            now,
            "ExceptionResolved",
            ct: ct).ConfigureAwait(false);

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result.Ok();
    }
}
