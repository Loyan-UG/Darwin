using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Businesses.DTOs;
using Darwin.Application.Loyalty.Queries;
using Darwin.Shared.Results;

namespace Darwin.Application.Businesses.Queries
{
    /// <summary>
    /// Loads a public business detail view and also retrieves the current user's loyalty account summary for that business.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This handler is intended for consumer/mobile "business detail" screens that also show
    /// the user's points/account status in that business.
    /// </para>
    /// <para>
    /// Business visibility rules are delegated to <see cref="GetBusinessPublicDetailHandler"/>.
    /// Loyalty account lookup is delegated to <see cref="GetMyLoyaltyAccountForBusinessHandler"/>.
    /// </para>
    /// </remarks>
    public sealed class GetBusinessPublicDetailWithMyAccountHandler
    {
        private readonly GetBusinessPublicDetailHandler _businessPublicDetailHandler;
        private readonly GetMyLoyaltyAccountForBusinessHandler _myAccountForBusinessHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetBusinessPublicDetailWithMyAccountHandler"/> class.
        /// </summary>
        public GetBusinessPublicDetailWithMyAccountHandler(
            GetBusinessPublicDetailHandler businessPublicDetailHandler,
            GetMyLoyaltyAccountForBusinessHandler myAccountForBusinessHandler)
        {
            _businessPublicDetailHandler = businessPublicDetailHandler ?? throw new ArgumentNullException(nameof(businessPublicDetailHandler));
            _myAccountForBusinessHandler = myAccountForBusinessHandler ?? throw new ArgumentNullException(nameof(myAccountForBusinessHandler));
        }

        /// <summary>
        /// Returns a combined model that contains:
        /// <list type="bullet">
        /// <item><description>Public business details (or null if not found / inactive).</description></item>
        /// <item><description>The current user's loyalty account summary for the business (nullable).</description></item>
        /// </list>
        /// </summary>
        /// <param name="businessId">The public business identifier.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A <see cref="Result{T}"/> that contains a payload on success; the payload can be null when the business is not visible.
        /// </returns>
        public async Task<Result<BusinessPublicDetailWithMyAccountDto?>> HandleAsync(
            Guid businessId,
            CancellationToken ct = default)
        {
            if (businessId == Guid.Empty)
            {
                // Treat empty as "not found" rather than throwing; WebApi can translate to 404.
                return Result<BusinessPublicDetailWithMyAccountDto?>.Ok(null);
            }

            // 1) Public business detail (includes active checks).
            var business = await _businessPublicDetailHandler
                .HandleAsync(businessId, ct)
                .ConfigureAwait(false);

            if (business is null)
            {
                // Business not found or not active/visible.
                return Result<BusinessPublicDetailWithMyAccountDto?>.Ok(null);
            }

            // 2) My loyalty account summary (Result-wrapped, and DTO can be null if user has no account yet).
            var myAccountResult = await _myAccountForBusinessHandler
                .HandleAsync(businessId, ct)
                .ConfigureAwait(false);

            if (!myAccountResult.Succeeded)
            {
                // Propagate the semantic error to WebApi. This keeps the boundary consistent.
                return Result<BusinessPublicDetailWithMyAccountDto?>.Fail(myAccountResult.Error ?? "Failed to load loyalty account.");
            }

            var dto = new BusinessPublicDetailWithMyAccountDto
            {
                Business = business,
                MyAccount = myAccountResult.Value,
                HasAccount = myAccountResult.Value is not null
            };

            return Result<BusinessPublicDetailWithMyAccountDto?>.Ok(dto);
        }
    }
}
