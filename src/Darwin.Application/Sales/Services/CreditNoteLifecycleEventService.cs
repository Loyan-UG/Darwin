using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Foundation;
using Darwin.Domain.Entities.Foundation;
using Darwin.Domain.Entities.Sales;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Sales.Services;

public sealed class CreditNoteLifecycleEventService
{
    public const string EntityType = "CreditNote";

    private readonly BusinessEventService _events;
    private readonly IAppDbContext _db;

    public CreditNoteLifecycleEventService(BusinessEventService events, IAppDbContext db)
    {
        _events = events ?? throw new ArgumentNullException(nameof(events));
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public Task<Result<Guid>> RecordCreatedAsync(CreditNote note, DateTime occurredAtUtc, CancellationToken ct = default)
        => RecordAsync(note, "sales.credit_note.created", $"sales.credit_note.created:{note.Id:N}", AuditTrailAction.Created, occurredAtUtc, ct);

    public Task<Result<Guid>> RecordStatusChangedAsync(CreditNote note, CreditNoteStatus from, CreditNoteStatus to, DateTime occurredAtUtc, CancellationToken ct = default)
        => RecordAsync(note, "sales.credit_note.status_changed", $"sales.credit_note.status_changed:{note.Id:N}:{from}:{to}", AuditTrailAction.StatusChanged, occurredAtUtc, ct);

    private async Task<Result<Guid>> RecordAsync(CreditNote note, string eventType, string eventKey, AuditTrailAction auditAction, DateTime occurredAtUtc, CancellationToken ct)
    {
        var payload = $$"""{"creditNoteId":"{{note.Id:N}}","creditNoteNumber":{{JsonString(note.CreditNoteNumber)}},"invoiceId":"{{note.InvoiceId:N}}","returnOrderId":{{JsonGuid(note.ReturnOrderId)}},"refundId":{{JsonGuid(note.RefundId)}},"status":"{{note.Status}}","reason":"{{note.Reason}}","grossMinor":{{note.TotalGrossMinor}},"netMinor":{{note.TotalNetMinor}},"taxMinor":{{note.TotalTaxMinor}},"currency":"{{Escape(note.Currency)}}","businessId":{{JsonGuid(note.BusinessId)}},"customerId":{{JsonGuid(note.CustomerId)}}}""";
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
                "Credit note lifecycle",
                Summary: null,
                CorrelationId: eventKey,
                CausationId: null,
                PayloadJson: payload,
                MetadataJson: $$"""{"eventKey":"{{Escape(eventKey)}}","source":"sales"}"""),
            ct).ConfigureAwait(false);
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
            ct).ConfigureAwait(false);
        return auditResult.Succeeded ? eventResult : Result<Guid>.Fail(auditResult.Error!);
    }

    private static string JsonGuid(Guid? value) => value.HasValue && value.Value != Guid.Empty ? $"\"{value.Value:N}\"" : "null";
    private static string JsonString(string? value) => string.IsNullOrWhiteSpace(value) ? "null" : $"\"{Escape(value)}\"";
    private static string Escape(string? value) => (value ?? string.Empty).Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
}
