using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Enums;
using Darwin.Infrastructure.Persistence.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Darwin.Infrastructure.Persistence.Seed.Sections
{
    /// <summary>
    /// Seeds CRM reference and demo operational data for German back-office scenarios.
    /// The section intentionally focuses on non-cart, non-session CRM records such as customers,
    /// leads, opportunities, notes, and invoices.
    /// </summary>
    public sealed class CrmSeedSection
    {
        private readonly ILogger<CrmSeedSection> _logger;

        private sealed record GuestCustomerSeed(
            string FirstName,
            string LastName,
            string Email,
            string Phone,
            string? CompanyName,
            string Street,
            string PostalCode,
            string City);

        private sealed record LeadSeed(
            string FirstName,
            string LastName,
            string? CompanyName,
            string Email,
            string Phone,
            string? Source,
            string? Notes);

        public CrmSeedSection(ILogger<CrmSeedSection> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Seeds CRM customers, segments, lead pipeline, interactions, and invoice demos.
        /// </summary>
        public async Task SeedAsync(DarwinDbContext db, CancellationToken ct = default)
        {
            _logger.LogInformation("Seeding CRM (customers/leads/opportunities/invoices) ...");

            var users = await db.Set<User>()
                .Where(x => !x.IsDeleted && x.IsActive)
                .OrderBy(x => x.Email)
                .ToListAsync(ct);

            var businesses = await db.Set<Business>()
                .Where(x => !x.IsDeleted && x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync(ct);

            var productVariants = await db.ProductVariants
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.Sku)
                .ToListAsync(ct);

            if (users.Count == 0)
            {
                _logger.LogWarning("Skipping CRM seeding because no users exist yet.");
                return;
            }

            if (productVariants.Count == 0)
            {
                _logger.LogWarning("Skipping CRM pipeline seeding because no product variants exist yet.");
                return;
            }

            var customers = await EnsureCustomersAsync(db, users, ct);
            var segments = await EnsureCustomerSegmentsAsync(db, ct);

            if (!await db.CustomerSegmentMemberships.AnyAsync(ct))
            {
                await SeedCustomerSegmentMembershipsAsync(db, customers, segments, ct);
            }

            if (!await db.CustomerAddresses.AnyAsync(ct))
            {
                await SeedCustomerAddressesAsync(db, customers, users, ct);
            }

            if (!await db.Consents.AnyAsync(ct))
            {
                await SeedConsentsAsync(db, customers, ct);
            }

            var leads = await EnsureLeadsAsync(db, users, customers, ct);
            var opportunities = await EnsureOpportunitiesAsync(db, customers, users, productVariants, ct);

            if (!await db.Interactions.AnyAsync(ct))
            {
                await SeedInteractionsAsync(db, customers, leads, opportunities, users, ct);
            }

            if (!await db.Invoices.AnyAsync(ct))
            {
                await SeedInvoicesAsync(db, customers, businesses, ct);
            }

            _logger.LogInformation("CRM seeding done.");
        }

        private static async Task<List<Customer>> EnsureCustomersAsync(DarwinDbContext db, IReadOnlyList<User> users, CancellationToken ct)
        {
            var existing = await db.Customers.OrderBy(x => x.Email).ToListAsync(ct);
            if (existing.Count > 0)
            {
                return existing;
            }

            var linkedUsers = users
                .Where(x => x.Email.StartsWith("cons", StringComparison.OrdinalIgnoreCase))
                .Take(5)
                .ToList();

            if (linkedUsers.Count < 5)
            {
                linkedUsers = users.Take(5).ToList();
            }

            var customers = new List<Customer>();

            for (var i = 0; i < linkedUsers.Count; i++)
            {
                var user = linkedUsers[i];
                customers.Add(new Customer
                {
                    UserId = user.Id,
                    FirstName = user.FirstName ?? string.Empty,
                    LastName = user.LastName ?? string.Empty,
                    Email = user.Email,
                    Phone = user.PhoneE164 ?? $"+49 30 50010{i + 1:D2}",
                    CompanyName = user.Company,
                    Notes = "Linked CRM profile seeded from identity user."
                });
            }

            customers.AddRange(GetGuestCustomerSeeds().Select(seed => new Customer
            {
                UserId = null,
                FirstName = seed.FirstName,
                LastName = seed.LastName,
                Email = seed.Email,
                Phone = seed.Phone,
                CompanyName = seed.CompanyName,
                Notes = "Guest CRM customer seeded for lead-to-customer workflows."
            }));

            db.Customers.AddRange(customers);
            await db.SaveChangesAsync(ct);

            return await db.Customers.OrderBy(x => x.Email).ToListAsync(ct);
        }

        private static async Task<List<CustomerSegment>> EnsureCustomerSegmentsAsync(DarwinDbContext db, CancellationToken ct)
        {
            var existing = await db.CustomerSegments.OrderBy(x => x.Name).ToListAsync(ct);
            if (existing.Count > 0)
            {
                return existing;
            }

            var definitions = new[]
            {
                ("Newsletter Abonnenten", "Kontakte mit aktivem E-Mail-Marketing-Einverständnis."),
                ("Stammkunden Berlin", "Kunden mit Schwerpunkt in Berlin und Umgebung."),
                ("B2B Handel", "Geschäftskunden mit Firmenbezug."),
                ("Hoher Warenkorb", "Kontakte mit überdurchschnittlichem Auftragswert."),
                ("Rückgewinnung", "Reaktivierungskampagnen für inaktive Kontakte."),
                ("Event Leads", "Kontakte aus Messen und lokalen Veranstaltungen."),
                ("Telefon Leads", "Leads mit priorisiertem Telefonkontakt."),
                ("Rechnungszahler", "CRM-Kunden mit Rechnungsprozessen."),
                ("VIP Service", "Premium-Betreuung und hoher SLA-Fokus."),
                ("Region Süd", "Kontakte mit Schwerpunkt Süddeutschland.")
            };

            db.CustomerSegments.AddRange(definitions.Select(x => new CustomerSegment
            {
                Name = x.Item1,
                Description = x.Item2
            }));

            await db.SaveChangesAsync(ct);
            return await db.CustomerSegments.OrderBy(x => x.Name).ToListAsync(ct);
        }

        private static async Task SeedCustomerSegmentMembershipsAsync(
            DarwinDbContext db,
            IReadOnlyList<Customer> customers,
            IReadOnlyList<CustomerSegment> segments,
            CancellationToken ct)
        {
            var memberships = new List<CustomerSegmentMembership>();

            for (var i = 0; i < customers.Count && i < segments.Count; i++)
            {
                memberships.Add(new CustomerSegmentMembership
                {
                    CustomerId = customers[i].Id,
                    CustomerSegmentId = segments[i].Id
                });
            }

            db.CustomerSegmentMemberships.AddRange(memberships);
            await db.SaveChangesAsync(ct);
        }

        private static async Task SeedCustomerAddressesAsync(
            DarwinDbContext db,
            IReadOnlyList<Customer> customers,
            IReadOnlyList<User> users,
            CancellationToken ct)
        {
            var userAddressMap = await db.Set<Address>()
                .Where(x => !x.IsDeleted && x.UserId.HasValue)
                .GroupBy(x => x.UserId!.Value)
                .ToDictionaryAsync(x => x.Key, x => x.OrderByDescending(a => a.IsDefaultShipping).ThenBy(a => a.City).First(), ct);

            var guestSeeds = GetGuestCustomerSeeds();
            var addresses = new List<CustomerAddress>();

            foreach (var customer in customers.Where(x => x.UserId.HasValue))
            {
                if (!userAddressMap.TryGetValue(customer.UserId!.Value, out var linkedAddress))
                {
                    continue;
                }

                addresses.Add(new CustomerAddress
                {
                    CustomerId = customer.Id,
                    AddressId = linkedAddress.Id,
                    Line1 = linkedAddress.Street1,
                    Line2 = linkedAddress.Street2,
                    City = linkedAddress.City,
                    State = null,
                    PostalCode = linkedAddress.PostalCode,
                    Country = linkedAddress.CountryCode,
                    IsDefaultBilling = true,
                    IsDefaultShipping = true
                });
            }

            foreach (var pair in customers.Where(x => !x.UserId.HasValue).Zip(guestSeeds, (customer, seed) => new { customer, seed }))
            {
                addresses.Add(new CustomerAddress
                {
                    CustomerId = pair.customer.Id,
                    AddressId = null,
                    Line1 = pair.seed.Street,
                    Line2 = null,
                    City = pair.seed.City,
                    State = null,
                    PostalCode = pair.seed.PostalCode,
                    Country = DomainDefaults.DefaultCountryCode,
                    IsDefaultBilling = true,
                    IsDefaultShipping = true
                });
            }

            db.CustomerAddresses.AddRange(addresses);
            await db.SaveChangesAsync(ct);
        }

        private static async Task SeedConsentsAsync(DarwinDbContext db, IReadOnlyList<Customer> customers, CancellationToken ct)
        {
            var consents = new List<Consent>();

            for (var i = 0; i < customers.Count; i++)
            {
                consents.Add(new Consent
                {
                    CustomerId = customers[i].Id,
                    Type = (i % 3) switch
                    {
                        0 => ConsentType.MarketingEmail,
                        1 => ConsentType.Sms,
                        _ => ConsentType.TermsOfService
                    },
                    Granted = i % 4 != 0,
                    GrantedAtUtc = DateTime.UtcNow.AddDays(-(14 + i)),
                    RevokedAtUtc = i % 4 == 0 ? DateTime.UtcNow.AddDays(-(3 + i)) : null
                });
            }

            db.Consents.AddRange(consents);
            await db.SaveChangesAsync(ct);
        }

        private static async Task<List<Lead>> EnsureLeadsAsync(
            DarwinDbContext db,
            IReadOnlyList<User> users,
            IReadOnlyList<Customer> customers,
            CancellationToken ct)
        {
            var existing = await db.Leads.OrderBy(x => x.Email).ToListAsync(ct);
            if (existing.Count > 0)
            {
                return existing;
            }

            var owners = users
                .Where(x => x.Email.StartsWith("biz", StringComparison.OrdinalIgnoreCase) || x.Email.StartsWith("admin", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (owners.Count == 0)
            {
                owners = users.Take(3).ToList();
            }

            var seeds = GetLeadSeeds();
            var leads = new List<Lead>();

            for (var i = 0; i < seeds.Length; i++)
            {
                leads.Add(new Lead
                {
                    FirstName = seeds[i].FirstName,
                    LastName = seeds[i].LastName,
                    CompanyName = seeds[i].CompanyName,
                    Email = seeds[i].Email,
                    Phone = seeds[i].Phone,
                    Source = seeds[i].Source,
                    Notes = seeds[i].Notes,
                    Status = i switch
                    {
                        < 2 => LeadStatus.New,
                        < 5 => LeadStatus.Qualified,
                        < 7 => LeadStatus.Disqualified,
                        _ => LeadStatus.Converted
                    },
                    AssignedToUserId = owners[i % owners.Count].Id,
                    CustomerId = i >= 7 ? customers[(i - 7) % customers.Count].Id : null
                });
            }

            db.Leads.AddRange(leads);
            await db.SaveChangesAsync(ct);

            return await db.Leads.OrderBy(x => x.Email).ToListAsync(ct);
        }

        private static async Task<List<Opportunity>> EnsureOpportunitiesAsync(
            DarwinDbContext db,
            IReadOnlyList<Customer> customers,
            IReadOnlyList<User> users,
            IReadOnlyList<Domain.Entities.Catalog.ProductVariant> productVariants,
            CancellationToken ct)
        {
            var existing = await db.Opportunities.OrderBy(x => x.Title).ToListAsync(ct);
            if (existing.Count > 0)
            {
                return existing;
            }

            var owners = users
                .Where(x => x.Email.StartsWith("biz", StringComparison.OrdinalIgnoreCase) || x.Email.StartsWith("admin", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (owners.Count == 0)
            {
                owners = users.Take(3).ToList();
            }

            var stages = new[]
            {
                OpportunityStage.Qualification,
                OpportunityStage.Proposal,
                OpportunityStage.Negotiation,
                OpportunityStage.ClosedWon,
                OpportunityStage.ClosedLost
            };

            var opportunities = new List<Opportunity>();
            for (var i = 0; i < customers.Count && i < 10; i++)
            {
                opportunities.Add(new Opportunity
                {
                    CustomerId = customers[i].Id,
                    Title = $"Angebot {i + 1:D2} für {customers[i].FirstName} {customers[i].LastName}".Trim(),
                    EstimatedValueMinor = 14900 + (i * 1750),
                    Stage = stages[i % stages.Length],
                    ExpectedCloseDateUtc = DateTime.UtcNow.AddDays(7 + i),
                    AssignedToUserId = owners[i % owners.Count].Id
                });
            }

            db.Opportunities.AddRange(opportunities);
            await db.SaveChangesAsync(ct);

            if (!await db.OpportunityItems.AnyAsync(ct))
            {
                var items = opportunities
                    .Take(10)
                    .Select((opportunity, index) =>
                    {
                        var variant = productVariants[index % productVariants.Count];
                        var quantity = 1 + (index % 3);
                        var unitPriceMinor = variant.BasePriceNetMinor > 0 ? variant.BasePriceNetMinor : 9900;

                        return new OpportunityItem
                        {
                            OpportunityId = opportunity.Id,
                            ProductVariantId = variant.Id,
                            Quantity = quantity,
                            UnitPriceMinor = unitPriceMinor
                        };
                    })
                    .ToList();

                db.OpportunityItems.AddRange(items);
                await db.SaveChangesAsync(ct);
            }

            return await db.Opportunities.OrderBy(x => x.Title).ToListAsync(ct);
        }

        private static async Task SeedInteractionsAsync(
            DarwinDbContext db,
            IReadOnlyList<Customer> customers,
            IReadOnlyList<Lead> leads,
            IReadOnlyList<Opportunity> opportunities,
            IReadOnlyList<User> users,
            CancellationToken ct)
        {
            var operators = users
                .Where(x => x.Email.StartsWith("biz", StringComparison.OrdinalIgnoreCase) || x.Email.StartsWith("admin", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (operators.Count == 0)
            {
                operators = users.Take(3).ToList();
            }

            var interactions = new List<Interaction>();

            for (var i = 0; i < customers.Count; i++)
            {
                interactions.Add(new Interaction
                {
                    CustomerId = customers[i].Id,
                    Type = InteractionType.Call,
                    Channel = InteractionChannel.Phone,
                    Subject = $"Willkommensanruf {i + 1:D2}",
                    Content = "Kunde wurde nach dem ersten Kontakt telefonisch begrüßt.",
                    UserId = operators[i % operators.Count].Id
                });
            }

            for (var i = 0; i < leads.Count && i < 10; i++)
            {
                interactions.Add(new Interaction
                {
                    LeadId = leads[i].Id,
                    Type = InteractionType.Email,
                    Channel = InteractionChannel.Email,
                    Subject = $"Lead Follow-up {i + 1:D2}",
                    Content = "Angebotsmail und Terminabstimmung wurden dokumentiert.",
                    UserId = operators[i % operators.Count].Id
                });
            }

            for (var i = 0; i < opportunities.Count && i < 10; i++)
            {
                interactions.Add(new Interaction
                {
                    OpportunityId = opportunities[i].Id,
                    Type = InteractionType.Meeting,
                    Channel = InteractionChannel.InPerson,
                    Subject = $"Angebotsgespräch {i + 1:D2}",
                    Content = "Produktdemo und Preisrahmen mit dem Kunden abgestimmt.",
                    UserId = operators[i % operators.Count].Id
                });
            }

            db.Interactions.AddRange(interactions);
            await db.SaveChangesAsync(ct);
        }

        private static async Task SeedInvoicesAsync(
            DarwinDbContext db,
            IReadOnlyList<Customer> customers,
            IReadOnlyList<Business> businesses,
            CancellationToken ct)
        {
            var invoices = new List<Invoice>();

            for (var i = 0; i < customers.Count && i < 10; i++)
            {
                var net = 8900 + (i * 1100);
                var tax = (long)Math.Round(net * 0.19m, MidpointRounding.AwayFromZero);

                invoices.Add(new Invoice
                {
                    BusinessId = businesses.Count > 0 ? businesses[i % businesses.Count].Id : null,
                    CustomerId = customers[i].Id,
                    OrderId = null,
                    PaymentId = null,
                    Status = i % 3 == 0 ? InvoiceStatus.Paid : InvoiceStatus.Open,
                    Currency = DomainDefaults.DefaultCurrency,
                    TotalNetMinor = net,
                    TotalTaxMinor = tax,
                    TotalGrossMinor = net + tax,
                    DueDateUtc = DateTime.UtcNow.AddDays(10 + i),
                    PaidAtUtc = i % 3 == 0 ? DateTime.UtcNow.AddDays(-(i + 1)) : null,
                    Lines = new List<InvoiceLine>
                    {
                        new InvoiceLine
                        {
                            Description = $"CRM-Angebotspaket {i + 1:D2}",
                            Quantity = 1,
                            UnitPriceNetMinor = net,
                            TaxRate = 0.19m,
                            TotalNetMinor = net,
                            TotalGrossMinor = net + tax
                        }
                    }
                });
            }

            db.Invoices.AddRange(invoices);
            await db.SaveChangesAsync(ct);
        }

        private static GuestCustomerSeed[] GetGuestCustomerSeeds() => new[]
        {
            new GuestCustomerSeed("Frieda", "Hartwig", "frieda.hartwig@darwin-demo.de", "+49 30 4001001", null, "Prenzlauer Allee 88", "10405", "Berlin"),
            new GuestCustomerSeed("Johann", "Küster", "johann.kuester@darwin-demo.de", "+49 89 4001002", "Küster Handel GmbH", "Tal 14", "80331", "München"),
            new GuestCustomerSeed("Greta", "Mahler", "greta.mahler@darwin-demo.de", "+49 221 4001003", null, "Apostelnstraße 21", "50667", "Köln"),
            new GuestCustomerSeed("Emil", "Voss", "emil.voss@darwin-demo.de", "+49 40 4001004", "Voss Gastroservice", "Steinstraße 19", "20095", "Hamburg"),
            new GuestCustomerSeed("Helene", "Graf", "helene.graf@darwin-demo.de", "+49 69 4001005", null, "Kaiserstraße 34", "60329", "Frankfurt am Main")
        };

        private static LeadSeed[] GetLeadSeeds() => new[]
        {
            new LeadSeed("Carla", "Sommer", "Sommer Events GmbH", "carla.sommer@sommer-events.de", "+49 30 5550101", "Messe Berlin", "Anfrage zu Kundenbindungsaktionen."),
            new LeadSeed("Matthias", "Brandner", null, "matthias.brandner@darwin-demo.de", "+49 89 5550102", "Website Kontaktformular", "Interesse an CRM- und Loyalty-Einführung."),
            new LeadSeed("Nele", "Pohl", "Pohl Retail KG", "nele.pohl@pohl-retail.de", "+49 221 5550103", "Empfehlung", "Benötigt Multi-Standort-Funktionen."),
            new LeadSeed("Sebastian", "Reuter", null, "sebastian.reuter@darwin-demo.de", "+49 40 5550104", "LinkedIn", "Fragt nach Integrationen in bestehende Systeme."),
            new LeadSeed("Hannah", "Kraft", "Kraft Feinkost GmbH", "hannah.kraft@kraft-feinkost.de", "+49 69 5550105", "Roadshow Frankfurt", "Möchte Demo für Back-Office und Front-Office."),
            new LeadSeed("Oliver", "Behnke", null, "oliver.behnke@darwin-demo.de", "+49 341 5550106", "Telefon", "Preisrahmen derzeit zu hoch."),
            new LeadSeed("Marlene", "Schaper", "Schaper Wellness", "marlene.schaper@schaper-wellness.de", "+49 351 5550107", "Instagram", "Benötigt CRM mit einfacher Kampagnensteuerung."),
            new LeadSeed("Niklas", "Faber", "Faber Technik OHG", "niklas.faber@faber-technik.de", "+49 911 5550108", "Partnernetzwerk", "Lead wurde bereits in einen CRM-Kunden konvertiert."),
            new LeadSeed("Lotte", "Riemer", null, "lotte.riemer@darwin-demo.de", "+49 511 5550109", "E-Mail Kampagne", "Konvertierter Lead mit Angebotsnachverfolgung."),
            new LeadSeed("Philipp", "Thiele", "Thiele Service GmbH", "philipp.thiele@thiele-service.de", "+49 201 5550110", "Bestandskunde", "Konvertierter Lead für Erweiterungsverkauf.")
        };
    }
}
