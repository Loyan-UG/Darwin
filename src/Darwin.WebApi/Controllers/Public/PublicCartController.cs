using Darwin.Application.CartCheckout.Commands;
using Darwin.Application.CartCheckout.DTOs;
using Darwin.Application.CartCheckout.Queries;
using Darwin.Contracts.Cart;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Darwin.WebApi.Controllers.Public;

/// <summary>
/// Storefront cart endpoints shared by anonymous visitors and authenticated members.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/public/cart")]
public sealed class PublicCartController : ApiControllerBase
{
    private readonly ComputeCartSummaryHandler _computeCartSummaryHandler;
    private readonly GetCartSummaryHandler _getCartSummaryHandler;
    private readonly AddOrIncreaseCartItemHandler _addOrIncreaseCartItemHandler;
    private readonly UpdateCartItemQuantityHandler _updateCartItemQuantityHandler;
    private readonly RemoveCartItemHandler _removeCartItemHandler;
    private readonly ApplyCouponHandler _applyCouponHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="PublicCartController"/> class.
    /// </summary>
    public PublicCartController(
        ComputeCartSummaryHandler computeCartSummaryHandler,
        GetCartSummaryHandler getCartSummaryHandler,
        AddOrIncreaseCartItemHandler addOrIncreaseCartItemHandler,
        UpdateCartItemQuantityHandler updateCartItemQuantityHandler,
        RemoveCartItemHandler removeCartItemHandler,
        ApplyCouponHandler applyCouponHandler)
    {
        _computeCartSummaryHandler = computeCartSummaryHandler ?? throw new ArgumentNullException(nameof(computeCartSummaryHandler));
        _getCartSummaryHandler = getCartSummaryHandler ?? throw new ArgumentNullException(nameof(getCartSummaryHandler));
        _addOrIncreaseCartItemHandler = addOrIncreaseCartItemHandler ?? throw new ArgumentNullException(nameof(addOrIncreaseCartItemHandler));
        _updateCartItemQuantityHandler = updateCartItemQuantityHandler ?? throw new ArgumentNullException(nameof(updateCartItemQuantityHandler));
        _removeCartItemHandler = removeCartItemHandler ?? throw new ArgumentNullException(nameof(removeCartItemHandler));
        _applyCouponHandler = applyCouponHandler ?? throw new ArgumentNullException(nameof(applyCouponHandler));
    }

