using Darwin.Application.Orders.DTOs;
using Darwin.Application.Orders.Queries;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Enums;
using Darwin.Infrastructure.Persistence.Db;
using Darwin.Tests.Integration.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Darwin.Tests.Integration.Orders;

public sealed class OrdersSearchProviderNeutralIntegrationTests
    : DeterministicIntegrationTestBase,
      IClassFixture<WebApplicationFactory<Program>>
{
    public OrdersSearchProviderNeutralIntegrationTests(WebApplicationFactory<Program> factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GetOrdersPage_Should_HandleEscapedSubstringAndCaseVariants_OnOrderNumberSearch()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DarwinDbContext>();

        var marker = Guid.NewGuid().ToString("N");
        var exactMatchNumber = $"order_%_probe[{marker}]";
        var unrelatedNumber = $"orderXprobe[{marker.Substring(0, 6)}]";
        var matchOrder = new Order { OrderNumber = exactMatchNumber, Currency = "EUR" };
        var unrelatedOrder = new Order { OrderNumber = unrelatedNumber, Currency = "EUR" };

        db.Set<Order>().AddRange(matchOrder, unrelatedOrder);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = scope.ServiceProvider.GetRequiredService<GetOrdersPageHandler>();
        var lowerCaseResult = await handler.HandleAsync(
            page: 1,
            pageSize: 20,
            query: exactMatchNumber,
            filter: OrderQueueFilter.All,
            ct: TestContext.Current.CancellationToken);

        var upperCaseResult = await handler.HandleAsync(
            page: 1,
            pageSize: 20,
            query: exactMatchNumber.ToUpperInvariant(),
            filter: OrderQueueFilter.All,
            ct: TestContext.Current.CancellationToken);

        lowerCaseResult.Total.Should().Be(1);
        lowerCaseResult.Items.Should().ContainSingle(x => x.Id == matchOrder.Id);

        upperCaseResult.Total.Should().Be(1);
        upperCaseResult.Items.Should().ContainSingle(x => x.Id == matchOrder.Id);
    }

    [Fact]
    public async Task GetShipmentsPage_Should_HandleEscapedSubstringAndCaseVariants_OnTrackingNumberSearch()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DarwinDbContext>();

        var marker = Guid.NewGuid().ToString("N");
        var exactMatchTracking = $"track_%_probe[{marker}]";
        var unrelatedTracking = $"trackXprobe[{marker.Substring(0, 6)}]";
        var order = new Order { OrderNumber = $"ord-{marker}", Currency = "EUR" };

        db.Set<Order>().Add(order);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        db.Set<Shipment>().AddRange(
            new Shipment
            {
                OrderId = order.Id,
                Carrier = "DHL",
                TrackingNumber = exactMatchTracking
            },
            new Shipment
            {
                OrderId = order.Id,
                Carrier = "DHL",
                TrackingNumber = unrelatedTracking
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = scope.ServiceProvider.GetRequiredService<GetShipmentsPageHandler>();
        var lowerCaseResult = await handler.HandleAsync(
            page: 1,
            pageSize: 20,
            query: $"track_%_probe[{marker}]",
            filter: ShipmentQueueFilter.All,
            ct: TestContext.Current.CancellationToken);

        var upperCaseResult = await handler.HandleAsync(
            page: 1,
            pageSize: 20,
            query: $"TRACK_%_PROBE[{marker}]",
            filter: ShipmentQueueFilter.All,
            ct: TestContext.Current.CancellationToken);

        lowerCaseResult.Total.Should().Be(1);
        lowerCaseResult.Items.Should().ContainSingle(x => x.TrackingNumber == exactMatchTracking);

        upperCaseResult.Total.Should().Be(1);
        upperCaseResult.Items.Should().ContainSingle(x => x.TrackingNumber == exactMatchTracking);
    }

    [Fact]
    public async Task GetShipmentProviderOperationsPage_Should_HandleEscapedSubstringAndCaseVariants_OnProviderSearch()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DarwinDbContext>();

        var marker = Guid.NewGuid().ToString("N");
        var exactMatchProvider = $"provider_%_probe[{marker}]";
        var unrelatedProvider = $"providerXprobe[{marker.Substring(0, 6)}]";
        var order = new Order { OrderNumber = $"op-{marker}", Currency = "EUR" };
        db.Set<Order>().Add(order);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var matchingShipment = new Shipment { OrderId = order.Id, Carrier = "DHL" };
        var unrelatedShipment = new Shipment { OrderId = order.Id, Carrier = "DHL" };
        db.Set<Shipment>().AddRange(matchingShipment, unrelatedShipment);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        db.Set<ShipmentProviderOperation>().AddRange(
            new ShipmentProviderOperation
            {
                ShipmentId = matchingShipment.Id,
                Provider = exactMatchProvider,
                OperationType = "CreateLabel"
            },
            new ShipmentProviderOperation
            {
                ShipmentId = unrelatedShipment.Id,
                Provider = unrelatedProvider,
                OperationType = "CreateLabel"
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = scope.ServiceProvider.GetRequiredService<GetShipmentProviderOperationsPageHandler>();
        var lowerCaseResult = await handler.HandleAsync(
            page: 1,
            pageSize: 20,
            filter: new ShipmentProviderOperationFilterDto { Query = $"provider_%_probe[{marker}]" },
            ct: TestContext.Current.CancellationToken);

        var upperCaseResult = await handler.HandleAsync(
            page: 1,
            pageSize: 20,
            filter: new ShipmentProviderOperationFilterDto { Query = $"PROVIDER_%_PROBE[{marker}]" },
            ct: TestContext.Current.CancellationToken);

        lowerCaseResult.Total.Should().Be(1);
        lowerCaseResult.Items.Should().ContainSingle(x => x.Provider == exactMatchProvider);

        upperCaseResult.Total.Should().Be(1);
        upperCaseResult.Items.Should().ContainSingle(x => x.Provider == exactMatchProvider);
    }
}
