using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Orders.DTOs;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Orders;
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
    private readonly IStringLocalizer<ValidationResource> _localizer;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateStorefrontPaymentIntentHandler"/> class.
    /// </summary>
    public CreateStorefrontPaymentIntentHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
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
            .FirstOrDefaultAsync(x => x.Id == dto.OrderId, ct)
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

        if (order.Payments.Any(x => x.Status is PaymentStatus.Captured or PaymentStatus.Completed))
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
                    .Where(x => x.UserId == order.UserId.Value)
                    .Select(x => (Guid?)x.Id)
                    .FirstOrDefaultAsync(ct)
                    .ConfigureAwait(false)
                : null;

            var providerPaymentIntentReference = IsStripeProvider(provider)
                ? $"pi_{Guid.NewGuid():N}"
                : null;
            var providerCheckoutSessionReference = IsStripeProvider(provider)
                ? $"cs_{Guid.NewGuid():N}"
                : null;

            existing = new Payment
            {
                OrderId = order.Id,
                UserId = order.UserId,
                CustomerId = customerId,
                AmountMinor = order.GrandTotalGrossMinor,
                Currency = order.Currency,
                Provider = provider,
                ProviderTransactionRef = providerCheckoutSessionReference ?? $"chk_{Guid.NewGuid():N}",
                ProviderPaymentIntentRef = providerPaymentIntentReference,
                ProviderCheckoutSessionRef = providerCheckoutSessionReference,
                Status = PaymentStatus.Pending
            };

            _db.Set<Payment>().Add(existing);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

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
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(15)
        };
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
    private readonly IStringLocalizer<ValidationResource> _localizer;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompleteStorefrontPaymentHandler"/> class.
    /// </summary>
    public CompleteStorefrontPaymentHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    }

    /// <summary>
    /// Applies the reported storefront payment outcome to the payment and order aggregates.
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
            .FirstOrDefaultAsync(x => x.Id == dto.OrderId, ct)
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

        if (payment.Status is PaymentStatus.Captured or PaymentStatus.Completed or PaymentStatus.Refunded)
        {
            throw new InvalidOperationException(_localizer["PaymentIsAlreadyFinalized"]);
        }

        if (!string.IsNullOrWhiteSpace(dto.ProviderReference))
        {
            payment.ProviderTransactionRef = dto.ProviderReference.Trim();
        }

        if (!string.IsNullOrWhiteSpace(dto.ProviderPaymentIntentReference))
        {
            payment.ProviderPaymentIntentRef = dto.ProviderPaymentIntentReference.Trim();
        }

        if (!string.IsNullOrWhiteSpace(dto.ProviderCheckoutSessionReference))
        {
            payment.ProviderCheckoutSessionRef = dto.ProviderCheckoutSessionReference.Trim();
        }
        else if (IsStripeProvider(payment.Provider) && !string.IsNullOrWhiteSpace(dto.ProviderReference))
        {
            payment.ProviderCheckoutSessionRef = dto.ProviderReference.Trim();
        }

        switch (dto.Outcome)
        {
            case StorefrontPaymentOutcome.Succeeded:
                payment.Status = PaymentStatus.Captured;
                payment.PaidAtUtc = DateTime.UtcNow;
                if (order.Status is OrderStatus.Created or OrderStatus.Confirmed)
                {
                    order.Status = OrderStatus.Paid;
                }
                payment.FailureReason = null;
                break;

            case StorefrontPaymentOutcome.Cancelled:
                payment.Status = PaymentStatus.Voided;
                payment.FailureReason = string.IsNullOrWhiteSpace(dto.FailureReason) ? "Checkout was cancelled by the shopper." : dto.FailureReason.Trim();
                break;

            case StorefrontPaymentOutcome.Failed:
                payment.Status = PaymentStatus.Failed;
                payment.FailureReason = string.IsNullOrWhiteSpace(dto.FailureReason) ? "Checkout failed at the payment provider." : dto.FailureReason.Trim();
                break;

            default:
                throw new InvalidOperationException(_localizer["UnsupportedStorefrontPaymentOutcome"]);
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        return new CompleteStorefrontPaymentResultDto
        {
            OrderId = order.Id,
            PaymentId = payment.Id,
            OrderStatus = order.Status,
            PaymentStatus = payment.Status,
            PaidAtUtc = payment.PaidAtUtc
        };
    }

    private static bool IsStripeProvider(string provider)
        => string.Equals(provider, "Stripe", StringComparison.OrdinalIgnoreCase);
}
