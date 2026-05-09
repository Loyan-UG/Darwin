using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Billing.Queries;
using Darwin.Application.CRM.DTOs;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Integration;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Entities.Settings;
using Darwin.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

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
            EnsureIssuedInvoiceFieldsUnchanged(invoice, dto, _localizer);

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

            var wasDraft = invoice.Status == InvoiceStatus.Draft;
            if (wasDraft && dto.Status is InvoiceStatus.Open or InvoiceStatus.Paid)
            {
                await InvoiceIssueReadinessGuard.ValidateAsync(_db, invoice, dto.CustomerId, _localizer, ct).ConfigureAwait(false);
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
            if (wasDraft && dto.Status is InvoiceStatus.Open or InvoiceStatus.Paid)
            {
                await InvoiceIssueSnapshotWriter.CaptureIfMissingAsync(_db, invoice, nowUtc, ct).ConfigureAwait(false);
            }

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

        private static void EnsureIssuedInvoiceFieldsUnchanged(
            Invoice invoice,
            InvoiceEditDto dto,
            IStringLocalizer<ValidationResource> localizer)
        {
            var isIssued = invoice.Status != InvoiceStatus.Draft ||
                invoice.IssuedAtUtc.HasValue ||
                !string.IsNullOrWhiteSpace(invoice.IssuedSnapshotJson);

            if (!isIssued)
            {
                return;
            }

            var hasChangedIssueCriticalField =
                invoice.BusinessId != dto.BusinessId ||
                invoice.CustomerId != dto.CustomerId ||
                invoice.OrderId != dto.OrderId ||
                !string.Equals(invoice.Currency, dto.Currency?.Trim(), StringComparison.OrdinalIgnoreCase) ||
                invoice.TotalNetMinor != dto.TotalNetMinor ||
                invoice.TotalTaxMinor != dto.TotalTaxMinor ||
                invoice.TotalGrossMinor != dto.TotalGrossMinor ||
                invoice.DueDateUtc != dto.DueDateUtc;

            if (hasChangedIssueCriticalField)
            {
                throw new InvalidOperationException(localizer["IssuedInvoiceFinancialFieldsCannotBeEdited"]);
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
                    if (invoice.Status == InvoiceStatus.Draft)
                    {
                        await InvoiceIssueReadinessGuard.ValidateAsync(_db, invoice, invoice.CustomerId, _localizer, ct).ConfigureAwait(false);
                    }

                    invoice.Status = dto.TargetStatus;
                    invoice.PaidAtUtc = null;
                    if (dto.TargetStatus == InvoiceStatus.Open)
                    {
                        await InvoiceIssueSnapshotWriter.CaptureIfMissingAsync(_db, invoice, _clock.UtcNow, ct).ConfigureAwait(false);
                    }

                    break;

                case InvoiceStatus.Paid:
                {
                    if (invoice.Status == InvoiceStatus.Draft)
                    {
                        await InvoiceIssueReadinessGuard.ValidateAsync(_db, invoice, invoice.CustomerId, _localizer, ct).ConfigureAwait(false);
                    }

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
                    await InvoiceIssueSnapshotWriter.CaptureIfMissingAsync(_db, invoice, nowUtc, ct).ConfigureAwait(false);
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

    public sealed class UpdateInvoiceReverseChargeDecisionHandler
    {
        private const int MaxNoteLength = 512;

        private readonly IAppDbContext _db;
        private readonly IClock _clock;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public UpdateInvoiceReverseChargeDecisionHandler(
            IAppDbContext db,
            IClock clock,
            IStringLocalizer<ValidationResource>? localizer = null)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _localizer = localizer ?? DefaultHandlerDependencies.DefaultLocalizer;
        }

        public async Task HandleAsync(InvoiceReverseChargeDecisionDto dto, CancellationToken ct = default)
        {
            if (dto.Id == Guid.Empty)
            {
                throw new InvalidOperationException(_localizer["InvoiceNotFound"]);
            }

            var invoice = await _db.Set<Invoice>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct)
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

            invoice.ReverseChargeApplied = dto.Applies;
            invoice.ReverseChargeReviewedAtUtc = _clock.UtcNow;
            invoice.ReverseChargeReviewNote = NormalizeNote(dto.Note);

            try
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new DbUpdateConcurrencyException(_localizer["ConcurrencyConflictDetected"]);
            }
        }

        private static string? NormalizeNote(string? note)
        {
            if (string.IsNullOrWhiteSpace(note))
            {
                return null;
            }

            var normalized = note.Trim();
            if (normalized.Length > MaxNoteLength)
            {
                throw new ValidationException(DefaultHandlerDependencies.DefaultLocalizer["ReverseChargeReviewNoteTooLong"]);
            }

            return normalized;
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

    public sealed class PurgeExpiredInvoiceArchivesHandler
    {
        private const int MaxBatchSize = 250;
        private const string EventType = "InvoiceArchivePurged";

        private readonly IAppDbContext _db;
        private readonly IClock _clock;

        public PurgeExpiredInvoiceArchivesHandler(IAppDbContext db, IClock? clock = null)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _clock = clock ?? DefaultHandlerDependencies.DefaultClock;
        }

        public async Task<PurgeExpiredInvoiceArchivesResultDto> HandleAsync(int batchSize = 100, CancellationToken ct = default)
        {
            var normalizedBatchSize = Math.Clamp(batchSize, 1, MaxBatchSize);
            var nowUtc = _clock.UtcNow;

            var invoices = await _db.Set<Invoice>()
                .Where(x => !x.IsDeleted)
                .Where(x => x.ArchiveRetainUntilUtc.HasValue && x.ArchiveRetainUntilUtc <= nowUtc)
                .Where(x => !x.ArchivePurgedAtUtc.HasValue)
                .Where(x => !string.IsNullOrWhiteSpace(x.IssuedSnapshotJson) || !string.IsNullOrWhiteSpace(x.IssuedSnapshotHashSha256))
                .OrderBy(x => x.ArchiveRetainUntilUtc)
                .ThenBy(x => x.Id)
                .Take(normalizedBatchSize)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            foreach (var invoice in invoices)
            {
                var retainedUntilUtc = invoice.ArchiveRetainUntilUtc;
                var policyVersion = invoice.ArchiveRetentionPolicyVersion;

                invoice.IssuedSnapshotJson = null;
                invoice.IssuedSnapshotHashSha256 = null;
                invoice.ArchivePurgedAtUtc = nowUtc;
                invoice.ArchivePurgeReason = "Retention period elapsed";

                _db.Set<EventLog>().Add(new EventLog
                {
                    Type = EventType,
                    OccurredAtUtc = nowUtc,
                    IdempotencyKey = $"{EventType}:{invoice.Id:N}",
                    PropertiesJson = JsonSerializer.Serialize(new
                    {
                        invoiceId = invoice.Id,
                        retainedUntilUtc,
                        policyVersion
                    })
                });
            }

            await _db.SaveChangesAsync(ct).ConfigureAwait(false);

            return new PurgeExpiredInvoiceArchivesResultDto
            {
                EvaluatedCount = invoices.Count,
                PurgedCount = invoices.Count,
                PurgedInvoiceIds = invoices.Select(x => x.Id).ToList()
            };
        }
    }

    internal static class InvoiceIssueReadinessGuard
    {
        public static async Task ValidateAsync(
            IAppDbContext db,
            Invoice invoice,
            Guid? customerId,
            IStringLocalizer<ValidationResource> localizer,
            CancellationToken ct)
        {
            var settings = await db.Set<SiteSetting>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);

            if (settings is null || !IsIssuerReady(settings))
            {
                throw new InvalidOperationException(localizer["InvoiceIssuerDataRequiredBeforeIssuing"]);
            }

            if (!settings.VatEnabled || !customerId.HasValue)
            {
                return;
            }

            var customer = await db.Set<Customer>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == customerId.Value && !x.IsDeleted, ct)
                .ConfigureAwait(false);

            if (customer is null)
            {
                return;
            }

            if (customer.TaxProfileType == CustomerTaxProfileType.Business && string.IsNullOrWhiteSpace(customer.VatId))
            {
                throw new InvalidOperationException(localizer["BusinessCustomerVatIdRequiredBeforeIssuingInvoice"]);
            }
        }

        private static bool IsIssuerReady(SiteSetting settings)
            => !string.IsNullOrWhiteSpace(settings.InvoiceIssuerLegalName) &&
               !string.IsNullOrWhiteSpace(settings.InvoiceIssuerTaxId) &&
               !string.IsNullOrWhiteSpace(settings.InvoiceIssuerAddressLine1) &&
               !string.IsNullOrWhiteSpace(settings.InvoiceIssuerPostalCode) &&
               !string.IsNullOrWhiteSpace(settings.InvoiceIssuerCity) &&
               !string.IsNullOrWhiteSpace(settings.InvoiceIssuerCountry);
    }

    internal static class InvoiceIssueSnapshotWriter
    {
        private static readonly JsonSerializerOptions SnapshotJsonOptions = new(JsonSerializerDefaults.Web);

        public static async Task CaptureIfMissingAsync(
            IAppDbContext db,
            Invoice invoice,
            DateTime nowUtc,
            CancellationToken ct)
        {
            if (invoice.IssuedAtUtc.HasValue && !string.IsNullOrWhiteSpace(invoice.IssuedSnapshotJson))
            {
                return;
            }

            var settings = await db.Set<SiteSetting>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);

            var customer = invoice.CustomerId.HasValue
                ? await db.Set<Customer>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == invoice.CustomerId.Value && !x.IsDeleted, ct)
                    .ConfigureAwait(false)
                : null;

            var business = invoice.BusinessId.HasValue
                ? await db.Set<Business>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == invoice.BusinessId.Value && !x.IsDeleted, ct)
                    .ConfigureAwait(false)
                : null;

            var lines = await db.Set<InvoiceLine>()
                .AsNoTracking()
                .Where(x => x.InvoiceId == invoice.Id && !x.IsDeleted)
                .OrderBy(x => x.CreatedAtUtc)
                .ThenBy(x => x.Id)
                .Select(x => new InvoiceIssueSnapshotLine(
                    x.Id,
                    x.Description,
                    x.Quantity,
                    x.UnitPriceNetMinor,
                    x.TaxRate,
                    x.TotalNetMinor,
                    x.TotalGrossMinor))
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var issuedAtUtc = invoice.IssuedAtUtc ?? nowUtc;
            invoice.IssuedAtUtc = issuedAtUtc;
            invoice.IssuedSnapshotJson ??= JsonSerializer.Serialize(
                new InvoiceIssueSnapshot(
                    1,
                    invoice.Id,
                    invoice.BusinessId,
                    invoice.CustomerId,
                    invoice.OrderId,
                    invoice.PaymentId,
                    invoice.Status,
                    invoice.Currency,
                    invoice.TotalNetMinor,
                    invoice.TotalTaxMinor,
                    invoice.TotalGrossMinor,
                    invoice.DueDateUtc,
                    invoice.PaidAtUtc,
                    issuedAtUtc,
                    new InvoiceIssueSnapshotIssuer(
                        settings?.InvoiceIssuerLegalName,
                        settings?.InvoiceIssuerTaxId,
                        settings?.InvoiceIssuerAddressLine1,
                        settings?.InvoiceIssuerPostalCode,
                        settings?.InvoiceIssuerCity,
                        settings?.InvoiceIssuerCountry),
                    customer is null
                        ? null
                        : new InvoiceIssueSnapshotCustomer(
                            customer.Id,
                            customer.FirstName,
                            customer.LastName,
                            customer.CompanyName,
                            customer.Email,
                            customer.Phone,
                            customer.TaxProfileType,
                            customer.VatId),
                    business is null
                        ? null
                        : new InvoiceIssueSnapshotBusiness(
                            business.Id,
                            business.Name,
                            business.LegalName,
                            business.TaxId,
                            business.DefaultCurrency,
                            business.DefaultCulture),
                    lines),
                SnapshotJsonOptions);

            invoice.IssuedSnapshotHashSha256 ??= ComputeSha256(invoice.IssuedSnapshotJson);
            invoice.ArchiveGeneratedAtUtc ??= issuedAtUtc;
            var retentionYears = Math.Clamp(settings?.InvoiceArchiveRetentionYears ?? 10, 1, 30);
            invoice.ArchiveRetainUntilUtc ??= issuedAtUtc.AddYears(retentionYears);
            invoice.ArchiveRetentionPolicyVersion ??= $"invoice-archive-retention:v1:{retentionYears}y";
        }

        private static string ComputeSha256(string value)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private sealed record InvoiceIssueSnapshot(
            int SchemaVersion,
            Guid InvoiceId,
            Guid? BusinessId,
            Guid? CustomerId,
            Guid? OrderId,
            Guid? PaymentId,
            InvoiceStatus Status,
            string Currency,
            long TotalNetMinor,
            long TotalTaxMinor,
            long TotalGrossMinor,
            DateTime DueDateUtc,
            DateTime? PaidAtUtc,
            DateTime IssuedAtUtc,
            InvoiceIssueSnapshotIssuer Issuer,
            InvoiceIssueSnapshotCustomer? Customer,
            InvoiceIssueSnapshotBusiness? Business,
            IReadOnlyCollection<InvoiceIssueSnapshotLine> Lines);

        private sealed record InvoiceIssueSnapshotIssuer(
            string? LegalName,
            string? TaxId,
            string? AddressLine1,
            string? PostalCode,
            string? City,
            string? Country);

        private sealed record InvoiceIssueSnapshotCustomer(
            Guid Id,
            string? FirstName,
            string? LastName,
            string? CompanyName,
            string? Email,
            string? Phone,
            CustomerTaxProfileType TaxProfileType,
            string? VatId);

        private sealed record InvoiceIssueSnapshotBusiness(
            Guid Id,
            string Name,
            string? LegalName,
            string? TaxId,
            string DefaultCurrency,
            string DefaultCulture);

        private sealed record InvoiceIssueSnapshotLine(
            Guid Id,
            string Description,
            int Quantity,
            long UnitPriceNetMinor,
            decimal TaxRate,
            long TotalNetMinor,
            long TotalGrossMinor);
    }
}

