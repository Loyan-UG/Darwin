using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Orders.DTOs;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Orders.Commands;

/// <summary>
/// Creates or reuses a storefront payment intent for an order that has already been placed.
/// </summary>
public sealed class CreateStorefrontPaymentIntentHandler
{
    private readonly IAppDbContext _db;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateStorefrontPaymentIntentHandler"/> class.
    /// </summary>
    public CreateStorefrontPaymentIntentHandler(IAppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    /// <summary>
    /// Creates or reuses a pending storefront payment for the specified order.
    /// </summary>
    public async Task<StorefrontPaymentIntentResultDto> HandleAsync(CreateStorefrontPaymentIntentDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (dto.OrderId == Guid.Empty)
        {
            throw new InvalidOperationException("OrderId is required.");
        }

        var provider = string.IsNullOrWhiteSpace(dto.Provider) ? "DarwinCheckout" : dto.Provider.Trim();

        var order = await _db.Set<Order>()
            .Include(x => x.Payments)
            .FirstOrDefaultAsync(x => x.Id == dto.OrderId, ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Order not found.");

        if (!Darwin.Application.Orders.Queries.GetStorefrontOrderConfirmationHandler.CanAccessOrder(order.UserId, order.OrderNumber, dto.UserId, dto.OrderNumber))
        {
            throw new InvalidOperationException("Order confirmation context is invalid.");
        }

        if (order.Status is OrderStatus.Cancelled or OrderStatus.Refunded)
        {
            throw new InvalidOperationException("Payment cannot be initiated for a cancelled or refunded order.");
        }

        if (order.Payments.Any(x => x.Status is PaymentStatus.Captured or PaymentStatus.Completed))
        {
            throw new InvalidOperationException("Order is already settled.");
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

            existing = new Payment
            {
                OrderId = order.Id,
                UserId = order.UserId,
                CustomerId = customerId,
                AmountMinor = order.GrandTotalGrossMinor,
                Currency = order.Currency,
                Provider = provider,
                ProviderTransactionRef = $"chk_{Guid.NewGuid():N}",
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
            AmountMinor = existing.AmountMinor,
            Currency = existing.Currency,
            Status = existing.Status,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(15)
        };
    }
}

/// <summary>
/// Finalizes a storefront payment attempt after the shopper returns from the PSP or hosted checkout.
/// </summary>
public sealed class CompleteStorefrontPaymentHandler
{
    private readonly IAppDbContext _db;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompleteStorefrontPaymentHandler"/> class.
    /// </summary>
    public CompleteStorefrontPaymentHandler(IAppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    /// <summary>
    /// Applies the reported storefront payment outcome to the payment and order aggregates.
    /// </summary>
    public async Task<CompleteStorefrontPaymentResultDto> HandleAsync(CompleteStorefrontPaymentDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (dto.OrderId == Guid.Empty || dto.PaymentId == Guid.Empty)
        {
            throw new InvalidOperationException("OrderId and PaymentId are required.");
        }

        var order = await _db.Set<Order>()
            .Include(x => x.Payments)
            .FirstOrDefaultAsync(x => x.Id == dto.OrderId, ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Order not found.");

        if (!Darwin.Application.Orders.Queries.GetStorefrontOrderConfirmationHandler.CanAccessOrder(order.UserId, order.OrderNumber, dto.UserId, dto.OrderNumber))
        {
            throw new InvalidOperationException("Order confirmation context is invalid.");
        }

        var payment = order.Payments.FirstOrDefault(x => x.Id == dto.PaymentId && !x.IsDeleted);
        if (payment is null)
        {
            throw new InvalidOperationException("Payment not found for the order.");
        }

        if (payment.Status is PaymentStatus.Captured or PaymentStatus.Completed or PaymentStatus.Refunded)
        {
            throw new InvalidOperationException("Payment is already finalized.");
        }

        if (!string.IsNullOrWhiteSpace(dto.ProviderReference))
        {
            payment.ProviderTransactionRef = dto.ProviderReference.Trim();
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
                throw new InvalidOperationException("Unsupported storefront payment outcome.");
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
}
