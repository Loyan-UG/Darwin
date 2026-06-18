using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Billing.Services;
using Darwin.Application.Orders.DTOs;
using Darwin.Application.Sales.Services;
using Darwin.Application.Orders.Validators;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Enums;
using FluentValidation;
using Microsoft.Extensions.Localization;
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
        private readonly IClock _clock;
        private readonly IValidator<PaymentCreateDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;
        private readonly SalesLifecycleEventService? _salesEvents;
        private readonly FinanceReceivablesPostingService? _financePosting;

        /// <summary>
        /// Initializes the handler with the application DbContext abstraction.
        /// </summary>
        public AddPaymentHandler(
            IAppDbContext db,
            IClock clock,
            IValidator<PaymentCreateDto> validator,
            IStringLocalizer<ValidationResource> localizer,
            SalesLifecycleEventService? salesEvents = null,
            FinanceReceivablesPostingService? financePosting = null)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
            _salesEvents = salesEvents;
            _financePosting = financePosting;
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
                .FirstOrDefaultAsync(o => o.Id == dto.OrderId && !o.IsDeleted, ct);
            if (order is null)
                throw new ValidationException(_localizer["OrderNotFound"]);

            // Currency must match the order currency to keep amounts consistent in reporting/export.
            if (!string.Equals(order.Currency, dto.Currency, StringComparison.OrdinalIgnoreCase))
                throw new ValidationException(_localizer["PaymentCurrencyMustMatchOrderCurrency"]);

            // Basic sanity: strictly positive amount and not above grand total.
            // (If partial payments/refunds are later allowed, replace this with a policy-aware check.)
            if (dto.AmountMinor <= 0 || dto.AmountMinor > order.GrandTotalGrossMinor)
                throw new ValidationException(_localizer["InvalidPaymentAmount"]);

            if (dto.Status is PaymentStatus.Refunded or PaymentStatus.Voided)
                throw new ValidationException(_localizer["PaymentCreateStatusInvalid"]);

            var nowUtc = _clock.UtcNow;
            var previousOrderStatus = order.Status;

            // Map to domain entity.
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                BusinessId = order.BusinessId,
                OrderId = order.Id,
                UserId = order.UserId,
                Provider = dto.Provider,
                ProviderTransactionRef = dto.ProviderReference,
                AmountMinor = dto.AmountMinor,
                Currency = dto.Currency,
                Status = dto.Status,
                FailureReason = dto.Status == PaymentStatus.Failed ? dto.FailureReason : null,
                PaidAtUtc = dto.Status is PaymentStatus.Captured or PaymentStatus.Completed ? nowUtc : null
            };

            await _db.Set<Payment>().AddAsync(payment, ct);

            // Optional: if a captured payment is recorded while order is still early-stage,
            // advance to Paid to reduce clicks for admins.
            if (dto.Status is PaymentStatus.Captured or PaymentStatus.Completed &&
                (order.Status == OrderStatus.Created || order.Status == OrderStatus.Confirmed))
            {
                order.Status = OrderStatus.Paid;
            }

            if (_salesEvents is not null)
            {
                var paymentEvent = await _salesEvents.RecordPaymentRecordedAsync(payment, order, nowUtc, ct).ConfigureAwait(false);
                if (!paymentEvent.Succeeded)
                {
                    throw new ValidationException(paymentEvent.Error);
                }

                if (previousOrderStatus != order.Status)
                {
                    var orderEvent = await _salesEvents.RecordOrderStatusChangedAsync(
                        order,
                        previousOrderStatus,
                        order.Status,
                        nowUtc,
                        ct)
                        .ConfigureAwait(false);
                    if (!orderEvent.Succeeded)
                    {
                        throw new ValidationException(orderEvent.Error);
                    }
                }
            }

            if (_financePosting is not null)
            {
                var posting = await _financePosting.PostPaymentRecordedAsync(payment, payment.PaidAtUtc ?? nowUtc, ct)
                    .ConfigureAwait(false);
                if (!posting.Succeeded)
                {
                    throw new ValidationException(posting.Error);
                }
            }

            await _db.SaveChangesAsync(ct);
        }
    }
}
