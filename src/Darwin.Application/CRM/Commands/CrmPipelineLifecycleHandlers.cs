using Darwin.Application.Abstractions.Persistence;
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
            if (dto.Id == Guid.Empty || dto.RowVersion.Length == 0)
            {
                return Result.Fail(_localizer["InvalidDeleteRequest"]);
            }

            var lead = await _db.Set<Lead>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct)
                .ConfigureAwait(false);

            if (lead is null)
            {
                return Result.Fail(_localizer["LeadNotFound"]);
            }

            if (!lead.RowVersion.SequenceEqual(dto.RowVersion))
            {
                return Result.Fail(_localizer["ItemConcurrencyConflict"]);
            }

            if (!TryApplyAction(lead, dto.Action))
            {
                return Result.Fail(_localizer["LeadLifecycleUnsupportedAction"]);
            }

            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return Result.Ok();
        }

        private static bool TryApplyAction(Lead lead, string action)
        {
            switch (action)
            {
                case "Qualify":
                    if (lead.Status == LeadStatus.Converted)
                    {
                        return false;
                    }

                    lead.Status = LeadStatus.Qualified;
                    return true;
                case "Disqualify":
                    if (lead.Status == LeadStatus.Converted)
                    {
                        return false;
                    }

                    lead.Status = LeadStatus.Disqualified;
                    return true;
                case "Reopen":
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
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public UpdateOpportunityLifecycleHandler(
            IAppDbContext db,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task<Result> HandleAsync(UpdateOpportunityLifecycleDto dto, CancellationToken ct = default)
        {
            if (dto.Id == Guid.Empty || dto.RowVersion.Length == 0)
            {
                return Result.Fail(_localizer["InvalidDeleteRequest"]);
            }

            var opportunity = await _db.Set<Opportunity>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct)
                .ConfigureAwait(false);

            if (opportunity is null)
            {
                return Result.Fail(_localizer["OpportunityNotFound"]);
            }

            if (!opportunity.RowVersion.SequenceEqual(dto.RowVersion))
            {
                return Result.Fail(_localizer["ItemConcurrencyConflict"]);
            }

            if (!TryApplyAction(opportunity, dto.Action))
            {
                return Result.Fail(_localizer["OpportunityLifecycleUnsupportedAction"]);
            }

            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return Result.Ok();
        }

        private static bool TryApplyAction(Opportunity opportunity, string action)
        {
            switch (action)
            {
                case "Advance":
                    opportunity.Stage = opportunity.Stage switch
                    {
                        OpportunityStage.Qualification => OpportunityStage.Proposal,
                        OpportunityStage.Proposal => OpportunityStage.Negotiation,
                        OpportunityStage.Negotiation => OpportunityStage.ClosedWon,
                        _ => opportunity.Stage
                    };
                    return true;
                case "CloseWon":
                    opportunity.Stage = OpportunityStage.ClosedWon;
                    opportunity.ExpectedCloseDateUtc ??= DateTime.UtcNow.Date;
                    return true;
                case "CloseLost":
                    opportunity.Stage = OpportunityStage.ClosedLost;
                    opportunity.ExpectedCloseDateUtc ??= DateTime.UtcNow.Date;
                    return true;
                case "Reopen":
                    opportunity.Stage = OpportunityStage.Qualification;
                    return true;
                default:
                    return false;
            }
        }
    }
}
