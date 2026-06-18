using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application;
using Darwin.Application.Foundation;
using Darwin.Application.Orders.Services;
using Darwin.Application.Sales.Commands;
using Darwin.Application.Sales.DTOs;
using Darwin.Application.Sales.Queries;
using Darwin.Application.Sales.Services;
using Darwin.Application.Sales.Validators;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Foundation;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Entities.Sales;
using Darwin.Domain.Enums;
using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.Sales;

public sealed class SalesQuoteHandlersTests
{
    private static readonly DateTime FixedNow = new(2026, 6, 12, 9, 30, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreateSalesQuote_Should_Normalize_Lines_And_Totals_Without_Official_Number()
    {
        await using var db = SalesQuoteTestDbContext.Create();
        var handler = CreateHandler(db);

        var id = await handler.HandleAsync(new SalesQuoteCreateDto
        {
            Title = "  Renewal quote  ",
            Currency = " eur ",
            Lines =
            {
                new SalesQuoteLineEditDto
                {
                    Name = "  Service plan ",
                    Sku = " SKU-1 ",
                    Quantity = 2,
                    UnitPriceNetMinor = 1000,
                    UnitPriceGrossMinor = 1190,
                    TaxRate = 0.19m
                }
            }
        }, TestContext.Current.CancellationToken);

        var quote = await db.Set<SalesQuote>().Include(x => x.Lines).SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);

        quote.Title.Should().Be("Renewal quote");
        quote.Currency.Should().Be("EUR");
        quote.Status.Should().Be(SalesQuoteStatus.Draft);
        quote.QuoteNumber.Should().BeNull();
        quote.TotalNetMinor.Should().Be(2000);
        quote.TotalTaxMinor.Should().Be(380);
        quote.TotalGrossMinor.Should().Be(2380);
        quote.Lines.Should().ContainSingle().Which.Name.Should().Be("Service plan");
    }

    [Fact]
    public async Task SendSalesQuote_Should_Reserve_Number_Once_And_Record_Event_Audit_Idempotently()
    {
        await using var db = SalesQuoteTestDbContext.Create();
        SeedQuoteNumberSequence(db);
        var quote = SeedQuote(db, SalesQuoteStatus.Draft, withLine: true);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = SendHandler(db);

        await handler.HandleAsync(new SalesQuoteLifecycleDto
        {
            Id = quote.Id,
            RowVersion = quote.RowVersion
        }, TestContext.Current.CancellationToken);

        var sent = await db.Set<SalesQuote>().SingleAsync(x => x.Id == quote.Id, TestContext.Current.CancellationToken);
        sent.Status.Should().Be(SalesQuoteStatus.Sent);
        sent.QuoteNumber.Should().Be("Q-20260612-001");
        db.Set<NumberSequence>().Single().NextValue.Should().Be(2);
        db.Set<BusinessEvent>().Should().ContainSingle(x => x.EventType == "sales.quote.sent");
        db.Set<AuditTrail>().Should().ContainSingle(x => x.EntityType == "SalesQuote" && x.EntityId == quote.Id);

        await RecordEvents(db).RecordStatusChangedAsync(
            sent,
            "sales.quote.sent",
            SalesQuoteStatus.Draft,
            SalesQuoteStatus.Sent,
            null,
            null,
            TestContext.Current.CancellationToken);

        db.Set<BusinessEvent>().Should().ContainSingle(x => x.EventType == "sales.quote.sent");
        db.Set<AuditTrail>().Should().ContainSingle(x => x.EntityType == "SalesQuote" && x.EntityId == quote.Id);
    }

    [Fact]
    public async Task UpdateSalesQuoteLifecycle_Should_Reject_Invalid_Transition_And_Convert_Accepted_To_Existing_Order()
    {
        await using var db = SalesQuoteTestDbContext.Create();
        var quote = SeedQuote(db, SalesQuoteStatus.Sent, withLine: true);
        var orderId = Guid.NewGuid();
        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-QUOTE",
            Currency = "EUR",
            OrderedAtUtc = FixedNow
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = LifecycleHandler(db);

        await handler.AcceptAsync(new SalesQuoteLifecycleDto
        {
            Id = quote.Id,
            RowVersion = quote.RowVersion
        }, TestContext.Current.CancellationToken);

        var accepted = await db.Set<SalesQuote>().SingleAsync(x => x.Id == quote.Id, TestContext.Current.CancellationToken);
        accepted.Status.Should().Be(SalesQuoteStatus.Accepted);

        await handler.Invoking(x => x.RejectAsync(new SalesQuoteLifecycleDto
            {
                Id = quote.Id,
                RowVersion = accepted.RowVersion
            }, TestContext.Current.CancellationToken))
            .Should()
            .ThrowAsync<ValidationException>();

        await handler.ConvertAsync(new SalesQuoteConvertDto
        {
            Id = quote.Id,
            RowVersion = accepted.RowVersion,
            ConvertedOrderId = orderId
        }, TestContext.Current.CancellationToken);

        var converted = await db.Set<SalesQuote>().SingleAsync(x => x.Id == quote.Id, TestContext.Current.CancellationToken);
        converted.Status.Should().Be(SalesQuoteStatus.Converted);
        converted.ConvertedOrderId.Should().Be(orderId);
        db.Set<BusinessEvent>().Should().Contain(x => x.EventType == "sales.quote.accepted");
        db.Set<BusinessEvent>().Should().Contain(x => x.EventType == "sales.quote.converted");
    }

