using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Shipping.Commands;
using Darwin.Application.Shipping.DTOs;
using Darwin.Application.Shipping.Queries;
using Darwin.Application.Shipping.Validators;
using Darwin.Domain.Entities.Shipping;
using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Moq;

namespace Darwin.Tests.Unit.Shipping;

/// <summary>
/// Handler-level unit tests for the Shipping module.
/// Covers <see cref="CreateShippingMethodHandler"/>, <see cref="UpdateShippingMethodHandler"/>,
/// <see cref="RateShipmentHandler"/>, <see cref="GetShippingMethodForEditHandler"/>,
/// <see cref="GetShippingMethodsPageHandler"/>, and <see cref="GetShippingMethodOpsSummaryHandler"/>.
/// </summary>
public sealed class ShippingHandlerTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // Shared helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static IStringLocalizer<Darwin.Application.ValidationResource> CreateLocalizer()
    {
        var mock = new Mock<IStringLocalizer<Darwin.Application.ValidationResource>>();
        mock.Setup(l => l[It.IsAny<string>()])
            .Returns<string>(name => new LocalizedString(name, name));
        mock.Setup(l => l[It.IsAny<string>(), It.IsAny<object[]>()])
            .Returns<string, object[]>((name, _) => new LocalizedString(name, name));
        return mock.Object;
    }

    private static ShippingMethod BuildMethod(
        string name = "Standard Shipping",
        string carrier = "DHL",
        string service = "PARCEL",
        bool isActive = true,
        string? countriesCsv = null,
        string? currency = null,
        byte[]? rowVersion = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Carrier = carrier,
            Service = service,
            IsActive = isActive,
            CountriesCsv = countriesCsv,
            Currency = currency,
            RowVersion = rowVersion ?? new byte[] { 1 }
        };

    // ─────────────────────────────────────────────────────────────────────────
    // CreateShippingMethodHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateShippingMethod_Should_Throw_ValidationException_When_Dto_Invalid()
    {
        await using var db = ShippingTestDbContext.Create();
        var handler = new CreateShippingMethodHandler(
            db, new ShippingMethodCreateValidator(), CreateLocalizer());

        var act = () => handler.HandleAsync(
            new ShippingMethodCreateDto { Name = "", Carrier = "DHL", Service = "PARCEL" },
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("empty Name violates the create validator");
    }

    [Fact]
    public async Task CreateShippingMethod_Should_Throw_ValidationException_When_Carrier_And_Service_Already_Exist()
    {
        await using var db = ShippingTestDbContext.Create();
        db.Set<ShippingMethod>().Add(BuildMethod(carrier: "DHL", service: "PARCEL"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateShippingMethodHandler(
            db, new ShippingMethodCreateValidator(), CreateLocalizer());

        var act = () => handler.HandleAsync(
            new ShippingMethodCreateDto { Name = "Another DHL Parcel", Carrier = "DHL", Service = "PARCEL" },
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>(
            "duplicate Carrier+Service combination must be rejected");
    }

    [Fact]
    public async Task CreateShippingMethod_Should_Persist_Method_Successfully()
    {
        await using var db = ShippingTestDbContext.Create();
        var handler = new CreateShippingMethodHandler(
            db, new ShippingMethodCreateValidator(), CreateLocalizer());

        await handler.HandleAsync(new ShippingMethodCreateDto
        {
            Name = "Express",
            Carrier = "DHL",
            Service = "EXPRESS",
            IsActive = true,
            Currency = "EUR",
            Rates = new List<ShippingRateDto>
            {
                new() { SortOrder = 0, PriceMinor = 999 }
            }
        }, TestContext.Current.CancellationToken);

        var method = db.Set<ShippingMethod>().Include(m => m.Rates).Single();
        method.Name.Should().Be("Express");
        method.Carrier.Should().Be("DHL");
        method.Service.Should().Be("EXPRESS");
        method.Currency.Should().Be("EUR");
        method.IsActive.Should().BeTrue();
        method.Rates.Should().HaveCount(1);
        method.Rates.Single().PriceMinor.Should().Be(999);
    }

    [Fact]
    public async Task CreateShippingMethod_Should_Trim_Name_And_Carrier()
    {
        await using var db = ShippingTestDbContext.Create();
        var handler = new CreateShippingMethodHandler(
            db, new ShippingMethodCreateValidator(), CreateLocalizer());

        await handler.HandleAsync(new ShippingMethodCreateDto
        {
            Name = "  Standard  ",
            Carrier = "  DHL  ",
            Service = "  PARCEL  ",
            Rates = new List<ShippingRateDto>()
        }, TestContext.Current.CancellationToken);

        var method = db.Set<ShippingMethod>().Single();
        method.Name.Should().Be("Standard", "Name must be trimmed on create");
        method.Carrier.Should().Be("DHL", "Carrier must be trimmed on create");
        method.Service.Should().Be("PARCEL", "Service must be trimmed on create");
    }

    [Fact]
    public async Task CreateShippingMethod_Should_Order_Rates_By_SortOrder()
    {
        await using var db = ShippingTestDbContext.Create();
        var handler = new CreateShippingMethodHandler(
            db, new ShippingMethodCreateValidator(), CreateLocalizer());

        await handler.HandleAsync(new ShippingMethodCreateDto
        {
            Name = "Tiered",
            Carrier = "DHL",
            Service = "PARCEL",
            Rates = new List<ShippingRateDto>
            {
                new() { SortOrder = 2, PriceMinor = 799 },
                new() { SortOrder = 0, PriceMinor = 299 },
                new() { SortOrder = 1, PriceMinor = 499 }
            }
        }, TestContext.Current.CancellationToken);

        var rates = db.Set<ShippingMethod>()
            .Include(m => m.Rates)
            .Single()
            .Rates
            .OrderBy(r => r.SortOrder)
            .ToList();

        rates[0].PriceMinor.Should().Be(299);
        rates[1].PriceMinor.Should().Be(499);
        rates[2].PriceMinor.Should().Be(799);
    }

    [Fact]
    public async Task CreateShippingMethod_Should_Store_Null_Currency_When_Currency_Is_Whitespace()
    {
        await using var db = ShippingTestDbContext.Create();
        var handler = new CreateShippingMethodHandler(
            db, new ShippingMethodCreateValidator(), CreateLocalizer());

        await handler.HandleAsync(new ShippingMethodCreateDto
        {
            Name = "Standard",
            Carrier = "DHL",
            Service = "PARCEL",
            Currency = "   ",
            Rates = new List<ShippingRateDto>()
        }, TestContext.Current.CancellationToken);

        var method = db.Set<ShippingMethod>().Single();
        method.Currency.Should().BeNull("whitespace-only currency should be stored as null");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UpdateShippingMethodHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateShippingMethod_Should_Throw_ValidationException_When_Dto_Invalid()
    {
        await using var db = ShippingTestDbContext.Create();
        var handler = new UpdateShippingMethodHandler(
            db, new ShippingMethodEditValidator(), CreateLocalizer());

        var act = () => handler.HandleAsync(
            new ShippingMethodEditDto
            {
                Id = Guid.Empty,
                RowVersion = new byte[] { 1 },
                Name = "Valid",
                Carrier = "DHL",
                Service = "PARCEL"
            },
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("empty Id violates the edit validator");
    }

    [Fact]
    public async Task UpdateShippingMethod_Should_Throw_InvalidOperationException_When_Method_Not_Found()
    {
        await using var db = ShippingTestDbContext.Create();
        var handler = new UpdateShippingMethodHandler(
            db, new ShippingMethodEditValidator(), CreateLocalizer());

        var act = () => handler.HandleAsync(
            new ShippingMethodEditDto
            {
                Id = Guid.NewGuid(),
                RowVersion = new byte[] { 1 },
                Name = "Ghost",
                Carrier = "DHL",
                Service = "PARCEL"
            },
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>("updating a missing method must fail");
    }

    [Fact]
    public async Task UpdateShippingMethod_Should_Throw_DbUpdateConcurrencyException_When_RowVersion_Mismatch()
    {
        await using var db = ShippingTestDbContext.Create();
        var method = BuildMethod(rowVersion: new byte[] { 1, 2, 3, 4 });
        db.Set<ShippingMethod>().Add(method);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateShippingMethodHandler(
            db, new ShippingMethodEditValidator(), CreateLocalizer());

        var act = () => handler.HandleAsync(
            new ShippingMethodEditDto
            {
                Id = method.Id,
                RowVersion = new byte[] { 9, 9, 9, 9 }, // stale
                Name = "Updated",
                Carrier = "DHL",
                Service = "PARCEL"
            },
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>(
            "a stale RowVersion must trigger a concurrency exception");
    }

    [Fact]
    public async Task UpdateShippingMethod_Should_Throw_ValidationException_When_Carrier_Service_Conflicts_With_Another()
    {
        await using var db = ShippingTestDbContext.Create();
        var existing = BuildMethod("Express", "DHL", "EXPRESS");
        var target = BuildMethod("Standard", "DHL", "PARCEL", rowVersion: new byte[] { 7 });
        db.Set<ShippingMethod>().AddRange(existing, target);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateShippingMethodHandler(
            db, new ShippingMethodEditValidator(), CreateLocalizer());

        var act = () => handler.HandleAsync(
            new ShippingMethodEditDto
            {
                Id = target.Id,
                RowVersion = new byte[] { 7 },
                Name = "Updated",
                Carrier = "DHL",
                Service = "EXPRESS" // conflicts with existing
            },
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>(
            "renaming to a carrier+service already used by another method must be rejected");
    }

    [Fact]
    public async Task UpdateShippingMethod_Should_Persist_Changes_Successfully()
    {
        await using var db = ShippingTestDbContext.Create();
        var rowVersion = new byte[] { 5 };
        var method = BuildMethod(rowVersion: rowVersion);
        db.Set<ShippingMethod>().Add(method);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateShippingMethodHandler(
            db, new ShippingMethodEditValidator(), CreateLocalizer());

        await handler.HandleAsync(new ShippingMethodEditDto
        {
            Id = method.Id,
            RowVersion = rowVersion,
            Name = "Updated Name",
            Carrier = "DHL",
            Service = "EXPRESS",
            IsActive = false,
            Currency = "USD",
            Rates = new List<ShippingRateDto>
            {
                new() { SortOrder = 0, PriceMinor = 1999 }
            }
        }, TestContext.Current.CancellationToken);

        var updated = db.Set<ShippingMethod>().Include(m => m.Rates).Single();
        updated.Name.Should().Be("Updated Name");
        updated.Service.Should().Be("EXPRESS");
        updated.IsActive.Should().BeFalse();
        updated.Currency.Should().Be("USD");
        updated.Rates.Should().HaveCount(1);
        updated.Rates.Single().PriceMinor.Should().Be(1999);
    }

    [Fact]
    public async Task UpdateShippingMethod_Should_Replace_All_Existing_Rates()
    {
        await using var db = ShippingTestDbContext.Create();
        var rowVersion = new byte[] { 6 };
        var method = BuildMethod(rowVersion: rowVersion);
        method.Rates.Add(new ShippingRate { Id = Guid.NewGuid(), PriceMinor = 500, SortOrder = 0 });
        method.Rates.Add(new ShippingRate { Id = Guid.NewGuid(), PriceMinor = 700, SortOrder = 1 });
        db.Set<ShippingMethod>().Add(method);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateShippingMethodHandler(
            db, new ShippingMethodEditValidator(), CreateLocalizer());

        await handler.HandleAsync(new ShippingMethodEditDto
        {
            Id = method.Id,
            RowVersion = rowVersion,
            Name = method.Name,
            Carrier = method.Carrier,
            Service = method.Service,
            Rates = new List<ShippingRateDto>
            {
                new() { SortOrder = 0, PriceMinor = 999 }
            }
        }, TestContext.Current.CancellationToken);

        var updated = db.Set<ShippingMethod>().Include(m => m.Rates).Single();
        updated.Rates.Should().HaveCount(1, "old rates are replaced by the new rate list");
        updated.Rates.Single().PriceMinor.Should().Be(999);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // RateShipmentHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task RateShipment_Should_Throw_ValidationException_When_Country_Invalid()
    {
        await using var db = ShippingTestDbContext.Create();
        var handler = new RateShipmentHandler(db);

        var act = () => handler.HandleAsync(
            new RateShipmentInputDto { Country = "", SubtotalNetMinor = 1000, ShipmentMass = 500 },
            "EUR",
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("an empty Country violates the input validator");
    }

    [Fact]
    public async Task RateShipment_Should_Return_Empty_When_No_Active_Methods_Exist()
    {
        await using var db = ShippingTestDbContext.Create();
        var handler = new RateShipmentHandler(db);

        var options = await handler.HandleAsync(
            new RateShipmentInputDto { Country = "DE", SubtotalNetMinor = 1000, ShipmentMass = 500 },
            "EUR",
            TestContext.Current.CancellationToken);

        options.Should().BeEmpty("no shipping methods exist");
    }

    [Fact]
    public async Task RateShipment_Should_Return_Empty_When_Method_Is_Inactive()
    {
        await using var db = ShippingTestDbContext.Create();
        var method = BuildMethod(isActive: false);
        method.Rates.Add(new ShippingRate { Id = Guid.NewGuid(), PriceMinor = 499, SortOrder = 0 });
        db.Set<ShippingMethod>().Add(method);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new RateShipmentHandler(db);
        var options = await handler.HandleAsync(
            new RateShipmentInputDto { Country = "DE", SubtotalNetMinor = 1000, ShipmentMass = 500 },
            "EUR",
            TestContext.Current.CancellationToken);

        options.Should().BeEmpty("inactive methods must not appear in shipping options");
    }

    [Fact]
    public async Task RateShipment_Should_Return_Option_When_Active_Method_Has_Matching_Rate()
    {
        await using var db = ShippingTestDbContext.Create();
        var method = BuildMethod("DHL Parcel", "DHL", "PARCEL", isActive: true, currency: "EUR");
        method.Rates.Add(new ShippingRate { Id = Guid.NewGuid(), PriceMinor = 499, SortOrder = 0 });
        db.Set<ShippingMethod>().Add(method);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new RateShipmentHandler(db);
        var options = await handler.HandleAsync(
            new RateShipmentInputDto { Country = "DE", SubtotalNetMinor = 5000, ShipmentMass = 1000 },
            "EUR",
            TestContext.Current.CancellationToken);

        options.Should().HaveCount(1);
        options[0].PriceMinor.Should().Be(499);
        options[0].Currency.Should().Be("EUR");
        options[0].Carrier.Should().Be("DHL");
    }

    [Fact]
    public async Task RateShipment_Should_Respect_MaxShipmentMass_Cap()
    {
        await using var db = ShippingTestDbContext.Create();
        var method = BuildMethod("DHL Parcel", "DHL", "PARCEL", isActive: true);
        // Only rate: capped at 2000g
        method.Rates.Add(new ShippingRate
        {
            Id = Guid.NewGuid(),
            PriceMinor = 499,
            SortOrder = 0,
            MaxShipmentMass = 2000
        });
        db.Set<ShippingMethod>().Add(method);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new RateShipmentHandler(db);

        // Shipment mass exceeds cap – no option should match
        var optionsHeavy = await handler.HandleAsync(
            new RateShipmentInputDto { Country = "DE", SubtotalNetMinor = 1000, ShipmentMass = 2001 },
            "EUR",
            TestContext.Current.CancellationToken);
        optionsHeavy.Should().BeEmpty("shipment exceeds the mass cap of the only rate");

        // Shipment mass within cap – option should match
        var optionsLight = await handler.HandleAsync(
            new RateShipmentInputDto { Country = "DE", SubtotalNetMinor = 1000, ShipmentMass = 1500 },
            "EUR",
            TestContext.Current.CancellationToken);
        optionsLight.Should().HaveCount(1, "shipment is within the mass cap");
    }

    [Fact]
    public async Task RateShipment_Should_Respect_Country_Restriction()
    {
        await using var db = ShippingTestDbContext.Create();
        var method = BuildMethod("DE Only", "DHL", "PARCEL", isActive: true, countriesCsv: "DE");
        method.Rates.Add(new ShippingRate { Id = Guid.NewGuid(), PriceMinor = 599, SortOrder = 0 });
        db.Set<ShippingMethod>().Add(method);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new RateShipmentHandler(db);

        var deOptions = await handler.HandleAsync(
            new RateShipmentInputDto { Country = "DE", SubtotalNetMinor = 1000, ShipmentMass = 500 },
            "EUR",
            TestContext.Current.CancellationToken);
        deOptions.Should().HaveCount(1, "method serves DE");

        var atOptions = await handler.HandleAsync(
            new RateShipmentInputDto { Country = "AT", SubtotalNetMinor = 1000, ShipmentMass = 500 },
            "EUR",
            TestContext.Current.CancellationToken);
        atOptions.Should().BeEmpty("method does not serve AT");
    }

    [Fact]
    public async Task RateShipment_Should_Return_Options_Ordered_By_Price()
    {
        await using var db = ShippingTestDbContext.Create();
        var cheap = BuildMethod("DHL Economy", "DHL", "ECONOMY", isActive: true);
        cheap.Rates.Add(new ShippingRate { Id = Guid.NewGuid(), PriceMinor = 299, SortOrder = 0 });
        var expensive = BuildMethod("DHL Express", "DHL", "EXPRESS", isActive: true);
        expensive.Rates.Add(new ShippingRate { Id = Guid.NewGuid(), PriceMinor = 999, SortOrder = 0 });
        db.Set<ShippingMethod>().AddRange(cheap, expensive);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new RateShipmentHandler(db);
        var options = await handler.HandleAsync(
            new RateShipmentInputDto { Country = "DE", SubtotalNetMinor = 1000, ShipmentMass = 500 },
            "EUR",
            TestContext.Current.CancellationToken);

        options.Should().HaveCount(2);
        options[0].PriceMinor.Should().BeLessThanOrEqualTo(options[1].PriceMinor,
            "options must be ordered cheapest first");
    }

    [Fact]
    public async Task RateShipment_Should_Use_Default_Currency_When_Method_Has_No_Currency()
    {
        await using var db = ShippingTestDbContext.Create();
        var method = BuildMethod("DHL Parcel", "DHL", "PARCEL", currency: null);
        method.Rates.Add(new ShippingRate { Id = Guid.NewGuid(), PriceMinor = 499, SortOrder = 0 });
        db.Set<ShippingMethod>().Add(method);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new RateShipmentHandler(db);
        var options = await handler.HandleAsync(
            new RateShipmentInputDto { Country = "DE", SubtotalNetMinor = 1000, ShipmentMass = 500 },
            "CHF",
            TestContext.Current.CancellationToken);

        options.Should().HaveCount(1);
        options[0].Currency.Should().Be("CHF", "the default currency should be used when the method has none");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetShippingMethodForEditHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetShippingMethodForEdit_Should_Return_Null_When_Not_Found()
    {
        await using var db = ShippingTestDbContext.Create();
        var handler = new GetShippingMethodForEditHandler(db);

        var result = await handler.HandleAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.Should().BeNull("no method exists with that id");
    }

    [Fact]
    public async Task GetShippingMethodForEdit_Should_Return_Correct_Projection()
    {
        await using var db = ShippingTestDbContext.Create();
        var method = BuildMethod("Standard", "DHL", "PARCEL", currency: "EUR");
        method.Rates.Add(new ShippingRate { Id = Guid.NewGuid(), PriceMinor = 499, SortOrder = 0 });
        db.Set<ShippingMethod>().Add(method);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetShippingMethodForEditHandler(db);
        var dto = await handler.HandleAsync(method.Id, TestContext.Current.CancellationToken);

        dto.Should().NotBeNull();
        dto!.Id.Should().Be(method.Id);
        dto.Name.Should().Be("Standard");
        dto.Carrier.Should().Be("DHL");
        dto.Service.Should().Be("PARCEL");
        dto.Currency.Should().Be("EUR");
        dto.Rates.Should().HaveCount(1);
        dto.Rates[0].PriceMinor.Should().Be(499);
    }

    [Fact]
    public async Task GetShippingMethodForEdit_Should_Return_Rates_Ordered_By_SortOrder()
    {
        await using var db = ShippingTestDbContext.Create();
        var method = BuildMethod();
        method.Rates.Add(new ShippingRate { Id = Guid.NewGuid(), PriceMinor = 799, SortOrder = 2 });
        method.Rates.Add(new ShippingRate { Id = Guid.NewGuid(), PriceMinor = 299, SortOrder = 0 });
        method.Rates.Add(new ShippingRate { Id = Guid.NewGuid(), PriceMinor = 499, SortOrder = 1 });
        db.Set<ShippingMethod>().Add(method);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetShippingMethodForEditHandler(db);
        var dto = await handler.HandleAsync(method.Id, TestContext.Current.CancellationToken);

        dto!.Rates[0].PriceMinor.Should().Be(299, "rates must be ordered by SortOrder ascending");
        dto.Rates[1].PriceMinor.Should().Be(499);
        dto.Rates[2].PriceMinor.Should().Be(799);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetShippingMethodsPageHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetShippingMethodsPage_Should_Return_Empty_When_No_Methods_Exist()
    {
        await using var db = ShippingTestDbContext.Create();
        var handler = new GetShippingMethodsPageHandler(db);

        var (items, total) = await handler.HandleAsync(1, 20, ct: TestContext.Current.CancellationToken);

        items.Should().BeEmpty();
        total.Should().Be(0);
    }

    [Fact]
    public async Task GetShippingMethodsPage_Should_Filter_By_Active_Status()
    {
        await using var db = ShippingTestDbContext.Create();
        db.Set<ShippingMethod>().AddRange(
            BuildMethod("Active", "DHL", "PARCEL", isActive: true),
            BuildMethod("Inactive", "DHL", "EXPRESS", isActive: false));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetShippingMethodsPageHandler(db);
        var (items, total) = await handler.HandleAsync(
            1, 20, filter: ShippingMethodQueueFilter.Active, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1, "only active methods should be returned for the Active filter");
        items.Single().Name.Should().Be("Active");
    }

    [Fact]
    public async Task GetShippingMethodsPage_Should_Filter_By_Inactive_Status()
    {
        await using var db = ShippingTestDbContext.Create();
        db.Set<ShippingMethod>().AddRange(
            BuildMethod("Active", "DHL", "PARCEL", isActive: true),
            BuildMethod("Inactive", "DHL", "EXPRESS", isActive: false));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetShippingMethodsPageHandler(db);
        var (items, total) = await handler.HandleAsync(
            1, 20, filter: ShippingMethodQueueFilter.Inactive, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1, "only inactive methods should be returned for the Inactive filter");
        items.Single().Name.Should().Be("Inactive");
    }

    [Fact]
    public async Task GetShippingMethodsPage_Should_Filter_By_Query_Term()
    {
        await using var db = ShippingTestDbContext.Create();
        db.Set<ShippingMethod>().AddRange(
            BuildMethod("DHL Parcel", "DHL", "PARCEL"),
            BuildMethod("UPS Standard", "UPS", "STANDARD"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetShippingMethodsPageHandler(db);
        var (items, total) = await handler.HandleAsync(
            1, 20, query: "UPS", ct: TestContext.Current.CancellationToken);

        total.Should().Be(1, "only the method matching the query should be returned");
        items.Single().Name.Should().Be("UPS Standard");
    }

    [Fact]
    public async Task GetShippingMethodsPage_Should_Filter_MissingRates()
    {
        await using var db = ShippingTestDbContext.Create();
        var withRates = BuildMethod("Rated", "DHL", "PARCEL");
        withRates.Rates.Add(new ShippingRate { Id = Guid.NewGuid(), PriceMinor = 499, SortOrder = 0 });
        var noRates = BuildMethod("No Rates", "DHL", "EXPRESS");
        db.Set<ShippingMethod>().AddRange(withRates, noRates);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetShippingMethodsPageHandler(db);
        var (items, total) = await handler.HandleAsync(
            1, 20, filter: ShippingMethodQueueFilter.MissingRates, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1, "only the method without rates should appear");
        items.Single().Name.Should().Be("No Rates");
    }

    [Fact]
    public async Task GetShippingMethodsPage_Should_Filter_GlobalCoverage()
    {
        await using var db = ShippingTestDbContext.Create();
        db.Set<ShippingMethod>().AddRange(
            BuildMethod("Global", "DHL", "PARCEL", countriesCsv: null),
            BuildMethod("DE Only", "DHL", "EXPRESS", countriesCsv: "DE"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetShippingMethodsPageHandler(db);
        var (items, total) = await handler.HandleAsync(
            1, 20, filter: ShippingMethodQueueFilter.GlobalCoverage, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1, "only methods with global coverage (null/empty CountriesCsv) should appear");
        items.Single().Name.Should().Be("Global");
    }

    [Fact]
    public async Task GetShippingMethodsPage_Should_Respect_Pagination()
    {
        await using var db = ShippingTestDbContext.Create();
        for (var i = 0; i < 5; i++)
            db.Set<ShippingMethod>().Add(BuildMethod($"Method {i}", "DHL", $"SVC{i}"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetShippingMethodsPageHandler(db);
        var (items, total) = await handler.HandleAsync(1, 3, ct: TestContext.Current.CancellationToken);

        total.Should().Be(5);
        items.Should().HaveCount(3, "page size is 3");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetShippingMethodOpsSummaryHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetShippingMethodOpsSummary_Should_Return_Zero_Counts_When_No_Methods()
    {
        await using var db = ShippingTestDbContext.Create();
        var handler = new GetShippingMethodOpsSummaryHandler(db);

        var summary = await handler.HandleAsync(TestContext.Current.CancellationToken);

        summary.TotalCount.Should().Be(0);
        summary.ActiveCount.Should().Be(0);
        summary.InactiveCount.Should().Be(0);
        summary.MissingRatesCount.Should().Be(0);
        summary.DhlCount.Should().Be(0);
        summary.GlobalCoverageCount.Should().Be(0);
        summary.MultiRateCount.Should().Be(0);
    }

    [Fact]
    public async Task GetShippingMethodOpsSummary_Should_Return_Correct_Counts()
    {
        await using var db = ShippingTestDbContext.Create();

        // Active DHL with 2 rates and global coverage
        var dhlMulti = BuildMethod("DHL Multi", "DHL", "PARCEL", isActive: true, countriesCsv: null);
        dhlMulti.Rates.Add(new ShippingRate { Id = Guid.NewGuid(), PriceMinor = 299, SortOrder = 0 });
        dhlMulti.Rates.Add(new ShippingRate { Id = Guid.NewGuid(), PriceMinor = 499, SortOrder = 1 });

        // Active DHL single rate, restricted to DE
        var dhlSingle = BuildMethod("DHL DE", "DHL", "EXPRESS", isActive: true, countriesCsv: "DE");
        dhlSingle.Rates.Add(new ShippingRate { Id = Guid.NewGuid(), PriceMinor = 999, SortOrder = 0 });

        // Inactive UPS, no rates
        var upsInactive = BuildMethod("UPS Standard", "UPS", "STANDARD", isActive: false, countriesCsv: null);

        db.Set<ShippingMethod>().AddRange(dhlMulti, dhlSingle, upsInactive);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetShippingMethodOpsSummaryHandler(db);
        var summary = await handler.HandleAsync(TestContext.Current.CancellationToken);

        summary.TotalCount.Should().Be(3);
        summary.ActiveCount.Should().Be(2, "dhlMulti and dhlSingle are active");
        summary.InactiveCount.Should().Be(1, "upsInactive is inactive");
        summary.MissingRatesCount.Should().Be(1, "upsInactive has no rates");
        summary.DhlCount.Should().Be(2, "dhlMulti and dhlSingle are DHL methods");
        summary.GlobalCoverageCount.Should().Be(2, "dhlMulti and upsInactive have null/empty CountriesCsv");
        summary.MultiRateCount.Should().Be(1, "dhlMulti has 2 rates");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // In-memory DbContext for Shipping tests
    // ─────────────────────────────────────────────────────────────────────────

    private sealed class ShippingTestDbContext : DbContext, IAppDbContext
    {
        private ShippingTestDbContext(DbContextOptions<ShippingTestDbContext> options)
            : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static ShippingTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<ShippingTestDbContext>()
                .UseInMemoryDatabase($"darwin_shipping_{Guid.NewGuid()}")
                .Options;
            return new ShippingTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ShippingMethod>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Name).HasMaxLength(256).IsRequired();
                b.Property(x => x.Carrier).HasMaxLength(64).IsRequired();
                b.Property(x => x.Service).HasMaxLength(64).IsRequired();
                b.Property(x => x.CountriesCsv).HasMaxLength(1000);
                b.Property(x => x.Currency).HasMaxLength(3);
                b.Property(x => x.IsActive);
                b.Property(x => x.RowVersion).IsRowVersion();
                b.HasMany(x => x.Rates)
                 .WithOne()
                 .HasForeignKey(r => r.ShippingMethodId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ShippingRate>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ShippingMethodId).IsRequired();
                b.Property(x => x.PriceMinor);
                b.Property(x => x.SortOrder);
                b.Property(x => x.MaxShipmentMass);
                b.Property(x => x.MaxSubtotalNetMinor);
            });
        }
    }
}
