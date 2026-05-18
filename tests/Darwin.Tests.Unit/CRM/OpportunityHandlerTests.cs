using Darwin.Application.Abstractions.Persistence;
using Darwin.Application;
using Darwin.Application.CRM.Commands;
using Darwin.Application.CRM.DTOs;
using Darwin.Application.CRM.Validators;
using FluentValidation;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.CRM;

/// <summary>
/// Unit tests for <see cref="CreateOpportunityHandler"/> and
/// <see cref="UpdateOpportunityHandler"/>.
/// </summary>
public sealed class OpportunityHandlerTests
{
    // ─── CreateOpportunityHandler ─────────────────────────────────────────────

    [Fact]
    public async Task CreateOpportunity_Should_PersistOpportunity_WhenCustomerExists()
    {
        await using var db = OpportunityDbContext.Create();
        var customerId = Guid.NewGuid();

        db.Set<Customer>().Add(MakeCustomer(customerId));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateOpportunityHandler(
            db,
            new OpportunityCreateValidator(),
            new TestStringLocalizer());

        var id = await handler.HandleAsync(new OpportunityCreateDto
        {
            CustomerId = customerId,
            Title = "  Enterprise License  ",
            EstimatedValueMinor = 500000,
            Stage = OpportunityStage.Qualification
        }, TestContext.Current.CancellationToken);

        id.Should().NotBeEmpty();

        var opportunity = await db.Set<Opportunity>().SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        opportunity.CustomerId.Should().Be(customerId);
        opportunity.Title.Should().Be("Enterprise License");
        opportunity.EstimatedValueMinor.Should().Be(500000);
        opportunity.Stage.Should().Be(OpportunityStage.Qualification);
    }

    [Fact]
    public async Task CreateOpportunity_Should_PersistItems_WhenItemsProvided()
    {
        await using var db = OpportunityDbContext.Create();
        var customerId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        db.Set<Customer>().Add(MakeCustomer(customerId));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateOpportunityHandler(
            db,
            new OpportunityCreateValidator(),
            new TestStringLocalizer());

        var id = await handler.HandleAsync(new OpportunityCreateDto
        {
            CustomerId = customerId,
            Title = "License Bundle",
            EstimatedValueMinor = 200000,
            Stage = OpportunityStage.Proposal,
            Items = new List<OpportunityItemDto>
            {
                new()
                {
                    ProductVariantId = variantId,
                    Quantity = 10,
                    UnitPriceMinor = 20000
                }
            }
        }, TestContext.Current.CancellationToken);

        var items = await db.Set<OpportunityItem>()
            .Where(x => x.OpportunityId == id)
            .ToListAsync(TestContext.Current.CancellationToken);

        items.Should().ContainSingle();
        items[0].ProductVariantId.Should().Be(variantId);
        items[0].Quantity.Should().Be(10);
        items[0].UnitPriceMinor.Should().Be(20000);
    }

