using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Orders.DTOs;
using Darwin.Application.Orders.Queries;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.Integration;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Darwin.Tests.Unit.Orders;

/// <summary>
/// Comprehensive unit tests for <see cref="GetShipmentsPageHandler"/> and
/// <see cref="GetShipmentOpsSummaryHandler"/>.
/// </summary>
public sealed class ShipmentOpsQueryHandlersTests
{
    // ─── helpers ──────────────────────────────────────────────────────────────

    private static ShipmentOpsTestDbContext CreateDb() => ShipmentOpsTestDbContext.Create();

    private static GetShipmentsPageHandler CreatePageHandler(
        ShipmentOpsTestDbContext db, DateTime? fixedNow = null)
    {
        IClock clock = fixedNow.HasValue
            ? new FixedClock(fixedNow.Value)
            : new FixedClock(DateTime.UtcNow);
        return new GetShipmentsPageHandler(db, clock);
    }

    private static GetShipmentOpsSummaryHandler CreateSummaryHandler(
        ShipmentOpsTestDbContext db, DateTime? fixedNow = null)
    {
        IClock clock = fixedNow.HasValue
            ? new FixedClock(fixedNow.Value)
            : new FixedClock(DateTime.UtcNow);
        return new GetShipmentOpsSummaryHandler(db, clock);
    }

    private static Order BuildOrder(string? number = null) =>
        new() { Id = Guid.NewGuid(), OrderNumber = number ?? $"ORD-{Guid.NewGuid():N}", Currency = "EUR" };

    private static Shipment BuildShipment(
        Guid orderId,
        string carrier = "DHL",
        string service = "Parcel",
        ShipmentStatus status = ShipmentStatus.Pending,
        string? trackingNumber = null,
        bool isDeleted = false,
        DateTime? createdAtUtc = null,
        DateTime? shippedAtUtc = null,
        DateTime? deliveredAtUtc = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Carrier = carrier,
            Service = service,
            Status = status,
            TrackingNumber = trackingNumber,
            IsDeleted = isDeleted,
            CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow,
            ShippedAtUtc = shippedAtUtc,
            DeliveredAtUtc = deliveredAtUtc
        };

    // ─── GetShipmentsPageHandler — empty state ────────────────────────────────

    [Fact]
    public async Task GetShipmentsPage_Should_ReturnEmpty_WhenNoShipmentsExist()
    {
        await using var db = CreateDb();
        var handler = CreatePageHandler(db);

        var (items, total) = await handler.HandleAsync(1, 20, ct: TestContext.Current.CancellationToken);

        total.Should().Be(0);
        items.Should().BeEmpty();
    }

    // ─── GetShipmentsPageHandler — soft-delete exclusion ─────────────────────

    [Fact]
    public async Task GetShipmentsPage_Should_ExcludeSoftDeletedShipments()
    {
        await using var db = CreateDb();
        var order = BuildOrder("ORD-DEL");
        db.Set<Order>().Add(order);
        db.Set<Shipment>().AddRange(
            BuildShipment(order.Id, isDeleted: false),
            BuildShipment(order.Id, isDeleted: true));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreatePageHandler(db);
        var (items, total) = await handler.HandleAsync(1, 20, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1, "soft-deleted shipments must be excluded");
        items.Should().ContainSingle();
    }

    // ─── GetShipmentsPageHandler — filter: All ────────────────────────────────

    [Fact]
    public async Task GetShipmentsPage_Filter_All_Should_ReturnAllNonDeleted()
    {
        await using var db = CreateDb();
        var order = BuildOrder("ORD-ALL");
        db.Set<Order>().Add(order);
        db.Set<Shipment>().AddRange(
            BuildShipment(order.Id, status: ShipmentStatus.Pending),
            BuildShipment(order.Id, status: ShipmentStatus.Shipped),
            BuildShipment(order.Id, status: ShipmentStatus.Returned));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreatePageHandler(db);
        var (items, total) = await handler.HandleAsync(
            1, 20, filter: ShipmentQueueFilter.All, ct: TestContext.Current.CancellationToken);

        total.Should().Be(3, "All filter returns every non-deleted shipment");
        items.Should().HaveCount(3);
    }

