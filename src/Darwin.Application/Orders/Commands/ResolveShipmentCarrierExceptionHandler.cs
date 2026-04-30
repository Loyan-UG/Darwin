using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
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
    private readonly IClock _clock;
    private readonly IStringLocalizer<ValidationResource> _localizer;

    public ResolveShipmentCarrierExceptionHandler(
        IAppDbContext db,
        IClock clock,
        IStringLocalizer<ValidationResource> localizer)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    }

    public async Task<Result> HandleAsync(ResolveShipmentCarrierExceptionDto dto, CancellationToken ct = default)
    {
        if (dto.ShipmentId == Guid.Empty)
        {
            return Result.Fail(_localizer["InvalidDeleteRequest"]);
        }

        var rowVersion = dto.RowVersion ?? Array.Empty<byte>();
        if (rowVersion.Length == 0)
        {
            return Result.Fail(_localizer["RowVersionRequired"]);
        }

        var normalizedNote = dto.ResolutionNote?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedNote))
        {
            return Result.Fail(_localizer["ShipmentCarrierExceptionResolutionNoteRequired"]);
        }

        var shipment = await _db.Set<Shipment>()
            .FirstOrDefaultAsync(x => x.Id == dto.ShipmentId && !x.IsDeleted, ct)
            .ConfigureAwait(false);

        if (shipment is null)
        {
            return Result.Fail(_localizer["ShipmentNotFoundForCarrierResolution"]);
        }

        var currentRowVersion = shipment.RowVersion ?? Array.Empty<byte>();
        if (!currentRowVersion.SequenceEqual(rowVersion))
        {
            return Result.Fail(_localizer["ItemConcurrencyConflict"]);
        }

        var now = _clock.UtcNow;
        shipment.LastCarrierEventKey = ResolutionEventKey;

        await ShipmentCarrierEventRecorder.AddIfMissingAsync(
            _db,
            shipment,
            ResolutionEventKey,
            now,
            "ExceptionResolved",
            ct: ct).ConfigureAwait(false);

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
}
