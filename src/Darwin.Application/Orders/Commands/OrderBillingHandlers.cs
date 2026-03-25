using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Orders.DTOs;
using Darwin.Application.Orders.Validators;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Orders.Commands
{
    /// <summary>
    /// Creates a refund record for an existing order payment.
    /// </summary>
    public sealed class AddRefundHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<RefundCreateDto> _validator;

        public AddRefundHandler(IAppDbContext db, IValidator<RefundCreateDto> validator)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public async Task<Guid> HandleAsync(RefundCreateDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);

            var order = await _db.Set<Order>()
                .FirstOrDefaultAsync(x => x.Id == dto.OrderId, ct)
                .ConfigureAwait(false);

            if (order is null)
            {
                throw new InvalidOperationException("Order not found.");
            }

            var payment = await _db.Set<Payment>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == dto.PaymentId && x.OrderId == dto.OrderId, ct)
                .ConfigureAwait(false);

            if (payment is null)
            {
                throw new InvalidOperationException("Payment not found for the order.");
            }

            if (!string.Equals(payment.Currency, dto.Currency, StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidationException("Refund currency does not match the payment currency.");
            }

            var refundedAmount = await _db.Set<Refund>()
                .AsNoTracking()
                .Where(x => x.PaymentId == dto.PaymentId && x.Status == RefundStatus.Completed)
                .SumAsync(x => (long?)x.AmountMinor, ct)
                .ConfigureAwait(false) ?? 0L;

            if (refundedAmount + dto.AmountMinor > payment.AmountMinor)
            {
                throw new ValidationException("Refund amount exceeds the remaining captured amount.");
            }

            var refund = new Refund
            {
                OrderId = dto.OrderId,
                PaymentId = dto.PaymentId,
                AmountMinor = dto.AmountMinor,
                Currency = dto.Currency.ToUpperInvariant(),
                Reason = dto.Reason.Trim(),
                Status = RefundStatus.Completed,
                CompletedAtUtc = DateTime.UtcNow
            };

            _db.Set<Refund>().Add(refund);

            var totalRefundedForOrder = await _db.Set<Refund>()
                .AsNoTracking()
                .Where(x => x.OrderId == dto.OrderId && x.Status == RefundStatus.Completed)
                .SumAsync(x => (long?)x.AmountMinor, ct)
                .ConfigureAwait(false) ?? 0L;

            var resultingRefunded = totalRefundedForOrder + dto.AmountMinor;
            if (resultingRefunded >= order.GrandTotalGrossMinor)
            {
                order.Status = OrderStatus.Refunded;
            }
            else if (resultingRefunded > 0)
            {
                order.Status = OrderStatus.PartiallyRefunded;
            }

            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return refund.Id;
        }
    }

    /// <summary>
    /// Creates a CRM invoice snapshot from an order.
    /// </summary>
    public sealed class CreateOrderInvoiceHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<OrderInvoiceCreateDto> _validator;

        public CreateOrderInvoiceHandler(IAppDbContext db, IValidator<OrderInvoiceCreateDto> validator)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public async Task<Guid> HandleAsync(OrderInvoiceCreateDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);

            var order = await _db.Set<Order>()
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == dto.OrderId, ct)
                .ConfigureAwait(false);

            if (order is null)
            {
                throw new InvalidOperationException("Order not found.");
            }

            Guid? customerId = dto.CustomerId;
            if (!customerId.HasValue && order.UserId.HasValue)
            {
                customerId = await _db.Set<Customer>()
                    .AsNoTracking()
                    .Where(x => x.UserId == order.UserId.Value)
                    .Select(x => (Guid?)x.Id)
                    .FirstOrDefaultAsync(ct)
                    .ConfigureAwait(false);
            }

            var invoice = new Invoice
            {
                BusinessId = dto.BusinessId,
                CustomerId = customerId,
                OrderId = order.Id,
                PaymentId = dto.PaymentId,
                Status = dto.PaymentId.HasValue ? InvoiceStatus.Paid : InvoiceStatus.Open,
                Currency = order.Currency,
                TotalNetMinor = order.SubtotalNetMinor,
                TotalTaxMinor = order.TaxTotalMinor,
                TotalGrossMinor = order.GrandTotalGrossMinor,
                DueDateUtc = dto.DueAtUtc ?? DateTime.UtcNow.AddDays(14),
                PaidAtUtc = dto.PaymentId.HasValue ? DateTime.UtcNow : null,
                Lines = order.Lines.Select(x => new InvoiceLine
                {
                    Description = string.IsNullOrWhiteSpace(x.Name) ? x.Sku : x.Name,
                    Quantity = x.Quantity,
                    UnitPriceNetMinor = x.UnitPriceNetMinor,
                    TaxRate = x.VatRate,
                    TotalNetMinor = x.UnitPriceNetMinor * x.Quantity,
                    TotalGrossMinor = x.LineGrossMinor
                }).ToList()
            };

            _db.Set<Invoice>().Add(invoice);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return invoice.Id;
        }
    }
}
