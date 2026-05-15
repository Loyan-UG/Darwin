using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Pricing.Commands;
using Darwin.Application.Pricing.DTOs;
using Darwin.Application.Pricing.Queries;
using Darwin.Application.Pricing.Validators;
using Darwin.Domain.Entities.Pricing;
using Darwin.Domain.Enums;
using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Moq;
using Xunit;

namespace Darwin.Tests.Unit.Pricing;

/// <summary>
/// Handler-level unit tests for the Pricing module.
/// Covers <see cref="ValidateCouponHandler"/>, <see cref="RedeemPromotionHandler"/>,
/// <see cref="CreatePromotionHandler"/>, <see cref="UpdatePromotionHandler"/>,
/// <see cref="GetPromotionsPageHandler"/>, <see cref="CreateTaxCategoryHandler"/>,
/// and <see cref="UpdateTaxCategoryHandler"/>.
/// </summary>
public sealed class PricingHandlerTests
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

    // ─────────────────────────────────────────────────────────────────────────
    // ValidateCouponHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ValidateCoupon_Should_Throw_ValidationException_When_Input_Invalid()
    {
        await using var db = PricingTestDbContext.Create();
        var handler = new ValidateCouponHandler(db);

        var act = () => handler.HandleAsync(
            new ValidateCouponInputDto { Code = "", SubtotalNetMinor = 100, Currency = "EUR" },
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("an empty code violates the input validator");
    }

    [Fact]
    public async Task ValidateCoupon_Should_Return_Invalid_When_Code_Not_Found()
    {
        await using var db = PricingTestDbContext.Create();
        var handler = new ValidateCouponHandler(db);

        var result = await handler.HandleAsync(
            new ValidateCouponInputDto { Code = "UNKNOWN", SubtotalNetMinor = 1000, Currency = "EUR" },
            TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse("no promotion with that code exists");
        result.Message.Should().Contain("Invalid");
    }

    [Fact]
    public async Task ValidateCoupon_Should_Return_Invalid_When_Promotion_Not_Active()
    {
        await using var db = PricingTestDbContext.Create();
        db.Set<Promotion>().Add(new Promotion
        {
            Id = Guid.NewGuid(),
            Name = "Inactive",
            Code = "INACTIVE",
            IsActive = false,
            Currency = "EUR",
            Type = PromotionType.Percentage,
            Percent = 10m
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ValidateCouponHandler(db);
        var result = await handler.HandleAsync(
            new ValidateCouponInputDto { Code = "INACTIVE", SubtotalNetMinor = 1000, Currency = "EUR" },
            TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse("an inactive promotion must be rejected");
    }

    [Fact]
    public async Task ValidateCoupon_Should_Return_Invalid_When_Promotion_Expired()
    {
        await using var db = PricingTestDbContext.Create();
        db.Set<Promotion>().Add(new Promotion
        {
            Id = Guid.NewGuid(),
            Name = "Expired",
            Code = "EXPIRED",
            IsActive = true,
            Currency = "EUR",
            Type = PromotionType.Percentage,
            Percent = 10m,
            EndsAtUtc = DateTime.UtcNow.AddDays(-1)
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ValidateCouponHandler(db);
        var result = await handler.HandleAsync(
            new ValidateCouponInputDto { Code = "EXPIRED", SubtotalNetMinor = 1000, Currency = "EUR" },
            TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse("an expired promotion must be rejected");
    }

    [Fact]
    public async Task ValidateCoupon_Should_Return_Invalid_When_Promotion_Not_Yet_Started()
    {
        await using var db = PricingTestDbContext.Create();
        db.Set<Promotion>().Add(new Promotion
        {
            Id = Guid.NewGuid(),
            Name = "Future",
            Code = "FUTURE",
            IsActive = true,
            Currency = "EUR",
            Type = PromotionType.Percentage,
            Percent = 10m,
            StartsAtUtc = DateTime.UtcNow.AddDays(2)
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ValidateCouponHandler(db);
        var result = await handler.HandleAsync(
            new ValidateCouponInputDto { Code = "FUTURE", SubtotalNetMinor = 1000, Currency = "EUR" },
            TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse("a promotion that hasn't started yet must be rejected");
    }

    [Fact]
    public async Task ValidateCoupon_Should_Return_Invalid_When_Currency_Mismatch()
    {
        await using var db = PricingTestDbContext.Create();
        db.Set<Promotion>().Add(new Promotion
        {
            Id = Guid.NewGuid(),
            Name = "EUR Only",
            Code = "EURONLY",
            IsActive = true,
            Currency = "EUR",
            Type = PromotionType.Percentage,
            Percent = 10m
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ValidateCouponHandler(db);
        var result = await handler.HandleAsync(
            new ValidateCouponInputDto { Code = "EURONLY", SubtotalNetMinor = 1000, Currency = "USD" },
            TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse("currency mismatch must be rejected");
        result.Message.Should().Contain("mismatch");
    }

    [Fact]
    public async Task ValidateCoupon_Should_Return_Invalid_When_Subtotal_Below_Minimum()
    {
        await using var db = PricingTestDbContext.Create();
        db.Set<Promotion>().Add(new Promotion
        {
            Id = Guid.NewGuid(),
            Name = "Min50",
            Code = "MIN50",
            IsActive = true,
            Currency = "EUR",
            Type = PromotionType.Percentage,
            Percent = 10m,
            MinSubtotalNetMinor = 5000
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ValidateCouponHandler(db);
        var result = await handler.HandleAsync(
            new ValidateCouponInputDto { Code = "MIN50", SubtotalNetMinor = 4999, Currency = "EUR" },
            TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse("subtotal is below minimum requirement");
        result.Message.Should().Contain("minimum");
    }

    [Fact]
    public async Task ValidateCoupon_Should_Return_Invalid_When_Global_Redemption_Cap_Reached()
    {
        await using var db = PricingTestDbContext.Create();
        var promoId = Guid.NewGuid();
        db.Set<Promotion>().Add(new Promotion
        {
            Id = promoId,
            Name = "LimitedCap",
            Code = "LIMITEDCAP",
            IsActive = true,
            Currency = "EUR",
            Type = PromotionType.Percentage,
            Percent = 10m,
            MaxRedemptions = 2
        });
        db.Set<PromotionRedemption>().AddRange(
            new PromotionRedemption { Id = Guid.NewGuid(), PromotionId = promoId, OrderId = Guid.NewGuid() },
            new PromotionRedemption { Id = Guid.NewGuid(), PromotionId = promoId, OrderId = Guid.NewGuid() });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ValidateCouponHandler(db);
        var result = await handler.HandleAsync(
            new ValidateCouponInputDto { Code = "LIMITEDCAP", SubtotalNetMinor = 1000, Currency = "EUR" },
            TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse("global redemption cap has been reached");
        result.Message.Should().Contain("limit");
    }

    [Fact]
    public async Task ValidateCoupon_Should_Return_Invalid_When_Per_Customer_Limit_Reached()
    {
        await using var db = PricingTestDbContext.Create();
        var promoId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        db.Set<Promotion>().Add(new Promotion
        {
            Id = promoId,
            Name = "PerCustomer",
            Code = "PERCUST",
            IsActive = true,
            Currency = "EUR",
            Type = PromotionType.Percentage,
            Percent = 15m,
            PerCustomerLimit = 1
        });
        db.Set<PromotionRedemption>().Add(new PromotionRedemption
        {
            Id = Guid.NewGuid(),
            PromotionId = promoId,
            UserId = userId,
            OrderId = Guid.NewGuid()
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ValidateCouponHandler(db);
        var result = await handler.HandleAsync(
            new ValidateCouponInputDto { Code = "PERCUST", SubtotalNetMinor = 1000, Currency = "EUR", UserId = userId },
            TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse("per-customer limit has been reached for this user");
        result.Message.Should().Contain("Per-customer");
    }

    [Fact]
    public async Task ValidateCoupon_Should_Apply_Percentage_Discount()
    {
        await using var db = PricingTestDbContext.Create();
        var promoId = Guid.NewGuid();
        db.Set<Promotion>().Add(new Promotion
        {
            Id = promoId,
            Name = "10Pct",
            Code = "TEN",
            IsActive = true,
            Currency = "EUR",
            Type = PromotionType.Percentage,
            Percent = 10m
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ValidateCouponHandler(db);
        var result = await handler.HandleAsync(
            new ValidateCouponInputDto { Code = "TEN", SubtotalNetMinor = 10000, Currency = "EUR" },
            TestContext.Current.CancellationToken);

        result.IsValid.Should().BeTrue();
        result.DiscountMinor.Should().Be(1000, "10% of 10000 minor units = 1000");
        result.PromotionId.Should().Be(promoId);
        result.Currency.Should().Be("EUR");
    }

    [Fact]
    public async Task ValidateCoupon_Should_Apply_Fixed_Amount_Discount()
    {
        await using var db = PricingTestDbContext.Create();
        var promoId = Guid.NewGuid();
        db.Set<Promotion>().Add(new Promotion
        {
            Id = promoId,
            Name = "5EurOff",
            Code = "FIVE",
            IsActive = true,
            Currency = "EUR",
            Type = PromotionType.Amount,
            AmountMinor = 500
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ValidateCouponHandler(db);
        var result = await handler.HandleAsync(
            new ValidateCouponInputDto { Code = "FIVE", SubtotalNetMinor = 2000, Currency = "EUR" },
            TestContext.Current.CancellationToken);

        result.IsValid.Should().BeTrue();
        result.DiscountMinor.Should().Be(500, "fixed amount discount of 500 minor units");
    }

    [Fact]
    public async Task ValidateCoupon_Should_Cap_Amount_Discount_At_Subtotal()
    {
        await using var db = PricingTestDbContext.Create();
        db.Set<Promotion>().Add(new Promotion
        {
            Id = Guid.NewGuid(),
            Name = "BigDiscount",
            Code = "BIGDISCOUNT",
            IsActive = true,
            Currency = "EUR",
            Type = PromotionType.Amount,
            AmountMinor = 99999
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ValidateCouponHandler(db);
        var result = await handler.HandleAsync(
            new ValidateCouponInputDto { Code = "BIGDISCOUNT", SubtotalNetMinor = 300, Currency = "EUR" },
            TestContext.Current.CancellationToken);

        result.IsValid.Should().BeTrue();
        result.DiscountMinor.Should().Be(300, "discount cannot exceed the subtotal");
    }

    [Fact]
    public async Task ValidateCoupon_Should_Return_Invalid_When_Conditions_Product_Not_Met()
    {
        await using var db = PricingTestDbContext.Create();
        var requiredProductId = Guid.NewGuid();
        db.Set<Promotion>().Add(new Promotion
        {
            Id = Guid.NewGuid(),
            Name = "ProductOnly",
            Code = "PRODONLY",
            IsActive = true,
            Currency = "EUR",
            Type = PromotionType.Percentage,
            Percent = 20m,
            ConditionsJson = $"{{\"includeProducts\":[\"{requiredProductId}\"]}}"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ValidateCouponHandler(db);
        var result = await handler.HandleAsync(new ValidateCouponInputDto
        {
            Code = "PRODONLY",
            SubtotalNetMinor = 1000,
            Currency = "EUR",
            ProductIds = new List<Guid> { Guid.NewGuid() } // different product
        }, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse("the basket does not contain the required product");
        result.Message.Should().Contain("conditions not met");
    }

    [Fact]
    public async Task ValidateCoupon_Should_Pass_When_Conditions_Product_Present_In_Basket()
    {
        await using var db = PricingTestDbContext.Create();
        var requiredProductId = Guid.NewGuid();
        db.Set<Promotion>().Add(new Promotion
        {
            Id = Guid.NewGuid(),
            Name = "ProductOnly2",
            Code = "PRODONLY2",
            IsActive = true,
            Currency = "EUR",
            Type = PromotionType.Percentage,
            Percent = 10m,
            ConditionsJson = $"{{\"includeProducts\":[\"{requiredProductId}\"]}}"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ValidateCouponHandler(db);
        var result = await handler.HandleAsync(new ValidateCouponInputDto
        {
            Code = "PRODONLY2",
            SubtotalNetMinor = 2000,
            Currency = "EUR",
            ProductIds = new List<Guid> { requiredProductId }
        }, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeTrue("the required product is present in the basket");
    }

    [Fact]
    public async Task ValidateCoupon_Should_Return_Invalid_When_ConditionsJson_Is_Malformed()
    {
        await using var db = PricingTestDbContext.Create();
        db.Set<Promotion>().Add(new Promotion
        {
            Id = Guid.NewGuid(),
            Name = "BadJson",
            Code = "BADJSON",
            IsActive = true,
            Currency = "EUR",
            Type = PromotionType.Percentage,
            Percent = 10m,
            ConditionsJson = "{ not valid json ["
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ValidateCouponHandler(db);
        var result = await handler.HandleAsync(new ValidateCouponInputDto
        {
            Code = "BADJSON",
            SubtotalNetMinor = 1000,
            Currency = "EUR",
            ProductIds = new List<Guid> { Guid.NewGuid() }
        }, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse("malformed conditions JSON must be treated as conditions not met");
    }

    [Fact]
    public async Task ValidateCoupon_Should_Match_Code_Case_Insensitively()
    {
        await using var db = PricingTestDbContext.Create();
        db.Set<Promotion>().Add(new Promotion
        {
            Id = Guid.NewGuid(),
            Name = "CaseTest",
            Code = "SUMMER10",
            IsActive = true,
            Currency = "EUR",
            Type = PromotionType.Percentage,
            Percent = 10m
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ValidateCouponHandler(db);
        var result = await handler.HandleAsync(
            new ValidateCouponInputDto { Code = "summer10", SubtotalNetMinor = 1000, Currency = "EUR" },
            TestContext.Current.CancellationToken);

        result.IsValid.Should().BeTrue("code matching must be case-insensitive");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // RedeemPromotionHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task RedeemPromotion_Should_Record_Redemption_For_Authenticated_User()
    {
        await using var db = PricingTestDbContext.Create();
        var promoId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var handler = new RedeemPromotionHandler(db);

        await handler.HandleAsync(promoId, orderId, userId, TestContext.Current.CancellationToken);

        var redemption = db.Set<PromotionRedemption>().Single();
        redemption.PromotionId.Should().Be(promoId);
        redemption.OrderId.Should().Be(orderId);
        redemption.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task RedeemPromotion_Should_Record_Redemption_For_Guest_Without_UserId()
    {
        await using var db = PricingTestDbContext.Create();
        var promoId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var handler = new RedeemPromotionHandler(db);

        await handler.HandleAsync(promoId, orderId, userId: null, TestContext.Current.CancellationToken);

        var redemption = db.Set<PromotionRedemption>().Single();
        redemption.PromotionId.Should().Be(promoId);
        redemption.OrderId.Should().Be(orderId);
        redemption.UserId.Should().BeNull("guest redemptions have no user id");
    }

    [Fact]
    public async Task RedeemPromotion_Should_Allow_Multiple_Redemptions_For_Different_Orders()
    {
        await using var db = PricingTestDbContext.Create();
        var promoId = Guid.NewGuid();
        var handler = new RedeemPromotionHandler(db);

        await handler.HandleAsync(promoId, Guid.NewGuid(), null, TestContext.Current.CancellationToken);
        await handler.HandleAsync(promoId, Guid.NewGuid(), null, TestContext.Current.CancellationToken);

        db.Set<PromotionRedemption>().Count().Should().Be(2, "each order gets its own redemption record");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CreatePromotionHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreatePromotion_Should_Throw_ValidationException_When_Dto_Invalid()
    {
        await using var db = PricingTestDbContext.Create();
        var handler = new CreatePromotionHandler(db, new PromotionCreateValidator(), CreateLocalizer());

        var act = () => handler.HandleAsync(
            new PromotionCreateDto { Name = "", Currency = "EUR" },
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("name is required");
    }

    [Fact]
    public async Task CreatePromotion_Should_Persist_Promotion_Successfully()
    {
        await using var db = PricingTestDbContext.Create();
        var handler = new CreatePromotionHandler(db, new PromotionCreateValidator(), CreateLocalizer());

        var dto = new PromotionCreateDto
        {
            Name = "Summer Sale",
            Currency = "EUR",
            Type = PromotionType.Percentage,
            Percent = 10m,
            IsActive = true
        };

        await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        var promo = db.Set<Promotion>().Single();
        promo.Name.Should().Be("Summer Sale");
        promo.Currency.Should().Be("EUR");
        promo.Percent.Should().Be(10m);
        promo.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreatePromotion_Should_Throw_When_Active_Code_Already_Exists()
    {
        await using var db = PricingTestDbContext.Create();
        db.Set<Promotion>().Add(new Promotion
        {
            Id = Guid.NewGuid(),
            Name = "Existing",
            Code = "DUPLICATE",
            IsActive = true,
            Currency = "EUR"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreatePromotionHandler(db, new PromotionCreateValidator(), CreateLocalizer());

        var act = () => handler.HandleAsync(
            new PromotionCreateDto { Name = "New", Currency = "EUR", Code = "DUPLICATE" },
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("duplicate active coupon codes must be rejected");
    }

    [Fact]
    public async Task CreatePromotion_Should_Allow_Duplicate_Code_When_Existing_Is_Inactive()
    {
        await using var db = PricingTestDbContext.Create();
        db.Set<Promotion>().Add(new Promotion
        {
            Id = Guid.NewGuid(),
            Name = "Old Inactive",
            Code = "REUSE",
            IsActive = false,
            Currency = "EUR"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreatePromotionHandler(db, new PromotionCreateValidator(), CreateLocalizer());

        await handler.HandleAsync(
            new PromotionCreateDto { Name = "New Active", Currency = "EUR", Code = "REUSE", IsActive = true },
            TestContext.Current.CancellationToken);

        db.Set<Promotion>().Count().Should().Be(2, "code reuse is allowed when the previous promotion is inactive");
    }

    [Fact]
    public async Task CreatePromotion_Should_Allow_Promotion_Without_Code()
    {
        await using var db = PricingTestDbContext.Create();
        var handler = new CreatePromotionHandler(db, new PromotionCreateValidator(), CreateLocalizer());

        await handler.HandleAsync(
            new PromotionCreateDto { Name = "No Code Promo", Currency = "EUR", Type = PromotionType.Amount, AmountMinor = 200 },
            TestContext.Current.CancellationToken);

        var promo = db.Set<Promotion>().Single();
        promo.Code.Should().BeNull("promotions without a coupon code are allowed");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UpdatePromotionHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdatePromotion_Should_Throw_ValidationException_When_Dto_Invalid()
    {
        await using var db = PricingTestDbContext.Create();
        var handler = new UpdatePromotionHandler(db, new PromotionEditValidator(), CreateLocalizer());

        var act = () => handler.HandleAsync(
            new PromotionEditDto { Id = Guid.Empty, RowVersion = new byte[] { 1 }, Name = "x", Currency = "EUR" },
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("an empty Id violates the edit validator");
    }

    [Fact]
    public async Task UpdatePromotion_Should_Throw_When_Promotion_Not_Found()
    {
        await using var db = PricingTestDbContext.Create();
        var handler = new UpdatePromotionHandler(db, new PromotionEditValidator(), CreateLocalizer());

        var act = () => handler.HandleAsync(
            new PromotionEditDto
            {
                Id = Guid.NewGuid(),
                RowVersion = new byte[] { 1 },
                Name = "Gone",
                Currency = "EUR"
            }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>("the promotion does not exist");
    }

    [Fact]
    public async Task UpdatePromotion_Should_Throw_When_RowVersion_Mismatch()
    {
        await using var db = PricingTestDbContext.Create();
        var promoId = Guid.NewGuid();
        db.Set<Promotion>().Add(new Promotion
        {
            Id = promoId,
            Name = "Test",
            Currency = "EUR",
            RowVersion = new byte[] { 1, 2, 3, 4 }
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdatePromotionHandler(db, new PromotionEditValidator(), CreateLocalizer());

        var act = () => handler.HandleAsync(
            new PromotionEditDto
            {
                Id = promoId,
                RowVersion = new byte[] { 9, 9, 9, 9 }, // stale version
                Name = "Updated",
                Currency = "EUR"
            }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>("a stale RowVersion must trigger a concurrency exception");
    }

    [Fact]
    public async Task UpdatePromotion_Should_Throw_ValidationException_When_RowVersion_Is_Empty()
    {
        await using var db = PricingTestDbContext.Create();
        var promoId = Guid.NewGuid();
        db.Set<Promotion>().Add(new Promotion
        {
            Id = promoId,
            Name = "Test",
            Currency = "EUR",
            RowVersion = new byte[] { 1, 2, 3, 4 }
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdatePromotionHandler(db, new PromotionEditValidator(), CreateLocalizer());

        var act = () => handler.HandleAsync(
            new PromotionEditDto
            {
                Id = promoId,
                RowVersion = Array.Empty<byte>(), // empty - should throw ValidationException
                Name = "Updated",
                Currency = "EUR"
            }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("an empty RowVersion must be rejected before the concurrency check");
    }

    [Fact]
    public async Task UpdatePromotion_Should_Persist_Changes_Successfully()
    {
        await using var db = PricingTestDbContext.Create();
        var promoId = Guid.NewGuid();
        var rowVersion = new byte[] { 1, 2, 3, 4 };
        db.Set<Promotion>().Add(new Promotion
        {
            Id = promoId,
            Name = "Original",
            Currency = "EUR",
            Type = PromotionType.Percentage,
            Percent = 5m,
            IsActive = true,
            RowVersion = rowVersion
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdatePromotionHandler(db, new PromotionEditValidator(), CreateLocalizer());
        await handler.HandleAsync(new PromotionEditDto
        {
            Id = promoId,
            RowVersion = rowVersion,
            Name = "Updated Name",
            Currency = "EUR",
            Type = PromotionType.Percentage,
            Percent = 20m,
            IsActive = false
        }, TestContext.Current.CancellationToken);

        var promo = db.Set<Promotion>().Single();
        promo.Name.Should().Be("Updated Name");
        promo.Percent.Should().Be(20m);
        promo.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task UpdatePromotion_Should_Throw_When_Code_Conflicts_With_Another_Active_Promotion()
    {
        await using var db = PricingTestDbContext.Create();
        var promoId = Guid.NewGuid();
        var otherPromoId = Guid.NewGuid();
        var rowVersion = new byte[] { 5, 6, 7, 8 };
        db.Set<Promotion>().AddRange(
            new Promotion
            {
                Id = promoId,
                Name = "Mine",
                Code = "MINE",
                IsActive = true,
                Currency = "EUR",
                RowVersion = rowVersion
            },
            new Promotion
            {
                Id = otherPromoId,
                Name = "Other",
                Code = "CONFLICT",
                IsActive = true,
                Currency = "EUR",
                RowVersion = new byte[] { 1 }
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdatePromotionHandler(db, new PromotionEditValidator(), CreateLocalizer());

        var act = () => handler.HandleAsync(new PromotionEditDto
        {
            Id = promoId,
            RowVersion = rowVersion,
            Name = "Mine",
            Currency = "EUR",
            Code = "CONFLICT" // conflicts with the other active promotion
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("setting a code that belongs to another active promotion must be rejected");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetPromotionsPageHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPromotionsPage_Should_Return_Empty_When_No_Promotions()
    {
        await using var db = PricingTestDbContext.Create();
        var handler = new GetPromotionsPageHandler(db);

        var (items, total) = await handler.HandleAsync(1, 20, TestContext.Current.CancellationToken);

        items.Should().BeEmpty();
        total.Should().Be(0);
    }

    [Fact]
    public async Task GetPromotionsPage_Should_Return_All_Items_And_Correct_Total()
    {
        await using var db = PricingTestDbContext.Create();
        for (var i = 1; i <= 5; i++)
        {
            db.Set<Promotion>().Add(new Promotion
            {
                Id = Guid.NewGuid(),
                Name = $"Promo {i}",
                Currency = "EUR"
            });
        }
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetPromotionsPageHandler(db);
        var (items, total) = await handler.HandleAsync(1, 20, TestContext.Current.CancellationToken);

        total.Should().Be(5, "there are exactly 5 promotions");
        items.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetPromotionsPage_Should_Respect_PageSize()
    {
        await using var db = PricingTestDbContext.Create();
        for (var i = 1; i <= 10; i++)
        {
            db.Set<Promotion>().Add(new Promotion
            {
                Id = Guid.NewGuid(),
                Name = $"Promo {i}",
                Currency = "EUR"
            });
        }
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetPromotionsPageHandler(db);
        var (items, total) = await handler.HandleAsync(page: 1, pageSize: 3, TestContext.Current.CancellationToken);

        total.Should().Be(10);
        items.Should().HaveCount(3, "only the first page of 3 should be returned");
    }

    [Fact]
    public async Task GetPromotionsPage_Should_Return_Correct_Second_Page()
    {
        await using var db = PricingTestDbContext.Create();
        for (var i = 1; i <= 5; i++)
        {
            db.Set<Promotion>().Add(new Promotion
            {
                Id = Guid.NewGuid(),
                Name = $"Promo {i}",
                Currency = "EUR"
            });
        }
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetPromotionsPageHandler(db);
        var (items, total) = await handler.HandleAsync(page: 2, pageSize: 3, TestContext.Current.CancellationToken);

        total.Should().Be(5);
        items.Should().HaveCount(2, "the second page of 3 from 5 items should return the remaining 2");
    }

    [Fact]
    public async Task GetPromotionsPage_Should_Clamp_Invalid_Page_To_First()
    {
        await using var db = PricingTestDbContext.Create();
        db.Set<Promotion>().Add(new Promotion { Id = Guid.NewGuid(), Name = "P", Currency = "EUR" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetPromotionsPageHandler(db);
        var (items, total) = await handler.HandleAsync(page: -5, pageSize: 20, TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Should().HaveCount(1, "negative page should be clamped to page 1");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CreateTaxCategoryHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateTaxCategory_Should_Throw_ValidationException_When_Dto_Invalid()
    {
        await using var db = PricingTestDbContext.Create();
        var handler = new CreateTaxCategoryHandler(db, new TaxCategoryCreateValidator(), CreateLocalizer());

        var act = () => handler.HandleAsync(
            new TaxCategoryCreateDto { Name = "", VatRate = 0.19m },
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("name is required");
    }

    [Fact]
    public async Task CreateTaxCategory_Should_Persist_TaxCategory_Successfully()
    {
        await using var db = PricingTestDbContext.Create();
        var handler = new CreateTaxCategoryHandler(db, new TaxCategoryCreateValidator(), CreateLocalizer());

        await handler.HandleAsync(
            new TaxCategoryCreateDto { Name = "Standard", VatRate = 0.19m, Notes = "19% German VAT" },
            TestContext.Current.CancellationToken);

        var cat = db.Set<TaxCategory>().Single();
        cat.Name.Should().Be("Standard");
        cat.VatRate.Should().Be(0.19m);
        cat.Notes.Should().Be("19% German VAT");
    }

    [Fact]
    public async Task CreateTaxCategory_Should_Throw_When_Name_Already_Exists()
    {
        await using var db = PricingTestDbContext.Create();
        db.Set<TaxCategory>().Add(new TaxCategory
        {
            Id = Guid.NewGuid(),
            Name = "Standard",
            VatRate = 0.19m
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateTaxCategoryHandler(db, new TaxCategoryCreateValidator(), CreateLocalizer());

        var act = () => handler.HandleAsync(
            new TaxCategoryCreateDto { Name = "Standard", VatRate = 0.07m },
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("tax category names must be unique");
    }

    [Fact]
    public async Task CreateTaxCategory_Should_Enforce_Name_Uniqueness_Case_Insensitively()
    {
        await using var db = PricingTestDbContext.Create();
        db.Set<TaxCategory>().Add(new TaxCategory
        {
            Id = Guid.NewGuid(),
            Name = "standard",
            VatRate = 0.19m
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateTaxCategoryHandler(db, new TaxCategoryCreateValidator(), CreateLocalizer());

        var act = () => handler.HandleAsync(
            new TaxCategoryCreateDto { Name = "STANDARD", VatRate = 0.07m },
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("name uniqueness check must be case-insensitive");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UpdateTaxCategoryHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateTaxCategory_Should_Throw_ValidationException_When_Dto_Invalid()
    {
        await using var db = PricingTestDbContext.Create();
        var handler = new UpdateTaxCategoryHandler(db, new TaxCategoryEditValidator(), CreateLocalizer());

        var act = () => handler.HandleAsync(
            new TaxCategoryEditDto { Id = Guid.Empty, RowVersion = new byte[] { 1 }, Name = "x", VatRate = 0.19m },
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("an empty Id violates the edit validator");
    }

    [Fact]
    public async Task UpdateTaxCategory_Should_Throw_When_TaxCategory_Not_Found()
    {
        await using var db = PricingTestDbContext.Create();
        var handler = new UpdateTaxCategoryHandler(db, new TaxCategoryEditValidator(), CreateLocalizer());

        var act = () => handler.HandleAsync(
            new TaxCategoryEditDto
            {
                Id = Guid.NewGuid(),
                RowVersion = new byte[] { 1 },
                Name = "Gone",
                VatRate = 0.07m
            }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>("the tax category does not exist");
    }

    [Fact]
    public async Task UpdateTaxCategory_Should_Throw_When_RowVersion_Mismatch()
    {
        await using var db = PricingTestDbContext.Create();
        var catId = Guid.NewGuid();
        db.Set<TaxCategory>().Add(new TaxCategory
        {
            Id = catId,
            Name = "Old",
            VatRate = 0.19m,
            RowVersion = new byte[] { 1, 2, 3, 4 }
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateTaxCategoryHandler(db, new TaxCategoryEditValidator(), CreateLocalizer());

        var act = () => handler.HandleAsync(
            new TaxCategoryEditDto
            {
                Id = catId,
                RowVersion = new byte[] { 9, 9, 9, 9 }, // stale
                Name = "Updated",
                VatRate = 0.07m
            }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>("a stale RowVersion must trigger a concurrency exception");
    }

    [Fact]
    public async Task UpdateTaxCategory_Should_Throw_ValidationException_When_RowVersion_Is_Empty()
    {
        await using var db = PricingTestDbContext.Create();
        var catId = Guid.NewGuid();
        db.Set<TaxCategory>().Add(new TaxCategory
        {
            Id = catId,
            Name = "Old",
            VatRate = 0.19m,
            RowVersion = new byte[] { 1, 2, 3, 4 }
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateTaxCategoryHandler(db, new TaxCategoryEditValidator(), CreateLocalizer());

        var act = () => handler.HandleAsync(
            new TaxCategoryEditDto
            {
                Id = catId,
                RowVersion = Array.Empty<byte>(), // empty - should throw ValidationException
                Name = "Updated",
                VatRate = 0.07m
            }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("an empty RowVersion must be rejected before the concurrency check");
    }

    [Fact]
    public async Task UpdateTaxCategory_Should_Persist_Changes_Successfully()
    {
        await using var db = PricingTestDbContext.Create();
        var catId = Guid.NewGuid();
        var rowVersion = new byte[] { 1, 2, 3, 4 };
        db.Set<TaxCategory>().Add(new TaxCategory
        {
            Id = catId,
            Name = "Old Name",
            VatRate = 0.19m,
            RowVersion = rowVersion
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateTaxCategoryHandler(db, new TaxCategoryEditValidator(), CreateLocalizer());
        await handler.HandleAsync(new TaxCategoryEditDto
        {
            Id = catId,
            RowVersion = rowVersion,
            Name = "Reduced Rate",
            VatRate = 0.07m,
            Notes = "Reduced German VAT"
        }, TestContext.Current.CancellationToken);

        var cat = db.Set<TaxCategory>().Single();
        cat.Name.Should().Be("Reduced Rate");
        cat.VatRate.Should().Be(0.07m);
        cat.Notes.Should().Be("Reduced German VAT");
    }

    [Fact]
    public async Task UpdateTaxCategory_Should_Throw_When_Name_Conflicts_With_Another_Category()
    {
        await using var db = PricingTestDbContext.Create();
        var catId = Guid.NewGuid();
        var rowVersion = new byte[] { 1 };
        db.Set<TaxCategory>().AddRange(
            new TaxCategory { Id = catId, Name = "Reduced", VatRate = 0.07m, RowVersion = rowVersion },
            new TaxCategory { Id = Guid.NewGuid(), Name = "Standard", VatRate = 0.19m, RowVersion = new byte[] { 2 } });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateTaxCategoryHandler(db, new TaxCategoryEditValidator(), CreateLocalizer());

        var act = () => handler.HandleAsync(new TaxCategoryEditDto
        {
            Id = catId,
            RowVersion = rowVersion,
            Name = "Standard", // conflicts with another category
            VatRate = 0.07m
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("renaming a category to an existing name must be rejected");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // In-memory DbContext for Pricing tests
    // ─────────────────────────────────────────────────────────────────────────

    private sealed class PricingTestDbContext : DbContext, IAppDbContext
    {
        private PricingTestDbContext(DbContextOptions<PricingTestDbContext> options)
            : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static PricingTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<PricingTestDbContext>()
                .UseInMemoryDatabase($"darwin_pricing_{Guid.NewGuid()}")
                .Options;
            return new PricingTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Promotion>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Name).HasMaxLength(256).IsRequired();
                b.Property(x => x.Currency).HasMaxLength(3).IsRequired();
                b.Property(x => x.Code).HasMaxLength(64);
                b.Property(x => x.RowVersion).IsRowVersion();
            });

            modelBuilder.Entity<PromotionRedemption>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.PromotionId).IsRequired();
                b.Property(x => x.OrderId).IsRequired();
            });

            modelBuilder.Entity<TaxCategory>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Name).HasMaxLength(128).IsRequired();
                b.Property(x => x.RowVersion).IsRowVersion();
            });
        }
    }
}
