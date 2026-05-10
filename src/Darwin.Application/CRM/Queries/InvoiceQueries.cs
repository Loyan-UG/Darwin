using Darwin.Application.Abstractions.Invoicing;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.CRM.Services;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Common;
using Darwin.Application.CRM.DTOs;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Xml.Linq;

namespace Darwin.Application.CRM.Queries
{
    public sealed class GetInvoicesPageHandler
    {
        private const int MaxPageSize = 200;

        private readonly IAppDbContext _db;
        private readonly IClock _clock;

        public GetInvoicesPageHandler(IAppDbContext db, IClock clock)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public async Task<(List<InvoiceListItemDto> Items, int Total)> HandleAsync(
            int page,
            int pageSize,
            string? query = null,
            InvoiceQueueFilter filter = InvoiceQueueFilter.All,
            CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;

            var baseQuery = _db.Set<Invoice>().AsNoTracking().Where(x => !x.IsDeleted);
            if (!string.IsNullOrWhiteSpace(query))
            {
                var q = QueryLikePattern.Contains(query);
                var hasGuidQuery = Guid.TryParse(query.Trim(), out var guidQuery);
                baseQuery = baseQuery.Where(x =>
                    EF.Functions.Like(x.Currency, q, QueryLikePattern.EscapeCharacter) ||
                    (hasGuidQuery &&
                     ((x.CustomerId.HasValue && x.CustomerId.Value == guidQuery) ||
                      (x.OrderId.HasValue && x.OrderId.Value == guidQuery) ||
                      (x.PaymentId.HasValue && x.PaymentId.Value == guidQuery))));
            }

            var todayUtc = _clock.UtcNow.Date;
            var dueSoonThresholdUtc = todayUtc.AddDays(7);
            baseQuery = filter switch
            {
                InvoiceQueueFilter.Draft => baseQuery.Where(x => x.Status == InvoiceStatus.Draft),
                InvoiceQueueFilter.DueSoon => baseQuery.Where(x => x.Status != InvoiceStatus.Paid && x.DueDateUtc >= todayUtc && x.DueDateUtc <= dueSoonThresholdUtc),
                InvoiceQueueFilter.Overdue => baseQuery.Where(x => x.Status != InvoiceStatus.Paid && x.DueDateUtc < todayUtc),
                InvoiceQueueFilter.MissingVatId => baseQuery.Where(x => x.CustomerId.HasValue && _db.Set<Customer>().Any(customer => !customer.IsDeleted && customer.Id == x.CustomerId.Value && customer.TaxProfileType == CustomerTaxProfileType.Business && (customer.VatId == null || customer.VatId == string.Empty))),
                InvoiceQueueFilter.Refunded => baseQuery.Where(x => x.PaymentId.HasValue && _db.Set<Refund>().Any(refund => !refund.IsDeleted && refund.PaymentId == x.PaymentId.Value && refund.Status == RefundStatus.Completed)),
                _ => baseQuery
            };

            var total = await baseQuery.CountAsync(ct).ConfigureAwait(false);
            var items = await baseQuery
                .OrderByDescending(x => x.ModifiedAtUtc ?? x.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new InvoiceListItemDto
                {
                    Id = x.Id,
                    BusinessId = x.BusinessId,
                    CustomerId = x.CustomerId,
                    OrderId = x.OrderId,
                    PaymentId = x.PaymentId,
                    Status = x.Status,
                    Currency = x.Currency,
                    TotalNetMinor = x.TotalNetMinor,
                    TotalTaxMinor = x.TotalTaxMinor,
                    TotalGrossMinor = x.TotalGrossMinor,
                    DueDateUtc = x.DueDateUtc,
                    PaidAtUtc = x.PaidAtUtc,
                    IssuedAtUtc = x.IssuedAtUtc,
                    HasIssuedSnapshot = !string.IsNullOrWhiteSpace(x.IssuedSnapshotJson),
                    IssuedSnapshotHashSha256 = x.IssuedSnapshotHashSha256,
                    ArchiveGeneratedAtUtc = x.ArchiveGeneratedAtUtc,
                    ArchiveRetainUntilUtc = x.ArchiveRetainUntilUtc,
                    ArchiveRetentionPolicyVersion = x.ArchiveRetentionPolicyVersion,
                    ArchivePurgedAtUtc = x.ArchivePurgedAtUtc,
                    ArchivePurgeReason = x.ArchivePurgeReason,
                    RowVersion = x.RowVersion
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            await EnrichInvoicesAsync(items, ct).ConfigureAwait(false);
            return (items, total);
        }

        private async Task EnrichInvoicesAsync(List<InvoiceListItemDto> items, CancellationToken ct)
        {
            if (items.Count == 0)
            {
                return;
            }

            var customerIds = items.Where(x => x.CustomerId.HasValue).Select(x => x.CustomerId!.Value).Distinct().ToList();
            var orderIds = items.Where(x => x.OrderId.HasValue).Select(x => x.OrderId!.Value).Distinct().ToList();
            var paymentIds = items.Where(x => x.PaymentId.HasValue).Select(x => x.PaymentId!.Value).Distinct().ToList();

            var customers = customerIds.Count == 0
                ? new List<Customer>()
                : await _db.Set<Customer>().AsNoTracking().Where(x => customerIds.Contains(x.Id) && !x.IsDeleted).ToListAsync(ct).ConfigureAwait(false);

            var userIds = customers.Where(x => x.UserId.HasValue).Select(x => x.UserId!.Value).Distinct().ToList();
            var users = userIds.Count == 0
                ? new Dictionary<Guid, User>()
                : await _db.Set<User>().AsNoTracking().Where(x => userIds.Contains(x.Id) && !x.IsDeleted).ToDictionaryAsync(x => x.Id, ct).ConfigureAwait(false);

            var orders = orderIds.Count == 0
                ? new Dictionary<Guid, string>()
                : await _db.Set<Order>().AsNoTracking().Where(x => orderIds.Contains(x.Id) && !x.IsDeleted).ToDictionaryAsync(x => x.Id, x => x.OrderNumber, ct).ConfigureAwait(false);

            var payments = paymentIds.Count == 0
                ? new Dictionary<Guid, Payment>()
                : await _db.Set<Payment>().AsNoTracking().Where(x => paymentIds.Contains(x.Id) && !x.IsDeleted).ToDictionaryAsync(x => x.Id, ct).ConfigureAwait(false);
            var refundTotals = paymentIds.Count == 0
                ? new Dictionary<Guid, long>()
                : await _db.Set<Refund>()
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted && x.Status == RefundStatus.Completed && paymentIds.Contains(x.PaymentId))
                    .GroupBy(x => x.PaymentId)
                    .Select(x => new { PaymentId = x.Key, AmountMinor = x.Sum(r => r.AmountMinor) })
                    .ToDictionaryAsync(x => x.PaymentId, x => x.AmountMinor, ct)
                    .ConfigureAwait(false);

            var customerMap = customers.ToDictionary(x => x.Id);
            foreach (var item in items)
            {
                if (item.CustomerId.HasValue && customerMap.TryGetValue(item.CustomerId.Value, out var customer))
                {
                    item.CustomerDisplayName = Darwin.Application.Billing.Queries.BillingPaymentDisplayFormatter.BuildCustomerDisplayName(customer, users);
                    item.CustomerTaxProfileType = customer.TaxProfileType;
                    item.CustomerVatId = customer.VatId;
                }

                if (item.OrderId.HasValue && orders.TryGetValue(item.OrderId.Value, out var orderNumber))
                {
                    item.OrderNumber = orderNumber;
                }

                if (item.PaymentId.HasValue && payments.TryGetValue(item.PaymentId.Value, out var payment))
                {
                    item.PaymentSummary = BuildPaymentSummary(payment);
                    var refundedAmountMinor = Darwin.Application.Billing.Queries.BillingReconciliationCalculator.ClampRefundedAmount(
                        payment.AmountMinor,
                        refundTotals.TryGetValue(payment.Id, out var paymentRefundedAmountMinor) ? paymentRefundedAmountMinor : 0L);
                    var settledAmountMinor = Darwin.Application.Billing.Queries.BillingReconciliationCalculator.CalculateSettledAmount(
                        item.TotalGrossMinor,
                        Darwin.Application.Billing.Queries.BillingReconciliationCalculator.CalculateNetCollectedAmount(payment.AmountMinor, refundedAmountMinor));

                    item.RefundedAmountMinor = Math.Min(refundedAmountMinor, item.TotalGrossMinor);
                    item.SettledAmountMinor = settledAmountMinor;
                    item.BalanceMinor = Darwin.Application.Billing.Queries.BillingReconciliationCalculator.CalculateBalanceAmount(item.TotalGrossMinor, settledAmountMinor);
                }
                else
                {
                    item.RefundedAmountMinor = 0L;
                    item.SettledAmountMinor = item.Status == InvoiceStatus.Paid ? item.TotalGrossMinor : 0L;
                    item.BalanceMinor = Darwin.Application.Billing.Queries.BillingReconciliationCalculator.CalculateBalanceAmount(item.TotalGrossMinor, item.SettledAmountMinor);
                }
            }
        }

        private static string BuildPaymentSummary(Payment payment)
        {
            return $"{payment.Provider} | {payment.Currency} {(payment.AmountMinor / 100.0M):0.00} | {payment.Status}";
        }
    }

