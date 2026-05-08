using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Payments;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Orders.DTOs;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Entities.Settings;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Orders.Commands;

/// <summary>
/// Creates or reuses a storefront payment intent for an order that has already been placed.
/// </summary>
public sealed class CreateStorefrontPaymentIntentHandler
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly IStringLocalizer<ValidationResource> _localizer;
    private readonly IStorefrontPaymentSessionClient? _paymentSessionClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateStorefrontPaymentIntentHandler"/> class.
    /// </summary>
    public CreateStorefrontPaymentIntentHandler(
        IAppDbContext db,
        IStringLocalizer<ValidationResource>? localizer = null,
        IClock? clock = null,
        IStorefrontPaymentSessionClient? paymentSessionClient = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? DefaultHandlerDependencies.DefaultClock;
        _localizer = localizer ?? DefaultHandlerDependencies.DefaultLocalizer;
        _paymentSessionClient = paymentSessionClient;
    }

    /// <summary>
    /// Creates or reuses a pending storefront payment for the specified order.
    /// </summary>
    public async Task<StorefrontPaymentIntentResultDto> HandleAsync(CreateStorefrontPaymentIntentDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (dto.OrderId == Guid.Empty)
        {
            throw new InvalidOperationException(_localizer["OrderIdRequired"]);
        }

        var provider = NormalizeProvider(dto.Provider);

        var order = await _db.Set<Order>()
            .Include(x => x.Payments)
            .FirstOrDefaultAsync(x => x.Id == dto.OrderId && !x.IsDeleted, ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException(_localizer["OrderNotFound"]);

        if (!Darwin.Application.Orders.Queries.GetStorefrontOrderConfirmationHandler.CanAccessOrder(order.UserId, order.OrderNumber, dto.UserId, dto.OrderNumber))
        {
            throw new InvalidOperationException(_localizer["OrderConfirmationContextIsInvalid"]);
        }

        if (order.Status is OrderStatus.Cancelled or OrderStatus.Refunded)
        {
            throw new InvalidOperationException(_localizer["PaymentCannotBeInitiatedForCancelledOrRefundedOrder"]);
        }

        if (order.Payments.Any(x => !x.IsDeleted && x.Status is PaymentStatus.Captured or PaymentStatus.Completed))
        {
            throw new InvalidOperationException(_localizer["OrderIsAlreadySettled"]);
        }

        var existing = order.Payments
            .Where(x => !x.IsDeleted &&
                        string.Equals(x.Provider, provider, StringComparison.OrdinalIgnoreCase) &&
                        x.AmountMinor == order.GrandTotalGrossMinor &&
                        x.Currency == order.Currency &&
                        x.Status is PaymentStatus.Pending or PaymentStatus.Authorized)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefault();

        if (existing is null)
        {
            var customerId = order.UserId.HasValue
                ? await _db.Set<Customer>()
                    .AsNoTracking()
                    .Where(x => x.UserId == order.UserId.Value && !x.IsDeleted)
                    .Select(x => (Guid?)x.Id)
                    .FirstOrDefaultAsync(ct)
                    .ConfigureAwait(false)
                : null;

            existing = new Payment
            {
                OrderId = order.Id,
                UserId = order.UserId,
                CustomerId = customerId,
                AmountMinor = order.GrandTotalGrossMinor,
                Currency = order.Currency,
                Provider = provider,
                ProviderTransactionRef = null,
                ProviderPaymentIntentRef = null,
                ProviderCheckoutSessionRef = null,
                Status = PaymentStatus.Pending
            };

            _db.Set<Payment>().Add(existing);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        var checkoutUrl = await EnsureProviderSessionAsync(order, existing, dto, provider, ct).ConfigureAwait(false);

        var nowUtc = _clock.UtcNow;
        return new StorefrontPaymentIntentResultDto
        {
            OrderId = order.Id,
            PaymentId = existing.Id,
            Provider = existing.Provider,
            ProviderReference = existing.ProviderTransactionRef ?? string.Empty,
            ProviderPaymentIntentReference = existing.ProviderPaymentIntentRef,
            ProviderCheckoutSessionReference = existing.ProviderCheckoutSessionRef,
            AmountMinor = existing.AmountMinor,
            Currency = existing.Currency,
            Status = existing.Status,
            ExpiresAtUtc = nowUtc.AddMinutes(15),
            CheckoutUrl = checkoutUrl
        };
    }

    private async Task<string?> EnsureProviderSessionAsync(Order order, Payment payment, CreateStorefrontPaymentIntentDto dto, string provider, CancellationToken ct)
    {
        if (!IsStripeProvider(provider))
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(payment.ProviderCheckoutSessionRef) ||
            !string.IsNullOrWhiteSpace(payment.ProviderTransactionRef))
        {
            return null;
        }

        if (_paymentSessionClient is null)
        {
            throw new InvalidOperationException(_localizer["StorefrontStripeCheckoutNotConfigured"]);
        }

        if (string.IsNullOrWhiteSpace(dto.ReturnUrl) || string.IsNullOrWhiteSpace(dto.CancelUrl))
        {
            throw new InvalidOperationException(_localizer["StorefrontStripeCheckoutUrlsRequired"]);
        }

        var siteSetting = await _db.Set<SiteSetting>()
            .AsNoTracking()
            .OrderBy(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException(_localizer["StorefrontStripeCheckoutNotConfigured"]);

        if (!siteSetting.StripeEnabled || string.IsNullOrWhiteSpace(siteSetting.StripeSecretKey))
        {
            throw new InvalidOperationException(_localizer["StorefrontStripeCheckoutNotConfigured"]);
        }

        var session = await _paymentSessionClient.CreateSessionAsync(new StorefrontPaymentSessionRequest
        {
            Provider = provider,
            SecretKey = siteSetting.StripeSecretKey,
            MerchantDisplayName = string.IsNullOrWhiteSpace(siteSetting.StripeMerchantDisplayName)
                ? siteSetting.Title
                : siteSetting.StripeMerchantDisplayName,
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            PaymentId = payment.Id,
            AmountMinor = payment.AmountMinor,
            Currency = payment.Currency,
            ReturnUrl = dto.ReturnUrl,
            CancelUrl = dto.CancelUrl
        }, ct).ConfigureAwait(false);

        payment.ProviderTransactionRef = session.ProviderReference;
        payment.ProviderPaymentIntentRef = session.ProviderPaymentIntentReference;
        payment.ProviderCheckoutSessionRef = session.ProviderCheckoutSessionReference;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        return session.CheckoutUrl;
    }

    private static bool IsStripeProvider(string provider)
        => string.Equals(provider, "Stripe", StringComparison.OrdinalIgnoreCase);

    private string NormalizeProvider(string? provider)
    {
        var normalized = string.IsNullOrWhiteSpace(provider) ? "Stripe" : provider.Trim();
        if (!IsStripeProvider(normalized))
        {
            throw new InvalidOperationException(_localizer["StorefrontPaymentProviderNotSupported"]);
        }

        return "Stripe";
    }
}

/// <summary>
/// Finalizes a storefront payment attempt after the shopper returns from the PSP or hosted checkout.
/// </summary>
public sealed class CompleteStorefrontPaymentHandler
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly IStringLocalizer<ValidationResource> _localizer;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompleteStorefrontPaymentHandler"/> class.
    /// </summary>
    public CompleteStorefrontPaymentHandler(IAppDbContext db, IStringLocalizer<ValidationResource>? localizer = null, IClock? clock = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? DefaultHandlerDependencies.DefaultClock;
        _localizer = localizer ?? DefaultHandlerDependencies.DefaultLocalizer;
    }

    /// <summary>
    /// Validates a storefront payment return without finalizing provider-owned payment state.
    /// </summary>
    public async Task<CompleteStorefrontPaymentResultDto> HandleAsync(CompleteStorefrontPaymentDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (dto.OrderId == Guid.Empty || dto.PaymentId == Guid.Empty)
        {
            throw new InvalidOperationException(_localizer["OrderIdAndPaymentIdAreRequired"]);
        }

        var order = await _db.Set<Order>()
            .Include(x => x.Payments)
            .FirstOrDefaultAsync(x => x.Id == dto.OrderId && !x.IsDeleted, ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException(_localizer["OrderNotFound"]);

        if (!Darwin.Application.Orders.Queries.GetStorefrontOrderConfirmationHandler.CanAccessOrder(order.UserId, order.OrderNumber, dto.UserId, dto.OrderNumber))
        {
            throw new InvalidOperationException(_localizer["OrderConfirmationContextIsInvalid"]);
        }

        var payment = order.Payments.FirstOrDefault(x => x.Id == dto.PaymentId && !x.IsDeleted);
        if (payment is null)
        {
            throw new InvalidOperationException(_localizer["PaymentNotFoundForOrder"]);
        }

        if (IsProviderFinalizedStatus(payment.Status))
        {
            return BuildResult(order, payment);
        }

        if (!Enum.IsDefined(dto.Outcome))
        {
            throw new InvalidOperationException(_localizer["UnsupportedStorefrontPaymentOutcome"]);
        }

        EnsureProviderReferencesMatch(payment, dto);

        if (!string.IsNullOrWhiteSpace(dto.ProviderReference))
        {
            payment.ProviderTransactionRef ??= dto.ProviderReference.Trim();
        }

        if (RequiresWebhookFinalization(payment.Provider))
        {
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return BuildResult(order, payment);
        }

        switch (dto.Outcome)
        {
            case StorefrontPaymentOutcome.Succeeded:
                payment.Status = PaymentStatus.Captured;
                payment.PaidAtUtc = _clock.UtcNow;
                if (order.Status is OrderStatus.Created)
                {
                    order.Status = OrderStatus.Paid;
                }

                break;
            case StorefrontPaymentOutcome.Cancelled:
                payment.Status = PaymentStatus.Voided;
                if (!string.IsNullOrWhiteSpace(dto.FailureReason))
                {
                    payment.FailureReason = dto.FailureReason;
                }
                break;
            case StorefrontPaymentOutcome.Failed:
                payment.Status = PaymentStatus.Failed;
                if (!string.IsNullOrWhiteSpace(dto.FailureReason))
                {
                    payment.FailureReason = dto.FailureReason;
                }
                break;
            default:
                throw new InvalidOperationException(_localizer["UnsupportedStorefrontPaymentOutcome"]);
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        return BuildResult(order, payment);
    }

    private void EnsureProviderReferencesMatch(Payment payment, CompleteStorefrontPaymentDto dto)
    {
        if (!string.IsNullOrWhiteSpace(dto.ProviderReference) &&
            !ReferenceMatchesAny(dto.ProviderReference, payment.ProviderTransactionRef, payment.ProviderCheckoutSessionRef, payment.ProviderPaymentIntentRef))
        {
            throw new InvalidOperationException(_localizer["StorefrontPaymentProviderReferenceMismatch"]);
        }

        if (!string.IsNullOrWhiteSpace(dto.ProviderPaymentIntentReference) &&
            !ReferenceMatches(dto.ProviderPaymentIntentReference, payment.ProviderPaymentIntentRef))
        {
            throw new InvalidOperationException(_localizer["StorefrontPaymentProviderReferenceMismatch"]);
        }

        if (!string.IsNullOrWhiteSpace(dto.ProviderCheckoutSessionReference) &&
            !ReferenceMatches(dto.ProviderCheckoutSessionReference, payment.ProviderCheckoutSessionRef))
        {
            throw new InvalidOperationException(_localizer["StorefrontPaymentProviderReferenceMismatch"]);
        }

    }

    private static bool ReferenceMatchesAny(string? provided, params string?[] expectedValues)
        => string.IsNullOrWhiteSpace(provided) ||
           expectedValues.Any(expected =>
               !string.IsNullOrWhiteSpace(expected) &&
               string.Equals(provided.Trim(), expected.Trim(), StringComparison.Ordinal));

    private static bool ReferenceMatches(string? provided, string? expected)
        => string.IsNullOrWhiteSpace(provided) ||
           (!string.IsNullOrWhiteSpace(expected) &&
            string.Equals(provided.Trim(), expected.Trim(), StringComparison.Ordinal));

    private static bool RequiresWebhookFinalization(string? provider)
        => string.Equals(provider, "Stripe", StringComparison.OrdinalIgnoreCase);

    private static bool IsProviderFinalizedStatus(PaymentStatus status)
        => status is PaymentStatus.Captured or PaymentStatus.Completed or PaymentStatus.Refunded or PaymentStatus.Voided;

    private static CompleteStorefrontPaymentResultDto BuildResult(Order order, Payment payment)
        => new()
        {
            OrderId = order.Id,
            PaymentId = payment.Id,
            OrderStatus = order.Status,
            PaymentStatus = payment.Status,
            PaidAtUtc = payment.PaidAtUtc
        };
}
