using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Inventory;
using Darwin.Domain.Enums;
using Darwin.Infrastructure.Persistence.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Darwin.Infrastructure.Persistence.Seed.Sections
{
    /// <summary>
    /// Seeds billing and lightweight accounting data for local development.
    /// The section keeps provider-linked subscription samples while also covering
    /// non-cart financial master data such as chart-of-accounts, expenses, and journal entries.
    /// </summary>
    public sealed class BillingSeedSection
    {
        private readonly ILogger<BillingSeedSection> _logger;

        private sealed record FinancialAccountSeed(string Code, string Name, AccountType Type);

        public BillingSeedSection(ILogger<BillingSeedSection> logger)
        {
            _logger = logger;
        }

        public async Task SeedAsync(DarwinDbContext db, CancellationToken ct = default)
        {
            _logger.LogInformation("Seeding Billing (plans/subscriptions/invoices/accounting) ...");

            if (!await db.BillingPlans.AnyAsync(ct))
            {
                await SeedPlansAsync(db, ct);
            }

            if (!await db.BusinessSubscriptions.AnyAsync(ct))
            {
                await SeedSubscriptionsAsync(db, ct);
            }

            if (!await db.SubscriptionInvoices.AnyAsync(ct))
            {
                await SeedInvoicesAsync(db, ct);
            }

            if (!await db.FinancialAccounts.AnyAsync(ct))
            {
                await SeedFinancialAccountsAsync(db, ct);
            }

            if (!await db.Expenses.AnyAsync(ct))
            {
                await SeedExpensesAsync(db, ct);
            }

            if (!await db.JournalEntries.AnyAsync(ct))
            {
                await SeedJournalEntriesAsync(db, ct);
            }

            _logger.LogInformation("Billing seeding done.");
        }

        private static async Task SeedPlansAsync(DarwinDbContext db, CancellationToken ct)
        {
            var plans = new List<BillingPlan>
            {
                new() { Code = "starter-month", Name = "Starter (Monat)", Description = "Basisplan für kleine Teams", PriceMinor = 1900, Interval = BillingInterval.Month, IntervalCount = 1, TrialDays = 14, IsActive = true, FeaturesJson = "{\"maxStaff\":3,\"exports\":false}" },
                new() { Code = "starter-year", Name = "Starter (Jahr)", Description = "Basisplan mit Jahresrabatt", PriceMinor = 19000, Interval = BillingInterval.Year, IntervalCount = 1, TrialDays = 14, IsActive = true, FeaturesJson = "{\"maxStaff\":3,\"exports\":false}" },
                new() { Code = "pro-month", Name = "Pro (Monat)", Description = "Erweiterte Funktionen", PriceMinor = 4900, Interval = BillingInterval.Month, IntervalCount = 1, TrialDays = 14, IsActive = true, FeaturesJson = "{\"maxStaff\":15,\"exports\":true}" },
                new() { Code = "pro-year", Name = "Pro (Jahr)", Description = "Pro mit Jahresrabatt", PriceMinor = 49000, Interval = BillingInterval.Year, IntervalCount = 1, TrialDays = 14, IsActive = true, FeaturesJson = "{\"maxStaff\":15,\"exports\":true}" },
                new() { Code = "plus-month", Name = "Plus (Monat)", Description = "Mittleres Paket", PriceMinor = 2900, Interval = BillingInterval.Month, IntervalCount = 1, TrialDays = 7, IsActive = true, FeaturesJson = "{\"maxStaff\":8,\"exports\":false}" },
                new() { Code = "plus-year", Name = "Plus (Jahr)", Description = "Plus mit Jahresrabatt", PriceMinor = 29000, Interval = BillingInterval.Year, IntervalCount = 1, TrialDays = 7, IsActive = true, FeaturesJson = "{\"maxStaff\":8,\"exports\":false}" },
                new() { Code = "enterprise-month", Name = "Enterprise (Monat)", Description = "Große Teams mit SLA", PriceMinor = 14900, Interval = BillingInterval.Month, IntervalCount = 1, TrialDays = 0, IsActive = true, FeaturesJson = "{\"maxStaff\":100,\"exports\":true,\"sla\":true}" },
                new() { Code = "enterprise-year", Name = "Enterprise (Jahr)", Description = "Enterprise mit Jahresrabatt", PriceMinor = 149000, Interval = BillingInterval.Year, IntervalCount = 1, TrialDays = 0, IsActive = true, FeaturesJson = "{\"maxStaff\":100,\"exports\":true,\"sla\":true}" },
                new() { Code = "basic-quarter", Name = "Basic (Quartal)", Description = "Quartalsplan", PriceMinor = 5900, Interval = BillingInterval.Month, IntervalCount = 3, TrialDays = 10, IsActive = true, FeaturesJson = "{\"maxStaff\":5,\"exports\":false}" },
                new() { Code = "trial-month", Name = "Test (Monat)", Description = "Kurzfristiger Testplan", PriceMinor = 900, Interval = BillingInterval.Month, IntervalCount = 1, TrialDays = 3, IsActive = true, FeaturesJson = "{\"maxStaff\":2,\"exports\":false}" }
            };

            db.AddRange(plans);
            await db.SaveChangesAsync(ct);
        }

        private static async Task SeedSubscriptionsAsync(DarwinDbContext db, CancellationToken ct)
        {
            var businesses = await db.Set<Business>().OrderBy(b => b.Name).ToListAsync(ct);
            var plans = await db.BillingPlans.OrderBy(p => p.PriceMinor).ToListAsync(ct);

            var subscriptions = new List<BusinessSubscription>();

            for (var i = 0; i < businesses.Count && i < 10; i++)
            {
                var plan = plans[i % plans.Count];

                subscriptions.Add(new BusinessSubscription
                {
                    BusinessId = businesses[i].Id,
                    BillingPlanId = plan.Id,
                    Provider = "Stripe",
                    ProviderCustomerId = $"cus_{Guid.NewGuid():N}",
                    ProviderSubscriptionId = $"sub_{Guid.NewGuid():N}",
                    Status = i % 2 == 0 ? SubscriptionStatus.Active : SubscriptionStatus.Trialing,
                    StartedAtUtc = DateTime.UtcNow.AddDays(-30 - i),
                    CurrentPeriodStartUtc = DateTime.UtcNow.AddDays(-10),
                    CurrentPeriodEndUtc = DateTime.UtcNow.AddDays(20),
                    CancelAtPeriodEnd = false,
                    TrialEndsAtUtc = DateTime.UtcNow.AddDays(7),
                    UnitPriceMinor = plan.PriceMinor,
                    Currency = "EUR",
                    MetadataJson = "{\"seed\":\"true\"}"
                });
            }

            db.AddRange(subscriptions);
            await db.SaveChangesAsync(ct);
        }

        private static async Task SeedInvoicesAsync(DarwinDbContext db, CancellationToken ct)
        {
            var subscriptions = await db.BusinessSubscriptions.OrderBy(s => s.StartedAtUtc).ToListAsync(ct);
            var invoices = new List<SubscriptionInvoice>();

            for (var i = 0; i < subscriptions.Count && i < 10; i++)
            {
                var businessId = subscriptions[i].BusinessId;
                if (!businessId.HasValue)
                {
                    continue;
                }

                invoices.Add(new SubscriptionInvoice
                {
                    BusinessSubscriptionId = subscriptions[i].Id,
                    BusinessId = businessId.Value,
                    Provider = "Stripe",
                    ProviderInvoiceId = $"inv_{Guid.NewGuid():N}",
                    IssuedAtUtc = DateTime.UtcNow.AddDays(-15 - i),
                    DueAtUtc = DateTime.UtcNow.AddDays(10),
                    Status = i % 2 == 0 ? SubscriptionInvoiceStatus.Paid : SubscriptionInvoiceStatus.Open,
                    TotalMinor = subscriptions[i].UnitPriceMinor,
                    Currency = "EUR",
                    HostedInvoiceUrl = $"https://invoices.darwin.dev/{subscriptions[i].Id}",
                    PdfUrl = $"https://invoices.darwin.dev/{subscriptions[i].Id}.pdf",
                    PaidAtUtc = i % 2 == 0 ? DateTime.UtcNow.AddDays(-5) : null,
                    LinesJson = "[{\"desc\":\"Abo\",\"amountMinor\":1900,\"qty\":1}]",
                    MetadataJson = "{\"seed\":\"true\"}"
                });
            }

            db.AddRange(invoices);
            await db.SaveChangesAsync(ct);
        }

        private static async Task SeedFinancialAccountsAsync(DarwinDbContext db, CancellationToken ct)
        {
            var businessId = await db.Set<Business>()
                .Where(x => !x.IsDeleted && x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => x.Id)
                .FirstOrDefaultAsync(ct);

            if (businessId == Guid.Empty)
            {
                return;
            }

            var seeds = new[]
            {
                new FinancialAccountSeed("1000", "Bank", AccountType.Asset),
                new FinancialAccountSeed("1100", "Kasse", AccountType.Asset),
                new FinancialAccountSeed("1200", "Forderungen", AccountType.Asset),
                new FinancialAccountSeed("1400", "Vorsteuer", AccountType.Asset),
                new FinancialAccountSeed("1600", "Lagerbestand", AccountType.Asset),
                new FinancialAccountSeed("3000", "Verbindlichkeiten", AccountType.Liability),
                new FinancialAccountSeed("3400", "Umsatzsteuer", AccountType.Liability),
                new FinancialAccountSeed("4000", "Abo-Umsatzerlöse", AccountType.Revenue),
                new FinancialAccountSeed("4100", "Shop-Umsatzerlöse", AccountType.Revenue),
                new FinancialAccountSeed("6000", "Betriebsaufwand", AccountType.Expense)
            };

            db.FinancialAccounts.AddRange(seeds.Select(x => new FinancialAccount
            {
                BusinessId = businessId,
                Code = x.Code,
                Name = x.Name,
                Type = x.Type
            }));

            await db.SaveChangesAsync(ct);
        }

        private static async Task SeedExpensesAsync(DarwinDbContext db, CancellationToken ct)
        {
            var businesses = await db.Set<Business>()
                .Where(x => !x.IsDeleted && x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync(ct);

            var suppliers = await db.Suppliers
                .OrderBy(x => x.Name)
                .ToListAsync(ct);

            var expenses = new List<Expense>();
            var categories = new[]
            {
                "Miete",
                "Energie",
                "Reinigung",
                "Wartung",
                "Verpackung",
                "Marketing",
                "Software",
                "Internet",
                "Bürobedarf",
                "Transport"
            };

            for (var i = 0; i < businesses.Count && i < 10; i++)
            {
                var supplier = suppliers.FirstOrDefault(x => x.BusinessId == businesses[i].Id);

                expenses.Add(new Expense
                {
                    BusinessId = businesses[i].Id,
                    SupplierId = supplier?.Id,
                    Category = categories[i],
                    Description = $"{categories[i]} März {DateTime.UtcNow.Year} für {businesses[i].Name}",
                    AmountMinor = 3500 + (i * 650),
                    ExpenseDateUtc = DateTime.UtcNow.Date.AddDays(-(i + 3))
                });
            }

            db.Expenses.AddRange(expenses);
            await db.SaveChangesAsync(ct);
        }

        private static async Task SeedJournalEntriesAsync(DarwinDbContext db, CancellationToken ct)
        {
            var businessId = await db.Set<Business>()
                .Where(x => !x.IsDeleted && x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => x.Id)
                .FirstOrDefaultAsync(ct);

            if (businessId == Guid.Empty)
            {
                return;
            }

            var accounts = await db.FinancialAccounts
                .Where(x => x.BusinessId == businessId)
                .OrderBy(x => x.Code)
                .ToListAsync(ct);

            var bank = accounts.FirstOrDefault(x => x.Code == "1000");
            var receivables = accounts.FirstOrDefault(x => x.Code == "1200");
            var revenue = accounts.FirstOrDefault(x => x.Code == "4000");
            var expense = accounts.FirstOrDefault(x => x.Code == "6000");

            if (bank is null || receivables is null || revenue is null || expense is null)
            {
                return;
            }

            var entries = new List<JournalEntry>();

            for (var i = 0; i < 10; i++)
            {
                var amount = 9500 + (i * 400);
                var isRevenueEntry = i % 2 == 0;

                entries.Add(new JournalEntry
                {
                    BusinessId = businessId,
                    EntryDateUtc = DateTime.UtcNow.Date.AddDays(-(i + 1)),
                    Description = isRevenueEntry
                        ? $"Seed Erlösbuchung {i + 1:D2}"
                        : $"Seed Aufwandbuchung {i + 1:D2}",
                    Lines = new List<JournalEntryLine>
                    {
                        new JournalEntryLine
                        {
                            AccountId = isRevenueEntry ? receivables.Id : expense.Id,
                            DebitMinor = amount,
                            CreditMinor = 0,
                            Memo = isRevenueEntry ? "Offene Forderung aus CRM/Subscription." : "Gebuchter Betriebsaufwand."
                        },
                        new JournalEntryLine
                        {
                            AccountId = isRevenueEntry ? revenue.Id : bank.Id,
                            DebitMinor = 0,
                            CreditMinor = amount,
                            Memo = isRevenueEntry ? "Umsatzerlös." : "Bankabgang."
                        }
                    }
                });
            }

            db.JournalEntries.AddRange(entries);
            await db.SaveChangesAsync(ct);
        }
    }
}
