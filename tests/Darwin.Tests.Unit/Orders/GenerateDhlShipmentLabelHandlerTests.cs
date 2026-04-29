using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Darwin.Application;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Orders.Commands;
using Darwin.Domain.Entities.Integration;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Entities.Settings;
using Darwin.Domain.Enums;
using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.Orders;

public sealed class GenerateDhlShipmentLabelHandlerTests
{
    [Fact]
    public async Task GenerateDhlShipmentLabelHandler_Should_QueueShipmentProviderOperation_ForDhlShipment()
    {
        await using var db = GenerateDhlShipmentLabelTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();

        db.Set<SiteSetting>().Add(new SiteSetting
        {
            Title = "Darwin",
            ContactEmail = "ops@darwin.de",
            DhlEnabled = true,
            DhlApiBaseUrl = "https://api-sandbox.dhl.example",
            DhlApiKey = "key",
            DhlApiSecret = "secret",
            DhlAccountNumber = "22222222220101",
            DhlShipperName = "Darwin Ops",
            DhlShipperEmail = "ops@darwin.de",
            DhlShipperPhoneE164 = "+4915112345678",
            DhlShipperStreet = "Musterstrasse 1",
            DhlShipperPostalCode = "10115",
            DhlShipperCity = "Berlin",
            DhlShipperCountry = "DE"
        });

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-DHL-LABEL-1",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            Status = ShipmentStatus.Pending
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GenerateDhlShipmentLabelHandler(db, new TestStringLocalizer());

        await handler.HandleAsync(shipmentId, null, TestContext.Current.CancellationToken);

        var shipment = await db.Set<Shipment>().SingleAsync(x => x.Id == shipmentId, TestContext.Current.CancellationToken);
        shipment.Status.Should().Be(ShipmentStatus.Pending);
        shipment.ProviderShipmentReference.Should().BeNull();
        shipment.TrackingNumber.Should().BeNull();
        shipment.LabelUrl.Should().BeNull();
        shipment.LastCarrierEventKey.Should().BeNull();

        var operation = await db.Set<ShipmentProviderOperation>().SingleAsync(TestContext.Current.CancellationToken);
        operation.ShipmentId.Should().Be(shipmentId);
        operation.Provider.Should().Be("DHL");
        operation.OperationType.Should().Be("GenerateLabel");
        operation.Status.Should().Be("Pending");

        db.Set<ShipmentCarrierEvent>().Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateDhlShipmentLabelHandler_Should_RejectNonDhlShipments()
    {
        await using var db = GenerateDhlShipmentLabelTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();

        db.Set<SiteSetting>().Add(new SiteSetting
        {
            Title = "Darwin",
            ContactEmail = "ops@darwin.de",
            DhlEnabled = true,
            DhlApiBaseUrl = "https://api-sandbox.dhl.example",
            DhlApiKey = "key",
            DhlApiSecret = "secret",
            DhlAccountNumber = "22222222220101",
            DhlShipperName = "Darwin Ops",
            DhlShipperEmail = "ops@darwin.de",
            DhlShipperPhoneE164 = "+4915112345678",
            DhlShipperStreet = "Musterstrasse 1",
            DhlShipperPostalCode = "10115",
            DhlShipperCity = "Berlin",
            DhlShipperCountry = "DE"
        });

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-DHL-LABEL-2",
            Currency = "EUR",
            Status = OrderStatus.Paid
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "UPS",
            Service = "Ground",
            Status = ShipmentStatus.Pending
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GenerateDhlShipmentLabelHandler(db, new TestStringLocalizer());

        var act = () => handler.HandleAsync(shipmentId, null, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>();
    }

    private sealed class GenerateDhlShipmentLabelTestDbContext : DbContext, IAppDbContext
    {
        private GenerateDhlShipmentLabelTestDbContext(DbContextOptions<GenerateDhlShipmentLabelTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static GenerateDhlShipmentLabelTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<GenerateDhlShipmentLabelTestDbContext>()
                .UseInMemoryDatabase($"darwin_generate_dhl_label_tests_{Guid.NewGuid()}")
                .Options;
            return new GenerateDhlShipmentLabelTestDbContext(options);
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

            modelBuilder.Entity<ShipmentProviderOperation>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Provider).IsRequired();
                builder.Property(x => x.OperationType).IsRequired();
                builder.Property(x => x.Status).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<SiteSetting>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Title).IsRequired();
                builder.Property(x => x.ContactEmail).IsRequired();
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
