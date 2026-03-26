using System.Security.Claims;
using Darwin.Application.Orders.Queries;
using Darwin.Application.Orders.Commands;
using Darwin.Application.Orders.DTOs;
using Darwin.Contracts.Orders;
using Darwin.Contracts.Shipping;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Darwin.WebApi.Controllers.Public;

/// <summary>
/// Storefront checkout endpoints shared by anonymous visitors and authenticated members.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/public/checkout")]
public sealed class PublicCheckoutController : ApiControllerBase
{
    private readonly CreateStorefrontCheckoutIntentHandler _createStorefrontCheckoutIntentHandler;
    private readonly PlaceOrderFromCartHandler _placeOrderFromCartHandler;
    private readonly CreateStorefrontPaymentIntentHandler _createStorefrontPaymentIntentHandler;
    private readonly GetStorefrontOrderConfirmationHandler _getStorefrontOrderConfirmationHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="PublicCheckoutController"/> class.
    /// </summary>
    public PublicCheckoutController(
        CreateStorefrontCheckoutIntentHandler createStorefrontCheckoutIntentHandler,
        PlaceOrderFromCartHandler placeOrderFromCartHandler,
        CreateStorefrontPaymentIntentHandler createStorefrontPaymentIntentHandler,
        GetStorefrontOrderConfirmationHandler getStorefrontOrderConfirmationHandler)
    {
        _createStorefrontCheckoutIntentHandler = createStorefrontCheckoutIntentHandler ?? throw new ArgumentNullException(nameof(createStorefrontCheckoutIntentHandler));
        _placeOrderFromCartHandler = placeOrderFromCartHandler ?? throw new ArgumentNullException(nameof(placeOrderFromCartHandler));
        _createStorefrontPaymentIntentHandler = createStorefrontPaymentIntentHandler ?? throw new ArgumentNullException(nameof(createStorefrontPaymentIntentHandler));
        _getStorefrontOrderConfirmationHandler = getStorefrontOrderConfirmationHandler ?? throw new ArgumentNullException(nameof(getStorefrontOrderConfirmationHandler));
    }

