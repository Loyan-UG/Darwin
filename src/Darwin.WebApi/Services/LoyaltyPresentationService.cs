using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Loyalty.Queries;
using Darwin.Contracts.Loyalty;
using Darwin.Shared.Results;
using Darwin.WebApi.Mappers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Darwin.WebApi.Services
{
    /// <summary>
    /// Default implementation of ILoyaltyPresentationService.
    /// 
    /// Behaviour & design notes:
    /// - Uses IMemoryCache to cache the list of available rewards per business for a short TTL.
    /// - Respects failIfMissing policy: when set, missing requested tier ids cause a failure Result.
    /// - Preserves the order of requested ids in the returned list.
    /// - Maps application DTOs to Darwin.Contracts types using LoyaltyContractsMapper.
    /// </summary>
    public sealed class LoyaltyPresentationService : ILoyaltyPresentationService
    {
        private readonly GetAvailableLoyaltyRewardsForBusinessHandler _availableRewardsHandler;
        private readonly IMemoryCache _cache;
        private readonly ILogger<LoyaltyPresentationService> _logger;

        // Cache TTL in seconds (short to avoid stale UI for admin changes).
        private const int DefaultAvailableRewardsCacheSeconds = 60;

        public LoyaltyPresentationService(
            GetAvailableLoyaltyRewardsForBusinessHandler availableRewardsHandler,
            IMemoryCache cache,
            ILogger<LoyaltyPresentationService> logger)
        {
            _availableRewardsHandler = availableRewardsHandler ?? throw new ArgumentNullException(nameof(availableRewardsHandler));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<Result<IReadOnlyList<LoyaltyRewardSummary>>> EnrichSelectedRewardsAsync(
            Guid businessId,
            IReadOnlyCollection<Guid>? selectedTierIds,
            bool failIfMissing,
            CancellationToken ct = default)
        {
            if (selectedTierIds is null || selectedTierIds.Count == 0)
            {
                return Result<IReadOnlyList<LoyaltyRewardSummary>>.Ok(Array.Empty<LoyaltyRewardSummary>());
            }

            // Normalize (remove empty GUIDs, preserve order and uniqueness)
            var orderedDistinct = selectedTierIds
                .Where(x => x != Guid.Empty)
                .Distinct()
                .ToList();

            if (orderedDistinct.Count == 0)
            {
                return Result<IReadOnlyList<LoyaltyRewardSummary>>.Ok(Array.Empty<LoyaltyRewardSummary>());
            }

            // Get available rewards (possibly from cache)
            var availableResult = await GetAvailableRewardsForBusinessAsync(businessId, ct).ConfigureAwait(false);
            if (!availableResult.Succeeded || availableResult.Value is null)
            {
                _logger.LogWarning("Failed to load available rewards for business {BusinessId}. failIfMissing={FailIfMissing}", businessId, failIfMissing);
                if (failIfMissing)
                {
                    return Result<IReadOnlyList<LoyaltyRewardSummary>>.Fail("Could not load business rewards for enrichment.");
                }

                return Result<IReadOnlyList<LoyaltyRewardSummary>>.Ok(Array.Empty<LoyaltyRewardSummary>());
            }

            var available = availableResult.Value;
            // Build lookup by tier id for fast mapping
            var dict = available.ToDictionary(r => r.LoyaltyRewardTierId);

            var missing = new List<Guid>();
            var resultList = new List<LoyaltyRewardSummary>(orderedDistinct.Count);

            foreach (var id in orderedDistinct)
            {
                if (dict.TryGetValue(id, out var rewardDto))
                {
                    resultList.Add(LoyaltyContractsMapper.ToContract(rewardDto));
                }
                else
                {
                    missing.Add(id);
                }
            }

            if (missing.Count > 0 && failIfMissing)
            {
                _logger.LogWarning("Missing {MissingCount} selected reward(s) for business {BusinessId}. MissingExample={MissingExample}", missing.Count, businessId, missing.FirstOrDefault());
                return Result<IReadOnlyList<LoyaltyRewardSummary>>.Fail("Some selected rewards are not available for this business.");
            }

            return Result<IReadOnlyList<LoyaltyRewardSummary>>.Ok(resultList);
        }

        /// <inheritdoc />
        public async Task<Result<IReadOnlyList<LoyaltyRewardSummary>>> GetAvailableRewardsForBusinessAsync(
            Guid businessId,
            CancellationToken ct = default)
        {
            if (businessId == Guid.Empty)
            {
                return Result<IReadOnlyList<LoyaltyRewardSummary>>.Fail("BusinessId is required.");
            }

            var cacheKey = GetCacheKey(businessId);

            if (_cache.TryGetValue(cacheKey, out IReadOnlyList<LoyaltyRewardSummary>? cached))
            {
                return Result<IReadOnlyList<LoyaltyRewardSummary>>.Ok(cached);
            }

            var handlerResult = await _availableRewardsHandler.HandleAsync(businessId, ct).ConfigureAwait(false);
            if (!handlerResult.Succeeded || handlerResult.Value is null)
            {
                _logger.LogWarning("GetAvailableLoyaltyRewardsForBusinessHandler failed for business {BusinessId}", businessId);
                return Result<IReadOnlyList<LoyaltyRewardSummary>>.Fail(handlerResult.Error ?? "Failed to load available rewards.");
            }

            var mapped = handlerResult.Value.Select(LoyaltyContractsMapper.ToContract).ToList().AsReadOnly();

            // Cache mapped results for a short interval to reduce DB pressure on hot businesses.
            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(DefaultAvailableRewardsCacheSeconds)
            };

            _cache.Set(cacheKey, mapped, cacheEntryOptions);

            return Result<IReadOnlyList<LoyaltyRewardSummary>>.Ok(mapped);
        }

        private static string GetCacheKey(Guid businessId) => $"loyalty:availableRewards:{businessId:N}";
    }
}