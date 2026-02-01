using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Enums;
using Darwin.Infrastructure.Persistence.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Darwin.Infrastructure.Persistence.Seed.Sections
{
    /// <summary>
    /// Seeds billing data for subscription flows:
    /// - Billing plans (10+)
    /// - Business subscriptions (10+)
    /// - Subscription invoices (10+)
    /// </summary>
    public sealed class BillingSeedSection
    {
        private readonly ILogger<BillingSeedSection> _logger;

        public BillingSeedSection(ILogger<BillingSeedSection> logger)
        {
            _logger = logger;
        }

        public async Task SeedAsync(DarwinDbContext db, CancellationToken ct = default)
        {
            _logger.LogInformation("Seeding Billing (plans/subscriptions/invoices) ...");

            if (!await db.BillingPlans.AnyAsync(ct))
                await SeedPlansAsync(db, ct);

            if (!await db.BusinessSubscriptions.AnyAsync(ct))
                await SeedSubscriptionsAsync(db, ct);

            if (!await db.SubscriptionInvoices.AnyAsync(ct))
                await SeedInvoicesAsync(db, ct);

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

            var subs = new List<BusinessSubscription>();

            for (var i = 0; i < businesses.Count && i < 10; i++)
            {
                var plan = plans[i % plans.Count];

                subs.Add(new BusinessSubscription
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

            db.AddRange(subs);
            await db.SaveChangesAsync(ct);
        }

        private static async Task SeedInvoicesAsync(DarwinDbContext db, CancellationToken ct)
        {
            var subs = await db.BusinessSubscriptions.OrderBy(s => s.StartedAtUtc).ToListAsync(ct);

            var invoices = new List<SubscriptionInvoice>();

            for (var i = 0; i < subs.Count && i < 10; i++)
            {
                invoices.Add(new SubscriptionInvoice
                {
                    BusinessSubscriptionId = subs[i].Id,
                    BusinessId = subs[i].BusinessId,
                    Provider = "Stripe",
                    ProviderInvoiceId = $"inv_{Guid.NewGuid():N}",
                    IssuedAtUtc = DateTime.UtcNow.AddDays(-15 - i),
                    DueAtUtc = DateTime.UtcNow.AddDays(10),
                    Status = i % 2 == 0 ? SubscriptionInvoiceStatus.Paid : SubscriptionInvoiceStatus.Open,
                    TotalMinor = subs[i].UnitPriceMinor,
                    Currency = "EUR",
                    HostedInvoiceUrl = $"https://invoices.darwin.dev/{subs[i].Id}",
                    PdfUrl = $"https://invoices.darwin.dev/{subs[i].Id}.pdf",
                    PaidAtUtc = i % 2 == 0 ? DateTime.UtcNow.AddDays(-5) : null,
                    LinesJson = "[{\"desc\":\"Abo\",\"amountMinor\":1900,\"qty\":1}]",
                    MetadataJson = "{\"seed\":\"true\"}"
                });
            }

            db.AddRange(invoices);
            await db.SaveChangesAsync(ct);
        }
    }
}