    [Fact]
    public async Task CreateOpportunity_Should_Throw_WhenCustomerNotFound()
    {
        await using var db = OpportunityDbContext.Create();

        var handler = new CreateOpportunityHandler(
            db,
            new OpportunityCreateValidator(),
            new TestStringLocalizer());

        var act = () => handler.HandleAsync(new OpportunityCreateDto
        {
            CustomerId = Guid.NewGuid(),
            Title = "Test Opportunity",
            EstimatedValueMinor = 1000,
            Stage = OpportunityStage.Qualification
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CustomerNotFound");
    }

    // ─── UpdateOpportunityHandler ─────────────────────────────────────────────

    [Fact]
    public async Task UpdateOpportunity_Should_PersistChanges_WhenOpportunityExists()
    {
        await using var db = OpportunityDbContext.Create();
        var customerId = Guid.NewGuid();
        var opportunityId = Guid.NewGuid();
        var rowVersion = new byte[] { 1, 2, 3, 4 };

        db.Set<Customer>().Add(MakeCustomer(customerId));
        db.Set<Opportunity>().Add(new Opportunity
        {
            Id = opportunityId,
            CustomerId = customerId,
            Title = "Old Title",
            EstimatedValueMinor = 100000,
            Stage = OpportunityStage.Qualification,
            RowVersion = rowVersion.ToArray()
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateOpportunityHandler(
            db,
            new OpportunityEditValidator(),
            new TestStringLocalizer());

        await handler.HandleAsync(new OpportunityEditDto
        {
            Id = opportunityId,
            RowVersion = rowVersion,
            CustomerId = customerId,
            Title = "  Updated Title  ",
            EstimatedValueMinor = 200000,
            Stage = OpportunityStage.Negotiation
        }, TestContext.Current.CancellationToken);

        var opportunity = await db.Set<Opportunity>().SingleAsync(x => x.Id == opportunityId, TestContext.Current.CancellationToken);
        opportunity.Title.Should().Be("Updated Title");
        opportunity.EstimatedValueMinor.Should().Be(200000);
        opportunity.Stage.Should().Be(OpportunityStage.Negotiation);
    }

    [Fact]
    public async Task UpdateOpportunity_Should_ReplaceItems_WhenNewItemsProvided()
    {
        await using var db = OpportunityDbContext.Create();
        var customerId = Guid.NewGuid();
        var opportunityId = Guid.NewGuid();
        var oldVariantId = Guid.NewGuid();
        var newVariantId = Guid.NewGuid();
        var rowVersion = new byte[] { 5, 6, 7, 8 };

        db.Set<Customer>().Add(MakeCustomer(customerId));
        var opportunity = new Opportunity
        {
            Id = opportunityId,
            CustomerId = customerId,
            Title = "Opportunity With Items",
            EstimatedValueMinor = 100000,
            Stage = OpportunityStage.Qualification,
            RowVersion = rowVersion.ToArray()
        };
        opportunity.Items.Add(new OpportunityItem
        {
            ProductVariantId = oldVariantId,
            Quantity = 5,
            UnitPriceMinor = 10000
        });
        db.Set<Opportunity>().Add(opportunity);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateOpportunityHandler(
            db,
            new OpportunityEditValidator(),
            new TestStringLocalizer());

        await handler.HandleAsync(new OpportunityEditDto
        {
            Id = opportunityId,
            RowVersion = rowVersion,
            CustomerId = customerId,
            Title = "Opportunity With Items",
            EstimatedValueMinor = 100000,
            Stage = OpportunityStage.Qualification,
            Items = new List<OpportunityItemDto>
            {
                new()
                {
                    ProductVariantId = newVariantId,
                    Quantity = 2,
                    UnitPriceMinor = 50000
                }
            }
        }, TestContext.Current.CancellationToken);

        var items = await db.Set<OpportunityItem>()
            .Where(x => x.OpportunityId == opportunityId)
            .ToListAsync(TestContext.Current.CancellationToken);

        items.Should().ContainSingle();
        items[0].ProductVariantId.Should().Be(newVariantId);
        items[0].Quantity.Should().Be(2);
    }

    [Fact]
    public async Task UpdateOpportunity_Should_Throw_WhenOpportunityNotFound()
    {
        await using var db = OpportunityDbContext.Create();

        var handler = new UpdateOpportunityHandler(
            db,
            new OpportunityEditValidator(),
            new TestStringLocalizer());

        var act = () => handler.HandleAsync(new OpportunityEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1 },
            CustomerId = Guid.NewGuid(),
            Title = "Does Not Exist",
            EstimatedValueMinor = 0,
            Stage = OpportunityStage.Qualification
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("OpportunityNotFound");
    }

    [Fact]
    public async Task UpdateOpportunity_Should_Throw_WhenRowVersionMismatches()
    {
        await using var db = OpportunityDbContext.Create();
        var customerId = Guid.NewGuid();
        var opportunityId = Guid.NewGuid();

        db.Set<Customer>().Add(MakeCustomer(customerId));
        db.Set<Opportunity>().Add(new Opportunity
        {
            Id = opportunityId,
            CustomerId = customerId,
            Title = "Concurrent Opportunity",
            EstimatedValueMinor = 100000,
            Stage = OpportunityStage.Qualification,
            RowVersion = new byte[] { 1, 2, 3, 4 }
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateOpportunityHandler(
            db,
            new OpportunityEditValidator(),
            new TestStringLocalizer());

        var act = () => handler.HandleAsync(new OpportunityEditDto
        {
            Id = opportunityId,
            RowVersion = new byte[] { 9, 9, 9, 9 },
            CustomerId = customerId,
            Title = "Concurrent Opportunity",
            EstimatedValueMinor = 100000,
            Stage = OpportunityStage.Qualification
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    [Fact]
    public async Task UpdateOpportunity_Should_Throw_WhenRowVersionIsEmpty()
    {
        await using var db = OpportunityDbContext.Create();
        var customerId = Guid.NewGuid();
        var opportunityId = Guid.NewGuid();

        db.Set<Customer>().Add(MakeCustomer(customerId));
        db.Set<Opportunity>().Add(new Opportunity
        {
            Id = opportunityId,
            CustomerId = customerId,
            Title = "Empty RowVersion Opportunity",
            EstimatedValueMinor = 100000,
            Stage = OpportunityStage.Qualification,
            RowVersion = new byte[] { 1, 2, 3, 4 }
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateOpportunityHandler(
            db,
            new OpportunityEditValidator(),
            new TestStringLocalizer());

        var act = () => handler.HandleAsync(new OpportunityEditDto
        {
            Id = opportunityId,
            RowVersion = Array.Empty<byte>(), // empty row version
            CustomerId = customerId,
            Title = "Empty RowVersion Opportunity",
            EstimatedValueMinor = 100000,
            Stage = OpportunityStage.Qualification
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("empty RowVersion is rejected by the validator before the handler's concurrency check");
    }

    // ─── Shared helpers ───────────────────────────────────────────────────────

    private static Customer MakeCustomer(Guid id) => new()
    {
        Id = id,
        FirstName = "Test",
        LastName = "Customer",
        Email = "test@customer.de",
        Phone = "+4917000000000"
    };

    private sealed class OpportunityDbContext : DbContext, IAppDbContext
    {
        private OpportunityDbContext(DbContextOptions<OpportunityDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static OpportunityDbContext Create()
        {
            var options = new DbContextOptionsBuilder<OpportunityDbContext>()
                .UseInMemoryDatabase($"darwin_opportunity_tests_{Guid.NewGuid()}")
                .Options;
            return new OpportunityDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Ignore<GeoCoordinate>();

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
        }
    }

    private sealed class TestStringLocalizer : IStringLocalizer<ValidationResource>
    {
        public LocalizedString this[string name] => new(name, name, resourceNotFound: false);

        public LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(name, arguments), resourceNotFound: false);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            Array.Empty<LocalizedString>();

        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
    }
}
