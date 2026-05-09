using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Billing.DTOs;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Billing.Queries
{
    public sealed class GetTaxComplianceOverviewHandler
    {
        private readonly IAppDbContext _db;
        private readonly IClock _clock;

        public GetTaxComplianceOverviewHandler(IAppDbContext db, IClock clock)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public async Task<TaxComplianceOverviewDto> HandleAsync(int invoicePageSize = 10, int customerPageSize = 10, CancellationToken ct = default)
        {
            if (invoicePageSize < 1) invoicePageSize = 10;
            if (customerPageSize < 1) customerPageSize = 10;

            var nowUtc = _clock.UtcNow;
            var dueSoonUtc = nowUtc.AddDays(7);
            var settings = await _db.Set<Domain.Entities.Settings.SiteSetting>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.Id)
                .Select(x => new { x.VatEnabled, x.AllowReverseCharge, x.InvoiceIssuerCountry })
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);
            var issuerCountry = NormalizeCountry(settings?.InvoiceIssuerCountry);
            var reverseChargeReviewEnabled = settings?.VatEnabled == true &&
                settings.AllowReverseCharge &&
                !string.IsNullOrWhiteSpace(issuerCountry);

            var businessCustomersQuery = _db.Set<Customer>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.TaxProfileType == CustomerTaxProfileType.Business);

            var missingVatCustomersQuery = businessCustomersQuery
                .Where(x => x.VatId == null || x.VatId.Trim() == string.Empty);
            var vatValidationReviewCustomersQuery = businessCustomersQuery
                .Where(x =>
                    x.VatId != null &&
                    x.VatId.Trim() != string.Empty &&
                    (x.VatValidationStatus == CustomerVatValidationStatus.Unknown ||
                     x.VatValidationStatus == CustomerVatValidationStatus.Invalid));

            var invoiceBaseQuery =
                from invoice in _db.Set<Invoice>().AsNoTracking()
                join customer in _db.Set<Customer>().AsNoTracking() on invoice.CustomerId equals customer.Id into customers
                from customer in customers.DefaultIfEmpty()
                join user in _db.Set<User>().AsNoTracking() on customer.UserId equals (Guid?)user.Id into users
                from user in users.DefaultIfEmpty()
                where !invoice.IsDeleted && (customer == null || !customer.IsDeleted) && (user == null || !user.IsDeleted)
                select new
                {
                    invoice,
                    customer,
                    user
                };

            var invoiceReviewQuery = invoiceBaseQuery.Where(x =>
                x.invoice.Status == InvoiceStatus.Draft ||
                (x.invoice.Status == InvoiceStatus.Open && x.invoice.DueDateUtc <= dueSoonUtc) ||
                (x.customer != null &&
                 x.customer.TaxProfileType == CustomerTaxProfileType.Business &&
                 (x.customer.VatId == null || x.customer.VatId.Trim() == string.Empty)) ||
                (reverseChargeReviewEnabled &&
                 x.customer != null &&
                 x.customer.TaxProfileType == CustomerTaxProfileType.Business &&
                 x.customer.VatId != null &&
                 x.customer.VatId.Trim() != string.Empty &&
                 x.invoice.ReverseChargeApplied == null &&
                 _db.Set<CustomerAddress>().Any(address =>
                     !address.IsDeleted &&
                     address.CustomerId == x.customer.Id &&
                     address.IsDefaultBilling &&
                     address.Country != null &&
                     address.Country.Trim() != string.Empty &&
                     address.Country.Trim().ToUpper() != issuerCountry)));

            var businessCustomersMissingVatIdCount = await missingVatCustomersQuery.CountAsync(ct).ConfigureAwait(false);
            var businessCustomersVatValidationReviewCount = await vatValidationReviewCustomersQuery.CountAsync(ct).ConfigureAwait(false);
            var businessInvoiceSummary = await invoiceBaseQuery
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    BusinessInvoicesMissingVatIdCount = g.Count(x =>
                        x.customer != null &&
                        x.customer.TaxProfileType == CustomerTaxProfileType.Business &&
                        (x.customer.VatId == null || x.customer.VatId.Trim() == string.Empty)),
                    ReverseChargeCandidateInvoiceCount = reverseChargeReviewEnabled
                        ? g.Count(x =>
                            x.customer != null &&
                            x.customer.TaxProfileType == CustomerTaxProfileType.Business &&
                            x.customer.VatId != null &&
                            x.customer.VatId.Trim() != string.Empty &&
                            x.invoice.ReverseChargeApplied == null &&
                            _db.Set<CustomerAddress>().Any(address =>
                                !address.IsDeleted &&
                                address.CustomerId == x.customer.Id &&
                                address.IsDefaultBilling &&
                                address.Country != null &&
                                address.Country.Trim() != string.Empty &&
                                address.Country.Trim().ToUpper() != issuerCountry))
                        : 0
                })
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);

            var invoiceStatusSummary = await _db.Set<Invoice>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    DraftInvoiceCount = g.Count(x => x.Status == InvoiceStatus.Draft),
                    DueSoonInvoiceCount = g.Count(x =>
                        x.Status == InvoiceStatus.Open &&
                        x.DueDateUtc >= nowUtc &&
                        x.DueDateUtc <= dueSoonUtc),
                    OverdueInvoiceCount = g.Count(x =>
                        x.Status == InvoiceStatus.Open &&
                        x.DueDateUtc < nowUtc)
                })
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);

            var invoiceRows = await invoiceReviewQuery
                .OrderByDescending(x => x.customer != null &&
                    x.customer.TaxProfileType == CustomerTaxProfileType.Business &&
                    (x.customer.VatId == null || x.customer.VatId.Trim() == string.Empty))
                .ThenBy(x => x.invoice.Status == InvoiceStatus.Open ? 0 : 1)
                .ThenBy(x => x.invoice.DueDateUtc)
                .ThenByDescending(x => x.invoice.ModifiedAtUtc ?? x.invoice.CreatedAtUtc)
                .Take(invoicePageSize)
                .Select(x => new
                {
                    x.invoice.Id,
                    x.invoice.RowVersion,
                    x.invoice.CustomerId,
                    x.invoice.OrderId,
                    x.invoice.PaymentId,
                    x.invoice.Status,
                    x.invoice.Currency,
                    x.invoice.TotalGrossMinor,
                    x.invoice.DueDateUtc,
                    x.invoice.ReverseChargeApplied,
                    x.invoice.ReverseChargeReviewedAtUtc,
                    x.invoice.CreatedAtUtc,
                    x.invoice.ModifiedAtUtc,
                    CustomerDisplayName = x.customer == null
                        ? string.Empty
                        : BuildCustomerDisplayName(x.customer, x.user),
                    CompanyName = x.customer != null ? x.customer.CompanyName : null,
                    CustomerTaxProfileType = x.customer != null ? (CustomerTaxProfileType?)x.customer.TaxProfileType : null,
                    CustomerVatId = x.customer != null ? x.customer.VatId : null,
                    CustomerCountry = x.customer != null
                        ? _db.Set<CustomerAddress>()
                            .Where(address =>
                                !address.IsDeleted &&
                                address.CustomerId == x.customer.Id &&
                                address.IsDefaultBilling)
                            .Select(address => address.Country)
                            .FirstOrDefault()
                        : null
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var orderIds = invoiceRows.Where(x => x.OrderId.HasValue).Select(x => x.OrderId!.Value).Distinct().ToList();
            var orderNumbers = orderIds.Count == 0
                ? new Dictionary<Guid, string>()
                : await _db.Set<Order>()
                    .AsNoTracking()
                    .Where(x => orderIds.Contains(x.Id) && !x.IsDeleted)
                    .ToDictionaryAsync(x => x.Id, x => x.OrderNumber, ct)
                    .ConfigureAwait(false);

            var invoiceItems = invoiceRows.Select(x => new TaxComplianceInvoiceReviewItemDto
            {
                Id = x.Id,
                RowVersion = x.RowVersion ?? Array.Empty<byte>(),
                CustomerId = x.CustomerId,
                CustomerDisplayName = x.CustomerDisplayName,
                CompanyName = x.CompanyName,
                CustomerTaxProfileType = x.CustomerTaxProfileType,
                CustomerVatId = x.CustomerVatId,
                OrderId = x.OrderId,
                OrderNumber = x.OrderId.HasValue && orderNumbers.TryGetValue(x.OrderId.Value, out var orderNumber) ? orderNumber : null,
                PaymentId = x.PaymentId,
                Status = x.Status,
                Currency = x.Currency,
                TotalGrossMinor = x.TotalGrossMinor,
                DueDateUtc = x.DueDateUtc,
                RequiresVatId =
                    x.CustomerTaxProfileType == CustomerTaxProfileType.Business &&
                    string.IsNullOrWhiteSpace(x.CustomerVatId),
                IsReverseChargeCandidate =
                    reverseChargeReviewEnabled &&
                    x.CustomerTaxProfileType == CustomerTaxProfileType.Business &&
                    !string.IsNullOrWhiteSpace(x.CustomerVatId) &&
                    x.ReverseChargeApplied == null &&
                    !string.IsNullOrWhiteSpace(x.CustomerCountry) &&
                    NormalizeCountry(x.CustomerCountry) != issuerCountry,
                ReverseChargeApplied = x.ReverseChargeApplied,
                ReverseChargeReviewedAtUtc = x.ReverseChargeReviewedAtUtc,
                IsDueSoon =
                    x.Status == InvoiceStatus.Open &&
                    x.DueDateUtc >= nowUtc &&
                    x.DueDateUtc <= dueSoonUtc,
                IsOverdue =
                    x.Status == InvoiceStatus.Open &&
                    x.DueDateUtc < nowUtc,
                CreatedAtUtc = x.CreatedAtUtc,
                ModifiedAtUtc = x.ModifiedAtUtc
            }).ToList();

            var customerItems = await
                (from customer in missingVatCustomersQuery.Concat(vatValidationReviewCustomersQuery)
                 join user in _db.Set<User>().AsNoTracking() on customer.UserId equals (Guid?)user.Id into users
                 from user in users.DefaultIfEmpty()
                 where user == null || !user.IsDeleted
                 let requiresVatId = customer.VatId == null || customer.VatId.Trim() == string.Empty
                 let requiresVatValidation = !requiresVatId &&
                     (customer.VatValidationStatus == CustomerVatValidationStatus.Unknown ||
                      customer.VatValidationStatus == CustomerVatValidationStatus.Invalid)
                 orderby customer.ModifiedAtUtc ?? customer.CreatedAtUtc descending
                 select new TaxComplianceCustomerReviewItemDto
                 {
                     Id = customer.Id,
                     RowVersion = customer.RowVersion ?? Array.Empty<byte>(),
                     UserId = customer.UserId,
                     DisplayName = BuildCustomerDisplayName(customer, user),
                     Email = customer.UserId.HasValue && user != null ? user.Email : customer.Email,
                     CompanyName = customer.CompanyName,
                     VatId = customer.VatId,
                     RequiresVatId = requiresVatId,
                     RequiresVatValidation = requiresVatValidation,
                     VatValidationStatus = customer.VatValidationStatus,
                     VatValidationCheckedAtUtc = customer.VatValidationCheckedAtUtc,
                     VatValidationSource = customer.VatValidationSource,
                     VatValidationMessage = customer.VatValidationMessage,
                     OpportunityCount = customer.Opportunities.Count,
                     CreatedAtUtc = customer.CreatedAtUtc,
                     ModifiedAtUtc = customer.ModifiedAtUtc
                 })
                .Take(customerPageSize)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return new TaxComplianceOverviewDto
            {
                Summary = new TaxComplianceOpsSummaryDto
                {
                    BusinessCustomersMissingVatIdCount = businessCustomersMissingVatIdCount,
                    BusinessCustomersVatValidationReviewCount = businessCustomersVatValidationReviewCount,
                    BusinessInvoicesMissingVatIdCount = businessInvoiceSummary?.BusinessInvoicesMissingVatIdCount ?? 0,
                    ReverseChargeCandidateInvoiceCount = businessInvoiceSummary?.ReverseChargeCandidateInvoiceCount ?? 0,
                    DraftInvoiceCount = invoiceStatusSummary?.DraftInvoiceCount ?? 0,
                    DueSoonInvoiceCount = invoiceStatusSummary?.DueSoonInvoiceCount ?? 0,
                    OverdueInvoiceCount = invoiceStatusSummary?.OverdueInvoiceCount ?? 0
                },
                InvoiceItems = invoiceItems,
                CustomerItems = customerItems
            };
        }

        private static string BuildCustomerDisplayName(Customer customer, User? user)
        {
            if (customer.UserId.HasValue && user != null)
            {
                var linkedName = $"{user.FirstName ?? string.Empty} {user.LastName ?? string.Empty}".Trim();
                if (!string.IsNullOrWhiteSpace(linkedName))
                {
                    return linkedName;
                }

                return user.Email;
            }

            var crmName = $"{customer.FirstName} {customer.LastName}".Trim();
            if (!string.IsNullOrWhiteSpace(crmName))
            {
                return crmName;
            }

            if (!string.IsNullOrWhiteSpace(customer.CompanyName))
            {
                return customer.CompanyName;
            }

            return customer.Email;
        }

        private static string NormalizeCountry(string? country) =>
            string.IsNullOrWhiteSpace(country) ? string.Empty : country.Trim().ToUpperInvariant();
    }

    public sealed class GetTaxComplianceInvoiceExportHandler
    {
        private const int MaxExportRows = 5000;

        private readonly IAppDbContext _db;
        private readonly IClock _clock;

        public GetTaxComplianceInvoiceExportHandler(IAppDbContext db, IClock clock)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public async Task<List<TaxComplianceInvoiceExportRowDto>> HandleAsync(int take = MaxExportRows, CancellationToken ct = default)
        {
            var normalizedTake = Math.Clamp(take, 1, MaxExportRows);
            var nowUtc = _clock.UtcNow;
            var dueSoonUtc = nowUtc.AddDays(7);
            var settings = await _db.Set<Domain.Entities.Settings.SiteSetting>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.Id)
                .Select(x => new { x.VatEnabled, x.AllowReverseCharge, x.InvoiceIssuerCountry })
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);
            var issuerCountry = NormalizeCountry(settings?.InvoiceIssuerCountry);
            var reverseChargeReviewEnabled = settings?.VatEnabled == true &&
                settings.AllowReverseCharge &&
                !string.IsNullOrWhiteSpace(issuerCountry);

            var rows = await
                (from invoice in _db.Set<Invoice>().AsNoTracking()
                 join customer in _db.Set<Customer>().AsNoTracking() on invoice.CustomerId equals customer.Id into customers
                 from customer in customers.DefaultIfEmpty()
                 join user in _db.Set<User>().AsNoTracking() on customer.UserId equals (Guid?)user.Id into users
                 from user in users.DefaultIfEmpty()
                 where !invoice.IsDeleted && (customer == null || !customer.IsDeleted) && (user == null || !user.IsDeleted)
                 orderby invoice.ModifiedAtUtc ?? invoice.CreatedAtUtc descending
                 select new
                 {
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
                     invoice.ReverseChargeApplied,
                     invoice.ReverseChargeReviewedAtUtc,
                     invoice.CreatedAtUtc,
                     invoice.ModifiedAtUtc,
                     CustomerDisplayName = customer == null
                         ? string.Empty
                         : BuildCustomerDisplayName(customer, user),
                     CompanyName = customer != null ? customer.CompanyName : null,
                     CustomerTaxProfileType = customer != null ? (CustomerTaxProfileType?)customer.TaxProfileType : null,
                     CustomerVatId = customer != null ? customer.VatId : null,
                     CustomerCountry = customer != null
                         ? _db.Set<CustomerAddress>()
                             .Where(address =>
                                 !address.IsDeleted &&
                                 address.CustomerId == customer.Id &&
                                 address.IsDefaultBilling)
                             .Select(address => address.Country)
                             .FirstOrDefault()
                         : null
                 })
                .Take(normalizedTake)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var orderIds = rows.Where(x => x.OrderId.HasValue).Select(x => x.OrderId!.Value).Distinct().ToList();
            var orderNumbers = orderIds.Count == 0
                ? new Dictionary<Guid, string>()
                : await _db.Set<Order>()
                    .AsNoTracking()
                    .Where(x => orderIds.Contains(x.Id) && !x.IsDeleted)
                    .ToDictionaryAsync(x => x.Id, x => x.OrderNumber, ct)
                    .ConfigureAwait(false);

            return rows.Select(x => new TaxComplianceInvoiceExportRowDto
            {
                InvoiceId = x.Id,
                BusinessId = x.BusinessId,
                CustomerId = x.CustomerId,
                CustomerDisplayName = x.CustomerDisplayName,
                CompanyName = x.CompanyName,
                CustomerTaxProfileType = x.CustomerTaxProfileType,
                CustomerVatId = x.CustomerVatId,
                OrderId = x.OrderId,
                OrderNumber = x.OrderId.HasValue && orderNumbers.TryGetValue(x.OrderId.Value, out var orderNumber) ? orderNumber : null,
                PaymentId = x.PaymentId,
                Status = x.Status,
                Currency = x.Currency,
                TotalNetMinor = x.TotalNetMinor,
                TotalTaxMinor = x.TotalTaxMinor,
                TotalGrossMinor = x.TotalGrossMinor,
                DueDateUtc = x.DueDateUtc,
                RequiresVatId =
                    x.CustomerTaxProfileType == CustomerTaxProfileType.Business &&
                    string.IsNullOrWhiteSpace(x.CustomerVatId),
                IsReverseChargeCandidate =
                    reverseChargeReviewEnabled &&
                    x.CustomerTaxProfileType == CustomerTaxProfileType.Business &&
                    !string.IsNullOrWhiteSpace(x.CustomerVatId) &&
                    x.ReverseChargeApplied == null &&
                    !string.IsNullOrWhiteSpace(x.CustomerCountry) &&
                    NormalizeCountry(x.CustomerCountry) != issuerCountry,
                ReverseChargeApplied = x.ReverseChargeApplied,
                ReverseChargeReviewedAtUtc = x.ReverseChargeReviewedAtUtc,
                IsDueSoon =
                    x.Status == InvoiceStatus.Open &&
                    x.DueDateUtc >= nowUtc &&
                    x.DueDateUtc <= dueSoonUtc,
                IsOverdue =
                    x.Status == InvoiceStatus.Open &&
                    x.DueDateUtc < nowUtc,
                CreatedAtUtc = x.CreatedAtUtc,
                ModifiedAtUtc = x.ModifiedAtUtc
            }).ToList();
        }

        private static string BuildCustomerDisplayName(Customer customer, User? user)
        {
            if (customer.UserId.HasValue && user != null)
            {
                var linkedName = $"{user.FirstName ?? string.Empty} {user.LastName ?? string.Empty}".Trim();
                if (!string.IsNullOrWhiteSpace(linkedName))
                {
                    return linkedName;
                }

                return user.Email;
            }

            var crmName = $"{customer.FirstName} {customer.LastName}".Trim();
            if (!string.IsNullOrWhiteSpace(crmName))
            {
                return crmName;
            }

            if (!string.IsNullOrWhiteSpace(customer.CompanyName))
            {
                return customer.CompanyName;
            }

            return customer.Email;
        }

        private static string NormalizeCountry(string? country) =>
            string.IsNullOrWhiteSpace(country) ? string.Empty : country.Trim().ToUpperInvariant();
    }
}
