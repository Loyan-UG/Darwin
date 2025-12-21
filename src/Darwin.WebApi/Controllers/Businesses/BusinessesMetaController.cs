using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Businesses.Queries;
using Darwin.Contracts.Businesses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Darwin.WebApi.Controllers.Businesses
{
    /// <summary>
    /// Provides meta endpoints for business discovery UIs (static or deterministic datasets).
    /// These endpoints are designed for mobile and web clients that need stable, server-driven
    /// reference data (e.g., category filter options) without hardcoding server enums.
    /// </summary>
    [ApiController]
    [Route("api/v1/businesses")]
    [Authorize]
    public sealed class BusinessesMetaController : ApiControllerBase
    {
        private readonly GetBusinessCategoryKindsHandler _getBusinessCategoryKindsHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="BusinessesMetaController"/> class.
        /// </summary>
        /// <param name="getBusinessCategoryKindsHandler">
        /// Application query handler that returns a deterministic list of business category kinds.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="getBusinessCategoryKindsHandler"/> is null.</exception>
        public BusinessesMetaController(GetBusinessCategoryKindsHandler getBusinessCategoryKindsHandler)
        {
            _getBusinessCategoryKindsHandler = getBusinessCategoryKindsHandler
                ?? throw new ArgumentNullException(nameof(getBusinessCategoryKindsHandler));
        }

        /// <summary>
        /// Returns the full list of business category kinds for UI filters.
        /// Notes:
        /// - This endpoint is safe to expose publicly (no user data).
        /// - Response shape is contract-first and stable for mobile parsing.
        /// - The Application DTO uses <c>Kind</c> (enum) + <c>Value</c> (short) + <c>DisplayName</c>.
        ///   The API contract additionally provides <c>Key</c> as a stable string token for clients.
        /// </summary>
        /// <param name="ct">Cancellation token propagated from the HTTP request.</param>
        /// <returns>A 200 OK with a stable list of category kinds.</returns>
        [HttpGet("category-kinds")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(BusinessCategoryKindsResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCategoryKinds(CancellationToken ct)
        {
            // The Application handler is deterministic and DB-free.
            // It returns a DTO with Items guaranteed non-null (init to Array.Empty<...>).
            var dto = await _getBusinessCategoryKindsHandler.HandleAsync(ct);

            // Defensive programming:
            // Although the DTO contract says Items is never null, we still guard to prevent
            // accidental null propagation if the Application layer changes in the future.
            var items = dto.Items ?? Array.Empty<Darwin.Application.Businesses.DTOs.BusinessCategoryKindItemDto>();

            var response = new BusinessCategoryKindsResponse
            {
                // Map Application DTO -> Contracts DTO (contract-first).
                // Application DTO DOES NOT have "Key". It has "Kind" (enum).
                // For the contract's "Key" we use a deterministic enum name token.
                Items = items
                    .Select(x => new BusinessCategoryKindItem
                    {
                        // Value is short in Application; contract uses int for easier client handling.
                        Value = x.Value,

                        // Stable key token for clients. This must remain deterministic across versions.
                        // We intentionally use enum name (not localized) so the client can store it safely.
                        Key = x.Kind.ToString(),

                        // Human-readable fallback label (English) provided by Application.
                        DisplayName = x.DisplayName ?? string.Empty
                    })
                    .ToList()
            };

            return Ok(response);
        }
    }
}