    // ─── GetShipmentsPageHandler — filter: Pending ───────────────────────────

    [Fact]
    public async Task GetShipmentsPage_Filter_Pending_Should_ReturnOnlyPendingAndPacked()
    {
        await using var db = CreateDb();
        var order = BuildOrder("ORD-PEND");
        db.Set<Order>().Add(order);
        db.Set<Shipment>().AddRange(
            BuildShipment(order.Id, status: ShipmentStatus.Pending),
            BuildShipment(order.Id, status: ShipmentStatus.Packed),
            BuildShipment(order.Id, status: ShipmentStatus.Shipped));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreatePageHandler(db);
        var (items, total) = await handler.HandleAsync(
            1, 20, filter: ShipmentQueueFilter.Pending, ct: TestContext.Current.CancellationToken);

        total.Should().Be(2, "Pending filter includes Pending and Packed, not Shipped");
        items.Should().AllSatisfy(i =>
            i.Status.Should().BeOneOf(ShipmentStatus.Pending, ShipmentStatus.Packed));
    }

    // ─── GetShipmentsPageHandler — filter: Shipped ───────────────────────────

    [Fact]
    public async Task GetShipmentsPage_Filter_Shipped_Should_ReturnOnlyShippedAndDelivered()
    {
        await using var db = CreateDb();
        var order = BuildOrder("ORD-SHIP");
        db.Set<Order>().Add(order);
        db.Set<Shipment>().AddRange(
            BuildShipment(order.Id, status: ShipmentStatus.Pending),
            BuildShipment(order.Id, status: ShipmentStatus.Shipped),
            BuildShipment(order.Id, status: ShipmentStatus.Delivered));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreatePageHandler(db);
        var (items, total) = await handler.HandleAsync(
            1, 20, filter: ShipmentQueueFilter.Shipped, ct: TestContext.Current.CancellationToken);

        total.Should().Be(2, "Shipped filter includes Shipped and Delivered, not Pending");
        items.Should().AllSatisfy(i =>
            i.Status.Should().BeOneOf(ShipmentStatus.Shipped, ShipmentStatus.Delivered));
    }

    // ─── GetShipmentsPageHandler — filter: MissingTracking ───────────────────

    [Fact]
    public async Task GetShipmentsPage_Filter_MissingTracking_Should_ReturnShippedWithNoTracking()
    {
        await using var db = CreateDb();
        var order = BuildOrder("ORD-MT");
        db.Set<Order>().Add(order);
        db.Set<Shipment>().AddRange(
            BuildShipment(order.Id, status: ShipmentStatus.Shipped, trackingNumber: null),
            BuildShipment(order.Id, status: ShipmentStatus.Delivered, trackingNumber: string.Empty),
            BuildShipment(order.Id, status: ShipmentStatus.Shipped, trackingNumber: "1Z999AA"),
            BuildShipment(order.Id, status: ShipmentStatus.Pending, trackingNumber: null)); // Pending excluded
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreatePageHandler(db);
        var (items, total) = await handler.HandleAsync(
            1, 20, filter: ShipmentQueueFilter.MissingTracking, ct: TestContext.Current.CancellationToken);

        total.Should().Be(2, "only Shipped/Delivered with no tracking number match");
    }

    // ─── GetShipmentsPageHandler — filter: Returned ──────────────────────────

    [Fact]
    public async Task GetShipmentsPage_Filter_Returned_Should_ReturnOnlyReturnedShipments()
    {
        await using var db = CreateDb();
        var order = BuildOrder("ORD-RET");
        db.Set<Order>().Add(order);
        db.Set<Shipment>().AddRange(
            BuildShipment(order.Id, status: ShipmentStatus.Returned),
            BuildShipment(order.Id, status: ShipmentStatus.Returned),
            BuildShipment(order.Id, status: ShipmentStatus.Shipped));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreatePageHandler(db);
        var (items, total) = await handler.HandleAsync(
            1, 20, filter: ShipmentQueueFilter.Returned, ct: TestContext.Current.CancellationToken);

        total.Should().Be(2, "Returned filter includes only Returned shipments");
        items.Should().AllSatisfy(i => i.Status.Should().Be(ShipmentStatus.Returned));
    }

