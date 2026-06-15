using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Foundation;
using Darwin.Domain.Entities.Foundation;
using Darwin.Domain.Entities.Sales;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Sales.Services;

public sealed class ReturnOrderLifecycleEventService
{
    public const string EntityType = "ReturnOrder";

    private readonly BusinessEventService _events;
    private readonly IAppDbContext _db;

    public ReturnOrderLifecycleEventService(BusinessEventService events, IAppDbContext db)
    {
        _events = events ?? throw new ArgumentNullException(nameof(events));
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public Task<Result<Guid>> RecordCreatedAsync(ReturnOrder order, DateTime occurredAtUtc, CancellationToken ct = default)
        => RecordAsync(order, "sales.return_order.created", $"sales.return_order.created:{order.Id:N}", AuditTrailAction.Created, occurredAtUtc, ct);

    public Task<Result<Guid>> RecordStatusChangedAsync(ReturnOrder order, ReturnOrderStatus from, ReturnOrderStatus to, DateTime occurredAtUtc, CancellationToken ct = default)
        => RecordAsync(order, "sales.return_order.status_changed", $"sales.return_order.status_changed:{order.Id:N}:{from}:{to}", AuditTrailAction.StatusChanged, occurredAtUtc, ct);

    private async Task<Result<Guid>> RecordAsync(ReturnOrder order, string eventType, string eventKey, AuditTrailAction auditAction, DateTime occurredAtUtc, CancellationToken ct)
    {
        var payload = $$"""{"returnOrderId":"{{order.Id:N}}","returnOrderNumber":{{JsonString(order.ReturnOrderNumber)}},"orderId":"{{order.OrderId:N}}","shipmentId":{{JsonGuid(order.ShipmentId)}},"invoiceId":{{JsonGuid(order.InvoiceId)}},"status":"{{order.Status}}","requestedQuantity":{{order.RequestedQuantity}},"approvedQuantity":{{order.ApprovedQuantity}},"receivedQuantity":{{order.ReceivedQuantity}},"acceptedQuantity":{{order.AcceptedQuantity}},"restockQuantity":{{order.RestockQuantity}},"refundEligibleGrossMinor":{{order.RefundEligibleGrossMinor}},"currency":"{{Escape(order.Currency)}}","businessId":{{JsonGuid(order.BusinessId)}},"customerId":{{JsonGuid(order.CustomerId)}}}""";
        var eventResult = await _events.AddEventAsync(
            new AddBusinessEventCommand(
                order.BusinessId,
                EntityType,
                order.Id,
                eventType,
                eventKey,
                occurredAtUtc,
                ActorUserId: null,
                BusinessEventSource.System,
                BusinessEventSeverity.Info,
                FoundationVisibility.Internal,
                "Return order lifecycle",
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
            .Where(x => x.EntityType == EntityType && x.EntityId == order.Id && x.Action == auditAction && x.CorrelationId == eventKey && !x.IsDeleted)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
        if (existingAuditId != Guid.Empty)
        {
            return eventResult;
        }

        var auditResult = await _events.AddAuditTrailAsync(
            new AddAuditTrailCommand(
                order.BusinessId,
                EntityType,
                order.Id,
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
