using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Foundation;
using Darwin.Domain.Entities.Foundation;
using Darwin.Domain.Entities.Sales;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Sales.Services;

public sealed class SalesQuoteLifecycleEventService
{
    private const string EntityType = "SalesQuote";
    private readonly IAppDbContext _db;
    private readonly BusinessEventService _events;

    public SalesQuoteLifecycleEventService(IAppDbContext db, BusinessEventService events)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _events = events ?? throw new ArgumentNullException(nameof(events));
    }

    public async Task RecordStatusChangedAsync(
        SalesQuote quote,
        string eventType,
        SalesQuoteStatus from,
        SalesQuoteStatus to,
        Guid? actorUserId,
        string? reason,
        CancellationToken ct = default)
    {
        var eventKey = $"sales.quote.status_changed:{quote.Id}:{from}:{to}";
        var payload = $$"""
        {
          "quoteId": "{{quote.Id}}",
          "quoteNumber": "{{Escape(quote.QuoteNumber)}}",
          "from": "{{from}}",
          "to": "{{to}}",
          "businessId": "{{quote.BusinessId}}",
          "customerId": "{{quote.CustomerId}}",
          "opportunityId": "{{quote.OpportunityId}}",
          "convertedOrderId": "{{quote.ConvertedOrderId}}",
          "currency": "{{Escape(quote.Currency)}}",
          "totalGrossMinor": {{quote.TotalGrossMinor}}
        }
        """;

        var eventId = await AddEventAsync(
            quote,
            eventType,
            eventKey,
            $"Sales quote {to}",
            reason,
            actorUserId,
            payload,
            ct).ConfigureAwait(false);

        await AddAuditIfMissingAsync(
            quote,
            eventId,
            AuditTrailAction.StatusChanged,
            actorUserId,
            reason,
            eventKey,
            "{\"status\":{\"from\":\"" + from + "\",\"to\":\"" + to + "\"}}",
            ct).ConfigureAwait(false);
    }

    public async Task RecordOrderCreatedAsync(
        SalesQuote quote,
        Guid orderId,
        string orderNumber,
        Guid? actorUserId,
        string? reason,
        CancellationToken ct = default)
    {
        var eventKey = $"sales.quote.order_created:{quote.Id}:{orderId}";
        var payload = $$"""
        {
          "quoteId": "{{quote.Id}}",
          "quoteNumber": "{{Escape(quote.QuoteNumber)}}",
          "orderId": "{{orderId}}",
          "orderNumber": "{{Escape(orderNumber)}}",
          "businessId": "{{quote.BusinessId}}",
          "customerId": "{{quote.CustomerId}}",
          "currency": "{{Escape(quote.Currency)}}",
          "totalGrossMinor": {{quote.TotalGrossMinor}}
        }
        """;

        var eventId = await AddEventAsync(
            quote,
            "sales.quote.order_created",
            eventKey,
            "Sales quote order created",
            reason,
            actorUserId,
            payload,
            ct).ConfigureAwait(false);

        await AddAuditIfMissingAsync(
            quote,
            eventId,
            AuditTrailAction.Created,
            actorUserId,
            reason,
            eventKey,
            payload,
            ct).ConfigureAwait(false);
    }

    private async Task<Guid> AddEventAsync(
        SalesQuote quote,
        string eventType,
        string eventKey,
        string title,
        string? reason,
        Guid? actorUserId,
        string payload,
        CancellationToken ct)
    {
        var eventResult = await _events.AddEventAsync(new AddBusinessEventCommand(
            quote.BusinessId,
            EntityType,
            quote.Id,
            eventType,
            eventKey,
            default,
            actorUserId,
            BusinessEventSource.User,
            BusinessEventSeverity.Info,
            FoundationVisibility.Internal,
            title,
            reason,
            eventKey,
            null,
            payload,
            "{}"), ct).ConfigureAwait(false);
        if (!eventResult.Succeeded)
        {
            throw new InvalidOperationException(eventResult.Error);
        }

        return eventResult.Value;
    }

    private async Task AddAuditIfMissingAsync(
        SalesQuote quote,
        Guid businessEventId,
        AuditTrailAction action,
        Guid? actorUserId,
        string? reason,
        string correlationId,
        string changeSetJson,
        CancellationToken ct)
    {
        var existingAudit = await _db.Set<AuditTrail>()
            .AsNoTracking()
            .AnyAsync(x =>
                x.BusinessEventId == businessEventId &&
                x.EntityType == EntityType &&
                x.EntityId == quote.Id &&
                x.Action == action &&
                !x.IsDeleted,
                ct)
            .ConfigureAwait(false);
        if (existingAudit)
        {
            return;
        }

        var auditResult = await _events.AddAuditTrailAsync(new AddAuditTrailCommand(
            quote.BusinessId,
            EntityType,
            quote.Id,
            action,
            default,
            actorUserId,
            businessEventId,
            reason,
            correlationId,
            changeSetJson,
            "{}"), ct).ConfigureAwait(false);
        if (!auditResult.Succeeded)
        {
            throw new InvalidOperationException(auditResult.Error);
        }
    }

    private static string Escape(string? value) => (value ?? string.Empty).Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
}