    public sealed class GetInvoiceForEditHandler
    {
        private readonly IAppDbContext _db;

        public GetInvoiceForEditHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task<InvoiceEditDto?> HandleAsync(Guid id, CancellationToken ct = default)
        {
            var invoice = await _db.Set<Invoice>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, ct)
                .ConfigureAwait(false);

            if (invoice is null)
            {
                return null;
            }

            string customerDisplayName = string.Empty;
            Customer? customer = null;
            if (invoice.CustomerId.HasValue)
            {
                customer = await _db.Set<Customer>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == invoice.CustomerId.Value, ct).ConfigureAwait(false);
                if (customer is not null)
                {
                    User? linkedUser = null;
                    if (customer.UserId.HasValue)
                    {
                        linkedUser = await _db.Set<User>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == customer.UserId.Value, ct).ConfigureAwait(false);
                    }

                    customerDisplayName = Darwin.Application.Billing.Queries.BillingPaymentDisplayFormatter.BuildCustomerDisplayName(customer, linkedUser);
                }
            }

            var orderNumber = invoice.OrderId.HasValue
                ? await _db.Set<Order>().AsNoTracking().Where(x => x.Id == invoice.OrderId.Value).Select(x => x.OrderNumber).FirstOrDefaultAsync(ct).ConfigureAwait(false)
                : null;

