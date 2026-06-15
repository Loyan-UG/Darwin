using Darwin.Application;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Foundation;
using Darwin.Application.Sales.Commands;
using Darwin.Application.Sales.DTOs;
using Darwin.Application.Sales.Queries;
using Darwin.Application.Sales.Services;
using Darwin.Application.Sales.Validators;
using Darwin.Domain.Entities.Foundation;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Entities.Sales;
using Darwin.Domain.Enums;
using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.Sales;

public sealed class DeliveryNoteHandlersTests
{
    private static readonly DateTime FixedNow = new(2026, 6, 12, 11, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreateDeliveryNoteFromShipment_Should_Snapshot_Catalog_And_NonCatalog_Lines_From_Shipment_Quantities()
    {
        await using var db = DeliveryNoteTestDbContext.Create();
        var seed = SeedOrderAndShipment(db, includeNonCatalogLine: true);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var id = await CreateHandler(db).HandleAsync(new DeliveryNoteCreateFromShipmentDto
        {
            ShipmentId = seed.ShipmentId,
            InternalNotes = "  Leave at receiving desk. "
        }, TestContext.Current.CancellationToken);

        var note = await db.Set<DeliveryNote>()
            .Include(x => x.Lines)
            .SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);

        note.Status.Should().Be(DeliveryNoteStatus.Draft);
        note.OrderId.Should().Be(seed.OrderId);
        note.ShipmentId.Should().Be(seed.ShipmentId);
        note.Carrier.Should().Be("DHL");
        note.Service.Should().Be("Parcel");
        note.TrackingNumber.Should().Be("TRACK-1");
        note.ShippingAddressJson.Should().Be("{\"line1\":\"Warehouse\"}");
        note.InternalNotes.Should().Be("Leave at receiving desk.");
        note.TotalQuantity.Should().Be(3);
        note.TotalNetMinor.Should().Be(3500);
        note.TotalTaxMinor.Should().Be(665);
        note.TotalGrossMinor.Should().Be(4165);

        note.Lines.Should().HaveCount(2);
        note.Lines.Should().Contain(x => x.ProductVariantId == seed.CatalogVariantId && x.Quantity == 2 && x.Name == "Coffee beans");
        note.Lines.Should().Contain(x => x.ProductVariantId == null && x.Quantity == 1 && x.Name == "Installation service");
        db.Set<BusinessEvent>().Should().ContainSingle(x => x.EventType == "sales.delivery_note.created");
        db.Set<AuditTrail>().Should().ContainSingle(x => x.EntityType == DeliveryNoteLifecycleEventService.EntityType && x.EntityId == note.Id);
    }

