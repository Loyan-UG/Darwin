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

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_NotInsertDuplicateCarrierTimelineRows_ForTrimmedCarrierAndReference()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var occurredAtUtc = DateTime.UtcNow.AddMinutes(-14);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-35",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-035",
            Status = ShipmentStatus.Packed
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var firstDto = new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-035",
            CarrierEventKey = "shipment.in_transit",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "InTransit"
        };

        var secondDto = new ApplyShipmentCarrierEventDto
        {
            Carrier = "  DHL  ",
            ProviderShipmentReference = "  dhl-ship-035  ",
            CarrierEventKey = "  shipment.in_transit  ",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "  InTransit  "
        };

        await handler.HandleAsync(firstDto, TestContext.Current.CancellationToken);
        await handler.HandleAsync(secondDto, TestContext.Current.CancellationToken);

        var carrierEvents = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync(TestContext.Current.CancellationToken);

        carrierEvents.Should().HaveCount(1);
        carrierEvents[0].CarrierEventKey.Should().Be("shipment.in_transit");
        carrierEvents[0].ProviderShipmentReference.Should().Be("dhl-ship-035");
        carrierEvents[0].Carrier.Should().Be("DHL");
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_NotInsertDuplicateCarrierTimelineRows_ForTrimmedCarrierEventKey()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var occurredAtUtc = DateTime.UtcNow.AddMinutes(-12);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-37",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-037",
            Status = ShipmentStatus.Packed
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var baseDto = new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-037",
            CarrierEventKey = "shipment.in_transit",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "InTransit"
        };

        var duplicateDto = new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-037",
            CarrierEventKey = "  shipment.in_transit  ",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "InTransit"
        };

        await handler.HandleAsync(baseDto, TestContext.Current.CancellationToken);
        var duplicateResult = await handler.HandleAsync(duplicateDto, TestContext.Current.CancellationToken);

        var carrierEvents = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync(TestContext.Current.CancellationToken);

        carrierEvents.Should().HaveCount(1);
        carrierEvents[0].CarrierEventKey.Should().Be("shipment.in_transit");
        duplicateResult.LastCarrierEventKey.Should().Be("shipment.in_transit");
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_NotOverwriteExistingExceptionFields_ForTrimmedDuplicateCallbackWhitespace()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var occurredAtUtc = DateTime.UtcNow.AddMinutes(-11);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-38",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-038",
            Status = ShipmentStatus.Packed
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-038",
            CarrierEventKey = "shipment.in_transit",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "InTransit",
            ExceptionCode = "  INITIAL  ",
            ExceptionMessage = "  Initial  "
        }, TestContext.Current.CancellationToken);

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "  dhl-ship-038  ",
            CarrierEventKey = "  shipment.in_transit  ",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = " InTransit ",
            ExceptionCode = "   ",
            ExceptionMessage = "   "
        }, TestContext.Current.CancellationToken);

        var carrierEvents = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync(TestContext.Current.CancellationToken);

        carrierEvents.Should().HaveCount(1);
        carrierEvents[0].CarrierEventKey.Should().Be("shipment.in_transit");
        carrierEvents[0].Carrier.Should().Be("DHL");
        carrierEvents[0].ProviderShipmentReference.Should().Be("dhl-ship-038");
        carrierEvents[0].ExceptionCode.Should().Be("INITIAL");
        carrierEvents[0].ExceptionMessage.Should().Be("Initial");
        carrierEvents[0].ProviderStatus.Should().Be("InTransit");
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_InsertNewCarrierTimeline_WhenOccurredAtUtcDiffersForTrimmedDuplicate()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var firstOccurredAtUtc = DateTime.UtcNow.AddMinutes(-20);
        var secondOccurredAtUtc = firstOccurredAtUtc.AddMinutes(1);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-39",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-039",
            Status = ShipmentStatus.Packed
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-039",
            CarrierEventKey = "shipment.in_transit",
            OccurredAtUtc = firstOccurredAtUtc,
            ProviderStatus = "InTransit"
        }, TestContext.Current.CancellationToken);

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "  DHL  ",
            ProviderShipmentReference = "  dhl-ship-039  ",
            CarrierEventKey = "  shipment.in_transit  ",
            OccurredAtUtc = secondOccurredAtUtc,
            ProviderStatus = "  InTransit  "
        }, TestContext.Current.CancellationToken);

        var carrierEvents = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync(TestContext.Current.CancellationToken);

        carrierEvents.Should().HaveCount(2);
        carrierEvents.Should().Contain(x => x.OccurredAtUtc == firstOccurredAtUtc);
        carrierEvents.Should().Contain(x => x.OccurredAtUtc == secondOccurredAtUtc);
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_InsertNewCarrierTimeline_ForTrimmedDuplicateButDifferentProviderStatus()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var occurredAtUtc = DateTime.UtcNow.AddMinutes(-10);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-40",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-040",
            Status = ShipmentStatus.Packed
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-040",
            CarrierEventKey = "shipment.in_transit",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "in_transit"
        }, TestContext.Current.CancellationToken);

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "  DHL  ",
            ProviderShipmentReference = "  dhl-ship-040  ",
            CarrierEventKey = "  shipment.in_transit  ",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "delayed"
        }, TestContext.Current.CancellationToken);

        var carrierEvents = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync(TestContext.Current.CancellationToken);

        carrierEvents.Should().HaveCount(2);
        carrierEvents.Should().Contain(e => e.ProviderStatus == "in_transit");
        carrierEvents.Should().Contain(e => e.ProviderStatus == "delayed");
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_InsertNewCarrierTimeline_WhenProviderStatusNullThenSet()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var occurredAtUtc = DateTime.UtcNow.AddMinutes(-9);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-41",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-041",
            Status = ShipmentStatus.Packed
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-041",
            CarrierEventKey = "shipment.in_transit",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "   "
        }, TestContext.Current.CancellationToken);

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-041",
            CarrierEventKey = "shipment.in_transit",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "InTransit"
        }, TestContext.Current.CancellationToken);

        var carrierEvents = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync(TestContext.Current.CancellationToken);

        carrierEvents.Should().HaveCount(2);
        carrierEvents.Should().Contain(e => e.ProviderStatus == null);
        carrierEvents.Should().Contain(e => e.ProviderStatus == "InTransit");
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_InsertNewCarrierTimeline_ForCarrierEventKeyCaseVariation()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var occurredAtUtc = DateTime.UtcNow.AddMinutes(-8);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-42",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-042",
            Status = ShipmentStatus.Packed
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-042",
            CarrierEventKey = "shipment.in_transit",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "InTransit"
        }, TestContext.Current.CancellationToken);

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-042",
            CarrierEventKey = "SHIPMENT.IN_TRANSIT",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "InTransit"
        }, TestContext.Current.CancellationToken);

        var carrierEvents = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync(TestContext.Current.CancellationToken);

        carrierEvents.Should().HaveCount(2);
        carrierEvents.Should().Contain(e => e.CarrierEventKey == "shipment.in_transit");
        carrierEvents.Should().Contain(e => e.CarrierEventKey == "SHIPMENT.IN_TRANSIT");
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_InsertNewCarrierTimeline_WhenProviderStatusCaseDiffers()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var occurredAtUtc = DateTime.UtcNow.AddMinutes(-7);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-43",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-043",
            Status = ShipmentStatus.Packed
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-043",
            CarrierEventKey = "shipment.in_transit",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "InTransit"
        }, TestContext.Current.CancellationToken);

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-043",
            CarrierEventKey = "shipment.in_transit",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "INTRANSIT"
        }, TestContext.Current.CancellationToken);

        var carrierEvents = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync(TestContext.Current.CancellationToken);

        carrierEvents.Should().HaveCount(2);
        carrierEvents.Should().Contain(e => e.ProviderStatus == "InTransit");
        carrierEvents.Should().Contain(e => e.ProviderStatus == "INTRANSIT");
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_UpdateShipmentMetadata_OnTrimmedDuplicateTimeline()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var occurredAtUtc = DateTime.UtcNow.AddMinutes(-6);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-44",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-044",
            TrackingNumber = "TRACK-OLD",
            LabelUrl = "https://labels.example.com/old.pdf",
            Status = ShipmentStatus.Packed
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-044",
            CarrierEventKey = "shipment.in_transit",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "InTransit",
            Service = "  Parcel  ",
            TrackingNumber = "  TRACK-OLD  ",
            LabelUrl = "  https://labels.example.com/old.pdf  "
        }, TestContext.Current.CancellationToken);

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-044",
            CarrierEventKey = "shipment.in_transit",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "InTransit",
            Service = "  Express  ",
            TrackingNumber = "  TRACK-NEW  ",
            LabelUrl = "  https://labels.example.com/new.pdf  "
        }, TestContext.Current.CancellationToken);

        var shipment = await db.Set<Shipment>()
            .SingleAsync(x => x.Id == shipmentId, TestContext.Current.CancellationToken);

        shipment.Service.Should().Be("Express");
        shipment.TrackingNumber.Should().Be("TRACK-NEW");
        shipment.LabelUrl.Should().Be("https://labels.example.com/new.pdf");

        var carrierEvents = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync(TestContext.Current.CancellationToken);

        carrierEvents.Should().HaveCount(1);
        carrierEvents[0].Carrier.Should().Be("DHL");
        carrierEvents[0].ProviderStatus.Should().Be("InTransit");
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_KeepShipmentMetadata_WhenDuplicateMetadataIsWhitespace()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var occurredAtUtc = DateTime.UtcNow.AddMinutes(-5);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-45",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Express",
            ProviderShipmentReference = "dhl-ship-045",
            TrackingNumber = "TRACK-BASE",
            LabelUrl = "https://labels.example.com/base.pdf",
            Status = ShipmentStatus.Packed
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-045",
            CarrierEventKey = "shipment.in_transit",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "InTransit",
            Service = "  Express  ",
            TrackingNumber = "  TRACK-BASE  ",
            LabelUrl = "  https://labels.example.com/base.pdf  "
        }, TestContext.Current.CancellationToken);

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-045",
            CarrierEventKey = "shipment.in_transit",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "InTransit",
            Service = "  ",
            TrackingNumber = "   ",
            LabelUrl = "   "
        }, TestContext.Current.CancellationToken);

        var shipment = await db.Set<Shipment>()
            .SingleAsync(x => x.Id == shipmentId, TestContext.Current.CancellationToken);

        shipment.Service.Should().Be("Express");
        shipment.TrackingNumber.Should().Be("TRACK-BASE");
        shipment.LabelUrl.Should().Be("https://labels.example.com/base.pdf");
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_UpdateExistingCarrierTimelineExceptions_ForTrimmedDuplicateCallback()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var occurredAtUtc = DateTime.UtcNow.AddMinutes(-13);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-36",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-036",
            Status = ShipmentStatus.Packed
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-036",
            CarrierEventKey = "shipment.in_transit",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "InTransit"
        }, TestContext.Current.CancellationToken);

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "  DHL  ",
            ProviderShipmentReference = "  dhl-ship-036  ",
            CarrierEventKey = "   shipment.in_transit   ",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "   InTransit   ",
            ExceptionCode = "  TRANSIENT  ",
            ExceptionMessage = "   Retrying  "
        }, TestContext.Current.CancellationToken);

        var carrierEvents = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync(TestContext.Current.CancellationToken);

        carrierEvents.Should().HaveCount(1);
        carrierEvents[0].Carrier.Should().Be("DHL");
        carrierEvents[0].ProviderShipmentReference.Should().Be("dhl-ship-036");
        carrierEvents[0].CarrierEventKey.Should().Be("shipment.in_transit");
        carrierEvents[0].ProviderStatus.Should().Be("InTransit");
        carrierEvents[0].ExceptionCode.Should().Be("TRANSIENT");
        carrierEvents[0].ExceptionMessage.Should().Be("Retrying");
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_Throw_When_Shipment_NotFound_ForCarrierShipmentReferenceAndCarrier()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var act = async () => await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "non-existing-ref",
            CarrierEventKey = "shipment.delivered",
            OccurredAtUtc = DateTime.UtcNow
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>()
            .WithMessage("ShipmentNotFoundForCarrierCallback");
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_Throw_When_Shipment_NotFound_ForCarrierCaseMismatch()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();

        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-44a",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-044a",
            Status = ShipmentStatus.Packed
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var act = async () => await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "dhl",
            ProviderShipmentReference = "dhl-ship-044a",
            CarrierEventKey = "shipment.in_transit",
            OccurredAtUtc = DateTime.UtcNow,
            ProviderStatus = "InTransit"
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>()
            .WithMessage("ShipmentNotFoundForCarrierCallback");

        var shipment = await db.Set<Shipment>()
            .SingleAsync(x => x.Id == shipmentId, TestContext.Current.CancellationToken);
        shipment.Status.Should().Be(ShipmentStatus.Packed);
        shipment.LastCarrierEventKey.Should().BeNull();
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_Throw_When_CarrierCaseMismatch_AfterWhitespacePadding()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();

        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-44d",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-044d",
            Status = ShipmentStatus.Packed
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var act = async () => await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = " dhl ",
            ProviderShipmentReference = "  dhl-ship-044d  ",
            CarrierEventKey = "shipment.in_transit",
            OccurredAtUtc = DateTime.UtcNow,
            ProviderStatus = "InTransit"
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>()
            .WithMessage("ShipmentNotFoundForCarrierCallback");

        var shipment = await db.Set<Shipment>()
            .SingleAsync(x => x.Id == shipmentId, TestContext.Current.CancellationToken);
        shipment.Status.Should().Be(ShipmentStatus.Packed);
        shipment.LastCarrierEventKey.Should().BeNull();

        var events = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync(TestContext.Current.CancellationToken);
        events.Should().BeEmpty();
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_Throw_When_ProviderShipmentReferenceCaseMismatch_WithWhitespacePadding()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();

        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-044e",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-044e",
            Status = ShipmentStatus.Packed
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var act = async () => await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = " DHL-SHIP-044E ",
            CarrierEventKey = "shipment.in_transit",
            OccurredAtUtc = DateTime.UtcNow,
            ProviderStatus = "InTransit"
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>()
            .WithMessage("ShipmentNotFoundForCarrierCallback");

        var shipment = await db.Set<Shipment>()
            .SingleAsync(x => x.Id == shipmentId, TestContext.Current.CancellationToken);
        shipment.Status.Should().Be(ShipmentStatus.Packed);
        shipment.LastCarrierEventKey.Should().BeNull();

        var events = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync(TestContext.Current.CancellationToken);
        events.Should().BeEmpty();
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_Throw_When_CarrierAndProviderShipmentReferenceMismatch_WithWhitespacePadding()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();

        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-044f",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-044f",
            Status = ShipmentStatus.Packed
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var act = async () => await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = " Fedex ",
            ProviderShipmentReference = " DHL-SHIP-044F ",
            CarrierEventKey = "shipment.in_transit",
            OccurredAtUtc = DateTime.UtcNow,
            ProviderStatus = "InTransit"
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>()
            .WithMessage("ShipmentNotFoundForCarrierCallback");

        var shipment = await db.Set<Shipment>()
            .SingleAsync(x => x.Id == shipmentId, TestContext.Current.CancellationToken);
        shipment.Status.Should().Be(ShipmentStatus.Packed);
        shipment.LastCarrierEventKey.Should().BeNull();

        var events = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync(TestContext.Current.CancellationToken);
        events.Should().BeEmpty();
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_Throw_When_Shipment_IsDeleted()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();

        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-44c",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-044c",
            Status = ShipmentStatus.Packed,
            IsDeleted = true
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var act = async () => await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-044c",
            CarrierEventKey = "shipment.in_transit",
            OccurredAtUtc = DateTime.UtcNow,
            ProviderStatus = "InTransit"
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>()
            .WithMessage("ShipmentNotFoundForCarrierCallback");

        var shipment = await db.Set<Shipment>()
            .SingleAsync(x => x.Id == shipmentId, TestContext.Current.CancellationToken);
        shipment.Status.Should().Be(ShipmentStatus.Packed);
        shipment.LastCarrierEventKey.Should().BeNull();

        var events = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync(TestContext.Current.CancellationToken);
        events.Should().BeEmpty();
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_NotThrow_When_ShipmentReference_Has_WhitespacePadding()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();

        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-44b",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-044b",
            Status = ShipmentStatus.Packed
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var result = await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "  dhl-ship-044b  ",
            CarrierEventKey = "shipment.in_transit",
            OccurredAtUtc = DateTime.UtcNow,
            ProviderStatus = "InTransit"
        }, TestContext.Current.CancellationToken);

        result.Status.Should().Be(ShipmentStatus.Shipped);
        result.LastCarrierEventKey.Should().Be("shipment.in_transit");

        var shipment = await db.Set<Shipment>()
            .SingleAsync(x => x.Id == shipmentId, TestContext.Current.CancellationToken);
        shipment.Status.Should().Be(ShipmentStatus.Shipped);
        shipment.LastCarrierEventKey.Should().Be("shipment.in_transit");

        var events = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync(TestContext.Current.CancellationToken);
        events.Should().HaveCount(1);
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_Throw_When_CarrierEventKey_Is_Whitespace()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();

        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-INVALID",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-404",
            Status = ShipmentStatus.Packed,
            LastCarrierEventKey = "shipment.ready_for_pickup"
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var act = async () => await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-404",
            CarrierEventKey = "   ",
            OccurredAtUtc = DateTime.UtcNow
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();

        var shipment = await db.Set<Shipment>()
            .SingleAsync(x => x.Id == shipmentId, TestContext.Current.CancellationToken);
        shipment.Status.Should().Be(ShipmentStatus.Packed);
        shipment.LastCarrierEventKey.Should().Be("shipment.ready_for_pickup");

        var events = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync(TestContext.Current.CancellationToken);
        events.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ApplyShipmentCarrierEventHandler_Should_Throw_When_Carrier_Is_Null_Or_Whitespace(string? carrier)
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();

        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-INVALID-2",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-405",
            Status = ShipmentStatus.Packed,
            LastCarrierEventKey = "shipment.ready_for_pickup"
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var act = async () => await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = carrier!,
            ProviderShipmentReference = "dhl-ship-405",
            CarrierEventKey = "shipment.exception",
            OccurredAtUtc = DateTime.UtcNow
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();

        var shipment = await db.Set<Shipment>()
            .SingleAsync(x => x.Id == shipmentId, TestContext.Current.CancellationToken);
        shipment.Status.Should().Be(ShipmentStatus.Packed);
        shipment.LastCarrierEventKey.Should().Be("shipment.ready_for_pickup");

        var events = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync(TestContext.Current.CancellationToken);
        events.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ApplyShipmentCarrierEventHandler_Should_Throw_When_ProviderShipmentReference_Is_Null_Or_Whitespace(string? providerShipmentReference)
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();

        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-INVALID-3",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-406",
            Status = ShipmentStatus.Packed,
            LastCarrierEventKey = "shipment.ready_for_pickup"
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var act = async () => await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = providerShipmentReference!,
            CarrierEventKey = "shipment.exception",
            OccurredAtUtc = DateTime.UtcNow
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();

        var shipment = await db.Set<Shipment>()
            .SingleAsync(x => x.Id == shipmentId, TestContext.Current.CancellationToken);
        shipment.Status.Should().Be(ShipmentStatus.Packed);
        shipment.LastCarrierEventKey.Should().Be("shipment.ready_for_pickup");

        var events = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync(TestContext.Current.CancellationToken);
        events.Should().BeEmpty();
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_Throw_When_OccurredAtUtc_Is_Default()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();

        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-INVALID-4",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-407",
            Status = ShipmentStatus.Packed,
            LastCarrierEventKey = "shipment.ready_for_pickup"
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var act = async () => await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-407",
            CarrierEventKey = "shipment.exception",
            OccurredAtUtc = default
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();

        var shipment = await db.Set<Shipment>()
            .SingleAsync(x => x.Id == shipmentId, TestContext.Current.CancellationToken);
        shipment.Status.Should().Be(ShipmentStatus.Packed);
        shipment.LastCarrierEventKey.Should().Be("shipment.ready_for_pickup");

        var events = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync(TestContext.Current.CancellationToken);
        events.Should().BeEmpty();
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_Throw_When_CarrierEventKey_Too_Long()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();

        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-INVALID-5",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-408",
            Status = ShipmentStatus.Packed,
            LastCarrierEventKey = "shipment.ready_for_pickup"
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var act = async () => await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-408",
            CarrierEventKey = new string('K', 129),
            OccurredAtUtc = DateTime.UtcNow
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();

        var shipment = await db.Set<Shipment>()
            .SingleAsync(x => x.Id == shipmentId, TestContext.Current.CancellationToken);
        shipment.Status.Should().Be(ShipmentStatus.Packed);
        shipment.LastCarrierEventKey.Should().Be("shipment.ready_for_pickup");

        var events = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync(TestContext.Current.CancellationToken);
        events.Should().BeEmpty();
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_Throw_When_TrackingNumber_Too_Long()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();

        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-INVALID-6",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-409",
            Status = ShipmentStatus.Packed,
            LastCarrierEventKey = "shipment.ready_for_pickup"
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var act = async () => await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-409",
            CarrierEventKey = "shipment.delivered",
            TrackingNumber = new string('T', 129),
            OccurredAtUtc = DateTime.UtcNow
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();

        var shipment = await db.Set<Shipment>()
            .SingleAsync(x => x.Id == shipmentId, TestContext.Current.CancellationToken);
        shipment.Status.Should().Be(ShipmentStatus.Packed);
        shipment.LastCarrierEventKey.Should().Be("shipment.ready_for_pickup");

        var events = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync(TestContext.Current.CancellationToken);
        events.Should().BeEmpty();
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_Throw_When_LabelUrl_Too_Long()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();

        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-INVALID-7",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-410",
            Status = ShipmentStatus.Packed,
            LastCarrierEventKey = "shipment.ready_for_pickup"
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var act = async () => await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-410",
            CarrierEventKey = "shipment.delivered",
            LabelUrl = new string('L', 2049),
            OccurredAtUtc = DateTime.UtcNow
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();

        var shipment = await db.Set<Shipment>()
            .SingleAsync(x => x.Id == shipmentId, TestContext.Current.CancellationToken);
        shipment.Status.Should().Be(ShipmentStatus.Packed);
        shipment.LastCarrierEventKey.Should().Be("shipment.ready_for_pickup");

        var events = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync(TestContext.Current.CancellationToken);
        events.Should().BeEmpty();
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_Throw_When_ProviderStatus_Too_Long()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();

        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-INVALID-8",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-411",
            Status = ShipmentStatus.Packed,
            LastCarrierEventKey = "shipment.ready_for_pickup"
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var act = async () => await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-411",
            CarrierEventKey = "shipment.delivered",
            ProviderStatus = new string('P', 65),
            OccurredAtUtc = DateTime.UtcNow
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();

        var shipment = await db.Set<Shipment>()
            .SingleAsync(x => x.Id == shipmentId, TestContext.Current.CancellationToken);
        shipment.Status.Should().Be(ShipmentStatus.Packed);
        shipment.LastCarrierEventKey.Should().Be("shipment.ready_for_pickup");

        var events = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync(TestContext.Current.CancellationToken);
        events.Should().BeEmpty();
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_Throw_When_ExceptionCode_Too_Long()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();

        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-INVALID-9",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-412",
            Status = ShipmentStatus.Packed,
            LastCarrierEventKey = "shipment.ready_for_pickup"
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var act = async () => await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-412",
            CarrierEventKey = "shipment.delivered",
            ExceptionCode = new string('E', 129),
            OccurredAtUtc = DateTime.UtcNow
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();

        var shipment = await db.Set<Shipment>()
            .SingleAsync(x => x.Id == shipmentId, TestContext.Current.CancellationToken);
        shipment.Status.Should().Be(ShipmentStatus.Packed);
        shipment.LastCarrierEventKey.Should().Be("shipment.ready_for_pickup");

        var events = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync(TestContext.Current.CancellationToken);
        events.Should().BeEmpty();
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_Throw_When_ExceptionMessage_Too_Long()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();

        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-INVALID-10",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-413",
            Status = ShipmentStatus.Packed,
            LastCarrierEventKey = "shipment.ready_for_pickup"
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var act = async () => await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-413",
            CarrierEventKey = "shipment.delivered",
            ExceptionMessage = new string('M', 513),
            OccurredAtUtc = DateTime.UtcNow
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();

        var shipment = await db.Set<Shipment>()
            .SingleAsync(x => x.Id == shipmentId, TestContext.Current.CancellationToken);
        shipment.Status.Should().Be(ShipmentStatus.Packed);
        shipment.LastCarrierEventKey.Should().Be("shipment.ready_for_pickup");

        var events = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync(TestContext.Current.CancellationToken);
        events.Should().BeEmpty();
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_Throw_When_Service_Too_Long()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();

        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-INVALID-11",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-414",
            Status = ShipmentStatus.Packed,
            LastCarrierEventKey = "shipment.ready_for_pickup"
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var act = async () => await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-414",
            CarrierEventKey = "shipment.delivered",
            Service = new string('S', 65),
            OccurredAtUtc = DateTime.UtcNow
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();

        var shipment = await db.Set<Shipment>()
            .SingleAsync(x => x.Id == shipmentId, TestContext.Current.CancellationToken);
        shipment.Status.Should().Be(ShipmentStatus.Packed);
        shipment.LastCarrierEventKey.Should().Be("shipment.ready_for_pickup");

        var events = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync(TestContext.Current.CancellationToken);
        events.Should().BeEmpty();
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_Throw_When_ProviderShipmentReference_Too_Long()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();

        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-INVALID-12",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-415",
            Status = ShipmentStatus.Packed,
            LastCarrierEventKey = "shipment.ready_for_pickup"
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var act = async () => await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = new string('R', 129),
            CarrierEventKey = "shipment.delivered",
            OccurredAtUtc = DateTime.UtcNow
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();

        var shipment = await db.Set<Shipment>()
            .SingleAsync(x => x.Id == shipmentId, TestContext.Current.CancellationToken);
        shipment.Status.Should().Be(ShipmentStatus.Packed);
        shipment.LastCarrierEventKey.Should().Be("shipment.ready_for_pickup");

        var events = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync(TestContext.Current.CancellationToken);
        events.Should().BeEmpty();
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_Throw_When_Carrier_Too_Long()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();

        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-INVALID-13",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-416",
            Status = ShipmentStatus.Packed,
            LastCarrierEventKey = "shipment.ready_for_pickup"
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var act = async () => await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = new string('D', 65),
            ProviderShipmentReference = "dhl-ship-416",
            CarrierEventKey = "shipment.delivered",
            OccurredAtUtc = DateTime.UtcNow
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();

        var shipment = await db.Set<Shipment>()
            .SingleAsync(x => x.Id == shipmentId, TestContext.Current.CancellationToken);
        shipment.Status.Should().Be(ShipmentStatus.Packed);
        shipment.LastCarrierEventKey.Should().Be("shipment.ready_for_pickup");

        var events = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync(TestContext.Current.CancellationToken);
        events.Should().BeEmpty();
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_Throw_When_Multiple_Validation_Rules_Fail()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();

        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-INVALID-14",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-417",
            Status = ShipmentStatus.Packed,
            LastCarrierEventKey = "shipment.ready_for_pickup"
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var act = async () => await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "",
            ProviderShipmentReference = "   ",
            CarrierEventKey = new string('K', 129),
            OccurredAtUtc = default
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();

        var shipment = await db.Set<Shipment>()
            .SingleAsync(x => x.Id == shipmentId, TestContext.Current.CancellationToken);
        shipment.Status.Should().Be(ShipmentStatus.Packed);
        shipment.LastCarrierEventKey.Should().Be("shipment.ready_for_pickup");

        var events = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync(TestContext.Current.CancellationToken);
        events.Should().BeEmpty();
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_Pass_When_CarrierEventKey_Is_Max_Length()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();

        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var occurredAtUtc = DateTime.UtcNow.AddMinutes(-3);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-INVALID-15",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-418",
            Status = ShipmentStatus.Packed
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var result = await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-418",
            CarrierEventKey = new string('K', 128),
            OccurredAtUtc = occurredAtUtc
        }, TestContext.Current.CancellationToken);

        result.Status.Should().Be(ShipmentStatus.Packed);
        result.LastCarrierEventKey.Should().Be(new string('K', 128));

        var shipment = await db.Set<Shipment>()
            .SingleAsync(x => x.Id == shipmentId, TestContext.Current.CancellationToken);
        shipment.LastCarrierEventKey.Should().Be(new string('K', 128));
        shipment.Status.Should().Be(ShipmentStatus.Packed);

        var events = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync(TestContext.Current.CancellationToken);
        events.Should().HaveCount(1);
        events[0].CarrierEventKey.Should().Be(new string('K', 128));
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_Pass_When_Optional_Fields_Are_Max_Length()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();

        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var occurredAtUtc = DateTime.UtcNow.AddMinutes(-2);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-INVALID-16",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-419",
            Status = ShipmentStatus.Packed
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var result = await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-419",
            CarrierEventKey = "shipment.delivered",
            TrackingNumber = new string('T', 128),
            LabelUrl = new string('L', 2048),
            Service = new string('S', 64),
            ProviderStatus = new string('P', 64),
            ExceptionCode = new string('E', 128),
            ExceptionMessage = new string('M', 512),
            OccurredAtUtc = occurredAtUtc
        }, TestContext.Current.CancellationToken);

        result.Status.Should().Be(ShipmentStatus.Delivered);
        result.TrackingNumber.Should().Be(new string('T', 128));
        result.LabelUrl.Should().Be(new string('L', 2048));
        result.Service.Should().Be(new string('S', 64));
        result.LastCarrierEventKey.Should().Be("shipment.delivered");

        var shipment = await db.Set<Shipment>()
            .SingleAsync(x => x.Id == shipmentId, TestContext.Current.CancellationToken);
        shipment.Status.Should().Be(ShipmentStatus.Delivered);
        shipment.TrackingNumber.Should().Be(new string('T', 128));
        shipment.LabelUrl.Should().Be(new string('L', 2048));
        shipment.Service.Should().Be(new string('S', 64));

        var events = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync(TestContext.Current.CancellationToken);
        events.Should().HaveCount(1);
        events[0].CarrierEventKey.Should().Be("shipment.delivered");
        events[0].ProviderStatus.Should().Be(new string('P', 64));
        events[0].ExceptionCode.Should().Be(new string('E', 128));
        events[0].ExceptionMessage.Should().Be(new string('M', 512));
        events[0].TrackingNumber.Should().Be(new string('T', 128));
        events[0].LabelUrl.Should().Be(new string('L', 2048));
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_Throw_When_Multiple_Optional_Fields_Are_Too_Long()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();

        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var occurredAtUtc = DateTime.UtcNow.AddMinutes(-2);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-INVALID-17",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-420",
            Status = ShipmentStatus.Packed,
            LastCarrierEventKey = "shipment.ready_for_pickup",
            TrackingNumber = "TRACK-INITIAL",
            LabelUrl = "https://labels.example.com/initial.pdf"
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var act = async () => await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-420",
            CarrierEventKey = "shipment.delivered",
            OccurredAtUtc = occurredAtUtc,
            TrackingNumber = new string('T', 129),
            LabelUrl = new string('L', 2049),
            Service = new string('S', 65),
            ProviderStatus = new string('P', 65),
            ExceptionCode = new string('E', 129),
            ExceptionMessage = new string('M', 513)
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();

        var shipment = await db.Set<Shipment>()
            .SingleAsync(x => x.Id == shipmentId, TestContext.Current.CancellationToken);
        shipment.Status.Should().Be(ShipmentStatus.Packed);
        shipment.LastCarrierEventKey.Should().Be("shipment.ready_for_pickup");
        shipment.TrackingNumber.Should().Be("TRACK-INITIAL");
        shipment.LabelUrl.Should().Be("https://labels.example.com/initial.pdf");

        var events = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync(TestContext.Current.CancellationToken);
        events.Should().BeEmpty();
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_Trim_Inputs_And_ResolveStatus_ByCarrierEventKey_WhenProviderStatusIsBlank()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var occurredAtUtc = DateTime.UtcNow.AddMinutes(-7);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-4",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-004",
            Status = ShipmentStatus.Packed
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var result = await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-004",
            TrackingNumber = "  TRACK-004  ",
            LabelUrl = "  https://labels.example.com/TRACK-004.pdf  ",
            Service = "  Express  ",
            CarrierEventKey = "   shipment.delivered   ",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "   ",
            ExceptionCode = "   ",
            ExceptionMessage = "   "
        }, TestContext.Current.CancellationToken);

        result.Status.Should().Be(ShipmentStatus.Delivered);
        result.TrackingNumber.Should().Be("TRACK-004");
        result.LabelUrl.Should().Be("https://labels.example.com/TRACK-004.pdf");
        result.Service.Should().Be("Express");
        result.LastCarrierEventKey.Should().Be("shipment.delivered");
        result.ShippedAtUtc.Should().Be(occurredAtUtc);
        result.DeliveredAtUtc.Should().Be(occurredAtUtc);

        var shipment = await db.Set<Shipment>()
            .SingleAsync(x => x.Id == shipmentId, TestContext.Current.CancellationToken);
        shipment.Carrier.Should().Be("DHL");
        shipment.ProviderShipmentReference.Should().Be("dhl-ship-004");
        shipment.TrackingNumber.Should().Be("TRACK-004");
        shipment.LabelUrl.Should().Be("https://labels.example.com/TRACK-004.pdf");
        shipment.Service.Should().Be("Express");
        shipment.Status.Should().Be(ShipmentStatus.Delivered);

        var carrierEvent = await db.Set<ShipmentCarrierEvent>()
            .SingleAsync(TestContext.Current.CancellationToken);
        carrierEvent.CarrierEventKey.Should().Be("shipment.delivered");
        carrierEvent.ProviderStatus.Should().BeNull();
        carrierEvent.ExceptionCode.Should().BeNull();
        carrierEvent.ExceptionMessage.Should().BeNull();
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_ResolveShipment_ByTrimmedCarrierAndReference()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var occurredAtUtc = DateTime.UtcNow.AddMinutes(-5);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-27",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-027",
            Status = ShipmentStatus.Packed
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var result = await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "  DHL  ",
            ProviderShipmentReference = "  dhl-ship-027  ",
            CarrierEventKey = "shipment.manifested",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "Packed"
        }, TestContext.Current.CancellationToken);

        result.Status.Should().Be(ShipmentStatus.Packed);
        result.Carrier.Should().Be("DHL");
        result.ProviderShipmentReference.Should().Be("dhl-ship-027");
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_UpdateExistingCarrierTimelineExceptionFields_WhenMissingInitially()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var occurredAtUtc = DateTime.UtcNow.AddMinutes(-10);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-5",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-005",
            Status = ShipmentStatus.Packed
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var baseDto = new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-005",
            CarrierEventKey = "shipment.in_transit",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "InTransit"
        };

        await handler.HandleAsync(baseDto, TestContext.Current.CancellationToken);

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-005",
            CarrierEventKey = "shipment.in_transit",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "InTransit",
            ExceptionCode = "TRANSIENT",
            ExceptionMessage = "Retrying"
        }, TestContext.Current.CancellationToken);

        var carrierEvents = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync(TestContext.Current.CancellationToken);

        carrierEvents.Should().HaveCount(1);
        carrierEvents[0].ExceptionCode.Should().Be("TRANSIENT");
        carrierEvents[0].ExceptionMessage.Should().Be("Retrying");
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_NotOverwriteExistingExceptionFields()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var occurredAtUtc = DateTime.UtcNow.AddMinutes(-11);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-16",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-016",
            Status = ShipmentStatus.Packed
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-016",
            CarrierEventKey = "shipment.exception",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "Exception",
            ExceptionCode = "INITIAL",
            ExceptionMessage = "Initial"
        }, TestContext.Current.CancellationToken);

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-016",
            CarrierEventKey = "shipment.exception",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "Exception",
            ExceptionCode = "UPDATED",
            ExceptionMessage = "Updated"
        }, TestContext.Current.CancellationToken);

        var carrierEvents = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync(TestContext.Current.CancellationToken);

        carrierEvents.Should().HaveCount(1);
        carrierEvents[0].ExceptionCode.Should().Be("INITIAL");
        carrierEvents[0].ExceptionMessage.Should().Be("Initial");
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_DedupeCarrierTimeline_WhenProviderStatusWhitespaceVariants()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var occurredAtUtc = DateTime.UtcNow.AddMinutes(-9);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-17",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-017",
            Status = ShipmentStatus.Packed
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-017",
            CarrierEventKey = "shipment.in_transit",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "   "
        }, TestContext.Current.CancellationToken);

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-017",
            CarrierEventKey = "shipment.in_transit",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "\t"
        }, TestContext.Current.CancellationToken);

        var carrierEvents = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync(TestContext.Current.CancellationToken);

        carrierEvents.Should().HaveCount(1);
        carrierEvents[0].ProviderStatus.Should().BeNull();
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_NotOverwriteExistingExceptionFields_WhenProviderStatus_Is_WhitespaceVariantForDuplicateFingerprint()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var occurredAtUtc = DateTime.UtcNow.AddMinutes(-8);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-18",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-018",
            Status = ShipmentStatus.Packed
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-018",
            CarrierEventKey = "shipment.in_transit",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "InTransit",
            ExceptionCode = "INITIAL",
            ExceptionMessage = "Initial message"
        }, TestContext.Current.CancellationToken);

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-018",
            CarrierEventKey = " shipment.in_transit ",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "\tInTransit\t",
            ExceptionCode = "UPDATED",
            ExceptionMessage = "Updated message"
        }, TestContext.Current.CancellationToken);

        var carrierEvents = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync(TestContext.Current.CancellationToken);

        carrierEvents.Should().HaveCount(1);
        carrierEvents[0].CarrierEventKey.Should().Be("shipment.in_transit");
        carrierEvents[0].ProviderStatus.Should().Be("InTransit");
        carrierEvents[0].ExceptionCode.Should().Be("INITIAL");
        carrierEvents[0].ExceptionMessage.Should().Be("Initial message");
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_CreateSecondCarrierTimeline_WhenOccurredAtUtcDiffers()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var firstTime = DateTime.UtcNow.AddMinutes(-20);
        var secondTime = firstTime.AddMinutes(1);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-10",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-010",
            Status = ShipmentStatus.Packed
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-010",
            CarrierEventKey = "shipment.in_transit",
            OccurredAtUtc = firstTime,
            ProviderStatus = "InTransit"
        }, TestContext.Current.CancellationToken);

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-010",
            CarrierEventKey = "shipment.in_transit",
            OccurredAtUtc = secondTime,
            ProviderStatus = "InTransit"
        }, TestContext.Current.CancellationToken);

        var carrierEvents = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .OrderBy(x => x.OccurredAtUtc)
            .ToListAsync(TestContext.Current.CancellationToken);

        carrierEvents.Should().HaveCount(2);
        carrierEvents[0].OccurredAtUtc.Should().Be(firstTime);
        carrierEvents[1].OccurredAtUtc.Should().Be(secondTime);
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_CreateDistinctCarrierTimeline_WhenProviderStatusDiffers()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var occurredAtUtc = DateTime.UtcNow.AddMinutes(-12);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-11",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-011",
            Status = ShipmentStatus.Packed
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-011",
            CarrierEventKey = "shipment.update",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "InTransit"
        }, TestContext.Current.CancellationToken);

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-011",
            CarrierEventKey = "shipment.update",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "Delivered"
        }, TestContext.Current.CancellationToken);

        var carrierEvents = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .OrderBy(x => x.ProviderStatus)
            .ToListAsync(TestContext.Current.CancellationToken);

        carrierEvents.Should().HaveCount(2);
        carrierEvents.Select(x => x.ProviderStatus).Should().Contain("InTransit");
        carrierEvents.Select(x => x.ProviderStatus).Should().Contain("Delivered");
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_NotCreateSecondTimeline_ForSameCallbackFingerprint_WithTrimmedCarrierEventKey()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var occurredAtUtc = DateTime.UtcNow.AddMinutes(-8);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-12",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-012",
            Status = ShipmentStatus.Packed
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-012",
            CarrierEventKey = "  shipment.in_transit  ",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "InTransit"
        }, TestContext.Current.CancellationToken);

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-012",
            CarrierEventKey = "shipment.in_transit",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "InTransit"
        }, TestContext.Current.CancellationToken);

        var carrierEvents = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync(TestContext.Current.CancellationToken);

        carrierEvents.Should().HaveCount(1);
        carrierEvents[0].CarrierEventKey.Should().Be("shipment.in_transit");
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_AddCarrierTimelineOnlyOnceAndPreserveExistingExceptionMessage()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var occurredAtUtc = DateTime.UtcNow.AddMinutes(-13);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-26",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-026",
            Status = ShipmentStatus.Packed
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var first = new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-026",
            CarrierEventKey = "shipment.in_transit",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "InTransit",
            ExceptionMessage = "first message"
        };

        var second = new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-026",
            CarrierEventKey = "shipment.in_transit",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "InTransit",
            ExceptionMessage = "second message"
        };

        await handler.HandleAsync(first, TestContext.Current.CancellationToken);
        await handler.HandleAsync(second, TestContext.Current.CancellationToken);

        var events = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync(TestContext.Current.CancellationToken);

        events.Should().HaveCount(1);
        events[0].ProviderStatus.Should().Be("InTransit");
        events[0].ExceptionMessage.Should().Be("first message");
        events[0].CarrierEventKey.Should().Be("shipment.in_transit");
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_CreateSecondTimeline_WhenWhitespaceStatusBecomesNonWhitespace()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var occurredAtUtc = DateTime.UtcNow.AddMinutes(-8);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-13",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-013",
            Status = ShipmentStatus.Packed
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-013",
            CarrierEventKey = "shipment.update",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "   "
        }, TestContext.Current.CancellationToken);

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-013",
            CarrierEventKey = "shipment.update",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "InTransit"
        }, TestContext.Current.CancellationToken);

        var carrierEvents = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .OrderBy(x => x.ProviderStatus)
            .ToListAsync(TestContext.Current.CancellationToken);

        carrierEvents.Should().HaveCount(2);
        carrierEvents.Select(x => x.ProviderStatus).Any(x => x == null).Should().BeTrue();
        carrierEvents.Select(x => x.ProviderStatus).Should().Contain("InTransit");
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_NotChangeShipmentStatus_WhenCarrierEventKeyIsUnknown()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var currentStatus = ShipmentStatus.Packed;
        var occurredAtUtc = DateTime.UtcNow.AddMinutes(-4);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-14",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-014",
            Status = currentStatus
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var result = await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-014",
            CarrierEventKey = "shipment.handling_started",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "   "
        }, TestContext.Current.CancellationToken);

        result.Status.Should().Be(currentStatus);
        result.LastCarrierEventKey.Should().Be("shipment.handling_started");

        var shipment = await db.Set<Shipment>()
            .SingleAsync(x => x.Id == shipmentId, TestContext.Current.CancellationToken);
        shipment.Status.Should().Be(currentStatus);
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_NotChangeShipmentStatus_WhenCarrierEventKeyIsUnknown_AndProviderStatusIsUnknown()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var shippedAtUtc = DateTime.UtcNow.AddHours(-6);
        var deliveredAtUtc = DateTime.UtcNow.AddHours(-2);
        var occurredAtUtc = DateTime.UtcNow;

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-35",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-035",
            Status = ShipmentStatus.Delivered,
            ShippedAtUtc = shippedAtUtc,
            DeliveredAtUtc = deliveredAtUtc
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var result = await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-035",
            CarrierEventKey = "shipment.exception",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "unknown_provider_status"
        }, TestContext.Current.CancellationToken);

        result.Status.Should().Be(ShipmentStatus.Delivered);
        result.ShippedAtUtc.Should().Be(shippedAtUtc);
        result.DeliveredAtUtc.Should().Be(deliveredAtUtc);
        result.LastCarrierEventKey.Should().Be("shipment.exception");
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_ResolveShipped_FromOutForDelivery_WhenProviderStatusBlank()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var occurredAtUtc = DateTime.UtcNow.AddMinutes(-3);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-15",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-015",
            Status = ShipmentStatus.Packed
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var result = await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-015",
            CarrierEventKey = "shipment.shipment.picked_up",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "   "
        }, TestContext.Current.CancellationToken);

        result.Status.Should().Be(ShipmentStatus.Shipped);
        result.ShippedAtUtc.Should().Be(occurredAtUtc);
        result.LastCarrierEventKey.Should().Be("shipment.shipment.picked_up");
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_ResolveDelivered_WhenCarrierEventKeyShippedButProviderStatusDelivered()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var shippedAtUtc = DateTime.UtcNow.AddHours(-3);
        var occurredAtUtc = DateTime.UtcNow.AddMinutes(-10);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-15a",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-015a",
            Status = ShipmentStatus.Packed,
            ShippedAtUtc = shippedAtUtc
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var result = await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-015a",
            CarrierEventKey = "shipment.out_for_delivery",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = " Delivered "
        }, TestContext.Current.CancellationToken);

        result.Status.Should().Be(ShipmentStatus.Delivered);
        result.DeliveredAtUtc.Should().Be(occurredAtUtc);
        result.LastCarrierEventKey.Should().Be("shipment.out_for_delivery");
        result.ShippedAtUtc.Should().Be(shippedAtUtc);
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_NotOverwrite_ExistingOptionalFields_WhenProvidedWhitespaceOnly()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var occurredAtUtc = DateTime.UtcNow.AddMinutes(-20);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-6",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Express",
            ProviderShipmentReference = "dhl-ship-006",
            TrackingNumber = "TRACK-006",
            LabelUrl = "https://labels.example.com/initial.pdf",
            Status = ShipmentStatus.Packed
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var result = await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-006",
            TrackingNumber = "   ",
            LabelUrl = "\t",
            Service = "   ",
            CarrierEventKey = "shipment.in_transit",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "InTransit"
        }, TestContext.Current.CancellationToken);

        result.TrackingNumber.Should().Be("TRACK-006");
        result.LabelUrl.Should().Be("https://labels.example.com/initial.pdf");
        result.Service.Should().Be("Express");
        result.Status.Should().Be(ShipmentStatus.Shipped);

        var shipment = await db.Set<Shipment>()
            .SingleAsync(x => x.Id == shipmentId, TestContext.Current.CancellationToken);
        shipment.TrackingNumber.Should().Be("TRACK-006");
        shipment.LabelUrl.Should().Be("https://labels.example.com/initial.pdf");
        shipment.Service.Should().Be("Express");
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_Clear_DeliveredAt_WhenReturningShipment()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var shippedAtUtc = DateTime.UtcNow.AddHours(-10);
        var deliveredAtUtc = DateTime.UtcNow.AddHours(-2);
        var returnAtUtc = DateTime.UtcNow.AddMinutes(-1);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-7",
            Currency = "EUR",
            Status = OrderStatus.Delivered
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-007",
            Status = ShipmentStatus.Delivered,
            ShippedAtUtc = shippedAtUtc,
            DeliveredAtUtc = deliveredAtUtc
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var result = await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-007",
            CarrierEventKey = "shipment.returned",
            OccurredAtUtc = returnAtUtc,
            ProviderStatus = "ReturnedToSender"
        }, TestContext.Current.CancellationToken);

        result.Status.Should().Be(ShipmentStatus.Returned);
        result.ShippedAtUtc.Should().Be(shippedAtUtc);
        result.DeliveredAtUtc.Should().BeNull();

        var shipment = await db.Set<Shipment>()
            .SingleAsync(x => x.Id == shipmentId, TestContext.Current.CancellationToken);
        shipment.Status.Should().Be(ShipmentStatus.Returned);
        shipment.DeliveredAtUtc.Should().BeNull();
        shipment.ShippedAtUtc.Should().Be(shippedAtUtc);
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_NotDowngradeReturnedShipment_OnLowerPriorityCarrierEvent()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var returnedAtUtc = DateTime.UtcNow.AddHours(-4);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-8",
            Currency = "EUR",
            Status = OrderStatus.Delivered
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-008",
            Status = ShipmentStatus.Returned,
            ShippedAtUtc = returnedAtUtc
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var result = await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-008",
            CarrierEventKey = "shipment.in_transit",
            OccurredAtUtc = returnedAtUtc.AddHours(1),
            ProviderStatus = "InTransit"
        }, TestContext.Current.CancellationToken);

        result.Status.Should().Be(ShipmentStatus.Returned);
        result.ShippedAtUtc.Should().Be(returnedAtUtc);
        result.DeliveredAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_NotDowngradeReturnedShipment_WhenCarrierEventKeyInTransitAndProviderStatusIsWhitespace()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var returnedAtUtc = DateTime.UtcNow.AddHours(-4);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-8a",
            Currency = "EUR",
            Status = OrderStatus.Delivered
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-008a",
            Status = ShipmentStatus.Returned,
            ShippedAtUtc = returnedAtUtc
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var result = await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-008a",
            CarrierEventKey = "shipment.in_transit",
            OccurredAtUtc = returnedAtUtc.AddHours(1),
            ProviderStatus = "   "
        }, TestContext.Current.CancellationToken);

        result.Status.Should().Be(ShipmentStatus.Returned);
        result.ShippedAtUtc.Should().Be(returnedAtUtc);
        result.DeliveredAtUtc.Should().BeNull();
        result.LastCarrierEventKey.Should().Be("shipment.in_transit");

        var events = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync(TestContext.Current.CancellationToken);
        events.Should().HaveCount(1);
        events[0].CarrierEventKey.Should().Be("shipment.in_transit");
        events[0].ProviderStatus.Should().BeNull();
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_AppendCarrierTimelineAfterReturned_WhenCarrierEventKeyChangesAndProviderStatusIsWhitespace()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var returnedAtUtc = DateTime.UtcNow.AddHours(-4);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-8b",
            Currency = "EUR",
            Status = OrderStatus.Delivered
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-008b",
            Status = ShipmentStatus.Returned,
            ShippedAtUtc = returnedAtUtc
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-008b",
            CarrierEventKey = "shipment.ready_for_pickup",
            OccurredAtUtc = returnedAtUtc.AddMinutes(-30),
            ProviderStatus = "   "
        }, TestContext.Current.CancellationToken);

        var result = await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-008b",
            CarrierEventKey = "shipment.returned",
            OccurredAtUtc = returnedAtUtc.AddMinutes(-20),
            ProviderStatus = "\t"
        }, TestContext.Current.CancellationToken);

        result.Status.Should().Be(ShipmentStatus.Returned);
        result.ShippedAtUtc.Should().Be(returnedAtUtc);
        result.DeliveredAtUtc.Should().BeNull();
        result.LastCarrierEventKey.Should().Be("shipment.returned");

        var shipment = await db.Set<Shipment>()
            .SingleAsync(x => x.Id == shipmentId, TestContext.Current.CancellationToken);
        shipment.Status.Should().Be(ShipmentStatus.Returned);
        shipment.DeliveredAtUtc.Should().BeNull();

        var events = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync(TestContext.Current.CancellationToken);
        events.Should().HaveCount(2);
        events.Should().Contain(x => x.CarrierEventKey == "shipment.ready_for_pickup" && x.ProviderStatus == null);
        events.Should().Contain(x => x.CarrierEventKey == "shipment.returned" && x.ProviderStatus == null);
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_NotDowngradeReturnedShipment_WhenCarrierEventIsUnknownAndProviderStatusIsDelivered()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var returnedAtUtc = DateTime.UtcNow.AddHours(-10);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-24",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-024",
            Status = ShipmentStatus.Returned,
            ShippedAtUtc = returnedAtUtc
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var result = await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-024",
            CarrierEventKey = "shipment.unknown_return",
            OccurredAtUtc = returnedAtUtc.AddHours(1),
            ProviderStatus = "  DELIVERED "
        }, TestContext.Current.CancellationToken);

        result.Status.Should().Be(ShipmentStatus.Returned);
        result.ShippedAtUtc.Should().Be(returnedAtUtc);
        result.DeliveredAtUtc.Should().BeNull();
        result.LastCarrierEventKey.Should().Be("shipment.unknown_return");
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_KeepReturned_WhenProviderStatusContainsDeliveredAndReturned()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var returnedAtUtc = DateTime.UtcNow.AddHours(-9);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-25",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-025",
            Status = ShipmentStatus.Returned,
            ShippedAtUtc = returnedAtUtc
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var result = await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-025",
            CarrierEventKey = "shipment.status_update",
            OccurredAtUtc = returnedAtUtc.AddHours(1),
            ProviderStatus = "Returned and Delivered"
        }, TestContext.Current.CancellationToken);

        result.Status.Should().Be(ShipmentStatus.Returned);
        result.ShippedAtUtc.Should().Be(returnedAtUtc);
        result.DeliveredAtUtc.Should().BeNull();

        var shipment = await db.Set<Shipment>()
            .SingleAsync(x => x.Id == shipmentId, TestContext.Current.CancellationToken);
        shipment.Status.Should().Be(ShipmentStatus.Returned);
        shipment.DeliveredAtUtc.Should().BeNull();
        shipment.LastCarrierEventKey.Should().Be("shipment.status_update");
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_ResolveReturnedStatus_FromProviderStatus_WithoutCarrierEventKeyword()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var occurredAtUtc = DateTime.UtcNow.AddMinutes(-6);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-9",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-009",
            Status = ShipmentStatus.Packed
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var result = await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-009",
            CarrierEventKey = "carrier.event",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "ReturnedToSender"
        }, TestContext.Current.CancellationToken);

        result.Status.Should().Be(ShipmentStatus.Returned);
        result.LastCarrierEventKey.Should().Be("carrier.event");
        result.ShippedAtUtc.Should().Be(occurredAtUtc);
        result.DeliveredAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_AdvanceShipment_ToReturned_WhenStatusContainsReturnAndTransit()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var shippedAtUtc = DateTime.UtcNow.AddHours(-4);
        var occurredAtUtc = DateTime.UtcNow.AddMinutes(-8);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-9a",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-009a",
            Status = ShipmentStatus.Shipped,
            ShippedAtUtc = shippedAtUtc
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var result = await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-009a",
            CarrierEventKey = "shipment.in_transit",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "In Transit ReturnedToSender"
        }, TestContext.Current.CancellationToken);

        result.Status.Should().Be(ShipmentStatus.Returned);
        result.ShippedAtUtc.Should().Be(shippedAtUtc);
        result.DeliveredAtUtc.Should().BeNull();
        result.LastCarrierEventKey.Should().Be("shipment.in_transit");

        var shipment = await db.Set<Shipment>()
            .SingleAsync(x => x.Id == shipmentId, TestContext.Current.CancellationToken);
        shipment.Status.Should().Be(ShipmentStatus.Returned);
        shipment.DeliveredAtUtc.Should().BeNull();
        shipment.ShippedAtUtc.Should().Be(shippedAtUtc);
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_AdvanceShipment_ToReturned_FromUnknownCarrierEventKey()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var shippedAtUtc = DateTime.UtcNow.AddHours(-4);
        var occurredAtUtc = DateTime.UtcNow.AddMinutes(-10);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-34",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-034",
            Status = ShipmentStatus.Shipped,
            ShippedAtUtc = shippedAtUtc
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var result = await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-034",
            CarrierEventKey = "carrier.unexpected",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "ReturnedToSender"
        }, TestContext.Current.CancellationToken);

        result.Status.Should().Be(ShipmentStatus.Returned);
        result.ShippedAtUtc.Should().Be(shippedAtUtc);
        result.DeliveredAtUtc.Should().BeNull();
        result.LastCarrierEventKey.Should().Be("carrier.unexpected");
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_NotDowngradeDeliveredShipment_WhenCarrierEventMapsToPacked()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var shippedAtUtc = DateTime.UtcNow.AddHours(-5);
        var deliveredAtUtc = DateTime.UtcNow.AddHours(-3);
        var occurredAtUtc = DateTime.UtcNow.AddHours(-1);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-18",
            Currency = "EUR",
            Status = OrderStatus.Delivered
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-018",
            Status = ShipmentStatus.Delivered,
            ShippedAtUtc = shippedAtUtc,
            DeliveredAtUtc = deliveredAtUtc
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var result = await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-018",
            CarrierEventKey = "shipment.label_created",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "   "
        }, TestContext.Current.CancellationToken);

        result.Status.Should().Be(ShipmentStatus.Delivered);
        result.ShippedAtUtc.Should().Be(shippedAtUtc);
        result.DeliveredAtUtc.Should().Be(deliveredAtUtc);
        result.LastCarrierEventKey.Should().Be("shipment.label_created");
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_ResolveShipped_WhenCarrierEventIsHandoff()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var occurredAtUtc = DateTime.UtcNow.AddMinutes(-6);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-19",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-019",
            Status = ShipmentStatus.Packed
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var result = await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-019",
            CarrierEventKey = "   handoff   ",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "   "
        }, TestContext.Current.CancellationToken);

        result.Status.Should().Be(ShipmentStatus.Shipped);
        result.ShippedAtUtc.Should().Be(occurredAtUtc);
        result.LastCarrierEventKey.Should().Be("handoff");
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_AdvanceShipment_ToPacked_WhenCarrierEventKeyMapsToPacked()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var occurredAtUtc = DateTime.UtcNow.AddMinutes(-10);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-20",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-020",
            Status = ShipmentStatus.Pending
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var result = await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-020",
            CarrierEventKey = "shipment.manifested",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "Packed"
        }, TestContext.Current.CancellationToken);

        result.Status.Should().Be(ShipmentStatus.Packed);
        result.ShippedAtUtc.Should().BeNull();
        result.DeliveredAtUtc.Should().BeNull();
        result.LastCarrierEventKey.Should().Be("shipment.manifested");
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_AdvanceShipment_ToPacked_WhenCarrierEventKeyIsPacked()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var occurredAtUtc = DateTime.UtcNow.AddMinutes(-12);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-28",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-028",
            Status = ShipmentStatus.Pending
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var result = await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-028",
            CarrierEventKey = "shipment.packed",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "Queued"
        }, TestContext.Current.CancellationToken);

        result.Status.Should().Be(ShipmentStatus.Packed);
        result.ShippedAtUtc.Should().BeNull();
        result.DeliveredAtUtc.Should().BeNull();
        result.LastCarrierEventKey.Should().Be("shipment.packed");
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_NotOverwriteExistingShippedAtUtc_ForShippedShipment()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var originalShippedAtUtc = DateTime.UtcNow.AddHours(-8);
        var occurredAtUtc = DateTime.UtcNow.AddHours(-1);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-30",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-030",
            Status = ShipmentStatus.Packed,
            ShippedAtUtc = originalShippedAtUtc
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var result = await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-030",
            CarrierEventKey = "shipment.in_transit",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "InTransit"
        }, TestContext.Current.CancellationToken);

        result.Status.Should().Be(ShipmentStatus.Shipped);
        result.ShippedAtUtc.Should().Be(originalShippedAtUtc);
        result.DeliveredAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_NotDowngradeShipped_WhenPackedEventArrivesLate()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var shippedAtUtc = DateTime.UtcNow.AddHours(-4);
        var occurredAtUtc = DateTime.UtcNow;

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-29",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-029",
            Status = ShipmentStatus.Shipped,
            ShippedAtUtc = shippedAtUtc
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var result = await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-029",
            CarrierEventKey = "shipment.label_created",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "LabelCreated"
        }, TestContext.Current.CancellationToken);

        result.Status.Should().Be(ShipmentStatus.Shipped);
        result.ShippedAtUtc.Should().Be(shippedAtUtc);
        result.DeliveredAtUtc.Should().BeNull();
        result.LastCarrierEventKey.Should().Be("shipment.label_created");
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_AdvanceShipment_ToPacked_WhenCarrierEventKeyIsReadyForPickup()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var occurredAtUtc = DateTime.UtcNow.AddMinutes(-12);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-24",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-024",
            Status = ShipmentStatus.Pending
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var result = await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-024",
            CarrierEventKey = "shipment.ready_for_pickup",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "Info"
        }, TestContext.Current.CancellationToken);

        result.Status.Should().Be(ShipmentStatus.Packed);
        result.ShippedAtUtc.Should().BeNull();
        result.DeliveredAtUtc.Should().BeNull();
        result.LastCarrierEventKey.Should().Be("shipment.ready_for_pickup");
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_NotDowngradeShipped_WhenReadyForPickupArrives()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var shippedAtUtc = DateTime.UtcNow.AddMinutes(-45);
        var occurredAtUtc = DateTime.UtcNow;

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-31",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-031",
            Status = ShipmentStatus.Shipped,
            ShippedAtUtc = shippedAtUtc
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var result = await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-031",
            CarrierEventKey = "shipment.ready_for_pickup",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "Queued"
        }, TestContext.Current.CancellationToken);

        result.Status.Should().Be(ShipmentStatus.Shipped);
        result.ShippedAtUtc.Should().Be(shippedAtUtc);
        result.DeliveredAtUtc.Should().BeNull();
        result.LastCarrierEventKey.Should().Be("shipment.ready_for_pickup");
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_NotOverwriteDeliveredAt_ForDeliveredShipment_WhenLowerPriorityEventArrives()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var shippedAtUtc = DateTime.UtcNow.AddHours(-10);
        var originalDeliveredAtUtc = DateTime.UtcNow.AddHours(-2);
        var occurredAtUtc = DateTime.UtcNow;

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-32",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-032",
            Status = ShipmentStatus.Delivered,
            ShippedAtUtc = shippedAtUtc,
            DeliveredAtUtc = originalDeliveredAtUtc
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var result = await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-032",
            CarrierEventKey = "shipment.label_created",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "Queued"
        }, TestContext.Current.CancellationToken);

        result.Status.Should().Be(ShipmentStatus.Delivered);
        result.ShippedAtUtc.Should().Be(shippedAtUtc);
        result.DeliveredAtUtc.Should().Be(originalDeliveredAtUtc);
        result.LastCarrierEventKey.Should().Be("shipment.label_created");
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_ResolveDeliveredFromProviderStatus_WhenCarrierEventKeyIsUnknown()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var occurredAtUtc = DateTime.UtcNow.AddMinutes(-6);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-33",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-033",
            Status = ShipmentStatus.Packed
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var result = await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-033",
            CarrierEventKey = "shipment.unknown_event",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = " Delivered "
        }, TestContext.Current.CancellationToken);

        result.Status.Should().Be(ShipmentStatus.Delivered);
        result.ShippedAtUtc.Should().Be(occurredAtUtc);
        result.DeliveredAtUtc.Should().Be(occurredAtUtc);
        result.LastCarrierEventKey.Should().Be("shipment.unknown_event");
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_ResolveStatus_FromMixedCaseProviderStatus_WhenCarrierEventKeyIsUnknown()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var occurredAtUtc = DateTime.UtcNow.AddMinutes(-4);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-22",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-022",
            Status = ShipmentStatus.Pending
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var result = await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-022",
            CarrierEventKey = " carrier.event ",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "  DeLiVeReD  "
        }, TestContext.Current.CancellationToken);

        result.Status.Should().Be(ShipmentStatus.Delivered);
        result.ShippedAtUtc.Should().Be(occurredAtUtc);
        result.DeliveredAtUtc.Should().Be(occurredAtUtc);
        result.LastCarrierEventKey.Should().Be("carrier.event");
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_ResolveReturnedStatus_FromMixedCaseProviderStatus_WhenCarrierEventKeyIsUnknown()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var occurredAtUtc = DateTime.UtcNow.AddMinutes(-3);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-23",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-023",
            Status = ShipmentStatus.Pending
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var result = await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-023",
            CarrierEventKey = "carrier.exception",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = " ReTuRnEdToSender "
        }, TestContext.Current.CancellationToken);

        result.Status.Should().Be(ShipmentStatus.Returned);
        result.ShippedAtUtc.Should().Be(occurredAtUtc);
        result.DeliveredAtUtc.Should().BeNull();
        result.LastCarrierEventKey.Should().Be("carrier.exception");
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_Trim_And_PreserveCase_ForCarrierEventKey_WhileResolvingStatus()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var occurredAtUtc = DateTime.UtcNow.AddMinutes(-2);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-21",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-021",
            Status = ShipmentStatus.Pending
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var result = await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-021",
            CarrierEventKey = "   SHIPMENT.IN_TRANSIT   ",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "   "
        }, TestContext.Current.CancellationToken);

        result.Status.Should().Be(ShipmentStatus.Shipped);
        result.LastCarrierEventKey.Should().Be("SHIPMENT.IN_TRANSIT");
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_Not_OverwriteDeliveredAt_ForDeliveredShipment_When_UnknownCarrierEventKey_AndDeliveredProviderStatus()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var shippedAtUtc = DateTime.UtcNow.AddHours(-12);
        var originalDeliveredAtUtc = DateTime.UtcNow.AddHours(-1);
        var occurredAtUtc = DateTime.UtcNow;

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-34",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-034",
            Status = ShipmentStatus.Delivered,
            ShippedAtUtc = shippedAtUtc,
            DeliveredAtUtc = originalDeliveredAtUtc
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var result = await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-034",
            CarrierEventKey = "shipment.custom_unknown",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = " DELIVERED "
        }, TestContext.Current.CancellationToken);

        result.Status.Should().Be(ShipmentStatus.Delivered);
        result.ShippedAtUtc.Should().Be(shippedAtUtc);
        result.DeliveredAtUtc.Should().Be(originalDeliveredAtUtc);
        result.LastCarrierEventKey.Should().Be("shipment.custom_unknown");
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_Pass_When_Carrier_And_ProviderShipmentReference_Are_Max_Length_And_Trimmed()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();

        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var maxCarrier = new string('C', 62);
        var maxProviderShipmentReference = new string('R', 126);
        var maxCarrierEventKey = "shipment.in_transit";

        await db.Set<Shipment>().AddAsync(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = maxCarrier,
            Service = "Parcel",
            ProviderShipmentReference = maxProviderShipmentReference,
            Status = ShipmentStatus.Packed
        }, TestContext.Current.CancellationToken);

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var result = await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = $" {maxCarrier} ",
            ProviderShipmentReference = $" {maxProviderShipmentReference} ",
            CarrierEventKey = $" {maxCarrierEventKey} ",
            OccurredAtUtc = DateTime.UtcNow
        }, TestContext.Current.CancellationToken);

        result.LastCarrierEventKey.Should().Be(maxCarrierEventKey);

        var persistedCarrierEvent = await db.Set<ShipmentCarrierEvent>()
            .SingleAsync(x => x.ShipmentId == shipmentId, TestContext.Current.CancellationToken);

        persistedCarrierEvent.Carrier.Should().Be(maxCarrier);
        persistedCarrierEvent.ProviderShipmentReference.Should().Be(maxProviderShipmentReference);
        persistedCarrierEvent.CarrierEventKey.Should().Be(maxCarrierEventKey);
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_Pass_When_Optional_Fields_Are_At_Max_Length_And_Trimmed()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();

        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();

        await db.Set<Shipment>().AddAsync(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-041",
            Status = ShipmentStatus.Packed
        }, TestContext.Current.CancellationToken);

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var result = await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-041",
            CarrierEventKey = " shipment.in_transit ",
            OccurredAtUtc = DateTime.UtcNow,
            TrackingNumber = $" {new string('T', 126)} ",
            LabelUrl = $" {new string('L', 2046)} ",
            Service = $" {new string('S', 62)} ",
            ProviderStatus = $" {new string('P', 62)} ",
            ExceptionCode = $" {new string('E', 126)} ",
            ExceptionMessage = $" {new string('M', 510)} "
        }, TestContext.Current.CancellationToken);

        result.LastCarrierEventKey.Should().Be("shipment.in_transit");
        result.Status.Should().Be(ShipmentStatus.Shipped);

        var persistedCarrierEvent = await db.Set<ShipmentCarrierEvent>()
            .SingleAsync(x => x.ShipmentId == shipmentId, TestContext.Current.CancellationToken);

        persistedCarrierEvent.Carrier.Should().Be("DHL");
        persistedCarrierEvent.ProviderShipmentReference.Should().Be("dhl-ship-041");
        persistedCarrierEvent.CarrierEventKey.Should().Be("shipment.in_transit");
        persistedCarrierEvent.TrackingNumber.Should().Be(new string('T', 126));
        persistedCarrierEvent.LabelUrl.Should().Be(new string('L', 2046));
        persistedCarrierEvent.Service.Should().Be(new string('S', 62));
        persistedCarrierEvent.ProviderStatus.Should().Be(new string('P', 62));
        persistedCarrierEvent.ExceptionCode.Should().Be(new string('E', 126));
        persistedCarrierEvent.ExceptionMessage.Should().Be(new string('M', 510));
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_InsertDuplicateCarrierTimeline_When_ProviderStatus_CaseDiffersForTrimmedFingerprint()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();

        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var occurredAtUtc = DateTime.UtcNow.AddMinutes(-7);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-42",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-042",
            Status = ShipmentStatus.Packed
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-042",
            CarrierEventKey = "shipment.in_transit",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "in_transit"
        }, TestContext.Current.CancellationToken);

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "  DHL ",
            ProviderShipmentReference = "  dhl-ship-042 ",
            CarrierEventKey = "  shipment.in_transit  ",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = " IN_TRANSIT "
        }, TestContext.Current.CancellationToken);

        var carrierEvents = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .OrderBy(x => x.Id)
            .ToListAsync(TestContext.Current.CancellationToken);

        carrierEvents.Should().HaveCount(2);
        carrierEvents.Should().Contain(e => e.ProviderStatus == "in_transit");
        carrierEvents.Should().Contain(e => e.ProviderStatus == "IN_TRANSIT");
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_NotInsertDuplicateCarrierTimeline_When_ProviderStatus_WhitespaceAndNull_Are_Equivalent()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();

        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var occurredAtUtc = DateTime.UtcNow.AddMinutes(-6);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-43",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-043",
            Status = ShipmentStatus.Packed
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-043",
            CarrierEventKey = "shipment.in_transit",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "   "
        }, TestContext.Current.CancellationToken);

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "  DHL ",
            ProviderShipmentReference = "  dhl-ship-043 ",
            CarrierEventKey = "  shipment.in_transit  ",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = null
        }, TestContext.Current.CancellationToken);

        var carrierEvents = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync(TestContext.Current.CancellationToken);

        carrierEvents.Should().HaveCount(1);
        carrierEvents[0].ProviderStatus.Should().BeNull();
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_UpdateMissingExceptionFields_ForNullProviderStatusDuplicate()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();

        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var occurredAtUtc = DateTime.UtcNow.AddMinutes(-6);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-43a",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-043a",
            Status = ShipmentStatus.Packed
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-043a",
            CarrierEventKey = "shipment.in_transit",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "   "
        }, TestContext.Current.CancellationToken);

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "  DHL  ",
            ProviderShipmentReference = "  dhl-ship-043a  ",
            CarrierEventKey = "  shipment.in_transit  ",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = null,
            ExceptionCode = "  CODE  ",
            ExceptionMessage = "  Msg  "
        }, TestContext.Current.CancellationToken);

        var carrierEvents = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync(TestContext.Current.CancellationToken);

        carrierEvents.Should().HaveCount(1);
        carrierEvents[0].Carrier.Should().Be("DHL");
        carrierEvents[0].CarrierEventKey.Should().Be("shipment.in_transit");
        carrierEvents[0].ProviderStatus.Should().BeNull();
        carrierEvents[0].ExceptionCode.Should().Be("CODE");
        carrierEvents[0].ExceptionMessage.Should().Be("Msg");
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_InsertDuplicateCarrierTimeline_For_Returned_WhenProviderStatus_Whitespace_AndOccurredAtDiffers()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();

        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var firstOccurredAtUtc = DateTime.UtcNow.AddMinutes(-12);
        var secondOccurredAtUtc = firstOccurredAtUtc.AddMinutes(1);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-53",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-053",
            Status = ShipmentStatus.Returned,
            ShippedAtUtc = firstOccurredAtUtc
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-053",
            CarrierEventKey = "shipment.returned",
            OccurredAtUtc = firstOccurredAtUtc,
            ProviderStatus = "   "
        }, TestContext.Current.CancellationToken);

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "  DHL  ",
            ProviderShipmentReference = "  dhl-ship-053  ",
            CarrierEventKey = "  shipment.returned  ",
            OccurredAtUtc = secondOccurredAtUtc,
            ProviderStatus = "\t"
        }, TestContext.Current.CancellationToken);

        var carrierEvents = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync(TestContext.Current.CancellationToken);

        carrierEvents.Should().HaveCount(2);
        carrierEvents.Should().Contain(x =>
            x.CarrierEventKey == "shipment.returned" &&
            x.OccurredAtUtc == firstOccurredAtUtc &&
            x.ProviderStatus == null);
        carrierEvents.Should().Contain(x =>
            x.CarrierEventKey == "shipment.returned" &&
            x.OccurredAtUtc == secondOccurredAtUtc &&
            x.ProviderStatus == null);

        var shipment = await db.Set<Shipment>()
            .SingleAsync(x => x.Id == shipmentId, TestContext.Current.CancellationToken);
        shipment.Status.Should().Be(ShipmentStatus.Returned);
        shipment.DeliveredAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_NotInsertDuplicateCarrierTimeline_For_Returned_WhenProviderStatus_WhitespaceAndNull_Are_Equivalent()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();

        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var occurredAtUtc = DateTime.UtcNow.AddMinutes(-7);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-54",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-054",
            Status = ShipmentStatus.Returned,
            ShippedAtUtc = occurredAtUtc
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-054",
            CarrierEventKey = "shipment.returned",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "   "
        }, TestContext.Current.CancellationToken);

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "  DHL  ",
            ProviderShipmentReference = "  dhl-ship-054  ",
            CarrierEventKey = "  shipment.returned  ",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = null
        }, TestContext.Current.CancellationToken);

        var carrierEvents = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync(TestContext.Current.CancellationToken);

        carrierEvents.Should().HaveCount(1);
        carrierEvents[0].Carrier.Should().Be("DHL");
        carrierEvents[0].CarrierEventKey.Should().Be("shipment.returned");
        carrierEvents[0].ProviderStatus.Should().BeNull();

        var shipment = await db.Set<Shipment>()
            .SingleAsync(x => x.Id == shipmentId, TestContext.Current.CancellationToken);
        shipment.Status.Should().Be(ShipmentStatus.Returned);
        shipment.DeliveredAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_NotInsertDuplicateCarrierTimeline_For_Delivered_WhenProviderStatus_WhitespaceAndNull_Are_Equivalent()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();

        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var occurredAtUtc = DateTime.UtcNow.AddMinutes(-7);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-55",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-055",
            Status = ShipmentStatus.Shipped
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-055",
            CarrierEventKey = "shipment.delivered",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "   "
        }, TestContext.Current.CancellationToken);

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "  DHL  ",
            ProviderShipmentReference = "  dhl-ship-055  ",
            CarrierEventKey = "  shipment.delivered  ",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = null
        }, TestContext.Current.CancellationToken);

        var carrierEvents = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync(TestContext.Current.CancellationToken);

        carrierEvents.Should().HaveCount(1);
        carrierEvents[0].Carrier.Should().Be("DHL");
        carrierEvents[0].CarrierEventKey.Should().Be("shipment.delivered");
        carrierEvents[0].ProviderStatus.Should().BeNull();

        var shipment = await db.Set<Shipment>()
            .SingleAsync(x => x.Id == shipmentId, TestContext.Current.CancellationToken);
        shipment.Status.Should().Be(ShipmentStatus.Delivered);
        shipment.DeliveredAtUtc.Should().Be(occurredAtUtc);
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_NotInsertDuplicateCarrierTimeline_For_Delivered_WhenCarrierEventKey_WhitespacePaddingIsEquivalent()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();

        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var occurredAtUtc = DateTime.UtcNow.AddMinutes(-8);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-56",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-056",
            Status = ShipmentStatus.Packed
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-056",
            CarrierEventKey = "shipment.delivered",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "Delivered"
        }, TestContext.Current.CancellationToken);

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "\tDHL\t",
            ProviderShipmentReference = "\t dhl-ship-056\t",
            CarrierEventKey = "\tshipment.delivered\t",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "   Delivered   "
        }, TestContext.Current.CancellationToken);

        var carrierEvents = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync(TestContext.Current.CancellationToken);

        carrierEvents.Should().HaveCount(1);
        carrierEvents[0].Carrier.Should().Be("DHL");
        carrierEvents[0].CarrierEventKey.Should().Be("shipment.delivered");
        carrierEvents[0].ProviderStatus.Should().Be("Delivered");

        var shipment = await db.Set<Shipment>()
            .SingleAsync(x => x.Id == shipmentId, TestContext.Current.CancellationToken);
        shipment.Status.Should().Be(ShipmentStatus.Delivered);
        shipment.DeliveredAtUtc.Should().Be(occurredAtUtc);
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_InsertDuplicateCarrierTimeline_For_Delivered_WhenProviderStatus_CaseDiffers()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();

        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var occurredAtUtc = DateTime.UtcNow.AddMinutes(-9);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-57",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-057",
            Status = ShipmentStatus.Packed
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-057",
            CarrierEventKey = "shipment.delivered",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "Delivered"
        }, TestContext.Current.CancellationToken);

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-057",
            CarrierEventKey = "shipment.delivered",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "DELIVERED"
        }, TestContext.Current.CancellationToken);

        var carrierEvents = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync(TestContext.Current.CancellationToken);

        carrierEvents.Should().HaveCount(2);
        carrierEvents.Should().Contain(x => x.ProviderStatus == "Delivered");
        carrierEvents.Should().Contain(x => x.ProviderStatus == "DELIVERED");

        var shipment = await db.Set<Shipment>()
            .SingleAsync(x => x.Id == shipmentId, TestContext.Current.CancellationToken);
        shipment.Status.Should().Be(ShipmentStatus.Delivered);
        shipment.DeliveredAtUtc.Should().Be(occurredAtUtc);
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_NotInsertDuplicateCarrierTimeline_For_Delivered_WhenProviderStatus_WhitespacePaddingEquivalent()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();

        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var occurredAtUtc = DateTime.UtcNow.AddMinutes(-9);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-58",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-058",
            Status = ShipmentStatus.Packed
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-058",
            CarrierEventKey = "shipment.delivered",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "Delivered"
        }, TestContext.Current.CancellationToken);

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-058",
            CarrierEventKey = "shipment.delivered",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "  Delivered  "
        }, TestContext.Current.CancellationToken);

        var carrierEvents = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync(TestContext.Current.CancellationToken);

        carrierEvents.Should().HaveCount(1);
        carrierEvents[0].Carrier.Should().Be("DHL");
        carrierEvents[0].CarrierEventKey.Should().Be("shipment.delivered");
        carrierEvents[0].ProviderStatus.Should().Be("Delivered");

        var shipment = await db.Set<Shipment>()
            .SingleAsync(x => x.Id == shipmentId, TestContext.Current.CancellationToken);
        shipment.Status.Should().Be(ShipmentStatus.Delivered);
        shipment.DeliveredAtUtc.Should().Be(occurredAtUtc);
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_NotInsertDuplicateCarrierTimeline_When_ProviderStatus_WhitespacePaddingIsEquivalent()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();

        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var occurredAtUtc = DateTime.UtcNow.AddMinutes(-10);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-52",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-052",
            Status = ShipmentStatus.Packed
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-052",
            CarrierEventKey = "shipment.in_transit",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "InTransit"
        }, TestContext.Current.CancellationToken);

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-052",
            CarrierEventKey = "shipment.in_transit",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "   InTransit   "
        }, TestContext.Current.CancellationToken);

        var carrierEvents = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync(TestContext.Current.CancellationToken);

        carrierEvents.Should().HaveCount(1);
        carrierEvents[0].CarrierEventKey.Should().Be("shipment.in_transit");
        carrierEvents[0].ProviderStatus.Should().Be("InTransit");
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_NotInsertDuplicateCarrierTimeline_For_Delivered_WhenProviderStatus_LeadingTrailingWhitespaceIsEquivalent()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();

        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var occurredAtUtc = DateTime.UtcNow.AddMinutes(-9);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-59",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-059",
            Status = ShipmentStatus.Packed
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-059",
            CarrierEventKey = "shipment.delivered",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = " Delivered "
        }, TestContext.Current.CancellationToken);

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-059",
            CarrierEventKey = "shipment.delivered",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "Delivered"
        }, TestContext.Current.CancellationToken);

        var carrierEvents = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync(TestContext.Current.CancellationToken);

        carrierEvents.Should().HaveCount(1);
        carrierEvents[0].Carrier.Should().Be("DHL");
        carrierEvents[0].CarrierEventKey.Should().Be("shipment.delivered");
        carrierEvents[0].ProviderStatus.Should().Be("Delivered");

        var shipment = await db.Set<Shipment>()
            .SingleAsync(x => x.Id == shipmentId, TestContext.Current.CancellationToken);
        shipment.Status.Should().Be(ShipmentStatus.Delivered);
        shipment.DeliveredAtUtc.Should().Be(occurredAtUtc);
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_UpdateMissingExceptionFields_For_Delivered_WhenDuplicateProviderStatusAndEventKey()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();

        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var occurredAtUtc = DateTime.UtcNow.AddMinutes(-9);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-60",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-060",
            Status = ShipmentStatus.Packed
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-060",
            CarrierEventKey = "shipment.delivered",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = " Delivered "
        }, TestContext.Current.CancellationToken);

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-060",
            CarrierEventKey = "shipment.delivered",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "Delivered",
            ExceptionCode = "  CODE  ",
            ExceptionMessage = "  Msg  "
        }, TestContext.Current.CancellationToken);

        var carrierEvents = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync(TestContext.Current.CancellationToken);

        carrierEvents.Should().HaveCount(1);
        carrierEvents[0].Carrier.Should().Be("DHL");
        carrierEvents[0].CarrierEventKey.Should().Be("shipment.delivered");
        carrierEvents[0].ProviderStatus.Should().Be("Delivered");
        carrierEvents[0].ExceptionCode.Should().Be("CODE");
        carrierEvents[0].ExceptionMessage.Should().Be("Msg");

        var shipment = await db.Set<Shipment>()
            .SingleAsync(x => x.Id == shipmentId, TestContext.Current.CancellationToken);
        shipment.Status.Should().Be(ShipmentStatus.Delivered);
        shipment.DeliveredAtUtc.Should().Be(occurredAtUtc);
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_InsertDuplicateCarrierTimeline_When_OccurredAtUtc_DiffersByOneSecond()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();

        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var baseOccurredAtUtc = DateTime.UtcNow.AddMinutes(-4);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-44",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-044",
            Status = ShipmentStatus.Packed
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-044",
            CarrierEventKey = "shipment.in_transit",
            OccurredAtUtc = baseOccurredAtUtc,
            ProviderStatus = "InTransit"
        }, TestContext.Current.CancellationToken);

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-044",
            CarrierEventKey = "shipment.in_transit",
            OccurredAtUtc = baseOccurredAtUtc.AddSeconds(1),
            ProviderStatus = "InTransit"
        }, TestContext.Current.CancellationToken);

        var carrierEvents = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .OrderBy(x => x.OccurredAtUtc)
            .ToListAsync(TestContext.Current.CancellationToken);

        carrierEvents.Should().HaveCount(2);
        carrierEvents[0].OccurredAtUtc.Should().Be(baseOccurredAtUtc);
        carrierEvents[1].OccurredAtUtc.Should().Be(baseOccurredAtUtc.AddSeconds(1));
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_InsertDuplicateCarrierTimeline_When_PreviousMatchIsDeleted()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();

        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var occurredAtUtc = DateTime.UtcNow.AddMinutes(-8);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-45",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-045",
            Status = ShipmentStatus.Packed
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-045",
            CarrierEventKey = "shipment.in_transit",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "in_transit"
        }, TestContext.Current.CancellationToken);

        var existingEvent = await db.Set<ShipmentCarrierEvent>()
            .SingleAsync(x =>
                x.ShipmentId == shipmentId &&
                x.CarrierEventKey == "shipment.in_transit" &&
                x.ProviderStatus == "in_transit" &&
                x.OccurredAtUtc == occurredAtUtc,
                TestContext.Current.CancellationToken);

        existingEvent.IsDeleted = true;

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-045",
            CarrierEventKey = "shipment.in_transit",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "in_transit"
        }, TestContext.Current.CancellationToken);

        var carrierEvents = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync(TestContext.Current.CancellationToken);

        carrierEvents.Should().HaveCount(2);
        carrierEvents.Count(x => x.IsDeleted).Should().Be(1);
        carrierEvents.Count(x => !x.IsDeleted).Should().Be(1);
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_InsertDuplicateCarrierTimeline_When_PreviousMatchIsDeleted_AndCarrierEventKey_CaseDiffers()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();

        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var occurredAtUtc = DateTime.UtcNow.AddMinutes(-9);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-46",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-046",
            Status = ShipmentStatus.Packed
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-046",
            CarrierEventKey = "shipment.custom_unknown",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "InTransit"
        }, TestContext.Current.CancellationToken);

        var existingEvent = await db.Set<ShipmentCarrierEvent>()
            .SingleAsync(x =>
                x.ShipmentId == shipmentId &&
                x.CarrierEventKey == "shipment.custom_unknown" &&
                x.OccurredAtUtc == occurredAtUtc,
                TestContext.Current.CancellationToken);

        existingEvent.IsDeleted = true;

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-046",
            CarrierEventKey = "SHIPMENT.CUSTOM_UNKNOWN",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "InTransit"
        }, TestContext.Current.CancellationToken);

        var carrierEvents = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync(TestContext.Current.CancellationToken);

        carrierEvents.Should().HaveCount(2);
        carrierEvents.Should().Contain(x => x.CarrierEventKey == "shipment.custom_unknown");
        carrierEvents.Should().Contain(x => x.CarrierEventKey == "SHIPMENT.CUSTOM_UNKNOWN");
        carrierEvents.Count(x => x.IsDeleted).Should().Be(1);
        carrierEvents.Count(x => !x.IsDeleted).Should().Be(1);
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_InsertDuplicateCarrierTimeline_When_PreviousDeletedMatchHasNullProviderStatus_ThenActualProviderStatus()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();

        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var occurredAtUtc = DateTime.UtcNow.AddMinutes(-11);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-47",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-047",
            Status = ShipmentStatus.Packed
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-047",
            CarrierEventKey = "shipment.in_transit",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "   "
        }, TestContext.Current.CancellationToken);

        var existingEvent = await db.Set<ShipmentCarrierEvent>()
            .SingleAsync(x =>
                x.ShipmentId == shipmentId &&
                x.CarrierEventKey == "shipment.in_transit" &&
                x.ProviderStatus == null &&
                x.OccurredAtUtc == occurredAtUtc,
                TestContext.Current.CancellationToken);

        existingEvent.IsDeleted = true;

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-047",
            CarrierEventKey = "shipment.in_transit",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "InTransit"
        }, TestContext.Current.CancellationToken);

        var carrierEvents = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync(TestContext.Current.CancellationToken);

        carrierEvents.Should().HaveCount(2);
        carrierEvents.Should().Contain(x => x.ProviderStatus == null);
        carrierEvents.Should().Contain(x => x.ProviderStatus == "InTransit");
        carrierEvents.Count(x => x.IsDeleted).Should().Be(1);
        carrierEvents.Count(x => !x.IsDeleted).Should().Be(1);
    }

    [Fact]
    public async Task ApplyShipmentCarrierEventHandler_Should_InsertNewCarrierTimeline_When_PreviousMatchIsDeleted_ButExceptionFieldsDiffer()
    {
        await using var db = ShipmentCarrierEventTestDbContext.Create();

        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var occurredAtUtc = DateTime.UtcNow.AddMinutes(-12);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-CARRIER-48",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            ProviderShipmentReference = "dhl-ship-048",
            Status = ShipmentStatus.Packed
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyShipmentCarrierEventHandler(
            db,
            new ApplyShipmentCarrierEventValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-048",
            CarrierEventKey = "shipment.in_transit",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "in_transit",
            ExceptionCode = "FIRST"
        }, TestContext.Current.CancellationToken);

        var existingEvent = await db.Set<ShipmentCarrierEvent>()
            .SingleAsync(x =>
                x.ShipmentId == shipmentId &&
                x.CarrierEventKey == "shipment.in_transit" &&
                x.ProviderStatus == "in_transit" &&
                x.OccurredAtUtc == occurredAtUtc,
                TestContext.Current.CancellationToken);

        existingEvent.IsDeleted = true;

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await handler.HandleAsync(new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "dhl-ship-048",
            CarrierEventKey = "shipment.in_transit",
            OccurredAtUtc = occurredAtUtc,
            ProviderStatus = "in_transit",
            ExceptionCode = "SECOND",
            ExceptionMessage = "Second message"
        }, TestContext.Current.CancellationToken);

        var carrierEvents = await db.Set<ShipmentCarrierEvent>()
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync(TestContext.Current.CancellationToken);

        carrierEvents.Should().HaveCount(2);
        carrierEvents.Count(x => x.IsDeleted).Should().Be(1);
        carrierEvents.Count(x => !x.IsDeleted).Should().Be(1);

        var activeEvent = carrierEvents.Single(x => !x.IsDeleted);
        activeEvent.ExceptionCode.Should().Be("SECOND");
        activeEvent.ExceptionMessage.Should().Be("Second message");
        activeEvent.ProviderStatus.Should().Be("in_transit");

        var deletedEvent = carrierEvents.Single(x => x.IsDeleted);
        deletedEvent.ExceptionCode.Should().Be("FIRST");
        deletedEvent.ExceptionMessage.Should().BeNull();
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