            var paymentSummary = string.Empty;
            long refundedAmountMinor = 0L;
            long settledAmountMinor = invoice.Status == InvoiceStatus.Paid ? invoice.TotalGrossMinor : 0L;
            long balanceMinor = Darwin.Application.Billing.Queries.BillingReconciliationCalculator.CalculateBalanceAmount(invoice.TotalGrossMinor, settledAmountMinor);
            if (invoice.PaymentId.HasValue)
            {
                var payment = await _db.Set<Payment>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == invoice.PaymentId.Value, ct).ConfigureAwait(false);
                if (payment is not null)
                {
                    paymentSummary = $"{payment.Provider} | {payment.Currency} {(payment.AmountMinor / 100.0M):0.00} | {payment.Status}";
                    refundedAmountMinor = Darwin.Application.Billing.Queries.BillingReconciliationCalculator.ClampRefundedAmount(
                        payment.AmountMinor,
                        await _db.Set<Refund>()
                            .AsNoTracking()
                            .Where(x => x.PaymentId == payment.Id && x.Status == RefundStatus.Completed)
                            .SumAsync(x => (long?)x.AmountMinor, ct)
                            .ConfigureAwait(false) ?? 0L);
                    settledAmountMinor = Darwin.Application.Billing.Queries.BillingReconciliationCalculator.CalculateSettledAmount(
                        invoice.TotalGrossMinor,
                        Darwin.Application.Billing.Queries.BillingReconciliationCalculator.CalculateNetCollectedAmount(payment.AmountMinor, refundedAmountMinor));
                    balanceMinor = Darwin.Application.Billing.Queries.BillingReconciliationCalculator.CalculateBalanceAmount(invoice.TotalGrossMinor, settledAmountMinor);
                }
            }

            return new InvoiceEditDto
            {
                Id = invoice.Id,
                RowVersion = invoice.RowVersion,
                BusinessId = invoice.BusinessId,
                CustomerId = invoice.CustomerId,
                CustomerDisplayName = customerDisplayName,
                CustomerTaxProfileType = customer?.TaxProfileType,
                CustomerVatId = customer?.VatId,
                OrderId = invoice.OrderId,
                OrderNumber = orderNumber,
                PaymentId = invoice.PaymentId,
                PaymentSummary = paymentSummary,
                Status = invoice.Status,
                Currency = invoice.Currency,
                TotalNetMinor = invoice.TotalNetMinor,
                TotalTaxMinor = invoice.TotalTaxMinor,
                TotalGrossMinor = invoice.TotalGrossMinor,
                RefundedAmountMinor = Math.Min(refundedAmountMinor, invoice.TotalGrossMinor),
                SettledAmountMinor = settledAmountMinor,
                BalanceMinor = balanceMinor,
                DueDateUtc = invoice.DueDateUtc,
                PaidAtUtc = invoice.PaidAtUtc,
                IssuedAtUtc = invoice.IssuedAtUtc,
                HasIssuedSnapshot = !string.IsNullOrWhiteSpace(invoice.IssuedSnapshotJson),
                IssuedSnapshotHashSha256 = invoice.IssuedSnapshotHashSha256,
                ArchiveGeneratedAtUtc = invoice.ArchiveGeneratedAtUtc,
                ArchiveRetainUntilUtc = invoice.ArchiveRetainUntilUtc,
                ArchiveRetentionPolicyVersion = invoice.ArchiveRetentionPolicyVersion,
                ArchivePurgedAtUtc = invoice.ArchivePurgedAtUtc,
                ArchivePurgeReason = invoice.ArchivePurgeReason
            };
        }
    }

    public sealed class GetInvoiceArchiveSnapshotHandler
    {
        private readonly IInvoiceArchiveStorage _archiveStorage;

        public GetInvoiceArchiveSnapshotHandler(
            IAppDbContext db,
            IInvoiceArchiveStorage? archiveStorage = null)
        {
            ArgumentNullException.ThrowIfNull(db);
            _archiveStorage = archiveStorage ?? new DatabaseInvoiceArchiveStorage(db);
        }

        public async Task<InvoiceArchiveSnapshotDto?> HandleAsync(Guid id, CancellationToken ct = default)
        {
            var artifact = await _archiveStorage.ReadAsync(id, ct).ConfigureAwait(false);
            if (artifact is null)
            {
                return null;
            }

            return new InvoiceArchiveSnapshotDto
            {
                InvoiceId = artifact.InvoiceId,
                IssuedAtUtc = artifact.IssuedAtUtc,
                FileName = artifact.FileName,
                SnapshotJson = artifact.Payload
            };
        }
    }

    public sealed class GetInvoiceArchiveDocumentHandler
    {
        private readonly GetInvoiceArchiveSnapshotHandler _snapshots;

        public GetInvoiceArchiveDocumentHandler(GetInvoiceArchiveSnapshotHandler snapshots)
        {
            _snapshots = snapshots ?? throw new ArgumentNullException(nameof(snapshots));
        }

        public async Task<InvoiceArchiveDocumentDto?> HandleAsync(Guid id, CancellationToken ct = default)
        {
            var snapshot = await _snapshots.HandleAsync(id, ct).ConfigureAwait(false);
            if (snapshot is null)
            {
                return null;
            }

            return new InvoiceArchiveDocumentDto
            {
                InvoiceId = snapshot.InvoiceId,
                IssuedAtUtc = snapshot.IssuedAtUtc,
                FileName = $"invoice-{snapshot.InvoiceId:N}-archive.html",
                Html = BuildHtml(snapshot.SnapshotJson)
            };
        }

        private static string BuildHtml(string snapshotJson)
        {
            using var document = JsonDocument.Parse(snapshotJson);
            var root = document.RootElement;
            var currency = GetString(root, "currency") ?? "EUR";
            var invoiceId = GetString(root, "invoiceId") ?? string.Empty;
            var issuedAtUtc = GetDateTime(root, "issuedAtUtc");
            var dueDateUtc = GetDateTime(root, "dueDateUtc");
            var issuer = TryGet(root, "issuer");
            var customer = TryGet(root, "customer");
            var business = TryGet(root, "business");
            var encoder = HtmlEncoder.Default;
            var html = new StringBuilder();

            html.AppendLine("<!doctype html>");
            html.AppendLine("<html lang=\"en\">");
            html.AppendLine("<head>");
            html.AppendLine("<meta charset=\"utf-8\">");
            html.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
            html.Append("<title>Invoice ").Append(encoder.Encode(invoiceId)).AppendLine("</title>");
            html.AppendLine("<style>body{font-family:Arial,sans-serif;margin:2rem;color:#1f2937}.header{display:flex;justify-content:space-between;gap:2rem;border-bottom:1px solid #d1d5db;padding-bottom:1rem;margin-bottom:1.5rem}.muted{color:#6b7280}.grid{display:grid;grid-template-columns:1fr 1fr;gap:1rem;margin-bottom:1.5rem}.box{border:1px solid #d1d5db;border-radius:8px;padding:1rem}table{width:100%;border-collapse:collapse;margin-top:1rem}th,td{border-bottom:1px solid #e5e7eb;padding:.6rem;text-align:left}th{background:#f9fafb}.amount{text-align:right}.totals{margin-left:auto;width:20rem}.small{font-size:.875rem}@media print{body{margin:1rem}.no-print{display:none}}</style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            html.AppendLine("<div class=\"header\">");
            html.AppendLine("<div>");
            html.AppendLine("<h1>Invoice archive document</h1>");
            html.Append("<div class=\"muted small\">Snapshot invoice ID: ").Append(encoder.Encode(invoiceId)).AppendLine("</div>");
            html.AppendLine("</div>");
            html.AppendLine("<div class=\"amount\">");
            html.Append("<div><strong>Issued:</strong> ").Append(encoder.Encode(FormatDate(issuedAtUtc))).AppendLine("</div>");
            html.Append("<div><strong>Due:</strong> ").Append(encoder.Encode(FormatDate(dueDateUtc))).AppendLine("</div>");
            html.AppendLine("</div>");
            html.AppendLine("</div>");

            html.AppendLine("<div class=\"grid\">");
            AppendParty(html, "Issuer", issuer, encoder);
            AppendParty(html, "Customer", customer, encoder);
            html.AppendLine("</div>");

            if (business.HasValue)
            {
                html.AppendLine("<div class=\"box\">");
                html.AppendLine("<h2>Business context</h2>");
                AppendLine(html, "Name", GetString(business.Value, "name"), encoder);
                AppendLine(html, "Legal name", GetString(business.Value, "legalName"), encoder);
                AppendLine(html, "Tax ID", GetString(business.Value, "taxId"), encoder);
                html.AppendLine("</div>");
            }

            html.AppendLine("<table>");
            html.AppendLine("<thead><tr><th>Description</th><th class=\"amount\">Qty</th><th class=\"amount\">Unit net</th><th class=\"amount\">Tax rate</th><th class=\"amount\">Net</th><th class=\"amount\">Gross</th></tr></thead>");
            html.AppendLine("<tbody>");
            if (root.TryGetProperty("lines", out var lines) && lines.ValueKind == JsonValueKind.Array)
            {
                foreach (var line in lines.EnumerateArray())
                {
                    html.AppendLine("<tr>");
                    html.Append("<td>").Append(encoder.Encode(GetString(line, "description") ?? string.Empty)).AppendLine("</td>");
                    html.Append("<td class=\"amount\">").Append(GetInt(line, "quantity").ToString(CultureInfo.InvariantCulture)).AppendLine("</td>");
                    html.Append("<td class=\"amount\">").Append(encoder.Encode(FormatMoney(GetLong(line, "unitPriceNetMinor"), currency))).AppendLine("</td>");
                    html.Append("<td class=\"amount\">").Append(encoder.Encode(FormatPercent(GetDecimal(line, "taxRate")))).AppendLine("</td>");
                    html.Append("<td class=\"amount\">").Append(encoder.Encode(FormatMoney(GetLong(line, "totalNetMinor"), currency))).AppendLine("</td>");
                    html.Append("<td class=\"amount\">").Append(encoder.Encode(FormatMoney(GetLong(line, "totalGrossMinor"), currency))).AppendLine("</td>");
                    html.AppendLine("</tr>");
                }
            }

            html.AppendLine("</tbody></table>");
            html.AppendLine("<table class=\"totals\">");
            AppendTotal(html, "Total net", GetLong(root, "totalNetMinor"), currency, encoder);
            AppendTotal(html, "Total tax", GetLong(root, "totalTaxMinor"), currency, encoder);
            AppendTotal(html, "Total gross", GetLong(root, "totalGrossMinor"), currency, encoder);
            html.AppendLine("</table>");
            html.AppendLine("<p class=\"muted small\">Generated from the immutable issued invoice snapshot. Keep retention and e-invoice submission rules in the compliance archive policy.</p>");
            html.AppendLine("</body></html>");
            return html.ToString();
        }

        private static JsonElement? TryGet(JsonElement element, string propertyName) =>
            element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Object ? value : null;

        private static string? GetString(JsonElement element, string propertyName) =>
            element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

        private static long GetLong(JsonElement element, string propertyName) =>
            element.TryGetProperty(propertyName, out var value) && value.TryGetInt64(out var result) ? result : 0L;

        private static int GetInt(JsonElement element, string propertyName) =>
            element.TryGetProperty(propertyName, out var value) && value.TryGetInt32(out var result) ? result : 0;

        private static decimal GetDecimal(JsonElement element, string propertyName) =>
            element.TryGetProperty(propertyName, out var value) && value.TryGetDecimal(out var result) ? result : 0m;

        private static DateTime? GetDateTime(JsonElement element, string propertyName) =>
            element.TryGetProperty(propertyName, out var value) && value.TryGetDateTime(out var result) ? result : null;

        private static string FormatDate(DateTime? value) =>
            value.HasValue ? value.Value.ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture) : "-";

        private static string FormatMoney(long minor, string currency) =>
            string.Create(CultureInfo.InvariantCulture, $"{minor / 100m:0.00} {currency}");

        private static string FormatPercent(decimal value) =>
            string.Create(CultureInfo.InvariantCulture, $"{value * 100m:0.####}%");

        private static void AppendParty(StringBuilder html, string title, JsonElement? party, HtmlEncoder encoder)
        {
            html.AppendLine("<div class=\"box\">");
            html.Append("<h2>").Append(encoder.Encode(title)).AppendLine("</h2>");
            if (!party.HasValue)
            {
                html.AppendLine("<div class=\"muted\">Not available</div>");
            }
            else
            {
                AppendLine(html, "Legal/name", GetString(party.Value, "legalName") ?? GetString(party.Value, "companyName") ?? BuildPersonName(party.Value), encoder);
                AppendLine(html, "Tax/VAT ID", GetString(party.Value, "taxId") ?? GetString(party.Value, "vatId"), encoder);
                AppendLine(html, "Email", GetString(party.Value, "email"), encoder);
                AppendLine(html, "Address", BuildAddress(party.Value), encoder);
            }

            html.AppendLine("</div>");
        }

        private static string BuildPersonName(JsonElement party)
        {
            var firstName = GetString(party, "firstName");
            var lastName = GetString(party, "lastName");
            return string.Join(' ', new[] { firstName, lastName }.Where(x => !string.IsNullOrWhiteSpace(x)));
        }

        private static string BuildAddress(JsonElement party)
        {
            return string.Join(", ", new[]
            {
                GetString(party, "addressLine1"),
                GetString(party, "postalCode"),
                GetString(party, "city"),
                GetString(party, "country")
            }.Where(x => !string.IsNullOrWhiteSpace(x)));
        }

        private static void AppendLine(StringBuilder html, string label, string? value, HtmlEncoder encoder)
        {
            html.Append("<div><span class=\"muted\">")
                .Append(encoder.Encode(label))
                .Append(":</span> ")
                .Append(encoder.Encode(string.IsNullOrWhiteSpace(value) ? "-" : value))
                .AppendLine("</div>");
        }

        private static void AppendTotal(StringBuilder html, string label, long amountMinor, string currency, HtmlEncoder encoder)
        {
            html.Append("<tr><th>")
                .Append(encoder.Encode(label))
                .Append("</th><td class=\"amount\">")
                .Append(encoder.Encode(FormatMoney(amountMinor, currency)))
                .AppendLine("</td></tr>");
        }
    }

    public sealed class GetInvoiceStructuredDataExportHandler
    {
        private static readonly JsonSerializerOptions ExportJsonOptions = new(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };

        private readonly GetInvoiceArchiveSnapshotHandler _snapshots;

        public GetInvoiceStructuredDataExportHandler(GetInvoiceArchiveSnapshotHandler snapshots)
        {
            _snapshots = snapshots ?? throw new ArgumentNullException(nameof(snapshots));
        }

        public async Task<InvoiceStructuredDataExportDto?> HandleAsync(Guid id, CancellationToken ct = default)
        {
            var snapshot = await _snapshots.HandleAsync(id, ct).ConfigureAwait(false);
            if (snapshot is null)
            {
                return null;
            }

            var structured = BuildStructuredInvoice(snapshot.SnapshotJson);
            return new InvoiceStructuredDataExportDto
            {
                InvoiceId = snapshot.InvoiceId,
                IssuedAtUtc = snapshot.IssuedAtUtc,
                FileName = $"invoice-{snapshot.InvoiceId:N}-structured-invoice.json",
                Json = JsonSerializer.Serialize(structured, ExportJsonOptions)
            };
        }

        private static StructuredInvoiceExport BuildStructuredInvoice(string snapshotJson)
        {
            using var document = JsonDocument.Parse(snapshotJson);
            var root = document.RootElement;
            var currency = GetString(root, "currency") ?? "EUR";
            var lines = ReadLines(root, currency);

            return new StructuredInvoiceExport(
                "darwin.structured-invoice.v1",
                "NotZugferdFacturX",
                "This artifact is a structured source model for future e-invoice generation. It is not a ZUGFeRD/Factur-X or XRechnung compliant artifact.",
                new StructuredInvoiceDocument(
                    GetGuid(root, "invoiceId"),
                    GetGuidOrNull(root, "businessId"),
                    GetGuidOrNull(root, "customerId"),
                    GetGuidOrNull(root, "orderId"),
                    GetGuidOrNull(root, "paymentId"),
                    GetString(root, "status") ?? string.Empty,
                    currency,
                    GetDateTime(root, "issuedAtUtc"),
                    GetDateTimeOrNull(root, "dueDateUtc"),
                    GetDateTimeOrNull(root, "paidAtUtc")),
                ReadParty(root, "issuer"),
                ReadParty(root, "customer"),
                ReadBusiness(root),
                lines,
                ReadTaxSummary(lines, currency),
                new StructuredInvoiceTotals(
                    GetLong(root, "totalNetMinor"),
                    GetLong(root, "totalTaxMinor"),
                    GetLong(root, "totalGrossMinor"),
                    currency));
        }

        private static IReadOnlyList<StructuredInvoiceLine> ReadLines(JsonElement root, string currency)
        {
            if (!root.TryGetProperty("lines", out var linesElement) || linesElement.ValueKind != JsonValueKind.Array)
            {
                return Array.Empty<StructuredInvoiceLine>();
            }

            return linesElement
                .EnumerateArray()
                .Select((line, index) => new StructuredInvoiceLine(
                    index + 1,
                    GetGuid(line, "id"),
                    GetString(line, "description") ?? string.Empty,
                    GetInt(line, "quantity"),
                    GetLong(line, "unitPriceNetMinor"),
                    GetDecimal(line, "taxRate"),
                    GetLong(line, "totalNetMinor"),
                    GetLong(line, "totalGrossMinor"),
                    GetLong(line, "totalGrossMinor") - GetLong(line, "totalNetMinor"),
                    currency))
                .ToList();
        }

        private static IReadOnlyList<StructuredInvoiceTaxSummary> ReadTaxSummary(IReadOnlyList<StructuredInvoiceLine> lines, string currency)
        {
            return lines
                .GroupBy(line => line.TaxRate)
                .OrderBy(group => group.Key)
                .Select(group =>
                {
                    var net = group.Sum(line => line.TotalNetMinor);
                    var gross = group.Sum(line => line.TotalGrossMinor);
                    return new StructuredInvoiceTaxSummary(group.Key, net, gross - net, gross, currency);
                })
                .ToList();
        }

        private static StructuredInvoiceParty? ReadParty(JsonElement root, string propertyName)
        {
            if (!root.TryGetProperty(propertyName, out var party) || party.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            return new StructuredInvoiceParty(
                GetGuidOrNull(party, "id"),
                GetString(party, "legalName") ?? GetString(party, "companyName") ?? BuildPersonName(party),
                GetString(party, "taxId"),
                GetString(party, "vatId"),
                GetString(party, "email"),
                GetString(party, "phone"),
                GetString(party, "addressLine1"),
                GetString(party, "postalCode"),
                GetString(party, "city"),
                GetString(party, "country"));
        }

        private static StructuredInvoiceBusiness? ReadBusiness(JsonElement root)
        {
            if (!root.TryGetProperty("business", out var business) || business.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            return new StructuredInvoiceBusiness(
                GetGuid(business, "id"),
                GetString(business, "name") ?? string.Empty,
                GetString(business, "legalName"),
                GetString(business, "taxId"),
                GetString(business, "defaultCurrency"),
                GetString(business, "defaultCulture"));
        }

        private static string BuildPersonName(JsonElement party)
        {
            var firstName = GetString(party, "firstName");
            var lastName = GetString(party, "lastName");
            return string.Join(' ', new[] { firstName, lastName }.Where(x => !string.IsNullOrWhiteSpace(x)));
        }

        private static string? GetString(JsonElement element, string propertyName) =>
            element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

        private static Guid GetGuid(JsonElement element, string propertyName) =>
            Guid.TryParse(GetString(element, propertyName), out var result) ? result : Guid.Empty;

        private static Guid? GetGuidOrNull(JsonElement element, string propertyName) =>
            Guid.TryParse(GetString(element, propertyName), out var result) ? result : null;

        private static long GetLong(JsonElement element, string propertyName) =>
            element.TryGetProperty(propertyName, out var value) && value.TryGetInt64(out var result) ? result : 0L;

        private static int GetInt(JsonElement element, string propertyName) =>
            element.TryGetProperty(propertyName, out var value) && value.TryGetInt32(out var result) ? result : 0;

        private static decimal GetDecimal(JsonElement element, string propertyName) =>
            element.TryGetProperty(propertyName, out var value) && value.TryGetDecimal(out var result) ? result : 0m;

        private static DateTime GetDateTime(JsonElement element, string propertyName) =>
            element.TryGetProperty(propertyName, out var value) &&
            value.ValueKind == JsonValueKind.String &&
            value.TryGetDateTime(out var result)
                ? result
                : default;

        private static DateTime? GetDateTimeOrNull(JsonElement element, string propertyName) =>
            element.TryGetProperty(propertyName, out var value) &&
            value.ValueKind == JsonValueKind.String &&
            value.TryGetDateTime(out var result)
                ? result
                : null;

        private sealed record StructuredInvoiceExport(
            string SchemaVersion,
            string ComplianceStatus,
            string ComplianceNote,
            StructuredInvoiceDocument Document,
            StructuredInvoiceParty? Seller,
            StructuredInvoiceParty? Buyer,
            StructuredInvoiceBusiness? Business,
            IReadOnlyList<StructuredInvoiceLine> Lines,
            IReadOnlyList<StructuredInvoiceTaxSummary> TaxSummary,
            StructuredInvoiceTotals Totals);

        private sealed record StructuredInvoiceDocument(
            Guid InvoiceId,
            Guid? BusinessId,
            Guid? CustomerId,
            Guid? OrderId,
            Guid? PaymentId,
            string Status,
            string Currency,
            DateTime IssuedAtUtc,
            DateTime? DueDateUtc,
            DateTime? PaidAtUtc);

        private sealed record StructuredInvoiceParty(
            Guid? Id,
            string DisplayName,
            string? TaxId,
            string? VatId,
            string? Email,
            string? Phone,
            string? AddressLine1,
            string? PostalCode,
            string? City,
            string? Country);

        private sealed record StructuredInvoiceBusiness(
            Guid Id,
            string Name,
            string? LegalName,
            string? TaxId,
            string? DefaultCurrency,
            string? DefaultCulture);

        private sealed record StructuredInvoiceLine(
            int LineNumber,
            Guid LineId,
            string Description,
            int Quantity,
            long UnitPriceNetMinor,
            decimal TaxRate,
            long TotalNetMinor,
            long TotalGrossMinor,
            long TotalTaxMinor,
            string Currency);

        private sealed record StructuredInvoiceTaxSummary(
            decimal TaxRate,
            long TotalNetMinor,
            long TotalTaxMinor,
            long TotalGrossMinor,
            string Currency);

        private sealed record StructuredInvoiceTotals(
            long TotalNetMinor,
            long TotalTaxMinor,
            long TotalGrossMinor,
            string Currency);
    }

    public sealed class GetInvoiceStructuredXmlExportHandler
    {
        private readonly GetInvoiceStructuredDataExportHandler _structuredData;

        public GetInvoiceStructuredXmlExportHandler(GetInvoiceStructuredDataExportHandler structuredData)
        {
            _structuredData = structuredData ?? throw new ArgumentNullException(nameof(structuredData));
        }

        public async Task<InvoiceStructuredXmlExportDto?> HandleAsync(Guid id, CancellationToken ct = default)
        {
            var export = await _structuredData.HandleAsync(id, ct).ConfigureAwait(false);
            if (export is null)
            {
                return null;
            }

            using var document = JsonDocument.Parse(export.Json);
            return new InvoiceStructuredXmlExportDto
            {
                InvoiceId = export.InvoiceId,
                IssuedAtUtc = export.IssuedAtUtc,
                FileName = $"invoice-{export.InvoiceId:N}-structured-invoice.xml",
                Xml = BuildXml(document.RootElement)
            };
        }

        private static string BuildXml(JsonElement root)
        {
            var xml = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement(
                    "DarwinStructuredInvoice",
                    new XAttribute("schemaVersion", GetString(root, "schemaVersion") ?? "darwin.structured-invoice.v1"),
                    new XAttribute("complianceStatus", GetString(root, "complianceStatus") ?? "NotZugferdFacturX"),
                    new XElement("ComplianceNote", GetString(root, "complianceNote") ?? "This artifact is not a ZUGFeRD/Factur-X or XRechnung compliant artifact."),
                    BuildObjectElement(root, "document", "Document"),
                    BuildObjectElement(root, "seller", "Seller"),
                    BuildObjectElement(root, "buyer", "Buyer"),
                    BuildObjectElement(root, "business", "Business"),
                    BuildArrayElement(root, "lines", "Lines", "Line"),
                    BuildArrayElement(root, "taxSummary", "TaxSummary", "Tax"),
                    BuildObjectElement(root, "totals", "Totals")));

            return xml.ToString(SaveOptions.DisableFormatting);
        }

        private static XElement BuildObjectElement(JsonElement root, string propertyName, string elementName)
        {
            if (!root.TryGetProperty(propertyName, out var value) || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                return new XElement(elementName);
            }

            return BuildElement(elementName, value);
        }

        private static XElement BuildArrayElement(JsonElement root, string propertyName, string elementName, string itemName)
        {
            var parent = new XElement(elementName);
            if (!root.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.Array)
            {
                return parent;
            }

            foreach (var item in value.EnumerateArray())
            {
                parent.Add(BuildElement(itemName, item));
            }

            return parent;
        }

        private static XElement BuildElement(string elementName, JsonElement value)
        {
            return value.ValueKind switch
            {
                JsonValueKind.Object => new XElement(
                    elementName,
                    value.EnumerateObject().Select(property => BuildElement(ToPascalCase(property.Name), property.Value))),
                JsonValueKind.Array => new XElement(
                    elementName,
                    value.EnumerateArray().Select(item => BuildElement("Item", item))),
                JsonValueKind.String => new XElement(elementName, value.GetString()),
                JsonValueKind.Number => new XElement(elementName, value.GetRawText()),
                JsonValueKind.True => new XElement(elementName, true),
                JsonValueKind.False => new XElement(elementName, false),
                _ => new XElement(elementName)
            };
        }

        private static string? GetString(JsonElement element, string propertyName) =>
            element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

        private static string ToPascalCase(string value) =>
            string.IsNullOrEmpty(value)
                ? value
                : char.ToUpperInvariant(value[0]) + value[1..];
    }
}