    [Fact]
    public async Task ConvertSalesQuoteToOrder_Should_Create_Order_From_Accepted_Catalog_Quote_Snapshot()
    {
        await using var db = SalesQuoteTestDbContext.Create();
        SeedOrderNumberSequence(db);
        var businessId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var quote = SeedQuote(db, SalesQuoteStatus.Accepted, withLine: true);
        quote.BusinessId = businessId;
        quote.CustomerId = customerId;
        quote.Currency = "USD";
        quote.BillingAddressJson = "{\"line1\":\"Billing\"}";
        quote.ShippingAddressJson = "{\"line1\":\"Shipping\"}";
        quote.InternalNotes = "Install after payment.";
        quote.Lines.Single().ProductVariantId = variantId;
        quote.Lines.Single().Sku = "CAT-1";
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = ConvertToOrderHandler(db);

        var orderId = await handler.HandleAsync(new SalesQuoteCreateOrderDto
        {
            Id = quote.Id,
            RowVersion = quote.RowVersion,
            ActorUserId = Guid.NewGuid(),
            Reason = "Accepted by customer"
        }, TestContext.Current.CancellationToken);

        var order = await db.Set<Order>()
            .Include(x => x.Lines)
            .SingleAsync(x => x.Id == orderId, TestContext.Current.CancellationToken);
        var converted = await db.Set<SalesQuote>().SingleAsync(x => x.Id == quote.Id, TestContext.Current.CancellationToken);

        order.OrderNumber.Should().Be("O-20260612-001");
        order.BusinessId.Should().Be(businessId);
        order.CustomerId.Should().Be(customerId);
        order.Currency.Should().Be("USD");
        order.SalesChannel.Should().Be(SalesChannel.Admin);
        order.OrderedAtUtc.Should().Be(FixedNow);
        order.BillingAddressJson.Should().Be("{\"line1\":\"Billing\"}");
        order.ShippingAddressJson.Should().Be("{\"line1\":\"Shipping\"}");
        order.SubtotalNetMinor.Should().Be(1000);
        order.TaxTotalMinor.Should().Be(190);
        order.GrandTotalGrossMinor.Should().Be(1190);
        order.InternalNotes.Should().Contain("Created from sales quote");
        order.InternalNotes.Should().Contain("Install after payment.");
        order.Lines.Should().ContainSingle().Which.VariantId.Should().Be(variantId);
        order.Lines.Single().Sku.Should().Be("CAT-1");
        converted.Status.Should().Be(SalesQuoteStatus.Converted);
        converted.ConvertedOrderId.Should().Be(order.Id);
        db.Set<BusinessEvent>().Should().ContainSingle(x => x.EventType == "sales.quote.order_created");
        db.Set<BusinessEvent>().Should().Contain(x => x.EventType == "sales.quote.converted");
        db.Set<AuditTrail>().Should().Contain(x => x.EntityType == "SalesQuote" && x.EntityId == quote.Id);
    }

    [Fact]
    public async Task ConvertSalesQuoteToOrder_Should_Create_Order_With_NonCatalog_Line_Without_Fake_Product()
    {
        await using var db = SalesQuoteTestDbContext.Create();
        var quote = SeedQuote(db, SalesQuoteStatus.Accepted, withLine: true);
        quote.Lines.Single().ProductVariantId = null;
        quote.Lines.Single().Name = "Custom onboarding package";
        quote.Lines.Single().Sku = null;
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = ConvertToOrderHandler(db);

        var orderId = await handler.HandleAsync(new SalesQuoteCreateOrderDto
        {
            Id = quote.Id,
            RowVersion = quote.RowVersion
        }, TestContext.Current.CancellationToken);

        var order = await db.Set<Order>()
            .Include(x => x.Lines)
            .SingleAsync(x => x.Id == orderId, TestContext.Current.CancellationToken);

        var line = order.Lines.Should().ContainSingle().Subject;
        line.VariantId.Should().BeNull();
        line.Name.Should().Be("Custom onboarding package");
        line.Sku.Should().BeEmpty();
        line.UnitPriceNetMinor.Should().Be(1000);
        line.LineTaxMinor.Should().Be(190);
    }

