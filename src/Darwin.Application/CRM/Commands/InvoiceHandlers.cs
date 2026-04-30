using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Billing.Queries;
using Darwin.Application.CRM.DTOs;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.CRM.Commands
{
    public sealed class UpdateInvoiceHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<InvoiceEditDto> _validator;
        private readonly IClock _clock;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public UpdateInvoiceHandler(
            IAppDbContext db,
            IValidator<InvoiceEditDto> validator,
            IStringLocalizer<ValidationResource>? localizer = null, IClock? clock = null)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _clock = clock ?? DefaultHandlerDependencies.DefaultClock;
            _localizer = localizer ?? DefaultHandlerDependencies.DefaultLocalizer;
        }

        public async Task HandleAsync(InvoiceEditDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);

            var invoice = await _db.Set<Invoice>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct)
                .ConfigureAwait(false);

            if (invoice is null)
            {
                throw new InvalidOperationException(_localizer["InvoiceNotFound"]);
            }

            var rowVersion = dto.RowVersion ?? Array.Empty<byte>();
            var currentVersion = invoice.RowVersion ?? Array.Empty<byte>();
            if (rowVersion.Length == 0 || !currentVersion.SequenceEqual(rowVersion))
            {
                throw new DbUpdateConcurrencyException(_localizer["ConcurrencyConflictDetected"]);
            }

            var previousPaymentId = invoice.PaymentId;

            if (dto.CustomerId.HasValue)
            {
                var customerExists = await _db.Set<Customer>()
                    .AsNoTracking()
                    .AnyAsync(x => x.Id == dto.CustomerId.Value && !x.IsDeleted, ct)
                    .ConfigureAwait(false);

                if (!customerExists)
                {
                    throw new InvalidOperationException(_localizer["LinkedCustomerNotFound"]);
                }
            }

            if (dto.PaymentId.HasValue)
            {
                var payment = await _db.Set<Payment>()
                    .FirstOrDefaultAsync(x => x.Id == dto.PaymentId.Value, ct)
                    .ConfigureAwait(false);

                if (payment is null)
                {
                    throw new InvalidOperationException(_localizer["LinkedPaymentNotFound"]);
                }

                if (payment.InvoiceId.HasValue && payment.InvoiceId.Value != invoice.Id)
                {
                    throw new InvalidOperationException(_localizer["LinkedPaymentAlreadyAssignedToAnotherInvoice"]);
                }

                payment.InvoiceId = invoice.Id;
                if (!payment.CustomerId.HasValue && dto.CustomerId.HasValue)
                {
                    payment.CustomerId = dto.CustomerId;
                }
            }

            invoice.BusinessId = dto.BusinessId;
            invoice.CustomerId = dto.CustomerId;
            invoice.OrderId = dto.OrderId;
            invoice.PaymentId = dto.PaymentId;
            invoice.Status = dto.Status;
            invoice.Currency = dto.Currency.Trim();
            invoice.TotalNetMinor = dto.TotalNetMinor;
            invoice.TotalTaxMinor = dto.TotalTaxMinor;
            invoice.TotalGrossMinor = dto.TotalGrossMinor;
            invoice.DueDateUtc = dto.DueDateUtc;
            var nowUtc = _clock.UtcNow;
            invoice.PaidAtUtc = dto.Status == Darwin.Domain.Enums.InvoiceStatus.Paid ? dto.PaidAtUtc ?? nowUtc : dto.PaidAtUtc;

            if (previousPaymentId.HasValue && previousPaymentId != dto.PaymentId)
            {
                var previousPayment = await _db.Set<Payment>()
                    .FirstOrDefaultAsync(x => x.Id == previousPaymentId.Value, ct)
                    .ConfigureAwait(false);

                if (previousPayment is not null && previousPayment.InvoiceId == invoice.Id)
                {
                    previousPayment.InvoiceId = null;
                }
            }

            try
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new DbUpdateConcurrencyException(_localizer["ConcurrencyConflictDetected"]);
            }
        }
    }

    public sealed class TransitionInvoiceStatusHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<InvoiceStatusTransitionDto> _validator;
        private readonly IClock _clock;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public TransitionInvoiceStatusHandler(
            IAppDbContext db,
            IValidator<InvoiceStatusTransitionDto> validator,
            IStringLocalizer<ValidationResource>? localizer = null, IClock? clock = null)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _clock = clock ?? DefaultHandlerDependencies.DefaultClock;
            _localizer = localizer ?? DefaultHandlerDependencies.DefaultLocalizer;
        }

        public async Task HandleAsync(InvoiceStatusTransitionDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);

            var invoice = await _db.Set<Invoice>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct)
                .ConfigureAwait(false);

            if (invoice is null)
            {
                throw new InvalidOperationException(_localizer["InvoiceNotFound"]);
            }

            var rowVersion = dto.RowVersion ?? Array.Empty<byte>();
            var currentVersion = invoice.RowVersion ?? Array.Empty<byte>();
            if (rowVersion.Length == 0 || !currentVersion.SequenceEqual(rowVersion))
            {
                throw new DbUpdateConcurrencyException(_localizer["ConcurrencyConflictDetected"]);
            }

            var payment = invoice.PaymentId.HasValue
                ? await _db.Set<Payment>()
                    .FirstOrDefaultAsync(x => x.Id == invoice.PaymentId.Value, ct)
                    .ConfigureAwait(false)
                : null;

            switch (dto.TargetStatus)
            {
                case InvoiceStatus.Draft:
                case InvoiceStatus.Open:
                    invoice.Status = dto.TargetStatus;
                    invoice.PaidAtUtc = null;
                    break;

                case InvoiceStatus.Paid:
                {
                    var nowUtc = _clock.UtcNow;
                    var paidAtUtc = dto.PaidAtUtc ?? nowUtc;
                    if (payment is not null)
                    {
                        if (payment.Status is PaymentStatus.Failed or PaymentStatus.Voided or PaymentStatus.Refunded)
                        {
                            throw new InvalidOperationException(_localizer["InvoicesCannotBeMarkedAsPaidWhileLinkedPaymentIsFailedVoidedOrRefunded"]);
                        }

                        if (payment.Status is PaymentStatus.Pending or PaymentStatus.Authorized)
                        {
                            payment.Status = PaymentStatus.Captured;
                        }

                        payment.PaidAtUtc ??= paidAtUtc;
                        if (!payment.CustomerId.HasValue && invoice.CustomerId.HasValue)
                        {
                            payment.CustomerId = invoice.CustomerId;
                        }
                    }

                    invoice.Status = InvoiceStatus.Paid;
                    invoice.PaidAtUtc = paidAtUtc;
                    break;
                }

                case InvoiceStatus.Cancelled:
                    if (payment is not null)
                    {
                        if (payment.Status is PaymentStatus.Captured or PaymentStatus.Completed)
                        {
                            throw new InvalidOperationException(_localizer["PaidInvoicesMustBeRefundedBeforeCancellation"]);
                        }

                        if (payment.Status is PaymentStatus.Pending or PaymentStatus.Authorized)
                        {
                            payment.Status = PaymentStatus.Voided;
                            payment.PaidAtUtc = null;
                        }
                    }

                    invoice.Status = InvoiceStatus.Cancelled;
                    invoice.PaidAtUtc = null;
                    break;

                default:
                    throw new InvalidOperationException(_localizer["UnsupportedInvoiceStatusTransition"]);
            }

            try
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new DbUpdateConcurrencyException(_localizer["ConcurrencyConflictDetected"]);
            }
        }
    }

    public sealed class CreateInvoiceRefundHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<InvoiceRefundCreateDto> _validator;
        private readonly IClock _clock;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public CreateInvoiceRefundHandler(
            IAppDbContext db,
            IValidator<InvoiceRefundCreateDto> validator,
            IStringLocalizer<ValidationResource>? localizer = null, IClock? clock = null)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _clock = clock ?? DefaultHandlerDependencies.DefaultClock;
            _localizer = localizer ?? DefaultHandlerDependencies.DefaultLocalizer;
        }

        public async Task<Guid> HandleAsync(InvoiceRefundCreateDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);

            var invoice = await _db.Set<Invoice>()
                .FirstOrDefaultAsync(x => x.Id == dto.InvoiceId, ct)
                .ConfigureAwait(false);

            if (invoice is null)
            {
                throw new InvalidOperationException(_localizer["InvoiceNotFound"]);
            }

            var rowVersion = dto.RowVersion ?? Array.Empty<byte>();
            var currentVersion = invoice.RowVersion ?? Array.Empty<byte>();
            if (rowVersion.Length == 0 || !currentVersion.SequenceEqual(rowVersion))
            {
                throw new DbUpdateConcurrencyException(_localizer["ConcurrencyConflictDetected"]);
            }

            if (!invoice.PaymentId.HasValue)
            {
                throw new InvalidOperationException(_localizer["OnlyInvoicesWithLinkedPaymentCanBeRefunded"]);
            }

            var payment = await _db.Set<Payment>()
                .FirstOrDefaultAsync(x => x.Id == invoice.PaymentId.Value, ct)
                .ConfigureAwait(false);

            if (payment is null)
            {
                throw new InvalidOperationException(_localizer["LinkedPaymentNotFound"]);
            }

            if (payment.Status is PaymentStatus.Pending or PaymentStatus.Authorized or PaymentStatus.Failed or PaymentStatus.Voided)
            {
                throw new ValidationException(_localizer["OnlyCapturedOrCompletedPaymentsCanBeRefunded"]);
            }

            if (!string.Equals(payment.Currency, dto.Currency, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(invoice.Currency, dto.Currency, StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidationException(_localizer["RefundCurrencyMustMatchLinkedInvoiceAndPaymentCurrency"]);
            }

            var refundedAmountMinor = await _db.Set<Refund>()
                .AsNoTracking()
                .Where(x => x.PaymentId == payment.Id && x.Status == RefundStatus.Completed)
                .SumAsync(x => (long?)x.AmountMinor, ct)
                .ConfigureAwait(false) ?? 0L;

            var refundableAgainstPaymentMinor = BillingReconciliationCalculator.CalculateNetCollectedAmount(payment.AmountMinor, refundedAmountMinor);
            var refundableAgainstInvoiceMinor = invoice.Status == InvoiceStatus.Cancelled
                ? 0L
                : BillingReconciliationCalculator.CalculateSettledAmount(invoice.TotalGrossMinor, refundableAgainstPaymentMinor);

            if (refundableAgainstInvoiceMinor <= 0)
            {
                throw new ValidationException(_localizer["NoRefundableAmountRemainingOnInvoice"]);
            }

            if (dto.AmountMinor > refundableAgainstPaymentMinor || dto.AmountMinor > refundableAgainstInvoiceMinor)
            {
                throw new ValidationException(_localizer["RefundAmountExceedsRemainingRefundableAmountOnInvoice"]);
            }

            var refund = new Refund
            {
                OrderId = invoice.OrderId,
                PaymentId = payment.Id,
                AmountMinor = dto.AmountMinor,
                Currency = dto.Currency.ToUpperInvariant(),
                Reason = dto.Reason.Trim(),
                Status = RefundStatus.Completed,
                CompletedAtUtc = _clock.UtcNow
            };

            _db.Set<Refund>().Add(refund);

            var resultingRefundedAmountMinor = refundedAmountMinor + dto.AmountMinor;
            var remainingNetCollectedMinor = BillingReconciliationCalculator.CalculateNetCollectedAmount(payment.AmountMinor, resultingRefundedAmountMinor);
            if (remainingNetCollectedMinor == 0)
            {
                payment.Status = PaymentStatus.Refunded;
                payment.PaidAtUtc = null;
                invoice.Status = InvoiceStatus.Cancelled;
                invoice.PaidAtUtc = null;
            }

            try
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new DbUpdateConcurrencyException(_localizer["ConcurrencyConflictDetected"]);
            }

            return refund.Id;
        }
    }
}

