using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Darwin.Application;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Orders.Commands;
using Darwin.Application.Orders.DTOs;
using Darwin.Domain.Entities.Integration;
using Darwin.Application.Orders.Validators;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Entities.Settings;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.Orders;

public sealed class AddShipmentHandlerTests
{
    [Fact]
    public async Task AddShipmentHandler_Should_QueueDhlShipmentCreation_WhenDhlReadinessIsConfigured()
    {
        await using var db = AddShipmentTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var orderLineId = Guid.NewGuid();

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
            OrderNumber = "ORD-ADD-SHIP-1",
            Currency = "EUR",
            Status = OrderStatus.Paid,
            Lines = new List<OrderLine>
            {
                new()
                {
                    Id = orderLineId,
                    OrderId = orderId,
                    Name = "Coffee",
                    Sku = "SKU-1",
                    Quantity = 2,
                    UnitPriceNetMinor = 1000,
                    UnitPriceGrossMinor = 1190,
                    LineTaxMinor = 190,
                    LineGrossMinor = 2380,
                    VatRate = 0.19m
                }
            }
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new AddShipmentHandler(
            db,
            new ShipmentCreateValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        await handler.HandleAsync(new ShipmentCreateDto
        {
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            TotalWeight = 900,
            Lines = new List<ShipmentLineCreateDto>
            {
                new()
                {
                    OrderLineId = orderLineId,
                    Quantity = 1
                }
            }
        }, TestContext.Current.CancellationToken);

        var shipment = await db.Set<Shipment>().SingleAsync(TestContext.Current.CancellationToken);
        shipment.Status.Should().Be(ShipmentStatus.Pending);
        shipment.ProviderShipmentReference.Should().BeNull();
        shipment.TrackingNumber.Should().BeNull();
        shipment.LabelUrl.Should().BeNull();
        shipment.LastCarrierEventKey.Should().Be("shipment.provider_create_queued");

        var operation = await db.Set<ShipmentProviderOperation>().SingleAsync(TestContext.Current.CancellationToken);
        operation.ShipmentId.Should().Be(shipment.Id);
        operation.Provider.Should().Be("DHL");
        operation.OperationType.Should().Be("CreateShipment");
        operation.Status.Should().Be("Pending");

        db.Set<ShipmentCarrierEvent>().Should().BeEmpty();
    }

    private sealed class AddShipmentTestDbContext : DbContext, IAppDbContext
    {
        private AddShipmentTestDbContext(DbContextOptions<AddShipmentTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static AddShipmentTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<AddShipmentTestDbContext>()
                .UseInMemoryDatabase($"darwin_add_shipment_tests_{Guid.NewGuid()}")
                .Options;
            return new AddShipmentTestDbContext(options);
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
                builder.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.OrderId);
            });

            modelBuilder.Entity<OrderLine>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Name).IsRequired();
                builder.Property(x => x.Sku).IsRequired();
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
