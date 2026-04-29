using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application;
using Darwin.Application.Businesses;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Loyalty;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Loyalty.Queries;

/// <summary>
/// Returns an aggregated loyalty overview spanning all businesses for the current authenticated member.
/// </summary>
public sealed class GetMyLoyaltyOverviewHandler
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUserService;
    private readonly IStringLocalizer<ValidationResource> _localizer;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetMyLoyaltyOverviewHandler"/> class.
    /// </summary>
    public GetMyLoyaltyOverviewHandler(IAppDbContext db, ICurrentUserService currentUserService, IStringLocalizer<ValidationResource> localizer)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    }

    /// <summary>
    /// Loads the aggregated loyalty overview for the current member.
    /// </summary>
    public Task<Result<MyLoyaltyOverviewDto>> HandleAsync(CancellationToken ct = default)
    {
        return HandleAsync(culture: null, ct);
    }

    /// <summary>
    /// Loads the aggregated loyalty overview for the current member with localized business names when available.
    /// </summary>
    public async Task<Result<MyLoyaltyOverviewDto>> HandleAsync(string? culture = null, CancellationToken ct = default)
    {
        var userId = _currentUserService.GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Result<MyLoyaltyOverviewDto>.Fail(_localizer["Unauthorized"]);
        }

        var accounts = await (
            from account in _db.Set<LoyaltyAccount>().AsNoTracking()
            join business in _db.Set<Business>().AsNoTracking() on account.BusinessId equals business.Id
            where account.UserId == userId && !account.IsDeleted && !business.IsDeleted
            orderby account.PointsBalance descending, business.Name
            select new LoyaltyAccountSummaryDto
            {
                Id = account.Id,
                BusinessId = business.Id,
                BusinessName = business.Name,
                BusinessAdminTextOverridesJson = business.AdminTextOverridesJson,
                BusinessDefaultCulture = business.DefaultCulture,
                PointsBalance = account.PointsBalance,
                LifetimePoints = account.LifetimePoints,
                Status = account.Status,
                LastAccrualAtUtc = account.LastAccrualAtUtc
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var thresholdsByBusiness = await LoyaltyRewardProgressProjection
            .LoadThresholdsByBusinessAsync(_db, accounts.Select(x => x.BusinessId), ct)
            .ConfigureAwait(false);

        foreach (var account in accounts)
        {
            account.BusinessName = BusinessPublicTextResolver.ResolveName(
                account.BusinessName ?? string.Empty,
                account.BusinessAdminTextOverridesJson,
                culture,
                account.BusinessDefaultCulture);

            LoyaltyRewardProgressProjection.ApplyToAccount(
                account,
                thresholdsByBusiness.TryGetValue(account.BusinessId, out var thresholds)
                    ? thresholds
                    : Array.Empty<LoyaltyRewardProgressProjection.RewardThreshold>());
        }

        return Result<MyLoyaltyOverviewDto>.Ok(new MyLoyaltyOverviewDto
        {
            TotalAccounts = accounts.Count,
            ActiveAccounts = accounts.Count(x => x.Status == LoyaltyAccountStatus.Active),
            TotalPointsBalance = accounts.Sum(x => x.PointsBalance),
            TotalLifetimePoints = accounts.Sum(x => x.LifetimePoints),
            LastAccrualAtUtc = accounts
                .Where(x => x.LastAccrualAtUtc.HasValue)
                .Select(x => x.LastAccrualAtUtc)
                .DefaultIfEmpty()
                .Max(),
            Accounts = accounts
        });
    }
}

