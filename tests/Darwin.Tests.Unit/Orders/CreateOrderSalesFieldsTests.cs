using Darwin.Application;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Orders.Commands;
using Darwin.Application.Orders.DTOs;
using Darwin.Application.Orders.Validators;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.Orders;

public sealed class CreateOrderSalesFieldsTests
{
    [Fact]
    public async Task CreateOrderHandler_Should_Persist_Additive_Sales_Reporting_Fields()
    {
        await using var db = CreateOrderSalesFieldsDbContext.Create();
        var nowUtc = new DateTime(2026, 6, 11, 12, 30, 0, DateTimeKind.Utc);
        var businessId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var handler = new CreateOrderHandler(
            db,
            new FixedClock(nowUtc),
            new OrderCreateValidator(new TestStringLocalizer()));

        var orderId = await handler.HandleAsync(new OrderCreateDto
        {
            UserId = userId,
            BusinessId = businessId,
            CustomerId = customerId,
            Currency = "EUR",
            BillingAddressJson = "{\"line1\":\"billing snapshot\"}",
            ShippingAddressJson = "{\"line1\":\"shipping snapshot\"}",
            Lines =
            [
                new OrderLineCreateDto
                {
                    VariantId = Guid.NewGuid(),
                    Name = "Admin line",
                    Sku = "ADM-001",
                    Quantity = 2,
                    UnitPriceNetMinor = 1000,
                    VatRate = 0.19m
                }
            ]
        }, TestContext.Current.CancellationToken);

        var order = await db.Set<Order>().SingleAsync(x => x.Id == orderId, TestContext.Current.CancellationToken);

        order.UserId.Should().Be(userId);
        order.BusinessId.Should().Be(businessId);
        order.CustomerId.Should().Be(customerId);
        order.SalesChannel.Should().Be(SalesChannel.Admin);
        order.OrderedAtUtc.Should().Be(nowUtc);
        order.BillingAddressJson.Should().Be("{\"line1\":\"billing snapshot\"}");
        order.ShippingAddressJson.Should().Be("{\"line1\":\"shipping snapshot\"}");
    }

    private sealed class CreateOrderSalesFieldsDbContext : DbContext, IAppDbContext
    {
        private CreateOrderSalesFieldsDbContext(DbContextOptions<CreateOrderSalesFieldsDbContext> options)
            : base(options)
        {
        }

        public static CreateOrderSalesFieldsDbContext Create()
        {
            var options = new DbContextOptionsBuilder<CreateOrderSalesFieldsDbContext>()
                .UseInMemoryDatabase($"darwin_create_order_sales_fields_{Guid.NewGuid():N}")
                .Options;
            return new CreateOrderSalesFieldsDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Order>();
            modelBuilder.Entity<OrderLine>();
        }
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTime utcNow) => UtcNow = utcNow;

        public DateTime UtcNow { get; }
    }

    private sealed class TestStringLocalizer : IStringLocalizer<ValidationResource>
    {
        public LocalizedString this[string name] => new(name, name, resourceNotFound: false);

        public LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(name, arguments), resourceNotFound: false);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => Array.Empty<LocalizedString>();

        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
    }
}