    /// <summary>
    /// Returns the current storefront cart for the authenticated member or anonymous visitor.
    /// </summary>
    [HttpGet]
    [HttpGet("/api/v1/cart")]
    [ProducesResponseType(typeof(PublicCartSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAsync([FromQuery] string? anonymousId, CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var normalizedAnonymousId = NormalizeAnonymousId(anonymousId);
        if (userId is null && normalizedAnonymousId is null)
        {
            return BadRequestProblem("AnonymousId is required when no authenticated member token is present.");
        }

        var dto = await _getCartSummaryHandler.HandleAsync(userId, normalizedAnonymousId, ct).ConfigureAwait(false);
        return dto is null ? NotFoundProblem("Cart not found.") : Ok(MapSummary(dto));
    }

    /// <summary>
    /// Adds or increases a storefront cart line.
    /// </summary>
    [HttpPost("items")]
    [HttpPost("/api/v1/cart/items")]
    [ProducesResponseType(typeof(PublicCartSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddItemAsync([FromBody] PublicCartAddItemRequest? request, CancellationToken ct = default)
    {
        if (request is null)
        {
            return BadRequestProblem("Request body is required.");
        }

        var userId = GetCurrentUserId();
        var normalizedAnonymousId = NormalizeAnonymousId(request.AnonymousId);
        if (userId is null && normalizedAnonymousId is null)
        {
            return BadRequestProblem("AnonymousId is required when no authenticated member token is present.");
        }

        if (request.VariantId == Guid.Empty)
        {
            return BadRequestProblem("VariantId must not be empty.");
        }

        if (request.Quantity <= 0)
        {
            return BadRequestProblem("Quantity must be a positive integer.");
        }

        try
        {
            await _addOrIncreaseCartItemHandler.HandleAsync(new CartAddItemDto
            {
                UserId = userId,
                AnonymousId = normalizedAnonymousId,
                VariantId = request.VariantId,
                Quantity = request.Quantity,
                UnitPriceNetMinor = request.UnitPriceNetMinor,
                VatRate = request.VatRate,
                Currency = request.Currency,
                SelectedAddOnValueIds = request.SelectedAddOnValueIds.ToList()
            }, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is InvalidOperationException || ex is FluentValidation.ValidationException)
        {
            return BadRequestProblem("Cart item could not be added.", ex.Message);
        }

        var summary = await _getCartSummaryHandler.HandleAsync(userId, normalizedAnonymousId, ct).ConfigureAwait(false);
        return summary is null ? NotFoundProblem("Cart not found after mutation.") : Ok(MapSummary(summary));
    }

    /// <summary>
    /// Updates the quantity of an existing storefront cart line.
    /// </summary>
    [HttpPut("items")]
    [HttpPut("/api/v1/cart/items")]
    [ProducesResponseType(typeof(PublicCartSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateItemAsync([FromBody] PublicCartUpdateItemRequest? request, CancellationToken ct = default)
    {
        if (request is null)
        {
            return BadRequestProblem("Request body is required.");
        }

        if (request.CartId == Guid.Empty || request.VariantId == Guid.Empty)
        {
            return BadRequestProblem("CartId and VariantId must not be empty.");
        }

        try
        {
            await _updateCartItemQuantityHandler.HandleAsync(new CartUpdateQtyDto
            {
                CartId = request.CartId,
                VariantId = request.VariantId,
                Quantity = request.Quantity,
                SelectedAddOnValueIdsJson = request.SelectedAddOnValueIdsJson
            }, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is InvalidOperationException || ex is FluentValidation.ValidationException)
        {
            return BadRequestProblem("Cart item could not be updated.", ex.Message);
        }

        return await ReloadCartAsync(request.CartId, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Removes a storefront cart line.
    /// </summary>
    [HttpDelete("items")]
    [HttpDelete("/api/v1/cart/items")]
    [ProducesResponseType(typeof(PublicCartSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RemoveItemAsync([FromBody] PublicCartRemoveItemRequest? request, CancellationToken ct = default)
    {
        if (request is null)
        {
            return BadRequestProblem("Request body is required.");
        }

        if (request.CartId == Guid.Empty || request.VariantId == Guid.Empty)
        {
            return BadRequestProblem("CartId and VariantId must not be empty.");
        }

        await _removeCartItemHandler.HandleAsync(new CartRemoveItemDto
        {
            CartId = request.CartId,
            VariantId = request.VariantId,
            SelectedAddOnValueIdsJson = request.SelectedAddOnValueIdsJson
        }, ct).ConfigureAwait(false);

        return await ReloadCartAsync(request.CartId, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Applies or clears a storefront coupon code.
    /// </summary>
    [HttpPost("coupon")]
    [HttpPost("/api/v1/cart/coupon")]
    [ProducesResponseType(typeof(PublicCartSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ApplyCouponAsync([FromBody] PublicCartApplyCouponRequest? request, CancellationToken ct = default)
    {
        if (request is null)
        {
            return BadRequestProblem("Request body is required.");
        }

        if (request.CartId == Guid.Empty)
        {
            return BadRequestProblem("CartId must not be empty.");
        }

        try
        {
            await _applyCouponHandler.HandleAsync(new CartApplyCouponDto
            {
                CartId = request.CartId,
                CouponCode = request.CouponCode
            }, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is InvalidOperationException || ex is FluentValidation.ValidationException)
        {
            return BadRequestProblem("Coupon could not be applied.", ex.Message);
        }

        return await ReloadCartAsync(request.CartId, ct).ConfigureAwait(false);
    }

    private async Task<IActionResult> ReloadCartAsync(Guid cartId, CancellationToken ct)
    {
        try
        {
            var summary = await _computeCartSummaryHandler.HandleAsync(cartId, ct).ConfigureAwait(false);
            return Ok(MapSummary(summary));
        }
        catch (InvalidOperationException)
        {
            return NotFoundProblem("Cart not found.");
        }
    }

    private static string? NormalizeAnonymousId(string? anonymousId)
        => string.IsNullOrWhiteSpace(anonymousId) ? null : anonymousId.Trim();

    private static PublicCartSummary MapSummary(CartSummaryDto dto)
        => new()
        {
            CartId = dto.CartId,
            Currency = dto.Currency,
            SubtotalNetMinor = dto.SubtotalNetMinor,
            VatTotalMinor = dto.VatTotalMinor,
            GrandTotalGrossMinor = dto.GrandTotalGrossMinor,
            CouponCode = dto.CouponCode,
            Items = dto.Items.Select(item => new PublicCartItemRow
            {
                VariantId = item.VariantId,
                Quantity = item.Quantity,
                UnitPriceNetMinor = item.UnitPriceNetMinor,
                AddOnPriceDeltaMinor = item.AddOnPriceDeltaMinor,
                VatRate = item.VatRate,
                LineNetMinor = item.LineNetMinor,
                LineVatMinor = item.LineVatMinor,
                LineGrossMinor = item.LineGrossMinor,
                SelectedAddOnValueIdsJson = item.SelectedAddOnValueIdsJson
            }).ToList()
        };
}