/// <summary>
/// Returns a business-scoped loyalty dashboard for the current authenticated member.
/// </summary>
public sealed class GetMyLoyaltyBusinessDashboardHandler
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUserService;
    private readonly IStringLocalizer<ValidationResource> _localizer;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetMyLoyaltyBusinessDashboardHandler"/> class.
    /// </summary>
    public GetMyLoyaltyBusinessDashboardHandler(IAppDbContext db, ICurrentUserService currentUserService, IStringLocalizer<ValidationResource> localizer)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    }

    /// <summary>
    /// Loads the current member's dashboard for a single business loyalty context.
    /// </summary>
    public Task<Result<MyLoyaltyBusinessDashboardDto?>> HandleAsync(Guid businessId, CancellationToken ct = default)
    {
        return HandleAsync(businessId, culture: null, ct);
    }

    /// <summary>
    /// Loads the current member's dashboard with localized business name when available.
    /// </summary>
    public async Task<Result<MyLoyaltyBusinessDashboardDto?>> HandleAsync(Guid businessId, string? culture = null, CancellationToken ct = default)
    {
        if (businessId == Guid.Empty)
        {
            return Result<MyLoyaltyBusinessDashboardDto?>.Fail(_localizer["BusinessIdRequired"]);
        }

        var userId = _currentUserService.GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Result<MyLoyaltyBusinessDashboardDto?>.Fail(_localizer["Unauthorized"]);
        }

        var account = await (
            from loyaltyAccount in _db.Set<LoyaltyAccount>().AsNoTracking()
            join business in _db.Set<Business>().AsNoTracking() on loyaltyAccount.BusinessId equals business.Id
            where loyaltyAccount.BusinessId == businessId
                  && loyaltyAccount.UserId == userId
                  && !loyaltyAccount.IsDeleted
                  && !business.IsDeleted
            select new LoyaltyAccountSummaryDto
            {
                Id = loyaltyAccount.Id,
                BusinessId = business.Id,
                BusinessName = business.Name,
                BusinessAdminTextOverridesJson = business.AdminTextOverridesJson,
                BusinessDefaultCulture = business.DefaultCulture,
                PointsBalance = loyaltyAccount.PointsBalance,
                LifetimePoints = loyaltyAccount.LifetimePoints,
                Status = loyaltyAccount.Status,
                LastAccrualAtUtc = loyaltyAccount.LastAccrualAtUtc
            })
            .SingleOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (account is null)
        {
            return Result<MyLoyaltyBusinessDashboardDto?>.Ok(null);
        }

        account.BusinessName = BusinessPublicTextResolver.ResolveName(
            account.BusinessName ?? string.Empty,
            account.BusinessAdminTextOverridesJson,
            culture,
            account.BusinessDefaultCulture);

        var program = await _db.Set<LoyaltyProgram>()
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.BusinessId == businessId && !x.IsDeleted && x.IsActive, ct)
            .ConfigureAwait(false);

        var rewards = new List<LoyaltyRewardSummaryDto>();
        if (program is not null)
        {
            var tierRows = await _db.Set<LoyaltyRewardTier>()
                .AsNoTracking()
                .Where(x => x.LoyaltyProgramId == program.Id && !x.IsDeleted)
                .OrderBy(x => x.PointsRequired)
                .Select(x => new
                {
                    x.Id,
                    x.Description,
                    x.MetadataJson,
                    x.PointsRequired,
                    x.AllowSelfRedemption
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            rewards = tierRows
                .Select(x =>
                {
                    var description = LoyaltyLocalizedTextResolver.Resolve(
                        x.MetadataJson,
                        culture,
                        "description",
                        x.Description ?? program.Name);

                    return new LoyaltyRewardSummaryDto
                    {
                        LoyaltyRewardTierId = x.Id,
                        BusinessId = businessId,
                        Name = description,
                        Description = description,
                        RequiredPoints = x.PointsRequired,
                        IsActive = program.IsActive,
                        RequiresConfirmation = !x.AllowSelfRedemption,
                        IsSelectable = account.Status == LoyaltyAccountStatus.Active && account.PointsBalance >= x.PointsRequired
                    };
                })
                .ToList();
        }

        var transactions = await _db.Set<LoyaltyPointsTransaction>()
            .AsNoTracking()
            .Where(x => x.BusinessId == businessId && x.LoyaltyAccountId == account.Id && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(5)
            .Select(x => new LoyaltyPointsTransactionDto
            {
                Id = x.Id,
                LoyaltyAccountId = x.LoyaltyAccountId,
                Type = x.Type,
                PointsDelta = x.PointsDelta,
                CreatedAtUtc = x.CreatedAtUtc,
                Reference = x.Reference,
                Notes = x.Notes,
                BusinessLocationId = x.BusinessLocationId,
                RewardRedemptionId = x.RewardRedemptionId
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var nextReward = rewards
            .Where(x => x.RequiredPoints > account.PointsBalance && x.IsActive)
            .OrderBy(x => x.RequiredPoints)
            .FirstOrDefault();

        LoyaltyRewardProgressProjection.ApplyToAccount(
            account,
            rewards
                .Where(x => x.IsActive)
                .OrderBy(x => x.RequiredPoints)
                .Select(x => new LoyaltyRewardProgressProjection.RewardThreshold
                {
                    Name = x.Name,
                    RequiredPoints = x.RequiredPoints
                })
                .ToList());

        return Result<MyLoyaltyBusinessDashboardDto?>.Ok(new MyLoyaltyBusinessDashboardDto
        {
            Account = account,
            AvailableRewardsCount = rewards.Count,
            RedeemableRewardsCount = rewards.Count(x => x.IsSelectable),
            NextReward = nextReward,
            RecentTransactions = transactions,
            PointsToNextReward = account.PointsToNextReward,
            NextRewardRequiredPoints = account.NextRewardRequiredPoints,
            NextRewardProgressPercent = account.NextRewardProgressPercent,
            ExpiryTrackingEnabled = false,
            PointsExpiringSoon = 0,
            NextPointsExpiryAtUtc = null
        });
    }
}
