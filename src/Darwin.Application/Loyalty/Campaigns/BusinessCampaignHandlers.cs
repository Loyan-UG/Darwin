using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Marketing;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Loyalty.Campaigns
{
    /// <summary>
    /// Queries business-owned campaigns for management screens.
    /// </summary>
    public sealed class GetBusinessCampaignsHandler
    {
        private readonly IAppDbContext _db;

        public GetBusinessCampaignsHandler(IAppDbContext db) => _db = db;

        public async Task<Result<GetBusinessCampaignsResultDto>> HandleAsync(Guid businessId, int page, int pageSize, CancellationToken ct = default)
        {
            if (businessId == Guid.Empty)
            {
                return Result<GetBusinessCampaignsResultDto>.Fail("BusinessId is required.");
            }

            var normalizedPage = page <= 0 ? 1 : page;
            var normalizedPageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 100);

            var query = _db.Set<Campaign>()
                .AsNoTracking()
                .Where(c => !c.IsDeleted && c.BusinessId == businessId);

            var total = await query.CountAsync(ct).ConfigureAwait(false);
            var nowUtc = DateTime.UtcNow;

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
                    CampaignState = c.IsActive
                        ? (!c.StartsAtUtc.HasValue || c.StartsAtUtc.Value <= nowUtc)
                            ? "Active"
                            : "Scheduled"
                        : "Expired",
                    TargetingJson = c.TargetingJson,
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
    }

    /// <summary>
    /// Creates a new business-scoped campaign.
    /// </summary>
    public sealed class CreateBusinessCampaignHandler
    {
        private readonly IAppDbContext _db;

        public CreateBusinessCampaignHandler(IAppDbContext db) => _db = db;

        public async Task<Result<Guid>> HandleAsync(CreateBusinessCampaignDto dto, CancellationToken ct = default)
        {
            if (dto.BusinessId == Guid.Empty)
            {
                return Result<Guid>.Fail("BusinessId is required.");
            }

            if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Title))
            {
                return Result<Guid>.Fail("Name and Title are required.");
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
                TargetingJson = string.IsNullOrWhiteSpace(dto.TargetingJson) ? "{}" : dto.TargetingJson,
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

        public UpdateBusinessCampaignHandler(IAppDbContext db) => _db = db;

        public async Task<Result> HandleAsync(UpdateBusinessCampaignDto dto, CancellationToken ct = default)
        {
            if (dto.BusinessId == Guid.Empty || dto.Id == Guid.Empty)
            {
                return Result.Fail("BusinessId and campaign Id are required.");
            }

            var entity = await _db.Set<Campaign>()
                .SingleOrDefaultAsync(c => !c.IsDeleted && c.Id == dto.Id && c.BusinessId == dto.BusinessId, ct)
                .ConfigureAwait(false);

            if (entity is null)
            {
                return Result.Fail("Campaign not found.");
            }

            _db.Entry(entity).Property(x => x.RowVersion).OriginalValue = dto.RowVersion;

            entity.Name = dto.Name.Trim();
            entity.Title = dto.Title.Trim();
            entity.Subtitle = dto.Subtitle?.Trim();
            entity.Body = dto.Body?.Trim();
            entity.MediaUrl = dto.MediaUrl?.Trim();
            entity.LandingUrl = dto.LandingUrl?.Trim();
            entity.Channels = (Domain.Enums.CampaignChannels)dto.Channels;
            entity.StartsAtUtc = dto.StartsAtUtc;
            entity.EndsAtUtc = dto.EndsAtUtc;
            entity.TargetingJson = string.IsNullOrWhiteSpace(dto.TargetingJson) ? "{}" : dto.TargetingJson;
            entity.PayloadJson = string.IsNullOrWhiteSpace(dto.PayloadJson) ? "{}" : dto.PayloadJson;

            try
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
                return Result.Ok();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Fail("Campaign was updated by another user. Refresh and retry.");
            }
        }
    }

    /// <summary>
    /// Activates or deactivates a campaign.
    /// </summary>
    public sealed class SetCampaignActivationHandler
    {
        private readonly IAppDbContext _db;

        public SetCampaignActivationHandler(IAppDbContext db) => _db = db;

        public async Task<Result> HandleAsync(SetCampaignActivationDto dto, CancellationToken ct = default)
        {
            if (dto.BusinessId == Guid.Empty || dto.Id == Guid.Empty)
            {
                return Result.Fail("BusinessId and campaign Id are required.");
            }

            var entity = await _db.Set<Campaign>()
                .SingleOrDefaultAsync(c => !c.IsDeleted && c.Id == dto.Id && c.BusinessId == dto.BusinessId, ct)
                .ConfigureAwait(false);

            if (entity is null)
            {
                return Result.Fail("Campaign not found.");
            }

            _db.Entry(entity).Property(x => x.RowVersion).OriginalValue = dto.RowVersion;
            entity.IsActive = dto.IsActive;

            try
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
                return Result.Ok();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Fail("Campaign was updated by another user. Refresh and retry.");
            }
        }
    }
}
