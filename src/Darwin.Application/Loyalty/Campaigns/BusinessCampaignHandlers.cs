using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Enums;
using Darwin.Domain.Entities.Marketing;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Loyalty.Campaigns
{
    /// <summary>
    /// Queries business-owned campaigns for management screens.
    /// </summary>
    public sealed class GetBusinessCampaignsHandler
    {
        private readonly IAppDbContext _db;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        private const string DraftCampaignState = "Draft";
        private const string ScheduledCampaignState = "Scheduled";
        private const string ActiveCampaignState = "Active";
        private const string ExpiredCampaignState = "Expired";

        public GetBusinessCampaignsHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db;
            _localizer = localizer;
        }

        public async Task<Result<GetBusinessCampaignsResultDto>> HandleAsync(Guid businessId, int page, int pageSize, LoyaltyCampaignQueueFilter filter = LoyaltyCampaignQueueFilter.All, CancellationToken ct = default)
        {
            if (businessId == Guid.Empty)
            {
                return Result<GetBusinessCampaignsResultDto>.Fail(_localizer["BusinessIdRequired"]);
            }

            var normalizedPage = page <= 0 ? 1 : page;
            var normalizedPageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 100);

            var query = _db.Set<Campaign>()
                .AsNoTracking()
                .Where(c => !c.IsDeleted && c.BusinessId == businessId);

            var nowUtc = DateTime.UtcNow;
            query = ApplyFilter(query, filter, nowUtc);
            var total = await query.CountAsync(ct).ConfigureAwait(false);

            var items = await query
                .OrderByDescending(c => c.CreatedAtUtc)
                .Skip((normalizedPage - 1) * normalizedPageSize)
                .Take(normalizedPageSize)
                .Select(c => new BusinessCampaignItemDto
                {
                    Id = c.Id,
                    BusinessId = c.BusinessId ?? Guid.Empty,
                    Name = c.Name,
                    Title = c.Title,
                    Subtitle = c.Subtitle,
                    Body = c.Body,
                    MediaUrl = c.MediaUrl,
                    LandingUrl = c.LandingUrl,
                    Channels = (short)c.Channels,
                    StartsAtUtc = c.StartsAtUtc,
                    EndsAtUtc = c.EndsAtUtc,
                    IsActive = c.IsActive,
                    CampaignState = ResolveCampaignState(c.IsActive, c.StartsAtUtc, c.EndsAtUtc, nowUtc),
                    TargetingJson = c.TargetingJson,
                    EligibilityRules = CampaignEligibilityRulesMapper.BuildRulesFromTargetingJson(c.TargetingJson),
                    PayloadJson = c.PayloadJson,
                    RowVersion = c.RowVersion
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return Result<GetBusinessCampaignsResultDto>.Ok(new GetBusinessCampaignsResultDto
            {
                Items = items,
                Total = total
            });
        }

        public async Task<BusinessCampaignOpsSummaryDto> GetSummaryAsync(Guid businessId, CancellationToken ct = default)
        {
            if (businessId == Guid.Empty)
            {
                return new BusinessCampaignOpsSummaryDto();
            }

            var nowUtc = DateTime.UtcNow;
            var query = _db.Set<Campaign>()
                .AsNoTracking()
                .Where(c => !c.IsDeleted && c.BusinessId == businessId);

            var totalCount = await query.CountAsync(ct).ConfigureAwait(false);
            var activeCount = await query.CountAsync(c => c.IsActive && (!c.StartsAtUtc.HasValue || c.StartsAtUtc <= nowUtc) && (!c.EndsAtUtc.HasValue || c.EndsAtUtc >= nowUtc), ct).ConfigureAwait(false);
            var scheduledCount = await query.CountAsync(c => c.IsActive && c.StartsAtUtc.HasValue && c.StartsAtUtc > nowUtc && (!c.EndsAtUtc.HasValue || c.EndsAtUtc >= nowUtc), ct).ConfigureAwait(false);
            var draftCount = await query.CountAsync(c => !c.IsActive && (!c.EndsAtUtc.HasValue || c.EndsAtUtc >= nowUtc), ct).ConfigureAwait(false);
            var expiredCount = await query.CountAsync(c => c.EndsAtUtc.HasValue && c.EndsAtUtc < nowUtc, ct).ConfigureAwait(false);
            var pushEnabledCount = await query.CountAsync(c => (((short)c.Channels) & 2) == 2, ct).ConfigureAwait(false);

            return new BusinessCampaignOpsSummaryDto
            {
                TotalCount = totalCount,
                ActiveCount = activeCount,
                ScheduledCount = scheduledCount,
                DraftCount = draftCount,
                ExpiredCount = expiredCount,
                PushEnabledCount = pushEnabledCount
            };
        }

        private static IQueryable<Campaign> ApplyFilter(IQueryable<Campaign> query, LoyaltyCampaignQueueFilter filter, DateTime nowUtc)
        {
            return filter switch
            {
                LoyaltyCampaignQueueFilter.Active => query.Where(c => c.IsActive && (!c.StartsAtUtc.HasValue || c.StartsAtUtc <= nowUtc) && (!c.EndsAtUtc.HasValue || c.EndsAtUtc >= nowUtc)),
                LoyaltyCampaignQueueFilter.Scheduled => query.Where(c => c.IsActive && c.StartsAtUtc.HasValue && c.StartsAtUtc > nowUtc && (!c.EndsAtUtc.HasValue || c.EndsAtUtc >= nowUtc)),
                LoyaltyCampaignQueueFilter.Draft => query.Where(c => !c.IsActive && (!c.EndsAtUtc.HasValue || c.EndsAtUtc >= nowUtc)),
                LoyaltyCampaignQueueFilter.Expired => query.Where(c => c.EndsAtUtc.HasValue && c.EndsAtUtc < nowUtc),
                LoyaltyCampaignQueueFilter.PushEnabled => query.Where(c => (((short)c.Channels) & 2) == 2),
                _ => query
            };
        }

        private static string ResolveCampaignState(bool isActive, DateTime? startsAtUtc, DateTime? endsAtUtc, DateTime nowUtc)
        {
            if (endsAtUtc.HasValue && endsAtUtc.Value < nowUtc)
            {
                return ExpiredCampaignState;
            }

            if (!isActive)
            {
                return DraftCampaignState;
            }

            if (startsAtUtc.HasValue && startsAtUtc.Value > nowUtc)
            {
                return ScheduledCampaignState;
            }

            return ActiveCampaignState;
        }
    }

    /// <summary>
    /// Queries campaign delivery attempts so operators can reconcile provider failures.
    /// </summary>
    public sealed class GetCampaignDeliveriesPageHandler
    {
        private readonly IAppDbContext _db;

        public GetCampaignDeliveriesPageHandler(IAppDbContext db)
        {
            _db = db;
        }

        public async Task<Result<GetCampaignDeliveriesResultDto>> HandleAsync(
            Guid? businessId,
            Guid? campaignId,
            int page,
            int pageSize,
            LoyaltyCampaignDeliveryQueueFilter filter = LoyaltyCampaignDeliveryQueueFilter.All,
            CancellationToken ct = default)
        {
            var normalizedPage = page <= 0 ? 1 : page;
            var normalizedPageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 100);
            var query = BuildBaseQuery(businessId, campaignId, filter);
            var total = await query.CountAsync(ct).ConfigureAwait(false);

            var items = await query
                .OrderByDescending(x => x.LastAttemptAtUtc ?? x.CreatedAtUtc)
                .Skip((normalizedPage - 1) * normalizedPageSize)
                .Take(normalizedPageSize)
                .Select(x => new CampaignDeliveryItemDto
                {
                    Id = x.Id,
                    CampaignId = x.CampaignId,
                    CampaignName = _db.Set<Campaign>()
                        .Where(c => c.Id == x.CampaignId)
                        .Select(c => c.Name)
                        .FirstOrDefault() ?? string.Empty,
                    CampaignTitle = _db.Set<Campaign>()
                        .Where(c => c.Id == x.CampaignId)
                        .Select(c => c.Title)
                        .FirstOrDefault() ?? string.Empty,
                    RecipientUserId = x.RecipientUserId,
                    BusinessId = x.BusinessId,
                    Channel = (short)x.Channel,
                    Status = (short)x.Status,
                    Destination = x.Destination,
                    AttemptCount = x.AttemptCount,
                    FirstAttemptAtUtc = x.FirstAttemptAtUtc,
                    LastAttemptAtUtc = x.LastAttemptAtUtc,
                    LastResponseCode = x.LastResponseCode,
                    ProviderMessageId = x.ProviderMessageId,
                    LastError = x.LastError,
                    IdempotencyKey = x.IdempotencyKey,
                    RowVersion = x.RowVersion
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return Result<GetCampaignDeliveriesResultDto>.Ok(new GetCampaignDeliveriesResultDto
            {
                Items = items,
                Total = total
            });
        }

        public async Task<CampaignDeliveryOpsSummaryDto> GetSummaryAsync(Guid? businessId, Guid? campaignId, CancellationToken ct = default)
        {
            var query = _db.Set<CampaignDelivery>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted);

            if (businessId.HasValue)
            {
                query = query.Where(x => x.BusinessId == businessId.Value);
            }

            if (campaignId.HasValue)
            {
                query = query.Where(x => x.CampaignId == campaignId.Value);
            }

            return new CampaignDeliveryOpsSummaryDto
            {
                TotalCount = await query.CountAsync(ct).ConfigureAwait(false),
                PendingCount = await query.CountAsync(x => x.Status == CampaignDeliveryStatus.Pending, ct).ConfigureAwait(false),
                InProgressCount = await query.CountAsync(x => x.Status == CampaignDeliveryStatus.InProgress, ct).ConfigureAwait(false),
                FailedCount = await query.CountAsync(x => x.Status == CampaignDeliveryStatus.Failed, ct).ConfigureAwait(false),
                SucceededCount = await query.CountAsync(x => x.Status == CampaignDeliveryStatus.Succeeded, ct).ConfigureAwait(false),
                CancelledCount = await query.CountAsync(x => x.Status == CampaignDeliveryStatus.Cancelled, ct).ConfigureAwait(false),
                NeedsAttentionCount = await query.CountAsync(x => x.Status == CampaignDeliveryStatus.Failed || x.Status == CampaignDeliveryStatus.InProgress, ct).ConfigureAwait(false)
            };
        }

        private IQueryable<CampaignDelivery> BuildBaseQuery(
            Guid? businessId,
            Guid? campaignId,
            LoyaltyCampaignDeliveryQueueFilter filter)
        {
            var query = _db.Set<CampaignDelivery>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted);

            if (businessId.HasValue)
            {
                query = query.Where(x => x.BusinessId == businessId.Value);
            }

            if (campaignId.HasValue)
            {
                query = query.Where(x => x.CampaignId == campaignId.Value);
            }

            return filter switch
            {
                LoyaltyCampaignDeliveryQueueFilter.Pending => query.Where(x => x.Status == CampaignDeliveryStatus.Pending),
                LoyaltyCampaignDeliveryQueueFilter.InProgress => query.Where(x => x.Status == CampaignDeliveryStatus.InProgress),
                LoyaltyCampaignDeliveryQueueFilter.Failed => query.Where(x => x.Status == CampaignDeliveryStatus.Failed),
                LoyaltyCampaignDeliveryQueueFilter.Succeeded => query.Where(x => x.Status == CampaignDeliveryStatus.Succeeded),
                LoyaltyCampaignDeliveryQueueFilter.Cancelled => query.Where(x => x.Status == CampaignDeliveryStatus.Cancelled),
                LoyaltyCampaignDeliveryQueueFilter.NeedsAttention => query.Where(x => x.Status == CampaignDeliveryStatus.Failed || x.Status == CampaignDeliveryStatus.InProgress),
                _ => query
            };
        }
    }

    /// <summary>
    /// Creates a new business-scoped campaign.
    /// </summary>
    public sealed class CreateBusinessCampaignHandler
    {
        private readonly IAppDbContext _db;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public CreateBusinessCampaignHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db;
            _localizer = localizer;
        }

        public async Task<Result<Guid>> HandleAsync(CreateBusinessCampaignDto dto, CancellationToken ct = default)
        {
            if (dto.BusinessId == Guid.Empty)
            {
                return Result<Guid>.Fail(_localizer["BusinessIdRequired"]);
            }

            if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Title))
            {
                return Result<Guid>.Fail(_localizer["BusinessCampaignNameAndTitleRequired"]);
            }

            var entity = new Campaign
            {
                BusinessId = dto.BusinessId,
                Name = dto.Name.Trim(),
                Title = dto.Title.Trim(),
                Subtitle = dto.Subtitle?.Trim(),
                Body = dto.Body?.Trim(),
                MediaUrl = dto.MediaUrl?.Trim(),
                LandingUrl = dto.LandingUrl?.Trim(),
                Channels = (Domain.Enums.CampaignChannels)dto.Channels,
                StartsAtUtc = dto.StartsAtUtc,
                EndsAtUtc = dto.EndsAtUtc,
                IsActive = false,
                TargetingJson = CampaignEligibilityRulesMapper.BuildTargetingJson(dto.TargetingJson, dto.EligibilityRules),
                PayloadJson = string.IsNullOrWhiteSpace(dto.PayloadJson) ? "{}" : dto.PayloadJson
            };

            _db.Set<Campaign>().Add(entity);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return Result<Guid>.Ok(entity.Id);
        }
    }

    /// <summary>
    /// Updates campaign content and targeting while enforcing business ownership and concurrency.
    /// </summary>
    public sealed class UpdateBusinessCampaignHandler
    {
        private readonly IAppDbContext _db;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public UpdateBusinessCampaignHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db;
            _localizer = localizer;
        }

        public async Task<Result> HandleAsync(UpdateBusinessCampaignDto dto, CancellationToken ct = default)
        {
            if (dto.BusinessId == Guid.Empty || dto.Id == Guid.Empty)
            {
                return Result.Fail(_localizer["BusinessCampaignBusinessAndCampaignRequired"]);
            }

            if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Title))
            {
                return Result.Fail(_localizer["BusinessCampaignNameAndTitleRequired"]);
            }

            var entity = await _db.Set<Campaign>()
                .SingleOrDefaultAsync(c => !c.IsDeleted && c.Id == dto.Id && c.BusinessId == dto.BusinessId, ct)
                .ConfigureAwait(false);

            if (entity is null)
            {
                return Result.Fail(_localizer["BusinessCampaignNotFound"]);
            }

            if (!entity.RowVersion.SequenceEqual(dto.RowVersion ?? Array.Empty<byte>()))
            {
                return Result.Fail(_localizer["BusinessCampaignConcurrencyConflict"]);
            }

            entity.Name = dto.Name.Trim();
            entity.Title = dto.Title.Trim();
            entity.Subtitle = dto.Subtitle?.Trim();
            entity.Body = dto.Body?.Trim();
            entity.MediaUrl = dto.MediaUrl?.Trim();
            entity.LandingUrl = dto.LandingUrl?.Trim();
            entity.Channels = (Domain.Enums.CampaignChannels)dto.Channels;
            entity.StartsAtUtc = dto.StartsAtUtc;
            entity.EndsAtUtc = dto.EndsAtUtc;
            entity.TargetingJson = CampaignEligibilityRulesMapper.BuildTargetingJson(dto.TargetingJson, dto.EligibilityRules);
            entity.PayloadJson = string.IsNullOrWhiteSpace(dto.PayloadJson) ? "{}" : dto.PayloadJson;

            try
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
                return Result.Ok();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Fail(_localizer["BusinessCampaignConcurrencyConflict"]);
            }
        }
    }

    /// <summary>
    /// Maps campaign targeting JSON to normalized eligibility rules and vice versa.
    /// </summary>
    internal static class CampaignEligibilityRulesMapper
    {
        public static List<PromotionEligibilityRuleDto> BuildRulesFromTargetingJson(string? targetingJson)
        {
            var rules = new List<PromotionEligibilityRuleDto>();
            if (string.IsNullOrWhiteSpace(targetingJson))
            {
                return rules;
            }

            try
            {
                using var document = JsonDocument.Parse(targetingJson);
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                {
                    return rules;
                }

                var root = document.RootElement;
                if (root.TryGetProperty("eligibilityRules", out var eligibilityArray) && eligibilityArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var ruleElement in eligibilityArray.EnumerateArray())
                    {
                        if (ruleElement.ValueKind != JsonValueKind.Object)
                        {
                            continue;
                        }

                        var mapped = BuildRuleFromElement(ruleElement);
                        if (mapped is not null)
                        {
                            rules.Add(mapped);
                        }
                    }

                    if (rules.Count > 0)
                    {
                        return rules;
                    }
                }

                var fallback = BuildRuleFromElement(root);
                if (fallback is not null)
                {
                    rules.Add(fallback);
                }
            }
            catch (JsonException)
            {
                // Keep compatibility with legacy free-form targeting payloads.
                return rules;
            }

            return rules;
        }

        public static string BuildTargetingJson(string? targetingJson, IReadOnlyCollection<PromotionEligibilityRuleDto>? eligibilityRules)
        {
            if (!string.IsNullOrWhiteSpace(targetingJson))
            {
                return targetingJson;
            }

            if (eligibilityRules is null || eligibilityRules.Count == 0)
            {
                return "{}";
            }

            var normalizedRules = eligibilityRules
                .Where(rule => rule is not null)
                .Select(rule => new Dictionary<string, object?>
                {
                    ["audienceKind"] = string.IsNullOrWhiteSpace(rule.AudienceKind) ? "JoinedMembers" : rule.AudienceKind.Trim(),
                    ["minPoints"] = rule.MinPoints,
                    ["maxPoints"] = rule.MaxPoints,
                    ["tierKey"] = string.IsNullOrWhiteSpace(rule.TierKey) ? null : rule.TierKey.Trim(),
                    ["note"] = string.IsNullOrWhiteSpace(rule.Note) ? null : rule.Note.Trim()
                })
                .ToList();

            if (normalizedRules.Count == 0)
            {
                return "{}";
            }

            var first = normalizedRules[0];
            var envelope = new Dictionary<string, object?>
            {
                ["audienceKind"] = first["audienceKind"],
                ["minPoints"] = first["minPoints"],
                ["maxPoints"] = first["maxPoints"],
                ["tierKey"] = first["tierKey"],
                ["note"] = first["note"],
                ["eligibilityRules"] = normalizedRules
            };

            return JsonSerializer.Serialize(envelope);
        }

        private static PromotionEligibilityRuleDto? BuildRuleFromElement(JsonElement element)
        {
            var audienceKind = TryGetString(element, "audienceKind") ?? TryGetString(element, "kind") ?? "JoinedMembers";
            var minPoints = TryGetInt32(element, "minPoints");
            var maxPoints = TryGetInt32(element, "maxPoints");
            var tierKey = TryGetString(element, "tierKey") ?? TryGetString(element, "tier");
            var note = TryGetString(element, "note");

            return new PromotionEligibilityRuleDto
            {
                AudienceKind = audienceKind,
                MinPoints = minPoints,
                MaxPoints = maxPoints,
                TierKey = tierKey,
                Note = note
            };
        }

        private static string? TryGetString(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            var value = property.GetString();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        private static int? TryGetInt32(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var property))
            {
                return null;
            }

            if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var numeric))
            {
                return numeric;
            }

            if (property.ValueKind == JsonValueKind.String && int.TryParse(property.GetString(), out var parsed))
            {
                return parsed;
            }

            return null;
        }
    }


    /// <summary>
    /// Activates or deactivates a campaign.
    /// </summary>
    public sealed class SetCampaignActivationHandler
    {
        private readonly IAppDbContext _db;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public SetCampaignActivationHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db;
            _localizer = localizer;
        }

        public async Task<Result> HandleAsync(SetCampaignActivationDto dto, CancellationToken ct = default)
        {
            if (dto.BusinessId == Guid.Empty || dto.Id == Guid.Empty)
            {
                return Result.Fail(_localizer["BusinessCampaignBusinessAndCampaignRequired"]);
            }

            var entity = await _db.Set<Campaign>()
                .SingleOrDefaultAsync(c => !c.IsDeleted && c.Id == dto.Id && c.BusinessId == dto.BusinessId, ct)
                .ConfigureAwait(false);

            if (entity is null)
            {
                return Result.Fail(_localizer["BusinessCampaignNotFound"]);
            }

            if (!entity.RowVersion.SequenceEqual(dto.RowVersion ?? Array.Empty<byte>()))
            {
                return Result.Fail(_localizer["BusinessCampaignConcurrencyConflict"]);
            }

            entity.IsActive = dto.IsActive;

            try
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
                return Result.Ok();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Fail(_localizer["BusinessCampaignConcurrencyConflict"]);
            }
        }
    }

    /// <summary>
    /// Lets WebAdmin operators reconcile or requeue individual campaign delivery attempts.
    /// </summary>
    public sealed class UpdateCampaignDeliveryStatusHandler
    {
        private readonly IAppDbContext _db;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public UpdateCampaignDeliveryStatusHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db;
            _localizer = localizer;
        }

        public async Task<Result> HandleAsync(UpdateCampaignDeliveryStatusDto dto, CancellationToken ct = default)
        {
            if (dto.Id == Guid.Empty || dto.RowVersion.Length == 0)
            {
                return Result.Fail(_localizer["InvalidDeleteRequest"]);
            }

            if (!Enum.IsDefined(typeof(CampaignDeliveryStatus), dto.Status))
            {
                return Result.Fail(_localizer["InvalidDeleteRequest"]);
            }

            var delivery = await _db.Set<CampaignDelivery>()
                .FirstOrDefaultAsync(x =>
                    !x.IsDeleted &&
                    x.Id == dto.Id &&
                    (!dto.BusinessId.HasValue || x.BusinessId == dto.BusinessId.Value), ct)
                .ConfigureAwait(false);

            if (delivery is null)
            {
                return Result.Fail(_localizer["CampaignDeliveryNotFound"]);
            }

            if (!delivery.RowVersion.SequenceEqual(dto.RowVersion))
            {
                return Result.Fail(_localizer["ItemConcurrencyConflict"]);
            }

            var nextStatus = (CampaignDeliveryStatus)dto.Status;
            delivery.Status = nextStatus;
            if (nextStatus == CampaignDeliveryStatus.Pending)
            {
                delivery.LastError = null;
                delivery.LastResponseCode = null;
            }
            else if (!string.IsNullOrWhiteSpace(dto.OperatorNote))
            {
                delivery.LastError = dto.OperatorNote.Trim();
            }

            var nowUtc = DateTime.UtcNow;
            delivery.LastAttemptAtUtc ??= nowUtc;
            if (nextStatus == CampaignDeliveryStatus.Succeeded && !delivery.FirstAttemptAtUtc.HasValue)
            {
                delivery.FirstAttemptAtUtc = nowUtc;
            }

            try
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
                return Result.Ok();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Fail(_localizer["ItemConcurrencyConflict"]);
            }
        }
    }
}