    // ─── GetShipmentsPageHandler — filter: Dhl ───────────────────────────────

    [Fact]
    public async Task GetShipmentsPage_Filter_Dhl_Should_ReturnOnlyDhlCarrierShipments()
    {
        await using var db = CreateDb();
        var order = BuildOrder("ORD-DHL");
        db.Set<Order>().Add(order);
        db.Set<Shipment>().AddRange(
            BuildShipment(order.Id, carrier: "DHL"),
            BuildShipment(order.Id, carrier: "DHL"),
            BuildShipment(order.Id, carrier: "UPS"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreatePageHandler(db);
        var (items, total) = await handler.HandleAsync(
            1, 20, filter: ShipmentQueueFilter.Dhl, ct: TestContext.Current.CancellationToken);

        total.Should().Be(2, "Dhl filter returns only shipments with Carrier=DHL");
        items.Should().AllSatisfy(i => i.IsDhl.Should().BeTrue());
    }

    // ─── GetShipmentsPageHandler — filter: MissingService ────────────────────

    [Fact]
    public async Task GetShipmentsPage_Filter_MissingService_Should_ReturnShipmentsWithNoService()
    {
        await using var db = CreateDb();
        var order = BuildOrder("ORD-MS");
        db.Set<Order>().Add(order);
        db.Set<Shipment>().AddRange(
            BuildShipment(order.Id, service: string.Empty),
            BuildShipment(order.Id, service: "Parcel")); // has service
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreatePageHandler(db);
        var (items, total) = await handler.HandleAsync(
            1, 20, filter: ShipmentQueueFilter.MissingService, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1, "MissingService filter returns only shipments without a service");
    }

    // ─── GetShipmentsPageHandler — filter: AwaitingHandoff ───────────────────

    [Fact]
    public async Task GetShipmentsPage_Should_FilterAwaitingHandoff_ByConfiguredThreshold()
    {
        var fixedNow = new DateTime(2026, 1, 20, 12, 0, 0, DateTimeKind.Utc);
        await using var db = CreateDb();
        var oldOrder = BuildOrder("ORD-OLD");
        var freshOrder = BuildOrder("ORD-FRESH");
        db.Set<Order>().AddRange(oldOrder, freshOrder);
        db.Set<Shipment>().AddRange(
            BuildShipment(oldOrder.Id, status: ShipmentStatus.Pending,
                createdAtUtc: fixedNow.AddHours(-30)),   // older than 24-hour threshold
            BuildShipment(freshOrder.Id, status: ShipmentStatus.Pending,
                createdAtUtc: fixedNow.AddHours(-4)));   // within threshold
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreatePageHandler(db, fixedNow);
        var (items, total) = await handler.HandleAsync(
            page: 1, pageSize: 20,
            filter: ShipmentQueueFilter.AwaitingHandoff,
            attentionDelayHours: 24, trackingGraceHours: 12,
            ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Should().ContainSingle();
        items[0].OrderNumber.Should().Be("ORD-OLD");
        items[0].AwaitingHandoff.Should().BeTrue();
    }

    // ─── GetShipmentsPageHandler — filter: TrackingOverdue ───────────────────

    [Fact]
    public async Task GetShipmentsPage_Filter_TrackingOverdue_Should_ReturnDhlShippedWithNoTrackingPastGrace()
    {
        var fixedNow = new DateTime(2026, 1, 20, 12, 0, 0, DateTimeKind.Utc);
        await using var db = CreateDb();
        var order = BuildOrder("ORD-TO");
        db.Set<Order>().Add(order);
        db.Set<Shipment>().AddRange(
            // DHL, Shipped, no tracking, past grace (15 h > 12 h) → overdue
            BuildShipment(order.Id, carrier: "DHL", status: ShipmentStatus.Shipped,
                shippedAtUtc: fixedNow.AddHours(-15), createdAtUtc: fixedNow.AddHours(-16)),
            // DHL, Shipped, no tracking, within grace (5 h < 12 h) → not overdue
            BuildShipment(order.Id, carrier: "DHL", status: ShipmentStatus.Shipped,
                shippedAtUtc: fixedNow.AddHours(-5), createdAtUtc: fixedNow.AddHours(-6)),
            // DHL, Shipped, HAS tracking → not missing
            BuildShipment(order.Id, carrier: "DHL", status: ShipmentStatus.Shipped,
                shippedAtUtc: fixedNow.AddHours(-20), createdAtUtc: fixedNow.AddHours(-21),
                trackingNumber: "1Z999AA"),
            // UPS, Shipped, no tracking, past grace → carrier not DHL → not overdue
            BuildShipment(order.Id, carrier: "UPS", status: ShipmentStatus.Shipped,
                shippedAtUtc: fixedNow.AddHours(-15), createdAtUtc: fixedNow.AddHours(-16)));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreatePageHandler(db, fixedNow);
        var (items, total) = await handler.HandleAsync(
            1, 20, filter: ShipmentQueueFilter.TrackingOverdue,
            attentionDelayHours: 24, trackingGraceHours: 12,
            ct: TestContext.Current.CancellationToken);

        total.Should().Be(1, "only DHL shipped shipments without tracking past the grace period");
        items[0].TrackingOverdue.Should().BeTrue();
    }

    // ─── GetShipmentsPageHandler — filter: CarrierReview ─────────────────────

    [Fact]
    public async Task GetShipmentsPage_Filter_CarrierReview_Should_ReturnDhlWithIssues()
    {
        await using var db = CreateDb();
        var order = BuildOrder("ORD-CR");
        db.Set<Order>().Add(order);
        db.Set<Shipment>().AddRange(
            // DHL, missing service → needs review
            BuildShipment(order.Id, carrier: "DHL", service: string.Empty,
                status: ShipmentStatus.Pending),
            // DHL, Returned → needs review
            BuildShipment(order.Id, carrier: "DHL", service: "Parcel",
                status: ShipmentStatus.Returned),
            // DHL, Shipped, no tracking → needs review
            BuildShipment(order.Id, carrier: "DHL", service: "Parcel",
                status: ShipmentStatus.Shipped, trackingNumber: null),
            // DHL, Shipped, has tracking → OK
            BuildShipment(order.Id, carrier: "DHL", service: "Parcel",
                status: ShipmentStatus.Shipped, trackingNumber: "1Z999AA"),
            // UPS, missing service → carrier not DHL → not in carrier review
            BuildShipment(order.Id, carrier: "UPS", service: string.Empty,
                status: ShipmentStatus.Pending));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreatePageHandler(db);
        var (items, total) = await handler.HandleAsync(
            1, 20, filter: ShipmentQueueFilter.CarrierReview, ct: TestContext.Current.CancellationToken);

        total.Should().Be(3, "DHL shipments with missing service, returned, or shipped+no-tracking need review");
        items.Should().AllSatisfy(i => i.IsDhl.Should().BeTrue());
    }

    // ─── GetShipmentsPageHandler — filter: ReturnFollowUp ────────────────────

    [Fact]
    public async Task GetShipmentsPage_Filter_ReturnFollowUp_Should_ReturnOnlyReturnedShipments()
    {
        await using var db = CreateDb();
        var order = BuildOrder("ORD-RF");
        db.Set<Order>().Add(order);
        db.Set<Shipment>().AddRange(
            BuildShipment(order.Id, status: ShipmentStatus.Returned),
            BuildShipment(order.Id, status: ShipmentStatus.Shipped));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreatePageHandler(db);
        var (items, total) = await handler.HandleAsync(
            1, 20, filter: ShipmentQueueFilter.ReturnFollowUp, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1, "ReturnFollowUp returns only Returned shipments");
        items[0].Status.Should().Be(ShipmentStatus.Returned);
    }

    // ─── GetShipmentsPageHandler — query search ───────────────────────────────

    [Fact]
    public async Task GetShipmentsPage_Should_FilterByQuery_OnCarrier()
    {
        await using var db = CreateDb();
        var order = BuildOrder("ORD-QC");
        db.Set<Order>().Add(order);
        db.Set<Shipment>().AddRange(
            BuildShipment(order.Id, carrier: "DHL"),
            BuildShipment(order.Id, carrier: "UPS"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreatePageHandler(db);
        var (items, total) = await handler.HandleAsync(
            1, 20, query: "UPS", ct: TestContext.Current.CancellationToken);

        total.Should().Be(1, "only the shipment matching the carrier query is returned");
        items.Single().Carrier.Should().Be("UPS");
    }

    [Fact]
    public async Task GetShipmentsPage_Should_FilterByQuery_OnTrackingNumber()
    {
        await using var db = CreateDb();
        var order = BuildOrder("ORD-QT");
        db.Set<Order>().Add(order);
        db.Set<Shipment>().AddRange(
            BuildShipment(order.Id, trackingNumber: "1Z999AABC"),
            BuildShipment(order.Id, trackingNumber: "XYZ123456"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreatePageHandler(db);
        var (items, total) = await handler.HandleAsync(
            1, 20, query: "1Z999", ct: TestContext.Current.CancellationToken);

        total.Should().Be(1, "only the shipment whose tracking number contains the query is returned");
        items.Single().TrackingNumber.Should().Be("1Z999AABC");
    }

    // ─── GetShipmentsPageHandler — pagination ─────────────────────────────────

    [Fact]
    public async Task GetShipmentsPage_Should_ApplyPagination()
    {
        await using var db = CreateDb();
        var order = BuildOrder("ORD-PAG");
        db.Set<Order>().Add(order);
        for (var i = 0; i < 5; i++)
            db.Set<Shipment>().Add(BuildShipment(order.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreatePageHandler(db);
        var (items, total) = await handler.HandleAsync(1, 3, ct: TestContext.Current.CancellationToken);

        total.Should().Be(5, "total reflects all non-deleted shipments");
        items.Should().HaveCount(3, "page size of 3 limits the returned items");
    }

    [Fact]
    public async Task GetShipmentsPage_Should_ClampInvalidPageToOne()
    {
        await using var db = CreateDb();
        var order = BuildOrder("ORD-PCLAMP");
        db.Set<Order>().Add(order);
        db.Set<Shipment>().Add(BuildShipment(order.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreatePageHandler(db);
        var (items, total) = await handler.HandleAsync(0, 20, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1, "page 0 is treated as page 1");
        items.Should().HaveCount(1);
    }

    // ─── GetShipmentsPageHandler — OrderNumber enrichment ─────────────────────

    [Fact]
    public async Task GetShipmentsPage_Should_EnrichOrderNumber_FromLinkedOrder()
    {
        await using var db = CreateDb();
        var order = BuildOrder("ORD-ENRICH");
        db.Set<Order>().Add(order);
        db.Set<Shipment>().Add(BuildShipment(order.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreatePageHandler(db);
        var (items, _) = await handler.HandleAsync(1, 20, ct: TestContext.Current.CancellationToken);

        items.Should().ContainSingle();
        items[0].OrderNumber.Should().Be("ORD-ENRICH", "OrderNumber should be populated from the linked Order");
    }

    // ─── GetShipmentOpsSummaryHandler — empty state ───────────────────────────

    [Fact]
    public async Task GetShipmentOpsSummary_Should_ReturnZeroCounts_WhenNoShipmentsExist()
    {
        await using var db = CreateDb();
        var handler = CreateSummaryHandler(db);

        var summary = await handler.HandleAsync(ct: TestContext.Current.CancellationToken);

        summary.PendingCount.Should().Be(0);
        summary.ShippedCount.Should().Be(0);
        summary.MissingTrackingCount.Should().Be(0);
        summary.ReturnedCount.Should().Be(0);
        summary.DhlCount.Should().Be(0);
        summary.MissingServiceCount.Should().Be(0);
        summary.AwaitingHandoffCount.Should().Be(0);
        summary.TrackingOverdueCount.Should().Be(0);
        summary.CarrierReviewCount.Should().Be(0);
        summary.ReturnFollowUpCount.Should().Be(0);
    }

    // ─── GetShipmentOpsSummaryHandler — full counts ───────────────────────────

    [Fact]
    public async Task GetShipmentOpsSummary_Should_ReturnCorrectCounts_AcrossAllCategories()
    {
        var fixedNow = new DateTime(2026, 1, 20, 12, 0, 0, DateTimeKind.Utc);
        await using var db = CreateDb();
        var order = BuildOrder("ORD-SUM");
        db.Set<Order>().Add(order);

        db.Set<Shipment>().AddRange(
            // Pending (contributes to PendingCount, AwaitingHandoffCount since >24h old)
            BuildShipment(order.Id, carrier: "DHL", service: "Parcel",
                status: ShipmentStatus.Pending,
                createdAtUtc: fixedNow.AddHours(-30)),

            // Packed (contributes to PendingCount only, within handoff threshold)
            BuildShipment(order.Id, carrier: "UPS", service: "Standard",
                status: ShipmentStatus.Packed,
                createdAtUtc: fixedNow.AddHours(-2)),

            // Shipped + DHL + no tracking (ShippedCount, MissingTrackingCount, DhlCount,
            //   CarrierReviewCount, TrackingOverdueCount when past grace)
            BuildShipment(order.Id, carrier: "DHL", service: "Parcel",
                status: ShipmentStatus.Shipped, trackingNumber: null,
                shippedAtUtc: fixedNow.AddHours(-15),
                createdAtUtc: fixedNow.AddHours(-16)),

            // Delivered + has tracking (ShippedCount, DhlCount)
            BuildShipment(order.Id, carrier: "DHL", service: "Express",
                status: ShipmentStatus.Delivered, trackingNumber: "1Z999AA"),

            // Returned (ReturnedCount, CarrierReviewCount since DHL, ReturnFollowUpCount)
            BuildShipment(order.Id, carrier: "DHL", service: "Parcel",
                status: ShipmentStatus.Returned),

            // MissingService on DHL (MissingServiceCount, CarrierReviewCount)
            BuildShipment(order.Id, carrier: "DHL", service: string.Empty,
                status: ShipmentStatus.Pending,
                createdAtUtc: fixedNow.AddHours(-1)),

            // UPS, no service (MissingServiceCount only, not DHL → not in CarrierReview)
            BuildShipment(order.Id, carrier: "UPS", service: string.Empty,
                status: ShipmentStatus.Pending,
                createdAtUtc: fixedNow.AddHours(-1)),

            // Soft-deleted — must not count in any metric
            BuildShipment(order.Id, isDeleted: true, status: ShipmentStatus.Pending)
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateSummaryHandler(db, fixedNow);
        var summary = await handler.HandleAsync(
            attentionDelayHours: 24, trackingGraceHours: 12,
            ct: TestContext.Current.CancellationToken);

        // Shipment 1: Pending DHL old, Shipment 2: Packed UPS fresh,
        // Shipment 6: Pending DHL no-service fresh, Shipment 7: Pending UPS no-service fresh
        summary.PendingCount.Should().Be(4, "Pending + Packed: DHL-pending + UPS-packed + DHL-no-service + UPS-no-service");

        // Shipment 3: Shipped DHL, Shipment 4: Delivered DHL
        summary.ShippedCount.Should().Be(2, "Shipped and Delivered");

        // Shipment 3: DHL Shipped no-tracking
        summary.MissingTrackingCount.Should().Be(1, "only Shipped/Delivered with null or empty tracking");

        // Shipment 5: Returned DHL
        summary.ReturnedCount.Should().Be(1);

        // Shipments 1, 3, 4, 5, 6 are DHL (5 total, soft-deleted excluded)
        summary.DhlCount.Should().Be(5, "DHL shipments: pending, shipped, delivered, returned, missing-service");

        // Shipments 6 (DHL no-service), 7 (UPS no-service)
        summary.MissingServiceCount.Should().Be(2, "two shipments with empty service strings");

        // Shipment 1: Pending DHL created 30 h ago, past the 24-hour handoff threshold
        summary.AwaitingHandoffCount.Should().Be(1, "one Pending/Packed shipment past the handoff threshold");

        // Shipment 3: DHL Shipped, no tracking, shippedAt 15 h ago > 12-h grace
        summary.TrackingOverdueCount.Should().Be(1, "one DHL Shipped without tracking past the grace window");

        // CarrierReview: DHL with (missing-service OR returned OR shipped+no-tracking)
        // Shipment 3 (shipped+no-tracking), Shipment 5 (returned), Shipment 6 (missing-service)
        summary.CarrierReviewCount.Should().Be(3, "three DHL shipments that need carrier review");

        // ReturnFollowUp = Returned
        summary.ReturnFollowUpCount.Should().Be(1, "same as ReturnedCount");
    }

    // ─── GetShipmentOpsSummaryHandler — soft-delete exclusion ─────────────────

    [Fact]
    public async Task GetShipmentOpsSummary_Should_ExcludeSoftDeletedShipments_FromAllCounts()
    {
        await using var db = CreateDb();
        var order = BuildOrder("ORD-SDEL");
        db.Set<Order>().Add(order);
        db.Set<Shipment>().Add(
            BuildShipment(order.Id, carrier: "DHL", status: ShipmentStatus.Shipped, isDeleted: true));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateSummaryHandler(db);
        var summary = await handler.HandleAsync(ct: TestContext.Current.CancellationToken);

        summary.DhlCount.Should().Be(0, "soft-deleted shipments must not count");
        summary.ShippedCount.Should().Be(0);
    }

    // ─── GetShipmentOpsSummaryHandler — existing tracking-overdue test ────────

    [Fact]
    public async Task GetShipmentOpsSummary_Should_CountTrackingOverdue_ByConfiguredGrace()
    {
        var fixedNow = new DateTime(2026, 1, 20, 12, 0, 0, DateTimeKind.Utc);
        await using var db = CreateDb();
        var orderId = Guid.NewGuid();
        db.Set<Order>().Add(new Order { Id = orderId, OrderNumber = "ORD-TRACK", Currency = "EUR" });
        db.Set<Shipment>().AddRange(
            // Shipped 15 h ago without tracking — past 12-hour grace → overdue
            new Shipment
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                Carrier = "DHL",
                Service = "Parcel",
                Status = ShipmentStatus.Shipped,
                CreatedAtUtc = fixedNow.AddHours(-16),
                ShippedAtUtc = fixedNow.AddHours(-15),
                TrackingNumber = null
            },
            // Shipped 5 h ago without tracking — within 12-hour grace → not overdue
            new Shipment
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                Carrier = "DHL",
                Service = "Parcel",
                Status = ShipmentStatus.Shipped,
                CreatedAtUtc = fixedNow.AddHours(-6),
                ShippedAtUtc = fixedNow.AddHours(-5),
                TrackingNumber = null
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateSummaryHandler(db, fixedNow);
        var result = await handler.HandleAsync(
            attentionDelayHours: 24, trackingGraceHours: 12,
            ct: TestContext.Current.CancellationToken);

        result.DhlCount.Should().Be(2);
        result.TrackingOverdueCount.Should().Be(1);
        result.MissingTrackingCount.Should().Be(2);
    }

    // ─── Test DbContext ────────────────────────────────────────────────────────

    private sealed class ShipmentOpsTestDbContext : DbContext, IAppDbContext
    {
        private ShipmentOpsTestDbContext(DbContextOptions<ShipmentOpsTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static ShipmentOpsTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<ShipmentOpsTestDbContext>()
                .UseInMemoryDatabase($"darwin_shipment_ops_tests_{Guid.NewGuid()}")
                .Options;
            return new ShipmentOpsTestDbContext(options);
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
                builder.Ignore(x => x.Lines);
                builder.Ignore(x => x.CarrierEvents);
            });

            modelBuilder.Entity<ShipmentCarrierEvent>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.ShipmentId).IsRequired();
                builder.Property(x => x.Carrier).IsRequired();
                builder.Property(x => x.ProviderShipmentReference).IsRequired();
                builder.Property(x => x.CarrierEventKey).IsRequired();
            });

            modelBuilder.Entity<ShipmentProviderOperation>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Provider).IsRequired();
                builder.Property(x => x.OperationType).IsRequired();
                builder.Property(x => x.Status).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<Payment>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Currency).IsRequired();
                builder.Property(x => x.Provider).IsRequired();
                builder.Property(x => x.IsDeleted);
                builder.Property(x => x.RowVersion).IsRequired();
            });
        }
    }
}
