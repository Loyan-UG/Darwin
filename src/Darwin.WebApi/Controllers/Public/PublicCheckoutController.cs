using Darwin.Application;
using Darwin.Application.Orders.Queries;
using Darwin.Application.Orders.Commands;
using Darwin.Application.Orders.DTOs;
using Darwin.Application.Settings.DTOs;
using Darwin.Contracts.Orders;
using Darwin.Contracts.Shipping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;
using Darwin.WebApi.Services;

namespace Darwin.WebApi.Controllers.Public;

/// <summary>
/// Storefront checkout endpoints shared by anonymous visitors and authenticated members.
/// </summary>
[ApiController]
[AllowAnonymous]
[EnableRateLimiting("public-storefront")]
[RequestTimeout("public-storefront")]
[Route("api/v1/public/checkout")]
public sealed class PublicCheckoutController : ApiControllerBase
{
    private const int MaxCheckoutRequestBytes = 32 * 1024;
    private const int MaxOrderNumberLength = 64;
    private const int MaxProviderLength = 64;
    private const int MaxProviderReferenceLength = 256;
    private const int MaxFailureReasonLength = 512;
    private const int MaxCultureLength = 16;
    private const int MaxAddressFullNameLength = 200;
    private const int MaxAddressCompanyLength = 200;
    private const int MaxAddressStreetLength = 300;
    private const int MaxAddressPostalCodeLength = 32;
    private const int MaxAddressCityLength = 150;
    private const int MaxAddressStateLength = 150;
    private const int MaxAddressCountryCodeLength = 2;
    private const int MaxAddressPhoneLength = 20;

    private readonly CreateStorefrontCheckoutIntentHandler _createStorefrontCheckoutIntentHandler;
    private readonly PlaceOrderFromCartHandler _placeOrderFromCartHandler;
    private readonly CreateStorefrontPaymentIntentHandler _createStorefrontPaymentIntentHandler;
    private readonly CompleteStorefrontPaymentHandler _completeStorefrontPaymentHandler;
    private readonly GetStorefrontOrderConfirmationHandler _getStorefrontOrderConfirmationHandler;
    private readonly StorefrontCheckoutUrlBuilder _checkoutUrlBuilder;
    private readonly IStringLocalizer<ValidationResource> _validationLocalizer;

    /// <summary>
    /// Initializes a new instance of the <see cref="PublicCheckoutController"/> class.
    /// </summary>
    public PublicCheckoutController(
        CreateStorefrontCheckoutIntentHandler createStorefrontCheckoutIntentHandler,
        PlaceOrderFromCartHandler placeOrderFromCartHandler,
        CreateStorefrontPaymentIntentHandler createStorefrontPaymentIntentHandler,
        CompleteStorefrontPaymentHandler completeStorefrontPaymentHandler,
        GetStorefrontOrderConfirmationHandler getStorefrontOrderConfirmationHandler,
        StorefrontCheckoutUrlBuilder checkoutUrlBuilder,
        IStringLocalizer<ValidationResource> validationLocalizer)
    {
        _createStorefrontCheckoutIntentHandler = createStorefrontCheckoutIntentHandler ?? throw new ArgumentNullException(nameof(createStorefrontCheckoutIntentHandler));
        _placeOrderFromCartHandler = placeOrderFromCartHandler ?? throw new ArgumentNullException(nameof(placeOrderFromCartHandler));
        _createStorefrontPaymentIntentHandler = createStorefrontPaymentIntentHandler ?? throw new ArgumentNullException(nameof(createStorefrontPaymentIntentHandler));
        _completeStorefrontPaymentHandler = completeStorefrontPaymentHandler ?? throw new ArgumentNullException(nameof(completeStorefrontPaymentHandler));
        _getStorefrontOrderConfirmationHandler = getStorefrontOrderConfirmationHandler ?? throw new ArgumentNullException(nameof(getStorefrontOrderConfirmationHandler));
        _checkoutUrlBuilder = checkoutUrlBuilder ?? throw new ArgumentNullException(nameof(checkoutUrlBuilder));
        _validationLocalizer = validationLocalizer ?? throw new ArgumentNullException(nameof(validationLocalizer));
    }

