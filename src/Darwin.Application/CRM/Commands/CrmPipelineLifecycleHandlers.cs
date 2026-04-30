using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
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
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public UpdateLeadLifecycleHandler(
            IAppDbContext db,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
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

            if (!TryApplyAction(lead, dto.Action))
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

            return Result.Ok();
        }

        private static bool TryApplyAction(Lead lead, string action)
        {
            switch ((action ?? string.Empty).Trim().ToUpperInvariant())
            {
                case "QUALIFY":
                    if (lead.Status == LeadStatus.Converted)
                    {
                        return false;
                    }

                    lead.Status = LeadStatus.Qualified;
                    return true;
                case "DISQUALIFY":
                    if (lead.Status == LeadStatus.Converted)
                    {
                        return false;
                    }

                    lead.Status = LeadStatus.Disqualified;
                    return true;
                case "REOPEN":
                    if (lead.Status == LeadStatus.Converted)
                    {
                        return false;
                    }

                    lead.Status = LeadStatus.New;
                    return true;
                default:
                    return false;
            }
        }
    }

    public sealed class UpdateOpportunityLifecycleHandler
    {
        private readonly IAppDbContext _db;
        private readonly IClock _clock;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public UpdateOpportunityLifecycleHandler(
            IAppDbContext db,
            IClock clock,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
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

            var todayUtc = _clock.UtcNow.Date;
            if (!TryApplyAction(opportunity, dto.Action, todayUtc))
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

            return Result.Ok();
        }

        private static bool TryApplyAction(Opportunity opportunity, string action, DateTime todayUtc)
        {
            switch ((action ?? string.Empty).Trim().ToUpperInvariant())
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
                    return true;
                case "CLOSEWON":
                    opportunity.Stage = OpportunityStage.ClosedWon;
                    opportunity.ExpectedCloseDateUtc ??= todayUtc;
                    return true;
                case "CLOSELOST":
                    opportunity.Stage = OpportunityStage.ClosedLost;
                    opportunity.ExpectedCloseDateUtc ??= todayUtc;
                    return true;
                case "REOPEN":
                    opportunity.Stage = OpportunityStage.Qualification;
                    return true;
                default:
                    return false;
            }
        }
    }
}
