using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Foundation;
using Darwin.Domain.Entities.Foundation;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Sales.Services;

public sealed class SalesLifecycleEventService
{
    public const string OrderEntityType = "Order";
    public const string InvoiceEntityType = "Invoice";
    public const string PaymentEntityType = "Payment";
    public const string ShipmentEntityType = "Shipment";
    public const string RefundEntityType = "Refund";

    private readonly BusinessEventService _events;
    private readonly IAppDbContext _db;

    public SalesLifecycleEventService(BusinessEventService events, IAppDbContext db)
    {
        _events = events ?? throw new ArgumentNullException(nameof(events));
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public Task<Result<Guid>> RecordOrderStatusChangedAsync(
        Order order,
        OrderStatus previousStatus,
        OrderStatus currentStatus,
        DateTime occurredAtUtc,
        CancellationToken ct = default)
        => RecordStatusChangedAsync(
            businessId: order.BusinessId,
            entityType: OrderEntityType,
            entityId: order.Id,
            eventType: "sales.order.status_changed",
            eventKey: $"sales.order.status_changed:{order.Id:N}:{previousStatus}:{currentStatus}",
            title: "Order status changed",
            payloadJson: $$"""{"orderId":"{{order.Id:N}}","orderNumber":"{{Escape(order.OrderNumber)}}","previousStatus":"{{previousStatus}}","currentStatus":"{{currentStatus}}","salesChannel":"{{order.SalesChannel}}","amountMinor":{{order.GrandTotalGrossMinor}},"currency":"{{Escape(order.Currency)}}","businessId":{{JsonGuid(order.BusinessId)}},"customerId":{{JsonGuid(order.CustomerId)}}}""",
            occurredAtUtc,
            ct);

    public Task<Result<Guid>> RecordPaymentRecordedAsync(
        Payment payment,
        Order order,
        DateTime occurredAtUtc,
        CancellationToken ct = default)
        => RecordCreatedAsync(
            businessId: order.BusinessId ?? payment.BusinessId,
            entityType: PaymentEntityType,
            entityId: payment.Id,
            eventType: "sales.payment.recorded",
            eventKey: $"sales.payment.recorded:{payment.Id:N}",
            title: "Payment recorded",
            payloadJson: $$"""{"paymentId":"{{payment.Id:N}}","orderId":"{{order.Id:N}}","orderNumber":"{{Escape(order.OrderNumber)}}","status":"{{payment.Status}}","amountMinor":{{payment.AmountMinor}},"currency":"{{Escape(payment.Currency)}}","businessId":{{JsonGuid(order.BusinessId ?? payment.BusinessId)}},"customerId":{{JsonGuid(payment.CustomerId ?? order.CustomerId)}}}""",
            occurredAtUtc,
            ct);

    public Task<Result<Guid>> RecordShipmentCreatedAsync(
        Shipment shipment,
        Order order,
        DateTime occurredAtUtc,
        CancellationToken ct = default)
        => RecordCreatedAsync(
            businessId: order.BusinessId,
            entityType: ShipmentEntityType,
            entityId: shipment.Id,
            eventType: "sales.shipment.created",
            eventKey: $"sales.shipment.created:{shipment.Id:N}",
            title: "Shipment created",
            payloadJson: $$"""{"shipmentId":"{{shipment.Id:N}}","orderId":"{{order.Id:N}}","orderNumber":"{{Escape(order.OrderNumber)}}","status":"{{shipment.Status}}","carrier":"{{Escape(shipment.Carrier)}}","service":"{{Escape(shipment.Service)}}","businessId":{{JsonGuid(order.BusinessId)}},"customerId":{{JsonGuid(order.CustomerId)}}}""",
            occurredAtUtc,
            ct);

    public Task<Result<Guid>> RecordInvoiceCreatedAsync(
        Invoice invoice,
        DateTime occurredAtUtc,
        CancellationToken ct = default)
        => RecordCreatedAsync(
            businessId: invoice.BusinessId,
            entityType: InvoiceEntityType,
            entityId: invoice.Id,
            eventType: "sales.invoice.created",
            eventKey: $"sales.invoice.created:{invoice.Id:N}",
            title: "Invoice created",
            payloadJson: $$"""{"invoiceId":"{{invoice.Id:N}}","invoiceNumber":{{JsonString(invoice.InvoiceNumber)}},"orderId":{{JsonGuid(invoice.OrderId)}},"paymentId":{{JsonGuid(invoice.PaymentId)}},"status":"{{invoice.Status}}","amountMinor":{{invoice.TotalGrossMinor}},"currency":"{{Escape(invoice.Currency)}}","businessId":{{JsonGuid(invoice.BusinessId)}},"customerId":{{JsonGuid(invoice.CustomerId)}}}""",
            occurredAtUtc,
            ct);

    public Task<Result<Guid>> RecordInvoiceStatusChangedAsync(
        Invoice invoice,
        InvoiceStatus previousStatus,
        InvoiceStatus currentStatus,
        DateTime occurredAtUtc,
        CancellationToken ct = default)
        => RecordStatusChangedAsync(
            businessId: invoice.BusinessId,
            entityType: InvoiceEntityType,
            entityId: invoice.Id,
            eventType: "sales.invoice.status_changed",
            eventKey: $"sales.invoice.status_changed:{invoice.Id:N}:{previousStatus}:{currentStatus}",
            title: "Invoice status changed",
            payloadJson: $$"""{"invoiceId":"{{invoice.Id:N}}","invoiceNumber":{{JsonString(invoice.InvoiceNumber)}},"orderId":{{JsonGuid(invoice.OrderId)}},"paymentId":{{JsonGuid(invoice.PaymentId)}},"previousStatus":"{{previousStatus}}","currentStatus":"{{currentStatus}}","amountMinor":{{invoice.TotalGrossMinor}},"currency":"{{Escape(invoice.Currency)}}","hasIssuedSnapshot":{{JsonBool(!string.IsNullOrWhiteSpace(invoice.IssuedSnapshotJson))}},"businessId":{{JsonGuid(invoice.BusinessId)}},"customerId":{{JsonGuid(invoice.CustomerId)}}}""",
            occurredAtUtc,
            ct);

    public Task<Result<Guid>> RecordRefundCreatedAsync(
        Refund refund,
        Payment payment,
        Order? order,
        Invoice? invoice,
        DateTime occurredAtUtc,
        CancellationToken ct = default)
        => RecordCreatedAsync(
            businessId: order?.BusinessId ?? payment.BusinessId ?? invoice?.BusinessId,
            entityType: RefundEntityType,
            entityId: refund.Id,
            eventType: "sales.refund.created",
            eventKey: $"sales.refund.created:{refund.Id:N}",
            title: "Refund created",
            payloadJson: $$"""{"refundId":"{{refund.Id:N}}","paymentId":"{{payment.Id:N}}","orderId":{{JsonGuid(refund.OrderId ?? payment.OrderId ?? invoice?.OrderId)}},"invoiceId":{{JsonGuid(invoice?.Id ?? payment.InvoiceId)}},"status":"{{refund.Status}}","amountMinor":{{refund.AmountMinor}},"currency":"{{Escape(refund.Currency)}}","paymentStatus":"{{payment.Status}}","orderStatus":{{JsonString(order?.Status.ToString())}},"invoiceStatus":{{JsonString(invoice?.Status.ToString())}},"businessId":{{JsonGuid(order?.BusinessId ?? payment.BusinessId ?? invoice?.BusinessId)}},"customerId":{{JsonGuid(payment.CustomerId ?? order?.CustomerId ?? invoice?.CustomerId)}}}""",
            occurredAtUtc,
            ct);

    private async Task<Result<Guid>> RecordCreatedAsync(
        Guid? businessId,
        string entityType,
        Guid entityId,
        string eventType,
        string eventKey,
        string title,
        string payloadJson,
        DateTime occurredAtUtc,
        CancellationToken ct)
        => await RecordAsync(
            businessId,
            entityType,
            entityId,
            eventType,
            eventKey,
            title,
            payloadJson,
            AuditTrailAction.Created,
            occurredAtUtc,
            ct)
            .ConfigureAwait(false);

    private async Task<Result<Guid>> RecordStatusChangedAsync(
        Guid? businessId,
        string entityType,
        Guid entityId,
        string eventType,
        string eventKey,
        string title,
        string payloadJson,
        DateTime occurredAtUtc,
        CancellationToken ct)
        => await RecordAsync(
            businessId,
            entityType,
            entityId,
            eventType,
            eventKey,
            title,
            payloadJson,
            AuditTrailAction.StatusChanged,
            occurredAtUtc,
            ct)
            .ConfigureAwait(false);

    private async Task<Result<Guid>> RecordAsync(
        Guid? businessId,
        string entityType,
        Guid entityId,
        string eventType,
        string eventKey,
        string title,
        string payloadJson,
        AuditTrailAction auditAction,
        DateTime occurredAtUtc,
        CancellationToken ct)
    {
        var eventResult = await _events.AddEventAsync(
            new AddBusinessEventCommand(
                businessId,
                entityType,
                entityId,
                eventType,
                eventKey,
                occurredAtUtc,
                ActorUserId: null,
                BusinessEventSource.System,
                BusinessEventSeverity.Info,
                FoundationVisibility.Internal,
                title,
                Summary: null,
                CorrelationId: eventKey,
                CausationId: null,
                PayloadJson: payloadJson,
                MetadataJson: BuildMetadata(eventKey)),
            ct)
            .ConfigureAwait(false);

        if (!eventResult.Succeeded)
        {
            return eventResult;
        }

        var existingAuditId = await _db.Set<AuditTrail>()
            .AsNoTracking()
            .Where(x =>
                x.EntityType == entityType &&
                x.EntityId == entityId &&
                x.Action == auditAction &&
                x.CorrelationId == eventKey &&
                !x.IsDeleted)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (existingAuditId != Guid.Empty)
        {
            return eventResult;
        }

        var auditResult = await _events.AddAuditTrailAsync(
                new AddAuditTrailCommand(
                    businessId,
                    entityType,
                    entityId,
                    auditAction,
                    occurredAtUtc,
                    ActorUserId: null,
                    BusinessEventId: eventResult.Value,
                    Reason: null,
                    CorrelationId: eventKey,
                    ChangeSetJson: payloadJson,
                    MetadataJson: BuildMetadata(eventKey)),
                ct)
            .ConfigureAwait(false);

        return auditResult.Succeeded ? eventResult : Result<Guid>.Fail(auditResult.Error!);
    }

    private static string BuildMetadata(string eventKey) =>
        $$"""{"eventKey":"{{Escape(eventKey)}}","source":"sales"}""";

    private static string JsonGuid(Guid? value) =>
        value.HasValue && value.Value != Guid.Empty ? $"\"{value.Value:N}\"" : "null";

    private static string JsonString(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "null" : $"\"{Escape(value)}\"";

    private static string JsonBool(bool value) => value ? "true" : "false";

    private static string Escape(string? value) =>
        (value ?? string.Empty)
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
}
