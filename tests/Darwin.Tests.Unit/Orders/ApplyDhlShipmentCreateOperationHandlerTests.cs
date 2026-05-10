using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Darwin.Application;
using Darwin.Application.Abstractions.Shipping;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Orders.DTOs;
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

public sealed class ApplyDhlShipmentCreateOperationHandlerTests
{
    [Fact]
    public async Task ApplyDhlShipmentCreateOperationHandler_Should_CreateCarrierMetadata_ButKeepShipmentOpen()
    {
        await using var db = ApplyDhlShipmentCreateOperationTestDbContext.Create();
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
            OrderNumber = "ORD-DHL-CREATE-APPLY-1",
            Currency = "EUR",
            Status = OrderStatus.Paid,
            BillingAddressJson = "{}",
            ShippingAddressJson = """{"FullName":"Ada Lovelace","Street1":"Teststrasse 12","PostalCode":"10115","City":"Berlin","CountryCode":"DE"}"""
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

        var handler = new ApplyDhlShipmentCreateOperationHandler(db, new FakeDhlShipmentProviderClient(), new FakeShipmentLabelStorage(), new TestStringLocalizer());

        var result = await handler.HandleAsync(shipmentId, TestContext.Current.CancellationToken);

        result.Status.Should().Be(ShipmentStatus.Pending);
        result.ProviderShipmentReference.Should().Be("00340434292135100100");
        result.TrackingNumber.Should().Be("00340434292135100100");
        result.LabelUrl.Should().Be("/uploads/dhl-label.pdf");
        result.LastCarrierEventKey.Should().Be("shipment.provider_created");
        result.TrackingUrl.Should().NotBeNull();

        var shipment = await db.Set<Shipment>().SingleAsync(x => x.Id == shipmentId, TestContext.Current.CancellationToken);
        shipment.Status.Should().Be(ShipmentStatus.Pending);
        shipment.ProviderShipmentReference.Should().Be(result.ProviderShipmentReference);
        shipment.TrackingNumber.Should().Be(result.TrackingNumber);
        shipment.LabelUrl.Should().Be("/uploads/dhl-label.pdf");
        shipment.LastCarrierEventKey.Should().Be("shipment.provider_created");

        var carrierEvent = await db.Set<ShipmentCarrierEvent>().SingleAsync(TestContext.Current.CancellationToken);
        carrierEvent.ShipmentId.Should().Be(shipmentId);
        carrierEvent.CarrierEventKey.Should().Be("shipment.provider_created");
        carrierEvent.ProviderStatus.Should().Be("Created");
        carrierEvent.ProviderShipmentReference.Should().Be(result.ProviderShipmentReference);

        var pendingLabelOperationCount = await db.Set<ShipmentProviderOperation>()
            .CountAsync(
                x => x.ShipmentId == shipmentId &&
                     x.Provider == "DHL" &&
                     x.OperationType == "GenerateLabel",
                TestContext.Current.CancellationToken);
        pendingLabelOperationCount.Should().Be(0);
    }

    [Fact]
    public async Task ApplyDhlShipmentCreateOperationHandler_Should_RejectNonDhlShipments()
    {
        await using var db = ApplyDhlShipmentCreateOperationTestDbContext.Create();
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
            OrderNumber = "ORD-DHL-CREATE-APPLY-2",
            Currency = "EUR",
            Status = OrderStatus.Paid,
            BillingAddressJson = "{}",
            ShippingAddressJson = """{"FullName":"Ada Lovelace","Street1":"Teststrasse 12","PostalCode":"10115","City":"Berlin","CountryCode":"DE"}"""
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

        var handler = new ApplyDhlShipmentCreateOperationHandler(db, new FakeDhlShipmentProviderClient(), new FakeShipmentLabelStorage(), new TestStringLocalizer());

        var act = () => handler.HandleAsync(shipmentId, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>();
    }

    private sealed class ApplyDhlShipmentCreateOperationTestDbContext : DbContext, IAppDbContext
    {
        private ApplyDhlShipmentCreateOperationTestDbContext(DbContextOptions<ApplyDhlShipmentCreateOperationTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static ApplyDhlShipmentCreateOperationTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<ApplyDhlShipmentCreateOperationTestDbContext>()
                .UseInMemoryDatabase($"darwin_apply_dhl_create_tests_{Guid.NewGuid()}")
                .Options;
            return new ApplyDhlShipmentCreateOperationTestDbContext(options);
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

    private sealed class FakeDhlShipmentProviderClient : IDhlShipmentProviderClient
    {
        public Task<DhlShipmentCreateResult> CreateShipmentAsync(SiteSetting settings, Order order, Shipment shipment, CheckoutAddressDto receiver, CancellationToken ct = default)
        {
            receiver.City.Should().Be("Berlin");
            return Task.FromResult(new DhlShipmentCreateResult
            {
                ProviderShipmentReference = "00340434292135100100",
                TrackingNumber = "00340434292135100100",
                LabelPdfBytes = [1, 2, 3]
            });
        }

        public Task<DhlShipmentLabelResult> GetLabelAsync(SiteSetting settings, Shipment shipment, CancellationToken ct = default)
            => Task.FromResult(new DhlShipmentLabelResult());

        public Task<DhlShipmentCreateResult> CreateReturnShipmentAsync(SiteSetting settings, Order order, Shipment shipment, CheckoutAddressDto returnSender, CancellationToken ct = default)
            => Task.FromResult(new DhlShipmentCreateResult());
    }

    private sealed class FakeShipmentLabelStorage : IShipmentLabelStorage
    {
        public Task<string> SaveLabelAsync(Guid shipmentId, string provider, byte[] content, string contentType, CancellationToken ct = default)
        {
            provider.Should().Be("DHL");
            content.Should().NotBeEmpty();
            return Task.FromResult("/uploads/dhl-label.pdf");
        }
    }
}
