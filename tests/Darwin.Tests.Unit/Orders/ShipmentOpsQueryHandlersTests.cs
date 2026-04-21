using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Orders.DTOs;
using Darwin.Application.Orders.Queries;
using Darwin.Domain.Entities.Integration;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Darwin.Tests.Unit.Orders;

public sealed class ShipmentOpsQueryHandlersTests
{
    [Fact]
    public async Task GetShipmentsPage_Should_FilterAwaitingHandoff_ByConfiguredThreshold()
    {
        await using var db = ShipmentOpsTestDbContext.Create();
        var oldOrderId = Guid.NewGuid();
        var freshOrderId = Guid.NewGuid();

        db.Set<Order>().AddRange(
            new Order { Id = oldOrderId, OrderNumber = "ORD-OLD", Currency = "EUR", Status = OrderStatus.Created },
            new Order { Id = freshOrderId, OrderNumber = "ORD-FRESH", Currency = "EUR", Status = OrderStatus.Created });

        db.Set<Shipment>().AddRange(
            new Shipment
            {
                Id = Guid.NewGuid(),
                OrderId = oldOrderId,
                Carrier = "DHL",
                Service = "Parcel",
                Status = ShipmentStatus.Pending,
                CreatedAtUtc = DateTime.UtcNow.AddHours(-30)
            },
            new Shipment
            {
                Id = Guid.NewGuid(),
                OrderId = freshOrderId,
                Carrier = "DHL",
                Service = "Parcel",
                Status = ShipmentStatus.Pending,
                CreatedAtUtc = DateTime.UtcNow.AddHours(-4)
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetShipmentsPageHandler(db);

        var result = await handler.HandleAsync(
            page: 1,
            pageSize: 20,
            filter: ShipmentQueueFilter.AwaitingHandoff,
            attentionDelayHours: 24,
            trackingGraceHours: 12,
            ct: TestContext.Current.CancellationToken);

        result.Total.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items[0].OrderNumber.Should().Be("ORD-OLD");
        result.Items[0].AwaitingHandoff.Should().BeTrue();
    }

    [Fact]
    public async Task GetShipmentOpsSummary_Should_CountTrackingOverdue_ByConfiguredGrace()
    {
        await using var db = ShipmentOpsTestDbContext.Create();
        var orderId = Guid.NewGuid();

        db.Set<Order>().Add(new Order { Id = orderId, OrderNumber = "ORD-TRACK", Currency = "EUR", Status = OrderStatus.Created });
        db.Set<Shipment>().AddRange(
            new Shipment
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                Carrier = "DHL",
                Service = "Parcel",
                Status = ShipmentStatus.Shipped,
                CreatedAtUtc = DateTime.UtcNow.AddHours(-16),
                ShippedAtUtc = DateTime.UtcNow.AddHours(-15),
                TrackingNumber = null
            },
            new Shipment
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                Carrier = "DHL",
                Service = "Parcel",
                Status = ShipmentStatus.Shipped,
                CreatedAtUtc = DateTime.UtcNow.AddHours(-6),
                ShippedAtUtc = DateTime.UtcNow.AddHours(-5),
                TrackingNumber = null
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetShipmentOpsSummaryHandler(db);

        var result = await handler.HandleAsync(
            attentionDelayHours: 24,
            trackingGraceHours: 12,
            ct: TestContext.Current.CancellationToken);

        result.DhlCount.Should().Be(2);
        result.TrackingOverdueCount.Should().Be(1);
        result.MissingTrackingCount.Should().Be(2);
    }

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
            });

            modelBuilder.Entity<ShipmentProviderOperation>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Provider).IsRequired();
                builder.Property(x => x.OperationType).IsRequired();
                builder.Property(x => x.Status).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });
        }
    }
}
