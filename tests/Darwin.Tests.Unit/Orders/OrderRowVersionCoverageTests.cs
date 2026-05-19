using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application;
using Darwin.Application.Inventory.Commands;
using Darwin.Application.Inventory.Validators;
using Darwin.Application.Orders.Commands;
using Darwin.Application.Orders.DTOs;
using Darwin.Application.Orders.Validators;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.Integration;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Enums;
using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.Orders;

/// <summary>
/// Verifies that Order command handlers handle null database RowVersion values safely
/// (no NullReferenceException) — covering <see cref="ResolveShipmentCarrierExceptionHandler"/>,
/// <see cref="UpdateShipmentProviderOperationHandler"/>, and <see cref="UpdateOrderStatusHandler"/>.
/// </summary>
public sealed class OrderRowVersionCoverageTests
{
    private static readonly DateTime FixedNow = new(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    // ─────────────────────────────────────────────────────────────────────────
    // ResolveShipmentCarrierExceptionHandler – null DB RowVersion
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ResolveShipmentCarrierException_Should_Fail_WhenDbRowVersionIsNull()
    {
        await using var db = OrderNullRowVersionDbContext.Create();
        var shipmentId = Guid.NewGuid();
        // Seed a Shipment with null RowVersion to simulate an entity where the column was never populated.
        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = Guid.NewGuid(),
            Carrier = "DHL",
            Service = "Parcel"
            // RowVersion = null
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ResolveShipmentCarrierExceptionHandler(db, new FixedClock(FixedNow), new TestLocalizer());

        var result = await handler.HandleAsync(new ResolveShipmentCarrierExceptionDto
        {
            ShipmentId = shipmentId,
            RowVersion = [1, 2, 3], // non-empty client version vs null DB version
            ResolutionNote = "Carrier confirmed delivery"
        }, TestContext.Current.CancellationToken);

        // Handler normalises null DB RowVersion to [] and detects [] != [1,2,3]
        // so a failure result must be returned — never a NullReferenceException.
        result.Succeeded.Should().BeFalse("null DB RowVersion vs non-empty client version is a concurrency conflict");
        result.Error.Should().NotBeNullOrEmpty();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UpdateShipmentProviderOperationHandler – null DB RowVersion
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateShipmentProviderOperation_Should_Fail_WhenDbRowVersionIsNull()
    {
        await using var db = OrderNullRowVersionDbContext.Create();
        var operationId = Guid.NewGuid();
        db.Set<ShipmentProviderOperation>().Add(new ShipmentProviderOperation
        {
            Id = operationId,
            Provider = "DHL",
            OperationType = "CreateShipment",
            Status = "Pending"
            // RowVersion = null
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateShipmentProviderOperationHandler(db, new FixedClock(FixedNow), new TestLocalizer());

        var result = await handler.HandleAsync(new UpdateShipmentProviderOperationDto
        {
            Id = operationId,
            RowVersion = [5, 6, 7],
            Action = "Process"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("null DB RowVersion vs non-empty client version is a concurrency conflict");
        result.Error.Should().NotBeNullOrEmpty();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UpdateOrderStatusHandler – null DB RowVersion
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateOrderStatus_Should_Throw_WhenDbRowVersionIsNull()
    {
        await using var db = OrderNullRowVersionDbContext.Create();
        var orderId = Guid.NewGuid();
        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "ORD-NULL-RV",
            Currency = "EUR",
            Status = OrderStatus.Created,
            BillingAddressJson = "{}",
            ShippingAddressJson = "{}"
            // RowVersion = null
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new TestLocalizer();
        var validator = new UpdateOrderStatusValidator(localizer);
        var reserve = new ReserveInventoryHandler(db, localizer);
        var release = new ReleaseInventoryReservationHandler(db, localizer);
        var allocateValidator = new InventoryAllocateForOrderValidator(localizer);
        var allocate = new AllocateInventoryForOrderHandler(db, allocateValidator, localizer);
        var handler = new UpdateOrderStatusHandler(db, validator, localizer, reserve, release, allocate);

        var act = () => handler.HandleAsync(new UpdateOrderStatusDto
        {
            OrderId = orderId,
            RowVersion = [8, 9, 10],
            NewStatus = OrderStatus.Confirmed
        }, TestContext.Current.CancellationToken);

        // Handler normalises null DB RowVersion to [] and detects [] != [8,9,10]
        // so a ValidationException must be thrown — never a NullReferenceException.
        await act.Should().ThrowAsync<ValidationException>("null DB RowVersion vs non-empty client is a concurrency conflict");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Shared infrastructure
    // ─────────────────────────────────────────────────────────────────────────

    private sealed class FixedClock : IClock
    {
        private readonly DateTime _utcNow;
        public FixedClock(DateTime utcNow) { _utcNow = utcNow; }
        public DateTime UtcNow => _utcNow;
    }

    private sealed class OrderNullRowVersionDbContext : DbContext, IAppDbContext
    {
        private OrderNullRowVersionDbContext(DbContextOptions<OrderNullRowVersionDbContext> options)
            : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static OrderNullRowVersionDbContext Create()
        {
            var options = new DbContextOptionsBuilder<OrderNullRowVersionDbContext>()
                .UseInMemoryDatabase($"darwin_order_null_rowversion_tests_{Guid.NewGuid()}")
                .Options;
            return new OrderNullRowVersionDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Allow null RowVersion so we can seed entities without concurrency tokens.
            modelBuilder.Entity<Order>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.OrderNumber).IsRequired();
                b.Property(x => x.Currency).IsRequired();
                b.Property(x => x.BillingAddressJson).IsRequired(false);
                b.Property(x => x.ShippingAddressJson).IsRequired(false);
                b.Property(x => x.RowVersion);
                // Navigations NOT ignored — UpdateOrderStatusHandler does .Include(Lines/Payments/Shipments).
                b.HasMany(x => x.Lines).WithOne().HasForeignKey(l => l.OrderId);
                b.HasMany(x => x.Payments).WithOne().HasForeignKey(p => p.OrderId);
                b.HasMany(x => x.Shipments).WithOne().HasForeignKey(s => s.OrderId);
            });

            modelBuilder.Entity<OrderLine>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Name).IsRequired(false);
                b.Property(x => x.Sku).IsRequired(false);
                b.Property(x => x.AddOnValueIdsJson).IsRequired(false);
                b.Property(x => x.RowVersion);
            });

            modelBuilder.Entity<Payment>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Provider).IsRequired(false);
                b.Property(x => x.Currency).IsRequired(false);
                b.Property(x => x.RowVersion);
            });

            modelBuilder.Entity<Shipment>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Carrier).IsRequired();
                b.Property(x => x.Service).IsRequired();
                b.Property(x => x.RowVersion);
                b.Ignore(x => x.CarrierEvents);
                b.HasMany(x => x.Lines).WithOne().HasForeignKey(l => l.ShipmentId);
            });

            modelBuilder.Entity<ShipmentLine>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.RowVersion);
            });

            modelBuilder.Entity<ShipmentProviderOperation>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Provider).IsRequired();
                b.Property(x => x.OperationType).IsRequired();
                b.Property(x => x.Status).IsRequired();
                b.Property(x => x.RowVersion);
            });
        }
    }

    private sealed class TestLocalizer : IStringLocalizer<ValidationResource>
    {
        public LocalizedString this[string name] => new(name, name, resourceNotFound: false);

        public LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(name, arguments), resourceNotFound: false);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            Array.Empty<LocalizedString>();

        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
    }
}