    /// <summary>
    /// Builds a storefront checkout intent from the authoritative cart and shipping context.
    /// </summary>
    [HttpPost("intent")]
    [HttpPost("/api/v1/checkout/intent")]
    [RequestSizeLimit(MaxCheckoutRequestBytes)]
    [ProducesResponseType(typeof(CreateCheckoutIntentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateIntentAsync([FromBody] CreateCheckoutIntentRequest? request, CancellationToken ct = default)
    {
        if (request is null)
        {
            return BadRequestProblem(_validationLocalizer["RequestPayloadRequired"]);
        }

        var addressValidation = ValidateAddressBounds(request.ShippingAddress);
        if (addressValidation is not null)
        {
            return BadRequestProblem(_validationLocalizer[addressValidation]);
        }

        try
        {
            var result = await _createStorefrontCheckoutIntentHandler.HandleAsync(new CreateStorefrontCheckoutIntentDto
            {
                CartId = request.CartId,
                UserId = GetCurrentUserId(),
                ShippingAddressId = request.ShippingAddressId,
                ShippingAddress = MapAddress(request.ShippingAddress),
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
            return BadRequestProblem(_validationLocalizer["CheckoutIntentCreationFailed"]);
        }
    }

    /// <summary>
    /// Places an order from the current storefront cart.
    /// </summary>
    [HttpPost("orders")]
    [HttpPost("/api/v1/checkout/orders")]
    [RequestSizeLimit(MaxCheckoutRequestBytes)]
    [ProducesResponseType(typeof(PlaceOrderFromCartResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PlaceOrderAsync([FromBody] PlaceOrderFromCartRequest? request, CancellationToken ct = default)
    {
        if (request is null)
        {
            return BadRequestProblem(_validationLocalizer["RequestPayloadRequired"]);
        }

        var billingAddressValidation = ValidateAddressBounds(request.BillingAddress);
        var shippingAddressValidation = ValidateAddressBounds(request.ShippingAddress);
        if (billingAddressValidation is not null || shippingAddressValidation is not null)
        {
            return BadRequestProblem(_validationLocalizer[billingAddressValidation ?? shippingAddressValidation!]);
        }

        var normalizedCulture = string.IsNullOrWhiteSpace(request.Culture)
            ? SiteSettingDto.DefaultCultureDefault
            : request.Culture.Trim();
        if (normalizedCulture.Length > MaxCultureLength)
        {
            return BadRequestProblem(_validationLocalizer["StorefrontCheckoutReferenceTooLong"]);
        }

        try
        {
            var result = await _placeOrderFromCartHandler.HandleAsync(new PlaceOrderFromCartDto
            {
                CartId = request.CartId,
                UserId = GetCurrentUserId(),
                BillingAddressId = request.BillingAddressId,
                ShippingAddressId = request.ShippingAddressId,
                SelectedShippingMethodId = request.SelectedShippingMethodId,
                BillingAddress = MapAddress(request.BillingAddress),
                ShippingAddress = MapAddress(request.ShippingAddress),
                ShippingTotalMinor = request.ShippingTotalMinor,
                Culture = normalizedCulture
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
            return BadRequestProblem(_validationLocalizer["OrderPlacementFailed"]);
        }
    }

    /// <summary>
    /// Creates or reuses a storefront payment intent for an already placed order.
    /// </summary>
    [HttpPost("orders/{orderId:guid}/payment-intent")]
    [HttpPost("/api/v1/checkout/orders/{orderId:guid}/payment-intent")]
    [RequestSizeLimit(MaxCheckoutRequestBytes)]
    [ProducesResponseType(typeof(CreateStorefrontPaymentIntentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreatePaymentIntentAsync(Guid orderId, [FromBody] CreateStorefrontPaymentIntentRequest? request, CancellationToken ct = default)
    {
        if (orderId == Guid.Empty)
        {
            return BadRequestProblem(_validationLocalizer["OrderIdRequired"]);
        }

        try
        {
            var normalizedOrderNumber = NormalizeNullable(request?.OrderNumber);
            var normalizedProvider = NormalizeNullable(request?.Provider) ?? "Stripe";
            if (normalizedOrderNumber?.Length > MaxOrderNumberLength ||
                normalizedProvider.Length > MaxProviderLength)
            {
                return BadRequestProblem(_validationLocalizer["StorefrontCheckoutReferenceTooLong"]);
            }

            var returnUrl = _checkoutUrlBuilder.BuildFrontOfficeConfirmationUrl(orderId, normalizedOrderNumber, cancelled: false);
            var cancelUrl = _checkoutUrlBuilder.BuildFrontOfficeConfirmationUrl(orderId, normalizedOrderNumber, cancelled: true);

            var result = await _createStorefrontPaymentIntentHandler.HandleAsync(new CreateStorefrontPaymentIntentDto
            {
                OrderId = orderId,
                UserId = GetCurrentUserId(),
                OrderNumber = normalizedOrderNumber,
                Provider = normalizedProvider,
                ReturnUrl = returnUrl,
                CancelUrl = cancelUrl
            }, ct).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(result.CheckoutUrl))
            {
                throw new InvalidOperationException(_validationLocalizer["StorefrontStripeCheckoutNotConfigured"]);
            }

            return Ok(new CreateStorefrontPaymentIntentResponse
            {
                OrderId = result.OrderId,
                PaymentId = result.PaymentId,
                Provider = result.Provider,
                ProviderReference = result.ProviderReference,
                ProviderPaymentIntentReference = result.ProviderPaymentIntentReference,
                ProviderCheckoutSessionReference = result.ProviderCheckoutSessionReference,
                AmountMinor = result.AmountMinor,
                Currency = result.Currency,
                Status = result.Status.ToString(),
                CheckoutUrl = result.CheckoutUrl,
                ReturnUrl = returnUrl,
                CancelUrl = cancelUrl,
                ExpiresAtUtc = result.ExpiresAtUtc
            });
        }
        catch (InvalidOperationException ex) when (IsLocalizedError(ex, "OrderNotFound") ||
                                                   IsLocalizedError(ex, "OrderConfirmationContextIsInvalid"))
        {
            return NotFoundProblem(_validationLocalizer["OrderNotFound"]);
        }
        catch (Exception ex) when (ex is InvalidOperationException || ex is FluentValidation.ValidationException)
        {
            return BadRequestProblem(_validationLocalizer["PaymentIntentCreationFailed"]);
        }
    }

    /// <summary>
    /// Finalizes a storefront payment after the shopper returns from the hosted checkout or PSP.
    /// </summary>
    [HttpPost("orders/{orderId:guid}/payments/{paymentId:guid}/complete")]
    [HttpPost("/api/v1/checkout/orders/{orderId:guid}/payments/{paymentId:guid}/complete")]
    [RequestSizeLimit(MaxCheckoutRequestBytes)]
    [ProducesResponseType(typeof(CompleteStorefrontPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompletePaymentAsync(Guid orderId, Guid paymentId, [FromBody] CompleteStorefrontPaymentRequest? request, CancellationToken ct = default)
    {
        if (orderId == Guid.Empty || paymentId == Guid.Empty)
        {
            return BadRequestProblem(_validationLocalizer["OrderIdAndPaymentIdAreRequired"]);
        }

        if (request is null)
        {
            return BadRequestProblem(_validationLocalizer["RequestPayloadRequired"]);
        }

        if (!Enum.TryParse<StorefrontPaymentOutcome>(NormalizeNullable(request.Outcome), ignoreCase: true, out var outcome))
        {
            return BadRequestProblem(_validationLocalizer["UnsupportedStorefrontPaymentOutcome"]);
        }

        var normalizedOrderNumber = NormalizeNullable(request.OrderNumber);
        var normalizedProviderReference = NormalizeNullable(request.ProviderReference);
        var normalizedProviderPaymentIntentReference = NormalizeNullable(request.ProviderPaymentIntentReference);
        var normalizedProviderCheckoutSessionReference = NormalizeNullable(request.ProviderCheckoutSessionReference);
        var normalizedFailureReason = NormalizeNullable(request.FailureReason);

        if (normalizedOrderNumber?.Length > MaxOrderNumberLength ||
            normalizedProviderReference?.Length > MaxProviderReferenceLength ||
            normalizedProviderPaymentIntentReference?.Length > MaxProviderReferenceLength ||
            normalizedProviderCheckoutSessionReference?.Length > MaxProviderReferenceLength)
        {
            return BadRequestProblem(_validationLocalizer["StorefrontCheckoutReferenceTooLong"]);
        }

        if (normalizedFailureReason?.Length > MaxFailureReasonLength)
        {
            return BadRequestProblem(_validationLocalizer["StorefrontCheckoutFailureReasonTooLong"]);
        }

        try
        {
            var result = await _completeStorefrontPaymentHandler.HandleAsync(new CompleteStorefrontPaymentDto
            {
                OrderId = orderId,
                PaymentId = paymentId,
                UserId = GetCurrentUserId(),
                OrderNumber = normalizedOrderNumber,
                ProviderReference = normalizedProviderReference,
                ProviderPaymentIntentReference = normalizedProviderPaymentIntentReference,
                ProviderCheckoutSessionReference = normalizedProviderCheckoutSessionReference,
                Outcome = outcome,
                FailureReason = normalizedFailureReason
            }, ct).ConfigureAwait(false);

            return Ok(new CompleteStorefrontPaymentResponse
            {
                OrderId = result.OrderId,
                PaymentId = result.PaymentId,
                OrderStatus = result.OrderStatus.ToString(),
                PaymentStatus = result.PaymentStatus.ToString(),
                PaidAtUtc = result.PaidAtUtc
            });
        }
        catch (InvalidOperationException ex) when (IsLocalizedError(ex, "OrderNotFound") ||
                                                   IsLocalizedError(ex, "OrderConfirmationContextIsInvalid") ||
                                                   IsLocalizedError(ex, "PaymentNotFoundForOrder"))
        {
            return NotFoundProblem(_validationLocalizer["OrderConfirmationNotFound"]);
        }
        catch (Exception ex) when (ex is InvalidOperationException || ex is FluentValidation.ValidationException)
        {
            return BadRequestProblem(_validationLocalizer["PaymentCompletionApplyFailed"]);
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
            return BadRequestProblem(_validationLocalizer["OrderIdRequired"]);
        }

        var normalizedOrderNumber = NormalizeNullable(orderNumber);
        if (normalizedOrderNumber?.Length > MaxOrderNumberLength)
        {
            return BadRequestProblem(_validationLocalizer["StorefrontCheckoutReferenceTooLong"]);
        }

        var confirmation = await _getStorefrontOrderConfirmationHandler.HandleAsync(new GetStorefrontOrderConfirmationDto
        {
            OrderId = orderId,
            UserId = GetCurrentUserId(),
            OrderNumber = normalizedOrderNumber
        }, ct).ConfigureAwait(false);

        if (confirmation is null)
        {
            return NotFoundProblem(_validationLocalizer["OrderConfirmationNotFound"]);
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
                CreatedAtUtc = payment.CreatedAtUtc,
                Provider = payment.Provider,
                ProviderReference = payment.ProviderReference,
                ProviderPaymentIntentReference = payment.ProviderPaymentIntentReference,
                ProviderCheckoutSessionReference = payment.ProviderCheckoutSessionReference,
                AmountMinor = payment.AmountMinor,
                Currency = payment.Currency,
                Status = payment.Status.ToString(),
                PaidAtUtc = payment.PaidAtUtc
            }).ToList()
        });
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

    private static CheckoutAddressDto? MapAddress(CheckoutAddress? address)
        => address is null
            ? null
            : new CheckoutAddressDto
            {
                FullName = (address.FullName ?? string.Empty).Trim(),
                Company = NormalizeNullable(address.Company),
                Street1 = (address.Street1 ?? string.Empty).Trim(),
                Street2 = NormalizeNullable(address.Street2),
                PostalCode = (address.PostalCode ?? string.Empty).Trim(),
                City = (address.City ?? string.Empty).Trim(),
                State = NormalizeNullable(address.State),
                CountryCode = (address.CountryCode ?? string.Empty).Trim().ToUpperInvariant(),
                PhoneE164 = NormalizeNullable(address.PhoneE164)
            };

    private static string? ValidateAddressBounds(CheckoutAddress? address)
    {
        if (address is null)
        {
            return null;
        }

        if (IsTooLong(address.FullName, MaxAddressFullNameLength) ||
            IsTooLong(address.Company, MaxAddressCompanyLength) ||
            IsTooLong(address.Street1, MaxAddressStreetLength) ||
            IsTooLong(address.Street2, MaxAddressStreetLength) ||
            IsTooLong(address.PostalCode, MaxAddressPostalCodeLength) ||
            IsTooLong(address.City, MaxAddressCityLength) ||
            IsTooLong(address.State, MaxAddressStateLength) ||
            IsTooLong(address.PhoneE164, MaxAddressPhoneLength))
        {
            return "StorefrontCheckoutAddressFieldTooLong";
        }

        var countryCode = NormalizeNullable(address.CountryCode);
        if (countryCode is not null && countryCode.Length != MaxAddressCountryCodeLength)
        {
            return "StorefrontCheckoutAddressCountryCodeInvalid";
        }

        return null;
    }

    private static bool IsTooLong(string? value, int maxLength)
        => NormalizeNullable(value)?.Length > maxLength;

    private static string? NormalizeNullable(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private bool IsLocalizedError(Exception exception, string resourceKey)
        => string.Equals(
            exception.Message,
            _validationLocalizer[resourceKey],
            StringComparison.Ordinal);

}
