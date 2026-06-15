using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.CRM.Services;
using Darwin.Application.CRM.DTOs;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.CRM.Commands
{
    public sealed class UpdateLeadLifecycleHandler
    {
        private readonly IAppDbContext _db;
        private readonly IClock _clock;
        private readonly IStringLocalizer<ValidationResource> _localizer;
        private readonly CrmFoundationPrimitiveService? _foundation;

        public UpdateLeadLifecycleHandler(
            IAppDbContext db,
            IStringLocalizer<ValidationResource> localizer,
            IClock? clock = null,
            CrmFoundationPrimitiveService? foundation = null)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _clock = clock ?? DefaultHandlerDependencies.DefaultClock;
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
            _foundation = foundation;
        }

        public async Task<Result> HandleAsync(UpdateLeadLifecycleDto dto, CancellationToken ct = default)
        {
            if (dto.Id == Guid.Empty)
            {
                return Result.Fail(_localizer["InvalidDeleteRequest"]);
            }

            var rowVersion = dto.RowVersion ?? Array.Empty<byte>();
            if (rowVersion.Length == 0)
            {
                return Result.Fail(_localizer["RowVersionRequired"]);
            }

            var lead = await _db.Set<Lead>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct)
                .ConfigureAwait(false);

            if (lead is null)
            {
                return Result.Fail(_localizer["LeadNotFound"]);
            }

            var currentVersion = lead.RowVersion ?? Array.Empty<byte>();
            if (!currentVersion.SequenceEqual(rowVersion))
            {
                return Result.Fail(_localizer["ItemConcurrencyConflict"]);
            }

            var previousStatus = lead.Status;
            var occurredAtUtc = _clock.UtcNow;
            var normalizedAction = NormalizeAction(dto.Action);
            if (!TryApplyAction(lead, normalizedAction, occurredAtUtc, dto.ClosedReason))
            {
                return Result.Fail(_localizer["LeadLifecycleUnsupportedAction"]);
            }

            try
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Fail(_localizer["ItemConcurrencyConflict"]);
            }

            var foundationResult = await RecordLeadLifecycleAsync(lead, previousStatus, normalizedAction, occurredAtUtc, dto.ClosedReason, ct)
                .ConfigureAwait(false);
            if (!foundationResult.Succeeded)
            {
                return Result.Fail(foundationResult.Error!);
            }

            return Result.Ok();
        }

        private async Task<Result> RecordLeadLifecycleAsync(
            Lead lead,
            LeadStatus previousStatus,
            string action,
            DateTime occurredAtUtc,
            string? reason,
            CancellationToken ct)
        {
            if (_foundation is null)
            {
                return Result.Ok();
            }

            var eventType = action switch
            {
                "QUALIFY" => "crm.lead.qualified",
                "DISQUALIFY" => "crm.lead.disqualified",
                "REOPEN" => "crm.lead.reopened",
                _ => "crm.lead.lifecycle"
            };
            var eventKey = $"{eventType}:{lead.Id:N}:{occurredAtUtc:yyyyMMddHHmmss}";
            var payloadJson = $$"""{"leadId":"{{lead.Id}}","from":"{{previousStatus}}","to":"{{lead.Status}}","action":"{{action}}"}""";
            var eventResult = await _foundation.RecordLifecycleEventAsync(
                CrmFoundationPrimitiveService.EntityTypes.Lead,
                lead.Id,
                eventType,
                eventKey,
                occurredAtUtc,
                lead.AssignedToUserId,
                $"Lead {lead.Status}",
                $"Lead moved from {previousStatus} to {lead.Status}.",
                payloadJson,
                AuditTrailAction.StatusChanged,
                reason,
                ct)
                .ConfigureAwait(false);
            if (!eventResult.Succeeded)
            {
                return Result.Fail(eventResult.Error!);
            }

            var activityResult = await _foundation.AddActivityAsync(
                CrmFoundationPrimitiveService.EntityTypes.Lead,
                lead.Id,
                eventType,
                occurredAtUtc,
                lead.AssignedToUserId,
                $"Lead {lead.Status}",
                $"Previous status: {previousStatus}",
                metadataJson: payloadJson,
                ct: ct)
                .ConfigureAwait(false);

            return activityResult.Succeeded ? Result.Ok() : Result.Fail(activityResult.Error!);
        }

        private static bool TryApplyAction(Lead lead, string action, DateTime nowUtc, string? closedReason)
        {
            switch (action)
            {
                case "QUALIFY":
                    if (lead.Status == LeadStatus.Converted)
                    {
                        return false;
                    }

                    lead.Status = LeadStatus.Qualified;
                    lead.QualifiedAtUtc ??= nowUtc;
                    lead.DisqualifiedAtUtc = null;
                    lead.ClosedReason = null;
                    return true;
                case "DISQUALIFY":
                    if (lead.Status == LeadStatus.Converted)
                    {
                        return false;
                    }

                    lead.Status = LeadStatus.Disqualified;
                    lead.DisqualifiedAtUtc ??= nowUtc;
                    lead.ClosedReason = NormalizeOptional(closedReason);
                    return true;
                case "REOPEN":
                    if (lead.Status == LeadStatus.Converted)
                    {
                        return false;
                    }

                    lead.Status = LeadStatus.New;
                    lead.QualifiedAtUtc = null;
                    lead.DisqualifiedAtUtc = null;
                    lead.ClosedReason = null;
                    return true;
                default:
                    return false;
            }
        }

        private static string NormalizeAction(string? action) =>
            (action ?? string.Empty).Trim().ToUpperInvariant();

        private static string? NormalizeOptional(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public sealed class UpdateOpportunityLifecycleHandler
    {
        private readonly IAppDbContext _db;
        private readonly IClock _clock;
        private readonly IStringLocalizer<ValidationResource> _localizer;
        private readonly CrmFoundationPrimitiveService? _foundation;

        public UpdateOpportunityLifecycleHandler(
            IAppDbContext db,
            IClock clock,
            IStringLocalizer<ValidationResource> localizer,
            CrmFoundationPrimitiveService? foundation = null)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
            _foundation = foundation;
        }

        public async Task<Result> HandleAsync(UpdateOpportunityLifecycleDto dto, CancellationToken ct = default)
        {
            if (dto.Id == Guid.Empty)
            {
                return Result.Fail(_localizer["InvalidDeleteRequest"]);
            }

            var rowVersion = dto.RowVersion ?? Array.Empty<byte>();
            if (rowVersion.Length == 0)
            {
                return Result.Fail(_localizer["RowVersionRequired"]);
            }

            var opportunity = await _db.Set<Opportunity>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct)
                .ConfigureAwait(false);

            if (opportunity is null)
            {
                return Result.Fail(_localizer["OpportunityNotFound"]);
            }

            var currentVersion = opportunity.RowVersion ?? Array.Empty<byte>();
            if (!currentVersion.SequenceEqual(rowVersion))
            {
                return Result.Fail(_localizer["ItemConcurrencyConflict"]);
            }

            var previousStage = opportunity.Stage;
            var todayUtc = _clock.UtcNow.Date;
            var normalizedAction = NormalizeAction(dto.Action);
            if (!TryApplyAction(opportunity, normalizedAction, todayUtc, dto.CloseReason))
            {
                return Result.Fail(_localizer["OpportunityLifecycleUnsupportedAction"]);
            }

            try
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Fail(_localizer["ItemConcurrencyConflict"]);
            }

            var foundationResult = await RecordOpportunityLifecycleAsync(opportunity, previousStage, normalizedAction, todayUtc, dto.CloseReason, ct)
                .ConfigureAwait(false);
            if (!foundationResult.Succeeded)
            {
                return Result.Fail(foundationResult.Error!);
            }

            return Result.Ok();
        }

        private async Task<Result> RecordOpportunityLifecycleAsync(
            Opportunity opportunity,
            OpportunityStage previousStage,
            string action,
            DateTime occurredAtUtc,
            string? reason,
            CancellationToken ct)
        {
            if (_foundation is null)
            {
                return Result.Ok();
            }

            var eventType = action switch
            {
                "ADVANCE" => "crm.opportunity.stage-changed",
                "CLOSEWON" => "crm.opportunity.closed-won",
                "CLOSELOST" => "crm.opportunity.closed-lost",
                "REOPEN" => "crm.opportunity.reopened",
                _ => "crm.opportunity.lifecycle"
            };
            var eventKey = $"{eventType}:{opportunity.Id:N}:{occurredAtUtc:yyyyMMddHHmmss}";
            var payloadJson = $$"""{"opportunityId":"{{opportunity.Id}}","from":"{{previousStage}}","to":"{{opportunity.Stage}}","action":"{{action}}"}""";
            var eventResult = await _foundation.RecordLifecycleEventAsync(
                CrmFoundationPrimitiveService.EntityTypes.Opportunity,
                opportunity.Id,
                eventType,
                eventKey,
                occurredAtUtc,
                opportunity.AssignedToUserId,
                $"Opportunity {opportunity.Stage}",
                $"Opportunity moved from {previousStage} to {opportunity.Stage}.",
                payloadJson,
                AuditTrailAction.StatusChanged,
                reason,
                ct)
                .ConfigureAwait(false);
            if (!eventResult.Succeeded)
            {
                return Result.Fail(eventResult.Error!);
            }

            var activityResult = await _foundation.AddActivityAsync(
                CrmFoundationPrimitiveService.EntityTypes.Opportunity,
                opportunity.Id,
                eventType,
                occurredAtUtc,
                opportunity.AssignedToUserId,
                $"Opportunity {opportunity.Stage}",
                $"Previous stage: {previousStage}",
                metadataJson: payloadJson,
                ct: ct)
                .ConfigureAwait(false);

            return activityResult.Succeeded ? Result.Ok() : Result.Fail(activityResult.Error!);
        }

        private static bool TryApplyAction(Opportunity opportunity, string action, DateTime todayUtc, string? closeReason)
        {
            switch (action)
            {
                case "ADVANCE":
                    if (opportunity.Stage is OpportunityStage.ClosedWon or OpportunityStage.ClosedLost)
                    {
                        return false;
                    }

                    opportunity.Stage = opportunity.Stage switch
                    {
                        OpportunityStage.Qualification => OpportunityStage.Proposal,
                        OpportunityStage.Proposal => OpportunityStage.Negotiation,
                        OpportunityStage.Negotiation => OpportunityStage.ClosedWon,
                        _ => opportunity.Stage
                    };
                    if (opportunity.Stage == OpportunityStage.ClosedWon)
                    {
                        opportunity.ClosedAtUtc ??= todayUtc;
                        opportunity.CloseReason = NormalizeOptional(closeReason);
                        opportunity.ForecastCategory = OpportunityForecastCategory.Closed;
                    }
                    return true;
                case "CLOSEWON":
                    opportunity.Stage = OpportunityStage.ClosedWon;
                    opportunity.ExpectedCloseDateUtc ??= todayUtc;
                    opportunity.ClosedAtUtc ??= todayUtc;
                    opportunity.CloseReason = NormalizeOptional(closeReason);
                    opportunity.ForecastCategory = OpportunityForecastCategory.Closed;
                    return true;
                case "CLOSELOST":
                    opportunity.Stage = OpportunityStage.ClosedLost;
                    opportunity.ExpectedCloseDateUtc ??= todayUtc;
                    opportunity.ClosedAtUtc ??= todayUtc;
                    opportunity.CloseReason = NormalizeOptional(closeReason);
                    opportunity.ForecastCategory = OpportunityForecastCategory.Closed;
                    return true;
                case "REOPEN":
                    opportunity.Stage = OpportunityStage.Qualification;
                    opportunity.ClosedAtUtc = null;
                    opportunity.CloseReason = null;
                    opportunity.ForecastCategory = OpportunityForecastCategory.Pipeline;
                    return true;
                default:
                    return false;
            }
        }

        private static string NormalizeAction(string? action) =>
            (action ?? string.Empty).Trim().ToUpperInvariant();

        private static string? NormalizeOptional(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