    /// <summary>
    /// Builds a storefront checkout intent from the authoritative cart and shipping context.
    /// </summary>
    [HttpPost("intent")]
    [HttpPost("/api/v1/checkout/intent")]
    [ProducesResponseType(typeof(CreateCheckoutIntentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateIntentAsync([FromBody] CreateCheckoutIntentRequest? request, CancellationToken ct = default)
    {
        if (request is null)
        {
            return BadRequestProblem("Request body is required.");
        }

        try
        {
            var result = await _createStorefrontCheckoutIntentHandler.HandleAsync(new CreateStorefrontCheckoutIntentDto
            {
                CartId = request.CartId,
                UserId = GetCurrentUserId(User),
                ShippingAddressId = request.ShippingAddressId,
                ShippingAddress = request.ShippingAddress is null
                    ? null
                    : new CheckoutAddressDto
                    {
                        FullName = request.ShippingAddress.FullName,
                        Company = request.ShippingAddress.Company,
                        Street1 = request.ShippingAddress.Street1,
                        Street2 = request.ShippingAddress.Street2,
                        PostalCode = request.ShippingAddress.PostalCode,
                        City = request.ShippingAddress.City,
                        State = request.ShippingAddress.State,
                        CountryCode = request.ShippingAddress.CountryCode,
                        PhoneE164 = request.ShippingAddress.PhoneE164
                    },
                SelectedShippingMethodId = request.SelectedShippingMethodId
            }, ct).ConfigureAwait(false);

            return Ok(new CreateCheckoutIntentResponse
            {
                CartId = result.CartId,
                Currency = result.Currency,
                SubtotalNetMinor = result.SubtotalNetMinor,
                VatTotalMinor = result.VatTotalMinor,
                GrandTotalGrossMinor = result.GrandTotalGrossMinor,
                ShipmentMass = result.ShipmentMass,
                RequiresShipping = result.RequiresShipping,
                ShippingCountryCode = result.ShippingCountryCode,
                SelectedShippingMethodId = result.SelectedShippingMethodId,
                SelectedShippingTotalMinor = result.SelectedShippingTotalMinor,
                ShippingOptions = result.ShippingOptions.Select(MapShippingOption).ToList()
            });
        }
        catch (Exception ex) when (ex is InvalidOperationException || ex is FluentValidation.ValidationException)
        {
            return BadRequestProblem("Checkout intent could not be created.", ex.Message);
        }
    }

    /// <summary>
    /// Places an order from the current storefront cart.
    /// </summary>
    [HttpPost("orders")]
    [HttpPost("/api/v1/checkout/orders")]
    [ProducesResponseType(typeof(PlaceOrderFromCartResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PlaceOrderAsync([FromBody] PlaceOrderFromCartRequest? request, CancellationToken ct = default)
    {
        if (request is null)
        {
            return BadRequestProblem("Request body is required.");
        }

        try
        {
            var result = await _placeOrderFromCartHandler.HandleAsync(new PlaceOrderFromCartDto
            {
                CartId = request.CartId,
                UserId = GetCurrentUserId(User),
                BillingAddressId = request.BillingAddressId,
                ShippingAddressId = request.ShippingAddressId,
                SelectedShippingMethodId = request.SelectedShippingMethodId,
                BillingAddress = request.BillingAddress is null
                    ? null
                    : new CheckoutAddressDto
                    {
                        FullName = request.BillingAddress.FullName,
                        Company = request.BillingAddress.Company,
                        Street1 = request.BillingAddress.Street1,
                        Street2 = request.BillingAddress.Street2,
                        PostalCode = request.BillingAddress.PostalCode,
                        City = request.BillingAddress.City,
                        State = request.BillingAddress.State,
                        CountryCode = request.BillingAddress.CountryCode,
                        PhoneE164 = request.BillingAddress.PhoneE164
                    },
                ShippingAddress = request.ShippingAddress is null
                    ? null
                    : new CheckoutAddressDto
                    {
                        FullName = request.ShippingAddress.FullName,
                        Company = request.ShippingAddress.Company,
                        Street1 = request.ShippingAddress.Street1,
                        Street2 = request.ShippingAddress.Street2,
                        PostalCode = request.ShippingAddress.PostalCode,
                        City = request.ShippingAddress.City,
                        State = request.ShippingAddress.State,
                        CountryCode = request.ShippingAddress.CountryCode,
                        PhoneE164 = request.ShippingAddress.PhoneE164
                    },
                ShippingTotalMinor = request.ShippingTotalMinor,
                Culture = string.IsNullOrWhiteSpace(request.Culture) ? "de-DE" : request.Culture.Trim()
            }, ct).ConfigureAwait(false);

            return Ok(new PlaceOrderFromCartResponse
            {
                OrderId = result.OrderId,
                OrderNumber = result.OrderNumber,
                Currency = result.Currency,
                GrandTotalGrossMinor = result.GrandTotalGrossMinor,
                Status = result.Status.ToString()
            });
        }
        catch (Exception ex) when (ex is InvalidOperationException || ex is FluentValidation.ValidationException)
        {
            return BadRequestProblem("Order could not be placed.", ex.Message);
        }
    }

    /// <summary>
    /// Creates or reuses a storefront payment intent for an already placed order.
    /// </summary>
    [HttpPost("orders/{orderId:guid}/payment-intent")]
    [HttpPost("/api/v1/checkout/orders/{orderId:guid}/payment-intent")]
    [ProducesResponseType(typeof(CreateStorefrontPaymentIntentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreatePaymentIntentAsync(Guid orderId, [FromBody] CreateStorefrontPaymentIntentRequest? request, CancellationToken ct = default)
    {
        if (orderId == Guid.Empty)
        {
            return BadRequestProblem("OrderId must not be empty.");
        }

        try
        {
            var result = await _createStorefrontPaymentIntentHandler.HandleAsync(new CreateStorefrontPaymentIntentDto
            {
                OrderId = orderId,
                UserId = GetCurrentUserId(User),
                OrderNumber = request?.OrderNumber,
                Provider = string.IsNullOrWhiteSpace(request?.Provider) ? "DarwinCheckout" : request.Provider.Trim()
            }, ct).ConfigureAwait(false);

            return Ok(new CreateStorefrontPaymentIntentResponse
            {
                OrderId = result.OrderId,
                PaymentId = result.PaymentId,
                Provider = result.Provider,
                ProviderReference = result.ProviderReference,
                AmountMinor = result.AmountMinor,
                Currency = result.Currency,
                Status = result.Status.ToString(),
                ExpiresAtUtc = result.ExpiresAtUtc
            });
        }
        catch (InvalidOperationException ex) when (string.Equals(ex.Message, "Order not found.", StringComparison.Ordinal))
        {
            return NotFoundProblem(ex.Message);
        }
        catch (Exception ex) when (ex is InvalidOperationException || ex is FluentValidation.ValidationException)
        {
            return BadRequestProblem("Payment intent could not be created.", ex.Message);
        }
    }

    /// <summary>
    /// Returns the storefront confirmation view for a placed order.
    /// </summary>
    [HttpGet("orders/{orderId:guid}/confirmation")]
    [HttpGet("/api/v1/checkout/orders/{orderId:guid}/confirmation")]
    [ProducesResponseType(typeof(StorefrontOrderConfirmationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConfirmationAsync(Guid orderId, [FromQuery] string? orderNumber, CancellationToken ct = default)
    {
        if (orderId == Guid.Empty)
        {
            return BadRequestProblem("OrderId must not be empty.");
        }

        var confirmation = await _getStorefrontOrderConfirmationHandler.HandleAsync(new GetStorefrontOrderConfirmationDto
        {
            OrderId = orderId,
            UserId = GetCurrentUserId(User),
            OrderNumber = orderNumber
        }, ct).ConfigureAwait(false);

        if (confirmation is null)
        {
            return NotFoundProblem("Order confirmation not found.");
        }

        return Ok(new StorefrontOrderConfirmationResponse
        {
            OrderId = confirmation.OrderId,
            OrderNumber = confirmation.OrderNumber,
            Currency = confirmation.Currency,
            SubtotalNetMinor = confirmation.SubtotalNetMinor,
            TaxTotalMinor = confirmation.TaxTotalMinor,
            ShippingTotalMinor = confirmation.ShippingTotalMinor,
            ShippingMethodId = confirmation.ShippingMethodId,
            ShippingMethodName = confirmation.ShippingMethodName,
            ShippingCarrier = confirmation.ShippingCarrier,
            ShippingService = confirmation.ShippingService,
            DiscountTotalMinor = confirmation.DiscountTotalMinor,
            GrandTotalGrossMinor = confirmation.GrandTotalGrossMinor,
            Status = confirmation.Status.ToString(),
            BillingAddressJson = confirmation.BillingAddressJson,
            ShippingAddressJson = confirmation.ShippingAddressJson,
            CreatedAtUtc = confirmation.CreatedAtUtc,
            Lines = confirmation.Lines.Select(line => new StorefrontOrderConfirmationLine
            {
                Id = line.Id,
                VariantId = line.VariantId,
                Name = line.Name,
                Sku = line.Sku,
                Quantity = line.Quantity,
                UnitPriceGrossMinor = line.UnitPriceGrossMinor,
                LineGrossMinor = line.LineGrossMinor
            }).ToList(),
            Payments = confirmation.Payments.Select(payment => new StorefrontOrderConfirmationPayment
            {
                Id = payment.Id,
                Provider = payment.Provider,
                ProviderReference = payment.ProviderReference,
                AmountMinor = payment.AmountMinor,
                Currency = payment.Currency,
                Status = payment.Status.ToString(),
                PaidAtUtc = payment.PaidAtUtc
            }).ToList()
        });
    }

    private static Guid? GetCurrentUserId(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var candidate =
            user.FindFirstValue(ClaimTypes.NameIdentifier) ??
            user.FindFirstValue("sub") ??
            user.FindFirstValue("uid");

        return Guid.TryParse(candidate, out var userId) ? userId : null;
    }

    private static PublicShippingOption MapShippingOption(StorefrontShippingOptionDto dto)
        => new()
        {
            MethodId = dto.MethodId,
            Name = dto.Name,
            PriceMinor = dto.PriceMinor,
            Currency = dto.Currency,
            Carrier = dto.Carrier,
            Service = dto.Service
        };
}
