using Darwin.Application;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Orders.Commands;
using Darwin.Application.Orders.DTOs;
using Darwin.Application.Orders.Validators;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.Orders;

public sealed class ShipmentCarrierEventHandlerTests
{
    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_AdvanceShipment_ToDelivered_AndPersistCarrierMetadata()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-1",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-001",
            Status = ShipmentStatus.Pending
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var occurredAtUtc = DateTime.UtcNow.AddMinutes(-5);
        var result = await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-001",
            TrackingNumber = "TRACK-001",
            LabelUrl = "https://labels.example.com/TRACK-001.pdf",
            Service = "Express",
            CarrierEventKey = "shipment.delivered",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "Delivered",
            ExceptionCode = "delivery.exception",
            ExceptionMessage = "Recipient not available"
        }, TestContext.Current.CancellationToken);

        result.Status.Should().Be(ShipmentStatus.Delivered);
        result.TrackingNumber.Should().Be("TRACK-001");
        result.LabelUrl.Should().Be("https://labels.example.com/TRACK-001.pdf");
        result.Service.Should().Be("Express");
        result.LastCarrierEventKey.Should().Be("shipment.delivered");
        result.ShippedAtUtc.Should().Be(occurredAtUtc);
        result.DeliveredAtUtc.Should().Be(occurredAtUtc);

        var shipment = await db.Set<Shipment>().SingleAsync(x => x.Id == shipmentId, TestContext.Current.CancellationToken);
        shipment.Status.Should().Be(ShipmentStatus.Delivered);
        shipment.TrackingNumber.Should().Be("TRACK-001");
        shipment.LabelUrl.Should().Be("https://labels.example.com/TRACK-001.pdf");
        shipment.Service.Should().Be("Express");
        shipment.LastCarrierEventKey.Should().Be("shipment.delivered");

        var carrierEvent = await db.Set<ShipmentCarrierEvent>().SingleAsync(TestContext.Current.CancellationToken);
        carrierEvent.ShipmentId.Should().Be(shipmentId);
        carrierEvent.CarrierEventKey.Should().Be("shipment.delivered");
        carrierEvent.ProviderStatus.Should().Be("Delivered");
        carrierEvent.ProviderShipmentReference.Should().Be("dhl-ship-001");
        carrierEvent.ExceptionCode.Should().Be("delivery.exception");
        carrierEvent.ExceptionMessage.Should().Be("Recipient not available");
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_NotDowngradeDeliveredShipment_OnLaterTransitEvent()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var shippedAtUtc = DateTime.UtcNow.AddHours(-2);
        var deliveredAtUtc = DateTime.UtcNow.AddHours(-1);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-2",
            Currency = "EUR",
            Status = OrderStatus.Delivered
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-002",
            TrackingNumber = "TRACK-002",
            Status = ShipmentStatus.Delivered,
            ShippedAtUtc = shippedAtUtc,
            DeliveredAtUtc = deliveredAtUtc,
            LastCarrierEventKey = "shipment.delivered"
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var result = await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-002",
            TrackingNumber = "TRACK-002",
            CarrierEventKey = "shipment.in_transit",
            OccurredAtUtc = DateTime.UtcNow,
            ProviderStatus = "InTransit"
        }, TestContext.Current.CancellationToken);

        result.Status.Should().Be(ShipmentStatus.Delivered);
        result.DeliveredAtUtc.Should().Be(deliveredAtUtc);
        result.ShippedAtUtc.Should().Be(shippedAtUtc);
        result.LastCarrierEventKey.Should().Be("shipment.in_transit");
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_NotInsertDuplicateCarrierTimelineRows_ForSameCallbackFingerprint()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var occurredAtUtc = DateTime.UtcNow.AddMinutes(-15);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-3",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-003",
            Status = ShipmentStatus.Packed
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var dto = new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-003",
            TrackingNumber = "TRACK-003",
            CarrierEventKey = "shipment.in_transit",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "InTransit"
        };

        await handler.HandleAsync(dto, TestContext.Current.CancellationToken);
        await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        var carrierEvents = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync(TestContext.Current.CancellationToken);

        carrierEvents.Should().HaveCount(1);
        carrierEvents[0].CarrierEventKey.Should().Be("shipment.in_transit");
    }

    private sealed class ShipmentCarrierEventTestDbContext : DbContext, IAppDbContext
    {
        private ShipmentCarrierEventTestDbContext(DbContextOptions<ShipmentCarrierEventTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static ShipmentCarrierEventTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<ShipmentCarrierEventTestDbContext>()
                .UseInMemoryDatabase($"darwin_shipment_carrier_event_tests_{Guid.NewGuid()}")
                .Options;
            return new ShipmentCarrierEventTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Order>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.OrderNumber).IsRequired();
                builder.Property(x => x.Currency).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<Shipment>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Carrier).IsRequired();
                builder.Property(x => x.Service).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
                builder.HasMany(x => x.CarrierEvents).WithOne().HasForeignKey(x => x.ShipmentId);
            });

            modelBuilder.Entity<ShipmentCarrierEvent>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Carrier).IsRequired();
                builder.Property(x => x.ProviderShipmentReference).IsRequired();
                builder.Property(x => x.CarrierEventKey).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
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
