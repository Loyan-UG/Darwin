using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Darwin.Application;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Shipping;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Orders.Commands;
using Darwin.Application.Orders.DTOs;
using Darwin.Domain.Entities.Integration;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Entities.Settings;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Moq;
using Darwin.Worker;

namespace Darwin.WebApi.Tests.Services;

public sealed class ShipmentProviderOperationBackgroundServiceTests
{
    [Fact]
    public void Ctor_Should_Throw_WhenDependenciesAreMissing()
    {
        var options = Options.Create(new ShipmentProviderOperationWorkerOptions());
        var clock = new FixedClock(DateTime.UtcNow);

        Action noScopeFactory = () =>
            new ShipmentProviderOperationBackgroundService(
                null!,
                options,
                clock,
                new Mock<ILogger<ShipmentProviderOperationBackgroundService>>().Object);

        Action noOptions = () =>
            new ShipmentProviderOperationBackgroundService(
                new Mock<IServiceScopeFactory>().Object,
                null!,
                clock,
                new Mock<ILogger<ShipmentProviderOperationBackgroundService>>().Object);

        Action noClock = () =>
            new ShipmentProviderOperationBackgroundService(
                new Mock<IServiceScopeFactory>().Object,
                options,
                null!,
                new Mock<ILogger<ShipmentProviderOperationBackgroundService>>().Object);

        Action noLogger = () =>
            new ShipmentProviderOperationBackgroundService(
                new Mock<IServiceScopeFactory>().Object,
                options,
                clock,
                null!);

        noScopeFactory.Should().Throw<ArgumentNullException>().WithParameterName("scopeFactory");
        noOptions.Should().Throw<ArgumentNullException>().WithParameterName("options");
        noClock.Should().Throw<ArgumentNullException>().WithParameterName("clock");
        noLogger.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task ExecuteAsync_Should_ProcessSuccessfulShipmentOperation_AsProcessed()
    {
        var nowUtc = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var operationId = Guid.NewGuid();

        await using var db = ShipmentProviderOperationBackgroundServiceTestDbContext.Create();
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
            OrderNumber = "ORD-SHIP-OP",
            Currency = "EUR",
            BillingAddressJson = "{}",
            ShippingAddressJson = """{"FullName":"Ada Lovelace","Street1":"Teststrasse 12","PostalCode":"10115","City":"Berlin","CountryCode":"DE"}"""
        });

        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel"
        });

        db.Set<ShipmentProviderOperation>().Add(new ShipmentProviderOperation
        {
            Id = operationId,
            ShipmentId = shipmentId,
            Provider = "DHL",
            OperationType = "CreateShipment",
            Status = "Pending"
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyDhlShipmentCreateOperationHandler(
            db,
            new FakeDhlShipmentProviderClient(),
            new FakeShipmentLabelStorage(),
            new TestStringLocalizer(),
            new FixedClock(nowUtc));

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);
        serviceProvider.Setup(x => x.GetService(typeof(ApplyDhlShipmentCreateOperationHandler))).Returns(handler);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ShipmentProviderOperationBackgroundService(
            scopeFactory.Object,
            Options.Create(new ShipmentProviderOperationWorkerOptions
            {
                Enabled = true,
                BatchSize = 5,
                MaxAttempts = 1
            }),
            new FixedClock(nowUtc),
            new Mock<ILogger<ShipmentProviderOperationBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ShipmentProviderOperation>()
            .SingleAsync(x => x.Id == operationId, TestContext.Current.CancellationToken);

        item.Status.Should().Be("Processed");
        item.AttemptCount.Should().Be(1);
        item.ProcessedAtUtc.Should().Be(nowUtc);
        item.FailureReason.Should().BeNullOrEmpty();
    }

    private sealed class FixedClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }

    private sealed class ShipmentProviderOperationBackgroundServiceTestDbContext : DbContext, IAppDbContext
    {
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<Shipment> Shipments { get; set; } = null!;
        public DbSet<ShipmentCarrierEvent> ShipmentCarrierEvents { get; set; } = null!;
        public DbSet<ShipmentProviderOperation> ShipmentProviderOperations { get; set; } = null!;
        public DbSet<SiteSetting> SiteSettings { get; set; } = null!;

        private ShipmentProviderOperationBackgroundServiceTestDbContext(
            DbContextOptions<ShipmentProviderOperationBackgroundServiceTestDbContext> options)
            : base(options)
        {
        }

        public static ShipmentProviderOperationBackgroundServiceTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<ShipmentProviderOperationBackgroundServiceTestDbContext>()
                .UseInMemoryDatabase($"darwin_shipment_operation_worker_tests_{Guid.NewGuid()}")
                .Options;

            return new ShipmentProviderOperationBackgroundServiceTestDbContext(options);
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();
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
            contentType.Should().Be("application/pdf");
            return Task.FromResult("/uploads/dhl-label.pdf");
        }
    }

    private sealed class TestStringLocalizer : IStringLocalizer<ValidationResource>
    {
        public LocalizedString this[string name] => new(name, name, resourceNotFound: false);

        public LocalizedString this[string name, params object[] arguments]
            => new(name, string.Format(name, arguments), resourceNotFound: false);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => Array.Empty<LocalizedString>();

        public IStringLocalizer WithCulture(CultureInfo culture)
            => this;
    }
}