    [Fact]
    public async Task CreateDeliveryNoteFromShipment_Should_Reject_Duplicate_And_Invalid_Shipment_Lines()
    {
        await using var db = DeliveryNoteTestDbContext.Create();
        var seed = SeedOrderAndShipment(db, includeNonCatalogLine: false);
        db.Set<DeliveryNote>().Add(new DeliveryNote
        {
            Id = Guid.NewGuid(),
            OrderId = seed.OrderId,
            ShipmentId = seed.ShipmentId,
            Status = DeliveryNoteStatus.Cancelled,
            Currency = "EUR",
            ShippingAddressJson = "{}",
            MetadataJson = "{}",
            RowVersion = new byte[] { 1 }
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await CreateHandler(db).Invoking(x => x.HandleAsync(new DeliveryNoteCreateFromShipmentDto
            {
                ShipmentId = seed.ShipmentId
            }, TestContext.Current.CancellationToken))
            .Should()
            .ThrowAsync<ValidationException>();

        await using var invalidDb = DeliveryNoteTestDbContext.Create();
        var invalidSeed = SeedOrderAndShipment(invalidDb, includeNonCatalogLine: false);
        await invalidDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        var invalidShipmentLine = await invalidDb.Set<ShipmentLine>().SingleAsync(TestContext.Current.CancellationToken);
        invalidShipmentLine.Quantity = 99;
        await invalidDb.SaveChangesAsync(TestContext.Current.CancellationToken);

        await CreateHandler(invalidDb).Invoking(x => x.HandleAsync(new DeliveryNoteCreateFromShipmentDto
            {
                ShipmentId = invalidSeed.ShipmentId
            }, TestContext.Current.CancellationToken))
            .Should()
            .ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task DeliveryNoteLifecycle_Should_Reserve_Number_Once_And_Record_Idempotent_Evidence()
    {
        await using var db = DeliveryNoteTestDbContext.Create();
        SeedDeliveryNoteNumberSequence(db);
        var seed = SeedOrderAndShipment(db, includeNonCatalogLine: false);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var noteId = await CreateHandler(db).HandleAsync(new DeliveryNoteCreateFromShipmentDto
        {
            ShipmentId = seed.ShipmentId
        }, TestContext.Current.CancellationToken);
        var created = await db.Set<DeliveryNote>().SingleAsync(x => x.Id == noteId, TestContext.Current.CancellationToken);
        created.RowVersion = new byte[] { 9, 9, 9 };
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = LifecycleHandler(db);
        var rowVersion = await db.Set<DeliveryNote>()
            .Where(x => x.Id == noteId)
            .Select(x => x.RowVersion)
            .SingleAsync(TestContext.Current.CancellationToken);

        await handler.PrepareAsync(new DeliveryNoteLifecycleDto
        {
            Id = noteId,
            RowVersion = rowVersion,
            ActorUserId = seed.ActorUserId
        }, TestContext.Current.CancellationToken);
        rowVersion = await db.Set<DeliveryNote>().Where(x => x.Id == noteId).Select(x => x.RowVersion).SingleAsync(TestContext.Current.CancellationToken);

        await handler.IssueAsync(new DeliveryNoteLifecycleDto
        {
            Id = noteId,
            RowVersion = rowVersion,
            ActorUserId = seed.ActorUserId
        }, TestContext.Current.CancellationToken);

        var issued = await db.Set<DeliveryNote>().SingleAsync(x => x.Id == noteId, TestContext.Current.CancellationToken);
        issued.Status.Should().Be(DeliveryNoteStatus.Issued);
        issued.DeliveryNoteNumber.Should().Be("DN-20260612-001");
        issued.IssuedByUserId.Should().Be(seed.ActorUserId);
        db.Set<NumberSequence>().Single().NextValue.Should().Be(2);

        var duplicate = await EventService(db).RecordStatusChangedAsync(
            issued,
            DeliveryNoteStatus.Prepared,
            DeliveryNoteStatus.Issued,
            FixedNow,
            TestContext.Current.CancellationToken);
        duplicate.Succeeded.Should().BeTrue(duplicate.Error);
        db.Set<BusinessEvent>().Should().ContainSingle(x =>
            x.EventKey == $"sales.delivery_note.status_changed:{noteId:N}:Prepared:Issued");
        db.Set<AuditTrail>().Should().ContainSingle(x =>
            x.EntityType == DeliveryNoteLifecycleEventService.EntityType &&
            x.EntityId == noteId &&
            x.CorrelationId == $"sales.delivery_note.status_changed:{noteId:N}:Prepared:Issued");
    }

    [Fact]
    public async Task DeliveryNoteLifecycle_Should_Reject_Invalid_Transitions_And_Stale_RowVersion()
    {
        await using var db = DeliveryNoteTestDbContext.Create();
        var seed = SeedOrderAndShipment(db, includeNonCatalogLine: false);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var noteId = await CreateHandler(db).HandleAsync(new DeliveryNoteCreateFromShipmentDto
        {
            ShipmentId = seed.ShipmentId
        }, TestContext.Current.CancellationToken);
        var created = await db.Set<DeliveryNote>().SingleAsync(x => x.Id == noteId, TestContext.Current.CancellationToken);
        created.RowVersion = new byte[] { 9, 9, 9 };
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = LifecycleHandler(db);

        await handler.Invoking(x => x.IssueAsync(new DeliveryNoteLifecycleDto
            {
                Id = noteId,
                RowVersion = new byte[] { 1, 2, 3 }
            }, TestContext.Current.CancellationToken))
            .Should()
            .ThrowAsync<DbUpdateConcurrencyException>();

        var currentRowVersion = await db.Set<DeliveryNote>().Where(x => x.Id == noteId).Select(x => x.RowVersion).SingleAsync(TestContext.Current.CancellationToken);
        await handler.Invoking(x => x.MarkDeliveredAsync(new DeliveryNoteLifecycleDto
            {
                Id = noteId,
                RowVersion = currentRowVersion
            }, TestContext.Current.CancellationToken))
            .Should()
            .ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task GetDeliveryNotesPage_And_Detail_Should_Filter_And_Return_NonNull_Collections()
    {
        await using var db = DeliveryNoteTestDbContext.Create();
        var seed = SeedOrderAndShipment(db, includeNonCatalogLine: false);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var noteId = await CreateHandler(db).HandleAsync(new DeliveryNoteCreateFromShipmentDto
        {
            ShipmentId = seed.ShipmentId
        }, TestContext.Current.CancellationToken);
        var note = await db.Set<DeliveryNote>().SingleAsync(x => x.Id == noteId, TestContext.Current.CancellationToken);
        note.DeliveryNoteNumber = "DN-MATCH";
        note.Status = DeliveryNoteStatus.Issued;
        note.IssuedAtUtc = FixedNow;
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var (items, total) = await new GetDeliveryNotesPageHandler(db).HandleAsync(
            page: 1,
            pageSize: 20,
            query: "MATCH",
            filter: DeliveryNoteDocumentFilter.Issued,
            businessId: seed.BusinessId,
            customerId: seed.CustomerId,
            issuedFromUtc: FixedNow.AddMinutes(-1),
            issuedToUtc: FixedNow.AddMinutes(1),
            ct: TestContext.Current.CancellationToken);
        var detail = await new GetDeliveryNoteDetailHandler(db).HandleAsync(noteId, TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Should().ContainSingle().Which.DeliveryNoteNumber.Should().Be("DN-MATCH");
        detail.Should().NotBeNull();
        detail!.Lines.Should().ContainSingle();
        detail.MetadataJson.Should().Be("{}");
    }

    [Fact]
    public void DeliveryNoteWorkflowPolicy_Should_Keep_Lifecycle_Rules_Centralized()
    {
        var policy = new DeliveryNoteWorkflowPolicy();

        policy.CanTransition(DeliveryNoteStatus.Draft, DeliveryNoteStatus.Prepared).Should().BeTrue();
        policy.CanTransition(DeliveryNoteStatus.Prepared, DeliveryNoteStatus.Issued).Should().BeTrue();
        policy.CanTransition(DeliveryNoteStatus.Issued, DeliveryNoteStatus.Shipped).Should().BeTrue();
        policy.CanTransition(DeliveryNoteStatus.Shipped, DeliveryNoteStatus.Delivered).Should().BeTrue();
        policy.CanTransition(DeliveryNoteStatus.Delivered, DeliveryNoteStatus.Cancelled).Should().BeFalse();
    }

    private static CreateDeliveryNoteFromShipmentHandler CreateHandler(DeliveryNoteTestDbContext db)
        => new(
            db,
            new DeliveryNoteCreateFromShipmentValidator(),
            new TestStringLocalizer(),
            new FixedClock(FixedNow),
            EventService(db));

    private static UpdateDeliveryNoteLifecycleHandler LifecycleHandler(DeliveryNoteTestDbContext db)
        => new(
            db,
            new DeliveryNoteLifecycleValidator(),
            new TestStringLocalizer(),
            new FixedClock(FixedNow),
            new NumberSequenceService(db, new FixedClock(FixedNow)),
            new DeliveryNoteWorkflowPolicy(),
            EventService(db));

    private static DeliveryNoteLifecycleEventService EventService(DeliveryNoteTestDbContext db)
        => new(new BusinessEventService(db, new FixedClock(FixedNow)), db);

    private static SeedIds SeedOrderAndShipment(DeliveryNoteTestDbContext db, bool includeNonCatalogLine)
    {
        var businessId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var catalogLineId = Guid.NewGuid();
        var nonCatalogLineId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        var order = new Order
        {
            Id = orderId,
            BusinessId = businessId,
            CustomerId = customerId,
            OrderNumber = "ORD-DN-1",
            Currency = "EUR",
            ShippingAddressJson = "{\"line1\":\"Warehouse\"}",
            RowVersion = new byte[] { 1 }
        };
        order.Lines.Add(new OrderLine
        {
            Id = catalogLineId,
            OrderId = orderId,
            VariantId = variantId,
            Name = "Coffee beans",
            Sku = "COF-1",
            Quantity = 3,
            UnitPriceNetMinor = 1000,
            UnitPriceGrossMinor = 1190,
            VatRate = 0.19m,
            LineTaxMinor = 570,
            LineGrossMinor = 3570,
            RowVersion = new byte[] { 2 }
        });
        if (includeNonCatalogLine)
        {
            order.Lines.Add(new OrderLine
            {
                Id = nonCatalogLineId,
                OrderId = orderId,
                VariantId = null,
                Name = "Installation service",
                Sku = string.Empty,
                Quantity = 1,
                UnitPriceNetMinor = 1500,
                UnitPriceGrossMinor = 1785,
                VatRate = 0.19m,
                LineTaxMinor = 285,
                LineGrossMinor = 1785,
                RowVersion = new byte[] { 3 }
            });
        }

        var shipment = new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = " DHL ",
            Service = " Parcel ",
            TrackingNumber = " TRACK-1 ",
            ProviderShipmentReference = " PSR-1 ",
            RowVersion = new byte[] { 4 }
        };
        shipment.Lines.Add(new ShipmentLine
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipmentId,
            OrderLineId = catalogLineId,
            Quantity = 2,
            RowVersion = new byte[] { 5 }
        });
        if (includeNonCatalogLine)
        {
            shipment.Lines.Add(new ShipmentLine
            {
                Id = Guid.NewGuid(),
                ShipmentId = shipmentId,
                OrderLineId = nonCatalogLineId,
                Quantity = 1,
                RowVersion = new byte[] { 6 }
            });
        }

        db.Set<Order>().Add(order);
        db.Set<Shipment>().Add(shipment);
        return new SeedIds(businessId, customerId, actorUserId, orderId, shipmentId, variantId);
    }

    private static void SeedDeliveryNoteNumberSequence(DeliveryNoteTestDbContext db)
    {
        db.Set<NumberSequence>().Add(new NumberSequence
        {
            Id = Guid.NewGuid(),
            DocumentType = NumberSequenceDocumentType.DeliveryNote,
            ScopeKey = NumberSequenceService.GlobalScopeKey,
            PrefixPattern = "DN-{yyyy}{MM}{dd}-{seq}",
            NextValue = 1,
            PaddingLength = 3,
            ResetPolicy = NumberSequenceResetPolicy.Never,
            IsActive = true,
            MetadataJson = "{}",
            RowVersion = new byte[] { 7 }
        });
    }

    private sealed record SeedIds(
        Guid BusinessId,
        Guid CustomerId,
        Guid ActorUserId,
        Guid OrderId,
        Guid ShipmentId,
        Guid CatalogVariantId);

    private sealed class DeliveryNoteTestDbContext : DbContext, IAppDbContext
    {
        private DeliveryNoteTestDbContext(DbContextOptions<DeliveryNoteTestDbContext> options)
            : base(options)
        {
        }

        public static DeliveryNoteTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<DeliveryNoteTestDbContext>()
                .UseInMemoryDatabase($"darwin_delivery_note_tests_{Guid.NewGuid():N}")
                .Options;
            return new DeliveryNoteTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Order>()
                .HasMany(x => x.Lines)
                .WithOne()
                .HasForeignKey(x => x.OrderId);
            modelBuilder.Entity<OrderLine>();
            modelBuilder.Entity<Shipment>()
                .HasMany(x => x.Lines)
                .WithOne()
                .HasForeignKey(x => x.ShipmentId);
            modelBuilder.Entity<ShipmentLine>();
            modelBuilder.Entity<DeliveryNote>()
                .HasMany(x => x.Lines)
                .WithOne()
                .HasForeignKey(x => x.DeliveryNoteId);
            modelBuilder.Entity<DeliveryNoteLine>();
            modelBuilder.Entity<NumberSequence>();
            modelBuilder.Entity<BusinessEvent>();
            modelBuilder.Entity<AuditTrail>();
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
