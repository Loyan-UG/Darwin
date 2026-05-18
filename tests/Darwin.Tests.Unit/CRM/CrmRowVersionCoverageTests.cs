using Darwin.Application.Abstractions.Persistence;
using Darwin.Application;
using Darwin.Application.CRM.Commands;
using Darwin.Application.CRM.DTOs;
using Darwin.Application.CRM.Validators;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Settings;
using Darwin.Domain.Enums;
using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.CRM;

/// <summary>
/// Verifies that CRM command handlers handle null database RowVersion values safely
/// (no NullReferenceException) and that validators/handlers reject missing or stale
/// row versions across customer, lead, opportunity, invoice, invoice-status, invoice-refund,
/// and customer-segment operations.
/// </summary>
public sealed class CrmRowVersionCoverageTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // UpdateCustomerHandler – null DB RowVersion
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateCustomer_Should_ThrowConcurrency_WhenDbRowVersionIsNull()
    {
        await using var db = CrmNullRowVersionDbContext.Create();
        var id = Guid.NewGuid();
        // Seed customer without RowVersion to simulate entity where the column is null in DB.
        db.Set<Customer>().Add(new Customer
        {
            Id = id,
            FirstName = "Jane", LastName = "Doe",
            Email = "jane@example.com", Phone = "+491234567"
            // RowVersion intentionally omitted (null)
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateCustomerHandler(
            db,
            new CustomerEditValidator(new TestLocalizer()),
            new TestLocalizer());

        var act = async () => await handler.HandleAsync(new CustomerEditDto
        {
            Id = id,
            FirstName = "Jane", LastName = "Doe",
            Email = "jane@example.com", Phone = "+491234567",
            RowVersion = [1, 2, 3] // non-empty client version against null DB version
        }, TestContext.Current.CancellationToken);

        // Handler normalises null DB RowVersion to [] and detects mismatch, so it must throw
        // DbUpdateConcurrencyException — never a NullReferenceException.
        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UpdateLeadHandler – null DB RowVersion
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateLead_Should_ThrowConcurrency_WhenDbRowVersionIsNull()
    {
        await using var db = CrmNullRowVersionDbContext.Create();
        var id = Guid.NewGuid();
        db.Set<Lead>().Add(new Lead
        {
            Id = id,
            FirstName = "Mark", LastName = "Smith",
            Email = "mark@example.com", Phone = "+490987654"
            // RowVersion = null
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateLeadHandler(
            db,
            new LeadEditValidator(),
            new TestLocalizer());

        var act = async () => await handler.HandleAsync(new LeadEditDto
        {
            Id = id,
            FirstName = "Mark", LastName = "Smith-Updated",
            Email = "mark@example.com", Phone = "+490987654",
            RowVersion = [7, 8, 9]
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UpdateOpportunityHandler – null DB RowVersion
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateOpportunity_Should_ThrowConcurrency_WhenDbRowVersionIsNull()
    {
        await using var db = CrmNullRowVersionDbContext.Create();
        var id = Guid.NewGuid();
        db.Set<Opportunity>().Add(new Opportunity
        {
            Id = id,
            Title = "Big Deal",
            CustomerId = Guid.NewGuid()
            // RowVersion = null
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateOpportunityHandler(
            db,
            new OpportunityEditValidator(),
            new TestLocalizer());

        var act = async () => await handler.HandleAsync(new OpportunityEditDto
        {
            Id = id,
            Title = "Big Deal Updated",
            CustomerId = Guid.NewGuid(),
            RowVersion = [4, 5, 6]
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UpdateCustomerSegmentHandler – null DB RowVersion
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateCustomerSegment_Should_ThrowConcurrency_WhenDbRowVersionIsNull()
    {
        await using var db = CrmNullRowVersionDbContext.Create();
        var id = Guid.NewGuid();
        db.Set<CustomerSegment>().Add(new CustomerSegment
        {
            Id = id,
            Name = "VIP Customers"
            // RowVersion = null
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateCustomerSegmentHandler(
            db,
            new CustomerSegmentEditValidator(),
            new TestLocalizer());

        var act = async () => await handler.HandleAsync(new CustomerSegmentEditDto
        {
            Id = id,
            Name = "VIP Customers Updated",
            RowVersion = [1, 2, 3]
        }, TestContext.Current.CancellationToken);

        // Handler checks null DB RowVersion safely: currentVersion = [] != [1,2,3] → concurrency exception.
        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TransitionInvoiceStatusHandler – empty RowVersion and null DB RowVersion
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task TransitionInvoiceStatus_Should_ThrowValidation_WhenRowVersionIsEmpty()
    {
        await using var db = CrmNullRowVersionDbContext.Create();

        var handler = new TransitionInvoiceStatusHandler(
            db,
            new InvoiceStatusTransitionValidator(),
            new TestLocalizer());

        var act = async () => await handler.HandleAsync(new InvoiceStatusTransitionDto
        {
            Id = Guid.NewGuid(),
            RowVersion = [], // empty — validator has NotEmpty
            TargetStatus = InvoiceStatus.Paid
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task TransitionInvoiceStatus_Should_ThrowConcurrency_WhenDbRowVersionIsNull()
    {
        await using var db = CrmNullRowVersionDbContext.Create();
        var invoiceId = Guid.NewGuid();
        SeedInvoiceSettings(db);
        db.Set<Invoice>().Add(new Invoice
        {
            Id = invoiceId,
            CustomerId = Guid.NewGuid(),
            Currency = "EUR",
            DueDateUtc = DateTime.UtcNow.AddDays(30)
            // RowVersion = null
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new TransitionInvoiceStatusHandler(
            db,
            new InvoiceStatusTransitionValidator(),
            new TestLocalizer());

        var act = async () => await handler.HandleAsync(new InvoiceStatusTransitionDto
        {
            Id = invoiceId,
            RowVersion = [9, 8, 7],
            TargetStatus = InvoiceStatus.Cancelled
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UpdateInvoiceHandler – empty RowVersion and null DB RowVersion
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateInvoice_Should_ThrowValidation_WhenRowVersionIsEmpty()
    {
        await using var db = CrmNullRowVersionDbContext.Create();

        var handler = new UpdateInvoiceHandler(
            db,
            new InvoiceEditValidator(),
            new TestLocalizer());

        var act = async () => await handler.HandleAsync(new InvoiceEditDto
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Currency = "EUR",
            RowVersion = [] // empty — validator has NotEmpty
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task UpdateInvoice_Should_ThrowConcurrency_WhenDbRowVersionIsNull()
    {
        await using var db = CrmNullRowVersionDbContext.Create();
        var invoiceId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        db.Set<Invoice>().Add(new Invoice
        {
            Id = invoiceId,
            CustomerId = customerId,
            Currency = "EUR"
            // RowVersion = null
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateInvoiceHandler(
            db,
            new InvoiceEditValidator(),
            new TestLocalizer());

        var act = async () => await handler.HandleAsync(new InvoiceEditDto
        {
            Id = invoiceId,
            CustomerId = customerId,
            Currency = "EUR",
            DueDateUtc = DateTime.UtcNow.AddDays(30),
            RowVersion = [5, 6, 7]
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CreateInvoiceRefundHandler – empty RowVersion
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateInvoiceRefund_Should_ThrowValidation_WhenRowVersionIsEmpty()
    {
        await using var db = CrmNullRowVersionDbContext.Create();

        var handler = new CreateInvoiceRefundHandler(
            db,
            new InvoiceRefundCreateValidator(),
            new TestLocalizer());

        var act = async () => await handler.HandleAsync(new InvoiceRefundCreateDto
        {
            InvoiceId = Guid.NewGuid(),
            AmountMinor = 100,
            Currency = "EUR",
            RowVersion = [] // empty — validator has NotEmpty
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Shared infrastructure
    // ─────────────────────────────────────────────────────────────────────────

    private static void SeedInvoiceSettings(IAppDbContext db)
    {
        db.Set<SiteSetting>().Add(new SiteSetting
        {
            VatEnabled = true,
            InvoiceIssuerLegalName = "Darwin GmbH",
            InvoiceIssuerTaxId = "DE123456789",
            InvoiceIssuerAddressLine1 = "Main Street 1",
            InvoiceIssuerPostalCode = "10115",
            InvoiceIssuerCity = "Berlin",
            InvoiceIssuerCountry = "DE"
        });
    }

    private sealed class CrmNullRowVersionDbContext : DbContext, IAppDbContext
    {
        private CrmNullRowVersionDbContext(DbContextOptions<CrmNullRowVersionDbContext> options)
            : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static CrmNullRowVersionDbContext Create()
        {
            var options = new DbContextOptionsBuilder<CrmNullRowVersionDbContext>()
                .UseInMemoryDatabase($"darwin_crm_null_rowversion_tests_{Guid.NewGuid()}")
                .Options;
            return new CrmNullRowVersionDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Customer>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.FirstName).IsRequired(false);
                b.Property(x => x.LastName).IsRequired(false);
                b.Property(x => x.Email).IsRequired(false);
                b.Property(x => x.Phone).IsRequired(false);
                b.Property(x => x.RowVersion);
                b.Ignore(x => x.CustomerSegments);
                b.Ignore(x => x.Interactions);
                b.Ignore(x => x.Consents);
                b.Ignore(x => x.Opportunities);
                b.Ignore(x => x.Invoices);
                // Addresses is NOT ignored — UpdateCustomerHandler does .Include(x => x.Addresses).
                b.HasMany(x => x.Addresses).WithOne().HasForeignKey("CustomerId");
            });

            modelBuilder.Entity<CustomerAddress>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Line1).IsRequired(false);
                b.Property(x => x.City).IsRequired(false);
                b.Property(x => x.PostalCode).IsRequired(false);
                b.Property(x => x.Country).IsRequired(false);
                b.Property(x => x.RowVersion);
            });

            modelBuilder.Entity<Lead>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.FirstName).IsRequired(false);
                b.Property(x => x.LastName).IsRequired(false);
                b.Property(x => x.Email).IsRequired(false);
                b.Property(x => x.Phone).IsRequired(false);
                b.Property(x => x.RowVersion);
                b.Ignore(x => x.Interactions);
            });

            modelBuilder.Entity<Opportunity>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Title).IsRequired(false);
                b.Property(x => x.RowVersion);
                b.Ignore(x => x.Interactions);
                // Items is NOT ignored — UpdateOpportunityHandler does .Include(x => x.Items).
                b.HasMany(x => x.Items).WithOne().HasForeignKey(i => i.OpportunityId);
            });

            modelBuilder.Entity<OpportunityItem>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.OpportunityId).IsRequired();
                b.Property(x => x.RowVersion);
            });

            modelBuilder.Entity<CustomerSegment>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Name).IsRequired(false);
                b.Property(x => x.RowVersion);
                b.Ignore(x => x.Memberships);
            });

            modelBuilder.Entity<Invoice>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Currency).IsRequired(false);
                b.Property(x => x.RowVersion);
            });

            modelBuilder.Entity<InvoiceLine>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Description).IsRequired(false);
                b.Property(x => x.RowVersion);
            });

            modelBuilder.Entity<Payment>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Provider).IsRequired(false);
                b.Property(x => x.Currency).IsRequired(false);
                b.Property(x => x.RowVersion);
            });

            modelBuilder.Entity<SiteSetting>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.RowVersion);
            });
        }
    }

    private sealed class TestLocalizer : IStringLocalizer<ValidationResource>
    {
        public LocalizedString this[string name] => new(name, name, resourceNotFound: false);

        public LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(name, arguments), resourceNotFound: false);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            Array.Empty<LocalizedString>();

        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
    }
}
