using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Mobile.Shared.Security;
using Darwin.Mobile.Shared.Services;
using Darwin.Shared.Results;

namespace Darwin.Mobile.Business.Services.Identity
{
    /// <summary>
    /// Provides a compact business/operator context for the Business app home screen.
    /// 
    /// Why this service exists:
    /// - Operators and testers must immediately know which business account is currently active.
    /// - The business id is authoritative in JWT claim ("business_id"), so we derive identity context from token first.
    /// - We enrich that claim with business detail from API for a better UX (name/category/city/description).
    /// 
    /// Security posture:
    /// - business_id is read from JWT claims issued by backend.
    /// - We never accept business id from user input here.
    /// </summary>
    public interface IBusinessIdentityContextService
    {
        /// <summary>
        /// Resolves the current business/operator context from token + API.
        /// </summary>
        Task<Result<BusinessIdentityContext>> GetCurrentAsync(CancellationToken ct);
    }

    /// <summary>
    /// UI-ready model for business identity summary shown in the Business app.
    /// </summary>
    public sealed class BusinessIdentityContext
    {
        public Guid BusinessId { get; init; }
        public string OperatorEmail { get; init; } = string.Empty;
        public string BusinessName { get; init; } = string.Empty;
        public string? Category { get; init; }
        public string? City { get; init; }
        public string? ShortDescription { get; init; }
    }

    public sealed class BusinessIdentityContextService : IBusinessIdentityContextService
    {
        private readonly ITokenStore _tokenStore;
        private readonly IBusinessService _businessService;

        public BusinessIdentityContextService(
            ITokenStore tokenStore,
            IBusinessService businessService)
        {
            _tokenStore = tokenStore ?? throw new ArgumentNullException(nameof(tokenStore));
            _businessService = businessService ?? throw new ArgumentNullException(nameof(businessService));
        }

        /// <inheritdoc />
        public async Task<Result<BusinessIdentityContext>> GetCurrentAsync(CancellationToken ct)
        {
            var (accessToken, _) = await _tokenStore.GetAccessAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return Result<BusinessIdentityContext>.Fail("No active access token found.");
            }

            if (!TryReadBusinessIdentityFromToken(accessToken, out var businessId, out var operatorEmail, out var parseError))
            {
                return Result<BusinessIdentityContext>.Fail(parseError ?? "Token does not contain a valid business context.");
            }

            // Best-effort enrichment from business detail endpoint.
            // Even if this call fails, we still return a minimal context so UX stays useful.
            var detail = await _businessService.GetAsync(businessId, ct).ConfigureAwait(false);

            var context = new BusinessIdentityContext
            {
                BusinessId = businessId,
                OperatorEmail = operatorEmail,
                BusinessName = !string.IsNullOrWhiteSpace(detail?.Name) ? detail!.Name : $"Business {businessId:D}",
                Category = detail?.Category,
                City = detail?.City,
                ShortDescription = detail?.ShortDescription
            };

            return Result<BusinessIdentityContext>.Ok(context);
        }

        private static bool TryReadBusinessIdentityFromToken(
            string accessToken,
            out Guid businessId,
            out string operatorEmail,
            out string? error)
        {
            businessId = Guid.Empty;
            operatorEmail = string.Empty;
            error = null;

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(accessToken);

                var businessClaim = jwt.Claims.FirstOrDefault(c =>
                    string.Equals(c.Type, "business_id", StringComparison.OrdinalIgnoreCase));

                if (businessClaim is null || !Guid.TryParse(businessClaim.Value, out businessId) || businessId == Guid.Empty)
                {
                    error = "Missing or invalid business_id claim.";
                    return false;
                }

                operatorEmail = jwt.Claims.FirstOrDefault(c =>
                        string.Equals(c.Type, JwtRegisteredClaimNames.Email, StringComparison.OrdinalIgnoreCase))?.Value
                    ?? jwt.Claims.FirstOrDefault(c =>
                        string.Equals(c.Type, "email", StringComparison.OrdinalIgnoreCase))?.Value
                    ?? jwt.Claims.FirstOrDefault(c =>
                        string.Equals(c.Type, JwtRegisteredClaimNames.Sub, StringComparison.OrdinalIgnoreCase))?.Value
                    ?? "unknown@operator";

                return true;
            }
            catch (Exception ex)
            {
                error = $"Failed to parse access token: {ex.Message}";
                return false;
            }
        }
    }
}
