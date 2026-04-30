using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
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
    private readonly IClock _clock;
    private readonly IStringLocalizer<ValidationResource> _localizer;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateStorefrontPaymentIntentHandler"/> class.
    /// </summary>
    public CreateStorefrontPaymentIntentHandler(IAppDbContext db, IStringLocalizer<ValidationResource>? localizer = null, IClock? clock = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? DefaultHandlerDependencies.DefaultClock;
        _localizer = localizer ?? DefaultHandlerDependencies.DefaultLocalizer;
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
            ExpiresAtUtc = nowUtc.AddMinutes(15)
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

        EnsureProviderReferencesMatch(payment, dto);

        if (IsProviderFinalizedStatus(payment.Status))
        {
            return BuildResult(order, payment);
        }

        if (!string.IsNullOrWhiteSpace(dto.ProviderReference))
        {
            payment.ProviderTransactionRef = dto.ProviderReference.Trim();
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

    private static bool ReferenceMatches(string? provided, string? expected)
        => string.IsNullOrWhiteSpace(provided) ||
           (!string.IsNullOrWhiteSpace(expected) &&
            string.Equals(provided.Trim(), expected.Trim(), StringComparison.Ordinal));

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
