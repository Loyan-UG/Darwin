using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Foundation;
using Darwin.Domain.Entities.Foundation;
using Darwin.Domain.Entities.Sales;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Sales.Services;

public sealed class DeliveryNoteLifecycleEventService
{
    public const string EntityType = "DeliveryNote";

    private readonly BusinessEventService _events;
    private readonly IAppDbContext _db;

    public DeliveryNoteLifecycleEventService(BusinessEventService events, IAppDbContext db)
    {
        _events = events ?? throw new ArgumentNullException(nameof(events));
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public Task<Result<Guid>> RecordCreatedAsync(DeliveryNote note, DateTime occurredAtUtc, CancellationToken ct = default)
        => RecordAsync(note, "sales.delivery_note.created", $"sales.delivery_note.created:{note.Id:N}", AuditTrailAction.Created, occurredAtUtc, ct);

    public Task<Result<Guid>> RecordStatusChangedAsync(
        DeliveryNote note,
        DeliveryNoteStatus from,
        DeliveryNoteStatus to,
        DateTime occurredAtUtc,
        CancellationToken ct = default)
        => RecordAsync(note, "sales.delivery_note.status_changed", $"sales.delivery_note.status_changed:{note.Id:N}:{from}:{to}", AuditTrailAction.StatusChanged, occurredAtUtc, ct);

    private async Task<Result<Guid>> RecordAsync(
        DeliveryNote note,
        string eventType,
        string eventKey,
        AuditTrailAction auditAction,
        DateTime occurredAtUtc,
        CancellationToken ct)
    {
        var payload = $$"""{"deliveryNoteId":"{{note.Id:N}}","deliveryNoteNumber":{{JsonString(note.DeliveryNoteNumber)}},"orderId":"{{note.OrderId:N}}","shipmentId":"{{note.ShipmentId:N}}","status":"{{note.Status}}","quantity":{{note.TotalQuantity}},"amountMinor":{{note.TotalGrossMinor}},"currency":"{{Escape(note.Currency)}}","businessId":{{JsonGuid(note.BusinessId)}},"customerId":{{JsonGuid(note.CustomerId)}}}""";
        var eventResult = await _events.AddEventAsync(
            new AddBusinessEventCommand(
                note.BusinessId,
                EntityType,
                note.Id,
                eventType,
                eventKey,
                occurredAtUtc,
                ActorUserId: null,
                BusinessEventSource.System,
                BusinessEventSeverity.Info,
                FoundationVisibility.Internal,
                "Delivery note lifecycle",
                Summary: null,
                CorrelationId: eventKey,
                CausationId: null,
                PayloadJson: payload,
                MetadataJson: $$"""{"eventKey":"{{Escape(eventKey)}}","source":"sales"}"""),
            ct)
            .ConfigureAwait(false);
        if (!eventResult.Succeeded)
        {
            return eventResult;
        }

        var existingAuditId = await _db.Set<AuditTrail>()
            .AsNoTracking()
            .Where(x => x.EntityType == EntityType && x.EntityId == note.Id && x.Action == auditAction && x.CorrelationId == eventKey && !x.IsDeleted)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
        if (existingAuditId != Guid.Empty)
        {
            return eventResult;
        }

        var auditResult = await _events.AddAuditTrailAsync(
            new AddAuditTrailCommand(
                note.BusinessId,
                EntityType,
                note.Id,
                auditAction,
                occurredAtUtc,
                ActorUserId: null,
                BusinessEventId: eventResult.Value,
                Reason: null,
                CorrelationId: eventKey,
                ChangeSetJson: payload,
                MetadataJson: $$"""{"eventKey":"{{Escape(eventKey)}}","source":"sales"}"""),
            ct)
            .ConfigureAwait(false);
        return auditResult.Succeeded ? eventResult : Result<Guid>.Fail(auditResult.Error!);
    }

    private static string JsonGuid(Guid? value) => value.HasValue && value.Value != Guid.Empty ? $"\"{value.Value:N}\"" : "null";
    private static string JsonString(string? value) => string.IsNullOrWhiteSpace(value) ? "null" : $"\"{Escape(value)}\"";
    private static string Escape(string? value) => (value ?? string.Empty).Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
}
