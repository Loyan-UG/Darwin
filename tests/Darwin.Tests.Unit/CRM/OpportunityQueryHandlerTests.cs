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
/// Unit tests for <see cref="GetOpportunitiesPageHandler"/> and
/// <see cref="GetOpportunityForEditHandler"/>.
/// </summary>
public sealed class OpportunityQueryHandlerTests
{
    // ─── GetOpportunitiesPageHandler ──────────────────────────────────────────

    [Fact]
    public async Task GetOpportunitiesPage_Should_ReturnAllOpportunities_WhenNoFilterApplied()
    {
        await using var db = OpportunityQueryDbContext.Create();
        var customerId = Guid.NewGuid();

        db.Set<Customer>().Add(MakeCustomer(customerId));
        db.Set<Opportunity>().AddRange(
            MakeOpportunity(customerId, "Deal A", OpportunityStage.Qualification),
            MakeOpportunity(customerId, "Deal B", OpportunityStage.Proposal));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetOpportunitiesPageHandler(db);

        var (items, total) = await handler.HandleAsync(1, 20, ct: TestContext.Current.CancellationToken);

        total.Should().Be(2);
        items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetOpportunitiesPage_Should_FilterByQueryString_WhenProvided()
    {
        await using var db = OpportunityQueryDbContext.Create();
        var customer1Id = Guid.NewGuid();
        var customer2Id = Guid.NewGuid();

        // Use distinct first names so query filtering via x.customer.FirstName works
        db.Set<Customer>().AddRange(
            MakeCustomer(customer1Id, firstName: "UniqueAlphaX"),
            MakeCustomer(customer2Id, firstName: "DifferentBeta"));
        db.Set<Opportunity>().AddRange(
            MakeOpportunity(customer1Id, "Deal for UniqueAlphaX customer", OpportunityStage.Qualification),
            MakeOpportunity(customer2Id, "Deal for DifferentBeta customer", OpportunityStage.Proposal));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetOpportunitiesPageHandler(db);

        var (items, total) = await handler.HandleAsync(1, 20, query: "UniqueAlphaX", ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items[0].CustomerDisplayName.Should().Contain("UniqueAlphaX");
    }

    [Fact]
    public async Task GetOpportunitiesPage_Should_FilterOpenOpportunities_WhenFilterIsOpen()
    {
        await using var db = OpportunityQueryDbContext.Create();
        var customerId = Guid.NewGuid();

        db.Set<Customer>().Add(MakeCustomer(customerId));
        db.Set<Opportunity>().AddRange(
            MakeOpportunity(customerId, "Open Deal", OpportunityStage.Qualification),
            MakeOpportunity(customerId, "Won Deal", OpportunityStage.ClosedWon),
            MakeOpportunity(customerId, "Lost Deal", OpportunityStage.ClosedLost));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetOpportunitiesPageHandler(db);

        var (items, total) = await handler.HandleAsync(1, 20, filter: OpportunityQueueFilter.Open, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items[0].Title.Should().Be("Open Deal");
    }

    [Fact]
    public async Task GetOpportunitiesPage_Should_FilterHighValueOpportunities_WhenFilterIsHighValue()
    {
        await using var db = OpportunityQueryDbContext.Create();
        var customerId = Guid.NewGuid();

        db.Set<Customer>().Add(MakeCustomer(customerId));
        db.Set<Opportunity>().AddRange(
            MakeOpportunity(customerId, "Big Deal", OpportunityStage.Qualification, estimatedValue: 200000),
            MakeOpportunity(customerId, "Small Deal", OpportunityStage.Qualification, estimatedValue: 5000));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetOpportunitiesPageHandler(db);

        var (items, total) = await handler.HandleAsync(1, 20, filter: OpportunityQueueFilter.HighValue, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items[0].Title.Should().Be("Big Deal");
        items[0].EstimatedValueMinor.Should().Be(200000);
    }

    [Fact]
    public async Task GetOpportunitiesPage_Should_RespectPagination()
    {
        await using var db = OpportunityQueryDbContext.Create();
        var customerId = Guid.NewGuid();

        db.Set<Customer>().Add(MakeCustomer(customerId));
        for (var i = 0; i < 7; i++)
        {
            db.Set<Opportunity>().Add(MakeOpportunity(customerId, $"Deal {i}", OpportunityStage.Qualification));
        }
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetOpportunitiesPageHandler(db);

        var (items, total) = await handler.HandleAsync(1, 3, ct: TestContext.Current.CancellationToken);

        total.Should().Be(7);
        items.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetOpportunitiesPage_Should_ProjectCustomerDisplayName()
    {
        await using var db = OpportunityQueryDbContext.Create();
        var customerId = Guid.NewGuid();

        db.Set<Customer>().Add(MakeCustomer(customerId, firstName: "Petra", lastName: "Müller"));
        db.Set<Opportunity>().Add(MakeOpportunity(customerId, "Named Customer Deal", OpportunityStage.Qualification));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetOpportunitiesPageHandler(db);

        var (items, _) = await handler.HandleAsync(1, 20, ct: TestContext.Current.CancellationToken);

        items[0].CustomerDisplayName.Should().Be("Petra Müller");
    }

    // ─── GetOpportunityForEditHandler ─────────────────────────────────────────

    [Fact]
    public async Task GetOpportunityForEdit_Should_ReturnOpportunityDto_WhenOpportunityExists()
    {
        await using var db = OpportunityQueryDbContext.Create();
        var customerId = Guid.NewGuid();
        var opportunityId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        db.Set<Customer>().Add(MakeCustomer(customerId, firstName: "Max", lastName: "Muster"));
        var opportunity = new Opportunity
        {
            Id = opportunityId,
            CustomerId = customerId,
            Title = "Full Enterprise License",
            EstimatedValueMinor = 500000,
            Stage = OpportunityStage.Negotiation,
            ExpectedCloseDateUtc = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            RowVersion = new byte[] { 1, 2, 3, 4 }
        };
        opportunity.Items.Add(new OpportunityItem
        {
            OpportunityId = opportunityId,
            ProductVariantId = variantId,
            Quantity = 100,
            UnitPriceMinor = 5000
        });
        db.Set<Opportunity>().Add(opportunity);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetOpportunityForEditHandler(db);

        var dto = await handler.HandleAsync(opportunityId, TestContext.Current.CancellationToken);

        dto.Should().NotBeNull();
        dto!.Id.Should().Be(opportunityId);
        dto.CustomerId.Should().Be(customerId);
        dto.CustomerDisplayName.Should().Be("Max Muster");
        dto.Title.Should().Be("Full Enterprise License");
        dto.EstimatedValueMinor.Should().Be(500000);
        dto.Stage.Should().Be(OpportunityStage.Negotiation);
        dto.ExpectedCloseDateUtc.Should().Be(new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc));
        dto.Items.Should().ContainSingle(x => x.ProductVariantId == variantId && x.Quantity == 100);
    }

    [Fact]
    public async Task GetOpportunityForEdit_Should_ReturnNull_WhenOpportunityDoesNotExist()
    {
        await using var db = OpportunityQueryDbContext.Create();

        var handler = new GetOpportunityForEditHandler(db);

        var dto = await handler.HandleAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        dto.Should().BeNull();
    }

    [Fact]
    public async Task GetOpportunityForEdit_Should_ReturnEmptyItems_WhenNoItemsOnOpportunity()
    {
        await using var db = OpportunityQueryDbContext.Create();
        var customerId = Guid.NewGuid();
        var opportunityId = Guid.NewGuid();

        db.Set<Customer>().Add(MakeCustomer(customerId));
        db.Set<Opportunity>().Add(new Opportunity
        {
            Id = opportunityId,
            CustomerId = customerId,
            Title = "No Items",
            EstimatedValueMinor = 10000,
            Stage = OpportunityStage.Qualification,
            RowVersion = new byte[] { 1 }
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetOpportunityForEditHandler(db);

        var dto = await handler.HandleAsync(opportunityId, TestContext.Current.CancellationToken);

        dto.Should().NotBeNull();
        dto!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetOpportunityForEdit_Should_IncludeInteractionCount()
    {
        await using var db = OpportunityQueryDbContext.Create();
        var customerId = Guid.NewGuid();
        var opportunityId = Guid.NewGuid();

        db.Set<Customer>().Add(MakeCustomer(customerId));
        db.Set<Opportunity>().Add(new Opportunity
        {
            Id = opportunityId,
            CustomerId = customerId,
            Title = "Opportunity With Interactions",
            EstimatedValueMinor = 10000,
            Stage = OpportunityStage.Qualification,
            RowVersion = new byte[] { 1 }
        });
        db.Set<Interaction>().AddRange(
            new Interaction { OpportunityId = opportunityId, Type = InteractionType.Call, Channel = InteractionChannel.Phone },
            new Interaction { OpportunityId = opportunityId, Type = InteractionType.Email, Channel = InteractionChannel.Email });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetOpportunityForEditHandler(db);

        var dto = await handler.HandleAsync(opportunityId, TestContext.Current.CancellationToken);

        dto.Should().NotBeNull();
        dto!.InteractionCount.Should().Be(2);
    }

    // ─── Shared helpers ───────────────────────────────────────────────────────

    private static Customer MakeCustomer(
        Guid id,
        string firstName = "Test",
        string lastName = "Customer") =>
        new()
        {
            Id = id,
            FirstName = firstName,
            LastName = lastName,
            Email = "test@customer.de",
            Phone = "+4917000000000"
        };

    private static Opportunity MakeOpportunity(
        Guid customerId,
        string title,
        OpportunityStage stage,
        long estimatedValue = 10000) =>
        new()
        {
            CustomerId = customerId,
            Title = title,
            EstimatedValueMinor = estimatedValue,
            Stage = stage
        };

    private sealed class OpportunityQueryDbContext : DbContext, IAppDbContext
    {
        private OpportunityQueryDbContext(DbContextOptions<OpportunityQueryDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static OpportunityQueryDbContext Create()
        {
            var options = new DbContextOptionsBuilder<OpportunityQueryDbContext>()
                .UseInMemoryDatabase($"darwin_opportunity_query_tests_{Guid.NewGuid()}")
                .Options;
            return new OpportunityQueryDbContext(options);
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
                b.Ignore(x => x.CustomerSegments);
                b.Ignore(x => x.Addresses);
                b.Ignore(x => x.Interactions);
                b.Ignore(x => x.Consents);
                b.Ignore(x => x.Opportunities);
                b.Ignore(x => x.Invoices);
            });

            modelBuilder.Entity<Opportunity>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Title).IsRequired();
                b.Property(x => x.RowVersion).IsRequired();
                b.Ignore(x => x.Interactions);
            });

            modelBuilder.Entity<OpportunityItem>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<Interaction>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.RowVersion).IsRequired();
            });
        }
    }
}