    [Theory]
    [InlineData(SalesQuoteStatus.Draft)]
    [InlineData(SalesQuoteStatus.Sent)]
    [InlineData(SalesQuoteStatus.Rejected)]
    [InlineData(SalesQuoteStatus.Expired)]
    [InlineData(SalesQuoteStatus.Converted)]
    public async Task ConvertSalesQuoteToOrder_Should_Reject_NonAccepted_Quotes(SalesQuoteStatus status)
    {
        await using var db = SalesQuoteTestDbContext.Create();
        var quote = SeedQuote(db, status, withLine: true);
        if (status == SalesQuoteStatus.Converted)
        {
            quote.ConvertedOrderId = Guid.NewGuid();
        }

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = ConvertToOrderHandler(db);

        await handler.Invoking(x => x.HandleAsync(new SalesQuoteCreateOrderDto
            {
                Id = quote.Id,
                RowVersion = quote.RowVersion
            }, TestContext.Current.CancellationToken))
            .Should()
            .ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task ConvertSalesQuoteToOrder_Should_Reject_Stale_RowVersion_And_Already_Converted_Quote()
    {
        await using var db = SalesQuoteTestDbContext.Create();
        var quote = SeedQuote(db, SalesQuoteStatus.Accepted, withLine: true);
        quote.ConvertedOrderId = Guid.NewGuid();
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = ConvertToOrderHandler(db);

        await handler.Invoking(x => x.HandleAsync(new SalesQuoteCreateOrderDto
            {
                Id = quote.Id,
                RowVersion = new byte[] { 9, 9, 9 }
            }, TestContext.Current.CancellationToken))
            .Should()
            .ThrowAsync<DbUpdateConcurrencyException>();

        await handler.Invoking(x => x.HandleAsync(new SalesQuoteCreateOrderDto
            {
                Id = quote.Id,
                RowVersion = quote.RowVersion
            }, TestContext.Current.CancellationToken))
            .Should()
            .ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task GetSalesQuotesPage_And_Detail_Should_Return_NonNull_Collections_And_Filtered_Results()
    {
        await using var db = SalesQuoteTestDbContext.Create();
        var match = SeedQuote(db, SalesQuoteStatus.Sent, withLine: true);
        match.Title = "Enterprise renewal";
        match.QuoteNumber = "Q-MATCH";
        match.ValidUntilUtc = FixedNow.AddDays(7);
        SeedQuote(db, SalesQuoteStatus.Draft, withLine: true).Title = "Other quote";
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var (items, total) = await new GetSalesQuotesPageHandler(db).HandleAsync(
            page: 1,
            pageSize: 20,
            query: "MATCH",
            filter: SalesQuoteDocumentFilter.Sent,
            businessId: null,
            customerId: null,
            opportunityId: null,
            validUntilFromUtc: FixedNow,
            validUntilToUtc: FixedNow.AddDays(14),
            ct: TestContext.Current.CancellationToken);
        var detail = await new GetSalesQuoteDetailHandler(db).HandleAsync(match.Id, TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Should().ContainSingle().Which.QuoteNumber.Should().Be("Q-MATCH");
        detail.Should().NotBeNull();
        detail!.Lines.Should().ContainSingle();
        detail.CustomerSnapshotJson.Should().Be("{}");
    }

    private static CreateSalesQuoteHandler CreateHandler(SalesQuoteTestDbContext db)
        => new(db, new SalesQuoteCreateValidator(new TestStringLocalizer()), new TestStringLocalizer());

    private static SendSalesQuoteHandler SendHandler(SalesQuoteTestDbContext db)
    {
        var clock = new FixedClock(FixedNow);
        return new(
            db,
            new SalesQuoteLifecycleValidator(),
            new TestStringLocalizer(),
            clock,
            new NumberSequenceService(db, clock),
            RecordEvents(db));
    }

    private static UpdateSalesQuoteLifecycleHandler LifecycleHandler(SalesQuoteTestDbContext db)
    {
        var clock = new FixedClock(FixedNow);
        return new(
            db,
            new SalesQuoteLifecycleValidator(),
            new SalesQuoteConvertValidator(),
            new TestStringLocalizer(),
            clock,
            RecordEvents(db));
    }

    private static ConvertSalesQuoteToOrderHandler ConvertToOrderHandler(SalesQuoteTestDbContext db)
    {
        var clock = new FixedClock(FixedNow);
        var sequenceService = new NumberSequenceService(db, clock);
        return new(
            db,
            new SalesQuoteLifecycleValidator(),
            new TestStringLocalizer(),
            clock,
            new OrderCreationService(db, clock, sequenceService),
            RecordEvents(db));
    }

    private static SalesQuoteLifecycleEventService RecordEvents(SalesQuoteTestDbContext db)
        => new(db, new BusinessEventService(db, new FixedClock(FixedNow)));

    private static SalesQuote SeedQuote(SalesQuoteTestDbContext db, SalesQuoteStatus status, bool withLine)
    {
        var quote = new SalesQuote
        {
            Id = Guid.NewGuid(),
            CreatedAtUtc = FixedNow,
            Title = "Quote",
            Status = status,
            Currency = "EUR",
            CustomerSnapshotJson = "{}",
            BillingAddressJson = "{}",
            ShippingAddressJson = "{}",
            RowVersion = new byte[] { 1, 2, 3 }
        };
        if (withLine)
        {
            quote.Lines.Add(new SalesQuoteLine
            {
                Id = Guid.NewGuid(),
                CreatedAtUtc = FixedNow,
                Name = "Line",
                Quantity = 1,
                UnitPriceNetMinor = 1000,
                UnitPriceGrossMinor = 1190,
                TaxRate = 0.19m,
                TotalNetMinor = 1000,
                TotalTaxMinor = 190,
                TotalGrossMinor = 1190,
                RowVersion = new byte[] { 4, 5, 6 }
            });
            quote.TotalNetMinor = 1000;
            quote.TotalTaxMinor = 190;
            quote.TotalGrossMinor = 1190;
        }

        db.Set<SalesQuote>().Add(quote);
        return quote;
    }

    private static void SeedQuoteNumberSequence(SalesQuoteTestDbContext db)
    {
        db.Set<NumberSequence>().Add(new NumberSequence
        {
            Id = Guid.NewGuid(),
            CreatedAtUtc = FixedNow,
            DocumentType = NumberSequenceDocumentType.SalesQuote,
            ScopeKey = NumberSequenceService.GlobalScopeKey,
            PrefixPattern = "Q-{yyyy}{MM}{dd}-{seq}",
            NextValue = 1,
            PaddingLength = 3,
            ResetPolicy = NumberSequenceResetPolicy.Never,
            IsActive = true,
            MetadataJson = "{}",
            RowVersion = new byte[] { 7, 8, 9 }
        });
    }

    private static void SeedOrderNumberSequence(SalesQuoteTestDbContext db)
    {
        db.Set<NumberSequence>().Add(new NumberSequence
        {
            Id = Guid.NewGuid(),
            CreatedAtUtc = FixedNow,
            DocumentType = NumberSequenceDocumentType.Order,
            ScopeKey = NumberSequenceService.GlobalScopeKey,
            PrefixPattern = "O-{yyyy}{MM}{dd}-{seq}",
            NextValue = 1,
            PaddingLength = 3,
            ResetPolicy = NumberSequenceResetPolicy.Never,
            IsActive = true,
            MetadataJson = "{}",
            RowVersion = new byte[] { 8, 9, 10 }
        });
    }

    private sealed class SalesQuoteTestDbContext : DbContext, IAppDbContext
    {
        private SalesQuoteTestDbContext(DbContextOptions<SalesQuoteTestDbContext> options)
            : base(options)
        {
        }

        public static SalesQuoteTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<SalesQuoteTestDbContext>()
                .UseInMemoryDatabase($"darwin_sales_quote_tests_{Guid.NewGuid():N}")
                .Options;
            return new SalesQuoteTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SalesQuote>()
                .HasMany(x => x.Lines)
                .WithOne()
                .HasForeignKey(x => x.SalesQuoteId);
            modelBuilder.Entity<SalesQuoteLine>();
            modelBuilder.Entity<NumberSequence>();
            modelBuilder.Entity<BusinessEvent>();
            modelBuilder.Entity<AuditTrail>();
            modelBuilder.Entity<Order>()
                .HasMany(x => x.Lines)
                .WithOne()
                .HasForeignKey(x => x.OrderId);
            modelBuilder.Entity<OrderLine>();
            modelBuilder.Entity<Customer>();
            modelBuilder.Entity<Opportunity>();
        }
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTime utcNow) => UtcNow = utcNow;
        public DateTime UtcNow { get; }
    }

    private sealed class TestStringLocalizer : IStringLocalizer<ValidationResource>
    {
        public LocalizedString this[string name] => new(name, name);
        public LocalizedString this[string name, params object[] arguments] => new(name, string.Format(name, arguments));
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => Array.Empty<LocalizedString>();
    }
}
