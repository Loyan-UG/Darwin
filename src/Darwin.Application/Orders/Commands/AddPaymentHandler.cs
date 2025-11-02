using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Orders.DTOs;
using Darwin.Application.Orders.Validators;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Orders.Commands
{
    /// <summary>
    /// Creates and attaches a <see cref="Payment"/> to an existing <see cref="Order"/>.
    /// Performs consistency validation (currency, amount) and, when persisted as Captured,
    /// advances the order status to <see cref="OrderStatus.Paid"/> if policy allows it.
    /// </summary>
    public sealed class AddPaymentHandler
    {
        private readonly IAppDbContext _db;
        private readonly PaymentCreateValidator _validator = new();

        /// <summary>
        /// Initializes the handler with the application DbContext abstraction.
        /// </summary>
        public AddPaymentHandler(IAppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        /// <summary>
        /// Creates a payment row for a given order and optionally advances order status.
        /// </summary>
        /// <exception cref="ValidationException">Thrown when input fails validation or cross-entity constraints.</exception>
        public async Task HandleAsync(PaymentCreateDto dto, CancellationToken ct = default)
        {
            var val = _validator.Validate(dto);
            if (!val.IsValid) throw new ValidationException(val.Errors);

            // Load target order (tracking required to update its status).
            var order = await _db.Set<Order>()
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.Id == dto.OrderId, ct);
            if (order is null)
                throw new ValidationException("Order not found.");

            // Currency must match the order currency to keep amounts consistent in reporting/export.
            if (!string.Equals(order.Currency, dto.Currency, StringComparison.OrdinalIgnoreCase))
                throw new ValidationException("Payment currency does not match order currency.");

            // Basic sanity: strictly positive amount and not above grand total.
            // (If partial payments/refunds are later allowed, replace this with a policy-aware check.)
            if (dto.AmountMinor <= 0 || dto.AmountMinor > order.GrandTotalGrossMinor)
                throw new ValidationException("Invalid payment amount.");

            // Map to domain entity.
            var payment = new Payment
            {
                OrderId = order.Id,
                Provider = dto.Provider,
                ProviderReference = dto.ProviderReference,
                AmountMinor = dto.AmountMinor,
                Currency = dto.Currency,
                Status = dto.Status,
                FailureReason = dto.Status == PaymentStatus.Failed ? dto.FailureReason : null,
                // CapturedAtUtc is set only when immediately captured.
                CapturedAtUtc = dto.Status == PaymentStatus.Captured ? DateTime.UtcNow : null
            };

            await _db.Set<Payment>().AddAsync(payment, ct);

            // Optional: if a captured payment is recorded while order is still early-stage,
            // advance to Paid to reduce clicks for admins.
            if (dto.Status == PaymentStatus.Captured &&
                (order.Status == OrderStatus.Created || order.Status == OrderStatus.Confirmed))
            {
                order.Status = OrderStatus.Paid;
            }

            await _db.SaveChangesAsync(ct);
        }
    }
}
