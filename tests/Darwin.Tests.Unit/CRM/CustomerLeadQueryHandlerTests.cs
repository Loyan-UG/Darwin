using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.CRM.DTOs;
using Darwin.Application.CRM.Queries;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Tests.Unit.CRM;

/// <summary>
/// Unit tests for CRM customer query handlers:
/// <see cref="GetCustomersPageHandler"/>, <see cref="GetCustomerForEditHandler"/>,
/// <see cref="GetLeadsPageHandler"/>, <see cref="GetLeadForEditHandler"/>, and
/// <see cref="GetCrmSummaryHandler"/>.
/// </summary>
public sealed class CustomerLeadQueryHandlerTests
{
    // ─── GetCustomersPageHandler ──────────────────────────────────────────────

    [Fact]
    public async Task GetCustomersPage_Should_ReturnAllCustomers_WhenNoFilterApplied()
    {
        await using var db = CrmQueryDbContext.Create();

        db.Set<Customer>().AddRange(
            MakeCustomer(email: "alpha@example.de", firstName: "Alpha"),
            MakeCustomer(email: "beta@example.de", firstName: "Beta"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetCustomersPageHandler(db);

        var (items, total) = await handler.HandleAsync(1, 20, ct: TestContext.Current.CancellationToken);

        total.Should().Be(2);
        items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetCustomersPage_Should_FilterByQueryString_WhenProvided()
    {
        await using var db = CrmQueryDbContext.Create();

        db.Set<Customer>().AddRange(
            MakeCustomer(email: "hans@example.de", firstName: "Hans"),
            MakeCustomer(email: "maria@example.de", firstName: "Maria"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetCustomersPageHandler(db);

        var (items, total) = await handler.HandleAsync(1, 20, query: "hans", ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Should().ContainSingle(x => x.Email == "hans@example.de");
    }

    [Fact]
    public async Task GetCustomersPage_Should_FilterByLinkedUser_WhenFilterIsLinkedUser()
    {
        await using var db = CrmQueryDbContext.Create();
        var userId = Guid.NewGuid();

        db.Set<User>().Add(MakeUser(userId));
        db.Set<Customer>().AddRange(
            MakeCustomer(email: "linked@example.de", firstName: "Linked", userId: userId),
            MakeCustomer(email: "unlinked@example.de", firstName: "Unlinked"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetCustomersPageHandler(db);

        var (items, total) = await handler.HandleAsync(1, 20, filter: CustomerQueueFilter.LinkedUser, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items[0].UserId.Should().Be(userId);
    }

    [Fact]
    public async Task GetCustomersPage_Should_FilterByBusiness_WhenFilterIsBusiness()
    {
        await using var db = CrmQueryDbContext.Create();

        db.Set<Customer>().AddRange(
            MakeCustomer(email: "business@example.de", firstName: "Biz", taxProfile: CustomerTaxProfileType.Business, companyName: "ACME GmbH"),
            MakeCustomer(email: "consumer@example.de", firstName: "Consumer", taxProfile: CustomerTaxProfileType.Consumer));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetCustomersPageHandler(db);

        var (items, total) = await handler.HandleAsync(1, 20, filter: CustomerQueueFilter.Business, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items[0].TaxProfileType.Should().Be(CustomerTaxProfileType.Business);
    }

    [Fact]
    public async Task GetCustomersPage_Should_RespectPagination()
    {
        await using var db = CrmQueryDbContext.Create();

        for (var i = 0; i < 5; i++)
        {
            db.Set<Customer>().Add(MakeCustomer(email: $"customer{i}@example.de", firstName: $"Customer{i}"));
        }
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetCustomersPageHandler(db);

        var (items, total) = await handler.HandleAsync(1, 2, ct: TestContext.Current.CancellationToken);

        total.Should().Be(5);
        items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetCustomersPage_Should_ReturnEmptyList_WhenNoCustomers()
    {
        await using var db = CrmQueryDbContext.Create();

        var handler = new GetCustomersPageHandler(db);

        var (items, total) = await handler.HandleAsync(1, 20, ct: TestContext.Current.CancellationToken);

        total.Should().Be(0);
        items.Should().BeEmpty();
    }

    // ─── GetCustomerForEditHandler ────────────────────────────────────────────

    [Fact]
    public async Task GetCustomerForEdit_Should_ReturnCustomerDto_WhenCustomerExists()
    {
        await using var db = CrmQueryDbContext.Create();
        var customerId = Guid.NewGuid();

        db.Set<Customer>().Add(new Customer
        {
            Id = customerId,
            FirstName = "Max",
            LastName = "Mustermann",
            Email = "max@example.de",
            Phone = "+491701234567",
            CompanyName = "ACME GmbH",
            TaxProfileType = CustomerTaxProfileType.Business,
            VatId = "DE123456789",
            Notes = "Important customer",
            RowVersion = new byte[] { 1, 2, 3, 4 }
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetCustomerForEditHandler(db);

        var dto = await handler.HandleAsync(customerId, TestContext.Current.CancellationToken);

        dto.Should().NotBeNull();
        dto!.Id.Should().Be(customerId);
        dto.FirstName.Should().Be("Max");
        dto.CompanyName.Should().Be("ACME GmbH");
        dto.TaxProfileType.Should().Be(CustomerTaxProfileType.Business);
        dto.VatId.Should().Be("DE123456789");
        dto.Notes.Should().Be("Important customer");
    }

    [Fact]
    public async Task GetCustomerForEdit_Should_ReturnNull_WhenCustomerDoesNotExist()
    {
        await using var db = CrmQueryDbContext.Create();

        var handler = new GetCustomerForEditHandler(db);

        var dto = await handler.HandleAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        dto.Should().BeNull();
    }

    [Fact]
    public async Task GetCustomerForEdit_Should_IncludeAddresses_WhenPresent()
    {
        await using var db = CrmQueryDbContext.Create();
        var customerId = Guid.NewGuid();

        var customer = new Customer
        {
            Id = customerId,
            FirstName = "Lisa",
            LastName = "Test",
            Email = "lisa@test.de",
            Phone = "+491700000000",
            RowVersion = new byte[] { 1 }
        };
        customer.Addresses.Add(new CustomerAddress
        {
            CustomerId = customerId,
            Line1 = "Teststr. 1",
            City = "Frankfurt",
            PostalCode = "60311",
            Country = "DE",
            IsDefaultBilling = true
        });
        db.Set<Customer>().Add(customer);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetCustomerForEditHandler(db);

        var dto = await handler.HandleAsync(customerId, TestContext.Current.CancellationToken);

        dto.Should().NotBeNull();
        dto!.Addresses.Should().ContainSingle();
        dto.Addresses[0].Line1.Should().Be("Teststr. 1");
        dto.Addresses[0].IsDefaultBilling.Should().BeTrue();
    }

    [Fact]
    public async Task GetCustomerForEdit_Should_IncludeSegmentAndOpportunityCounts()
    {
        await using var db = CrmQueryDbContext.Create();
        var customerId = Guid.NewGuid();
        var segmentId = Guid.NewGuid();

        db.Set<Customer>().Add(new Customer
        {
            Id = customerId,
            FirstName = "Peter",
            LastName = "Test",
            Email = "peter@test.de",
            Phone = "+491700000001",
            RowVersion = new byte[] { 1 }
        });
        db.Set<CustomerSegment>().Add(new CustomerSegment
        {
            Id = segmentId,
            Name = "Premium"
        });
        db.Set<CustomerSegmentMembership>().Add(new CustomerSegmentMembership
        {
            CustomerId = customerId,
            CustomerSegmentId = segmentId
        });
        db.Set<Opportunity>().Add(new Opportunity
        {
            CustomerId = customerId,
            Title = "Deal",
            EstimatedValueMinor = 50000,
            Stage = OpportunityStage.Qualification
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetCustomerForEditHandler(db);

        var dto = await handler.HandleAsync(customerId, TestContext.Current.CancellationToken);

        dto.Should().NotBeNull();
        dto!.SegmentCount.Should().Be(1);
        dto.OpportunityCount.Should().Be(1);
    }

    // ─── GetLeadsPageHandler ──────────────────────────────────────────────────

    [Fact]
    public async Task GetLeadsPage_Should_ReturnAllLeads_WhenNoFilterApplied()
    {
        await using var db = CrmQueryDbContext.Create();

        db.Set<Lead>().AddRange(
            MakeLead(email: "lead1@example.de", firstName: "Lead1"),
            MakeLead(email: "lead2@example.de", firstName: "Lead2"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetLeadsPageHandler(db);

        var (items, total) = await handler.HandleAsync(1, 20, ct: TestContext.Current.CancellationToken);

        total.Should().Be(2);
        items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetLeadsPage_Should_FilterByQueryString_WhenProvided()
    {
        await using var db = CrmQueryDbContext.Create();

        db.Set<Lead>().AddRange(
            MakeLead(email: "alice@leads.de", firstName: "Alice"),
            MakeLead(email: "bob@leads.de", firstName: "Bob"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetLeadsPageHandler(db);

        var (items, total) = await handler.HandleAsync(1, 20, query: "alice", ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items[0].Email.Should().Be("alice@leads.de");
    }

    [Fact]
    public async Task GetLeadsPage_Should_FilterByQualifiedStatus()
    {
        await using var db = CrmQueryDbContext.Create();

        db.Set<Lead>().AddRange(
            MakeLead(email: "qualified@leads.de", firstName: "Qualified", status: LeadStatus.Qualified),
            MakeLead(email: "new@leads.de", firstName: "New", status: LeadStatus.New));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetLeadsPageHandler(db);

        var (items, total) = await handler.HandleAsync(1, 20, filter: LeadQueueFilter.Qualified, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items[0].Status.Should().Be(LeadStatus.Qualified);
    }

    [Fact]
    public async Task GetLeadsPage_Should_FilterUnconvertedLeads_WhenFilterIsUnconverted()
    {
        await using var db = CrmQueryDbContext.Create();
        var customerId = Guid.NewGuid();

        db.Set<Lead>().AddRange(
            MakeLead(email: "converted@leads.de", firstName: "Converted", customerId: customerId),
            MakeLead(email: "fresh@leads.de", firstName: "Fresh"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetLeadsPageHandler(db);

        var (items, total) = await handler.HandleAsync(1, 20, filter: LeadQueueFilter.Unconverted, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items[0].Email.Should().Be("fresh@leads.de");
    }

    [Fact]
    public async Task GetLeadsPage_Should_RespectPagination()
    {
        await using var db = CrmQueryDbContext.Create();

        for (var i = 0; i < 6; i++)
        {
            db.Set<Lead>().Add(MakeLead(email: $"lead{i}@leads.de", firstName: $"Lead{i}"));
        }
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetLeadsPageHandler(db);

        var (items, total) = await handler.HandleAsync(1, 3, ct: TestContext.Current.CancellationToken);

        total.Should().Be(6);
        items.Should().HaveCount(3);
    }

    // ─── GetLeadForEditHandler ────────────────────────────────────────────────

    [Fact]
    public async Task GetLeadForEdit_Should_ReturnLeadDto_WhenLeadExists()
    {
        await using var db = CrmQueryDbContext.Create();
        var leadId = Guid.NewGuid();

        db.Set<Lead>().Add(new Lead
        {
            Id = leadId,
            FirstName = "Karl",
            LastName = "Bauer",
            Email = "karl@bauer.de",
            Phone = "+4917012345678",
            CompanyName = "Karl GmbH",
            Source = "Website",
            Notes = "Interested in Pro plan",
            Status = LeadStatus.Qualified,
            RowVersion = new byte[] { 1, 2, 3 }
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetLeadForEditHandler(db);

        var dto = await handler.HandleAsync(leadId, TestContext.Current.CancellationToken);

        dto.Should().NotBeNull();
        dto!.Id.Should().Be(leadId);
        dto.FirstName.Should().Be("Karl");
        dto.Email.Should().Be("karl@bauer.de");
        dto.CompanyName.Should().Be("Karl GmbH");
        dto.Source.Should().Be("Website");
        dto.Notes.Should().Be("Interested in Pro plan");
        dto.Status.Should().Be(LeadStatus.Qualified);
    }

    [Fact]
    public async Task GetLeadForEdit_Should_ReturnNull_WhenLeadDoesNotExist()
    {
        await using var db = CrmQueryDbContext.Create();

        var handler = new GetLeadForEditHandler(db);

        var dto = await handler.HandleAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        dto.Should().BeNull();
    }

    // ─── GetCrmSummaryHandler ─────────────────────────────────────────────────

    [Fact]
    public async Task GetCrmSummary_Should_ReturnZeroCounts_WhenNoCrmData()
    {
        await using var db = CrmQueryDbContext.Create();

        var handler = new GetCrmSummaryHandler(db);

        var summary = await handler.HandleAsync(TestContext.Current.CancellationToken);

        summary.CustomerCount.Should().Be(0);
        summary.LeadCount.Should().Be(0);
        summary.QualifiedLeadCount.Should().Be(0);
        summary.OpenOpportunityCount.Should().Be(0);
        summary.OpenPipelineMinor.Should().Be(0);
        summary.SegmentCount.Should().Be(0);
    }

    [Fact]
    public async Task GetCrmSummary_Should_CountCustomersAndLeadsCorrectly()
    {
        await using var db = CrmQueryDbContext.Create();

        db.Set<Customer>().AddRange(
            MakeCustomer(email: "c1@test.de", firstName: "C1"),
            MakeCustomer(email: "c2@test.de", firstName: "C2"));

        db.Set<Lead>().AddRange(
            MakeLead(email: "l1@test.de", firstName: "L1", status: LeadStatus.New),
            MakeLead(email: "l2@test.de", firstName: "L2", status: LeadStatus.Qualified),
            MakeLead(email: "l3@test.de", firstName: "L3", status: LeadStatus.Qualified));

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetCrmSummaryHandler(db);

        var summary = await handler.HandleAsync(TestContext.Current.CancellationToken);

        summary.CustomerCount.Should().Be(2);
        summary.LeadCount.Should().Be(3);
        summary.QualifiedLeadCount.Should().Be(2);
    }

    [Fact]
    public async Task GetCrmSummary_Should_CountOpenOpportunitiesAndPipeline()
    {
        await using var db = CrmQueryDbContext.Create();
        var customerId = Guid.NewGuid();

        db.Set<Customer>().Add(MakeCustomer(email: "c@test.de", firstName: "C", id: customerId));
        db.Set<Opportunity>().AddRange(
            new Opportunity
            {
                CustomerId = customerId,
                Title = "Open Deal",
                EstimatedValueMinor = 100000,
                Stage = OpportunityStage.Qualification
            },
            new Opportunity
            {
                CustomerId = customerId,
                Title = "Won Deal",
                EstimatedValueMinor = 200000,
                Stage = OpportunityStage.ClosedWon
            },
            new Opportunity
            {
                CustomerId = customerId,
                Title = "Lost Deal",
                EstimatedValueMinor = 50000,
                Stage = OpportunityStage.ClosedLost
            });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetCrmSummaryHandler(db);

        var summary = await handler.HandleAsync(TestContext.Current.CancellationToken);

        summary.OpenOpportunityCount.Should().Be(1);
        summary.OpenPipelineMinor.Should().Be(100000);
    }

    [Fact]
    public async Task GetCrmSummary_Should_CountSegments()
    {
        await using var db = CrmQueryDbContext.Create();

        db.Set<CustomerSegment>().AddRange(
            new CustomerSegment { Name = "VIP" },
            new CustomerSegment { Name = "Regular" },
            new CustomerSegment { Name = "At Risk" });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetCrmSummaryHandler(db);

        var summary = await handler.HandleAsync(TestContext.Current.CancellationToken);

        summary.SegmentCount.Should().Be(3);
    }

    // ─── Shared helpers ───────────────────────────────────────────────────────

    private static Customer MakeCustomer(
        string email,
        string firstName,
        Guid? id = null,
        Guid? userId = null,
        CustomerTaxProfileType taxProfile = CustomerTaxProfileType.Consumer,
        string? companyName = null) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            UserId = userId,
            FirstName = firstName,
            LastName = "Test",
            Email = email,
            Phone = "+4917000000000",
            TaxProfileType = taxProfile,
            CompanyName = companyName
        };

    private static Lead MakeLead(
        string email,
        string firstName,
        LeadStatus status = LeadStatus.New,
        Guid? customerId = null) =>
        new()
        {
            FirstName = firstName,
            LastName = "TestLead",
            Email = email,
            Phone = "+4917000000001",
            Status = status,
            CustomerId = customerId
        };

    private static User MakeUser(Guid userId) =>
        new("user@example.com", "hashed-password", "security-stamp")
        {
            Id = userId,
            FirstName = "Test",
            LastName = "User",
            RowVersion = new byte[] { 1, 2, 3, 4 }
        };

    private sealed class CrmQueryDbContext : DbContext, IAppDbContext
    {
        private CrmQueryDbContext(DbContextOptions<CrmQueryDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static CrmQueryDbContext Create()
        {
            var options = new DbContextOptionsBuilder<CrmQueryDbContext>()
                .UseInMemoryDatabase($"darwin_crm_query_tests_{Guid.NewGuid()}")
                .Options;
            return new CrmQueryDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Ignore<GeoCoordinate>();

            modelBuilder.Entity<User>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Email).IsRequired();
                b.Property(x => x.NormalizedEmail).IsRequired();
                b.Property(x => x.UserName).IsRequired();
                b.Property(x => x.NormalizedUserName).IsRequired();
                b.Property(x => x.PasswordHash).IsRequired();
                b.Property(x => x.SecurityStamp).IsRequired();
                b.Property(x => x.Locale).IsRequired();
                b.Property(x => x.Currency).IsRequired();
                b.Property(x => x.Timezone).IsRequired();
                b.Property(x => x.ChannelsOptInJson).IsRequired();
                b.Property(x => x.FirstTouchUtmJson).IsRequired();
                b.Property(x => x.LastTouchUtmJson).IsRequired();
                b.Property(x => x.ExternalIdsJson).IsRequired();
                b.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<Customer>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.FirstName).IsRequired();
                b.Property(x => x.LastName).IsRequired();
                b.Property(x => x.Email).IsRequired();
                b.Property(x => x.Phone).IsRequired();
                b.Property(x => x.RowVersion).IsRequired();
                b.Ignore(x => x.Interactions);
                b.Ignore(x => x.Consents);
                b.Ignore(x => x.Invoices);
            });

            modelBuilder.Entity<CustomerAddress>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Line1).IsRequired();
                b.Property(x => x.City).IsRequired();
                b.Property(x => x.PostalCode).IsRequired();
                b.Property(x => x.Country).IsRequired();
                b.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<CustomerSegment>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Name).IsRequired();
                b.Property(x => x.RowVersion).IsRequired();
                b.Ignore(x => x.Memberships);
            });

            modelBuilder.Entity<CustomerSegmentMembership>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<Lead>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.FirstName).IsRequired();
                b.Property(x => x.LastName).IsRequired();
                b.Property(x => x.Email).IsRequired();
                b.Property(x => x.Phone).IsRequired();
                b.Property(x => x.RowVersion).IsRequired();
                b.Ignore(x => x.Interactions);
            });

            modelBuilder.Entity<Opportunity>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Title).IsRequired();
                b.Property(x => x.RowVersion).IsRequired();
                b.Ignore(x => x.Items);
                b.Ignore(x => x.Interactions);
            });

            modelBuilder.Entity<Interaction>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<Consent>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.RowVersion).IsRequired();
            });
        }
    }
}
