using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.CRM.DTOs;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.CRM.Commands
{
    public sealed class UpdateInvoiceHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<InvoiceEditDto> _validator;

        public UpdateInvoiceHandler(IAppDbContext db, IValidator<InvoiceEditDto> validator)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public async Task HandleAsync(InvoiceEditDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);

            var invoice = await _db.Set<Invoice>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct)
                .ConfigureAwait(false);

            if (invoice is null)
            {
                throw new InvalidOperationException("Invoice not found.");
            }

            if (!invoice.RowVersion.SequenceEqual(dto.RowVersion))
            {
                throw new DbUpdateConcurrencyException("Concurrency conflict detected.");
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
                    throw new InvalidOperationException("Linked customer not found.");
                }
            }

            if (dto.PaymentId.HasValue)
            {
                var payment = await _db.Set<Payment>()
                    .FirstOrDefaultAsync(x => x.Id == dto.PaymentId.Value, ct)
                    .ConfigureAwait(false);

                if (payment is null)
                {
                    throw new InvalidOperationException("Linked payment not found.");
                }

                if (payment.InvoiceId.HasValue && payment.InvoiceId.Value != invoice.Id)
                {
                    throw new InvalidOperationException("Linked payment is already assigned to another invoice.");
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
            invoice.PaidAtUtc = dto.Status == Darwin.Domain.Enums.InvoiceStatus.Paid ? dto.PaidAtUtc ?? DateTime.UtcNow : dto.PaidAtUtc;

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

            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    public sealed class TransitionInvoiceStatusHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<InvoiceStatusTransitionDto> _validator;

        public TransitionInvoiceStatusHandler(IAppDbContext db, IValidator<InvoiceStatusTransitionDto> validator)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public async Task HandleAsync(InvoiceStatusTransitionDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);

            var invoice = await _db.Set<Invoice>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct)
                .ConfigureAwait(false);

            if (invoice is null)
            {
                throw new InvalidOperationException("Invoice not found.");
            }

            if (!invoice.RowVersion.SequenceEqual(dto.RowVersion))
            {
                throw new DbUpdateConcurrencyException("Concurrency conflict detected.");
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
                    var paidAtUtc = dto.PaidAtUtc ?? DateTime.UtcNow;
                    if (payment is not null)
                    {
                        if (payment.Status is PaymentStatus.Failed or PaymentStatus.Voided or PaymentStatus.Refunded)
                        {
                            throw new InvalidOperationException("Invoices cannot be marked as paid while the linked payment is failed, voided, or refunded.");
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
                            throw new InvalidOperationException("Paid invoices must be refunded before cancellation.");
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
                    throw new InvalidOperationException("Unsupported invoice status transition.");
            }

            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }
}
