using Darwin.Contracts.Cms;
using System.Net;
using System.Net.Http.Json;
using Darwin.Contracts.Cart;
using Darwin.Contracts.Catalog;
using Darwin.Contracts.Common;
using Darwin.Contracts.Orders;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

using Darwin.Tests.Integration.Support;

namespace Darwin.Tests.Integration.Storefront;

/// <summary>
///     Provides end-to-end storefront smoke coverage for the seeded non-loyalty
///     commerce graph that Darwin.Web depends on during local development.
/// </summary>
public sealed class StorefrontCommerceSmokeTests : DeterministicIntegrationTestBase, IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly Guid SeededSmokeVariantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    /// <summary>
    ///     Initializes the test suite with the shared testing host factory.
    /// </summary>
    /// <param name="factory">Shared WebApplicationFactory instance.</param>
    public StorefrontCommerceSmokeTests(WebApplicationFactory<Program> factory)
        : base(factory)
    {
    }

    /// <summary>
    ///     Verifies that the deterministic seed exposes enough catalog and category
    ///     data for realistic storefront discovery testing.
    /// </summary>
    [Fact]
    public async Task SeededStorefrontDiscovery_Should_ExposeCatalogAndCategoryBaseline()
    {
        using var client = CreateHttpsClient();

        var categories = await client.GetFromJsonAsync<PagedResponse<PublicCategorySummary>>(
            "/api/v1/public/catalog/categories?culture=de-DE&page=1&pageSize=50",
            TestContext.Current.CancellationToken);
        var products = await client.GetFromJsonAsync<PagedResponse<PublicProductSummary>>(
            "/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=100",
            TestContext.Current.CancellationToken);

        categories.Should().NotBeNull();
        categories!.Total.Should().BeGreaterThanOrEqualTo(10);
        categories.Items.Should().Contain(item => item.Slug == "iphones");

        products.Should().NotBeNull();
        products!.Total.Should().BeGreaterThanOrEqualTo(20);
        products.Items.Should().Contain(item => !string.IsNullOrWhiteSpace(item.Slug));

        var englishCategories = await client.GetFromJsonAsync<PagedResponse<PublicCategorySummary>>(
            "/api/v1/public/catalog/categories?culture=en-US&page=1&pageSize=50",
            TestContext.Current.CancellationToken);
        var englishProducts = await client.GetFromJsonAsync<PagedResponse<PublicProductSummary>>(
            "/api/v1/public/catalog/products?culture=en-US&page=1&pageSize=100",
            TestContext.Current.CancellationToken);

        englishCategories.Should().NotBeNull();
        englishCategories!.Items.Should().OnlyContain(item =>
            !string.IsNullOrWhiteSpace(item.Name) &&
            !string.IsNullOrWhiteSpace(item.Slug));
        englishCategories.Items.Should().Contain(item => item.Name == "Android phones");

        englishProducts.Should().NotBeNull();
        englishProducts!.Items.Should().OnlyContain(item =>
            !string.IsNullOrWhiteSpace(item.Name) &&
            !string.IsNullOrWhiteSpace(item.Slug));
        englishProducts.Items.Should().Contain(item => item.Name == "iPhone 15 Pro 128GB");
    }
    /// <summary>
    ///     Verifies that the seeded public menus expose the main storefront and legal
    ///     routes needed to exercise the full web shell during local development.
    /// </summary>
    [Fact]
    public async Task SeededStorefrontMenus_Should_ExposePrimaryAndFooterNavigationBaseline()
    {
        using var client = CreateHttpsClient();

        var mainNavigation = await client.GetFromJsonAsync<PublicMenu>(
            "/api/v1/public/cms/menus/main-navigation?culture=de-DE",
            TestContext.Current.CancellationToken);
        var footerNavigation = await client.GetFromJsonAsync<PublicMenu>(
            "/api/v1/public/cms/menus/Footer?culture=de-DE",
            TestContext.Current.CancellationToken);

        mainNavigation.Should().NotBeNull();
        mainNavigation!.Items.Should().Contain(item => item.Url == "/");
        mainNavigation.Items.Should().Contain(item => item.Url == "/catalog");
        mainNavigation.Items.Should().Contain(item => item.Url == "/cms");
        mainNavigation.Items.Should().Contain(item => item.Url == "/account");
        mainNavigation.Items.Should().Contain(item => item.Url == "/cart");
        mainNavigation.Items.Should().Contain(item => item.Url == "/checkout");
        mainNavigation.Items.Should().Contain(item => item.Url == "/orders");
        mainNavigation.Items.Should().Contain(item => item.Url == "/invoices");

        footerNavigation.Should().NotBeNull();
        footerNavigation!.Items.Should().Contain(item => item.Url == "/cms/impressum");
        footerNavigation.Items.Should().Contain(item => item.Url == "/cms/datenschutz");
        footerNavigation.Items.Should().Contain(item => item.Url == "/cms/agb");
        footerNavigation.Items.Should().Contain(item => item.Url == "/cms/widerruf");
        footerNavigation.Items.Should().Contain(item => item.Url == "/cms/kontakt");
        footerNavigation.Items.Should().Contain(item => item.Url == "/account/sign-in");
        footerNavigation.Items.Should().Contain(item => item.Url == "/account/register");
        footerNavigation.Items.Should().Contain(item => item.Url == "/account/profile");
        footerNavigation.Items.Should().Contain(item => item.Url == "/account/preferences");
        footerNavigation.Items.Should().Contain(item => item.Url == "/account/addresses");
        footerNavigation.Items.Should().Contain(item => item.Url == "/account/security");
        footerNavigation.Items.Should().Contain(item => item.Url == "/orders");
        footerNavigation.Items.Should().Contain(item => item.Url == "/invoices");
        footerNavigation.Items.Should().Contain(item => item.Url == "/mock-checkout");
    }

    /// <summary>
    ///     Verifies that the seeded storefront supports the main anonymous commerce
    ///     path from cart mutation through payment handoff, return validation, and confirmation.
    /// </summary>
    [Fact]
    public async Task AnonymousStorefrontFlow_Should_SupportCartCheckoutPaymentAndConfirmation()
    {
        using var client = CreateHttpsClient();
        var anonymousId = $"storefront-smoke-{Guid.NewGuid():N}";

        using var addItemResponse = await client.PostAsJsonAsync(
            "/api/v1/public/cart/items",
            new PublicCartAddItemRequest
            {
                AnonymousId = anonymousId,
                VariantId = SeededSmokeVariantId,
                Quantity = 1
            },
            TestContext.Current.CancellationToken);

        addItemResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var cart = await addItemResponse.Content.ReadFromJsonAsync<PublicCartSummary>(
            cancellationToken: TestContext.Current.CancellationToken);
        cart.Should().NotBeNull();
        cart!.CartId.Should().NotBeEmpty();
        cart.Items.Should().ContainSingle(item => item.VariantId == SeededSmokeVariantId);

        using var couponResponse = await client.PostAsJsonAsync(
            "/api/v1/public/cart/coupon",
            new PublicCartApplyCouponRequest
            {
                CartId = cart.CartId,
                CouponCode = "WELCOME10"
            },
            TestContext.Current.CancellationToken);

        couponResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var cartWithCoupon = await couponResponse.Content.ReadFromJsonAsync<PublicCartSummary>(
            cancellationToken: TestContext.Current.CancellationToken);
        cartWithCoupon.Should().NotBeNull();
        cartWithCoupon!.CartId.Should().Be(cart.CartId);
        cartWithCoupon.CouponCode.Should().Be("WELCOME10");

        using var intentResponse = await client.PostAsJsonAsync(
            "/api/v1/public/checkout/intent",
            new CreateCheckoutIntentRequest
            {
                CartId = cartWithCoupon.CartId,
                ShippingAddress = CreateCheckoutAddress()
            },
            TestContext.Current.CancellationToken);

        intentResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var intent = await intentResponse.Content.ReadFromJsonAsync<CreateCheckoutIntentResponse>(
            cancellationToken: TestContext.Current.CancellationToken);
        intent.Should().NotBeNull();
        intent!.ShippingOptions.Should().NotBeEmpty();

        var selectedShipping = intent.ShippingOptions[0];

        using var orderResponse = await client.PostAsJsonAsync(
            "/api/v1/public/checkout/orders",
            new PlaceOrderFromCartRequest
            {
                CartId = cartWithCoupon.CartId,
                BillingAddress = CreateCheckoutAddress(),
                ShippingAddress = CreateCheckoutAddress(),
                SelectedShippingMethodId = selectedShipping.MethodId,
                ShippingTotalMinor = selectedShipping.PriceMinor,
                Culture = "de-DE"
            },
            TestContext.Current.CancellationToken);

        orderResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var placedOrder = await orderResponse.Content.ReadFromJsonAsync<PlaceOrderFromCartResponse>(
            cancellationToken: TestContext.Current.CancellationToken);
        placedOrder.Should().NotBeNull();
        placedOrder!.OrderId.Should().NotBeEmpty();
        placedOrder.OrderNumber.Should().NotBeNullOrWhiteSpace();

        using var paymentIntentResponse = await client.PostAsJsonAsync(
            $"/api/v1/public/checkout/orders/{placedOrder.OrderId:D}/payment-intent",
            new CreateStorefrontPaymentIntentRequest
            {
                OrderNumber = placedOrder.OrderNumber
            },
            TestContext.Current.CancellationToken);

        paymentIntentResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var paymentIntent = await paymentIntentResponse.Content.ReadFromJsonAsync<CreateStorefrontPaymentIntentResponse>(
            cancellationToken: TestContext.Current.CancellationToken);
        paymentIntent.Should().NotBeNull();
        paymentIntent!.PaymentId.Should().NotBeEmpty();
        paymentIntent.Provider.Should().Be("Stripe");
        paymentIntent.CheckoutUrl.Should().Contain("mock-checkout");

        using var completePaymentResponse = await client.PostAsJsonAsync(
            $"/api/v1/public/checkout/orders/{placedOrder.OrderId:D}/payments/{paymentIntent.PaymentId:D}/complete",
            new CompleteStorefrontPaymentRequest
            {
                OrderNumber = placedOrder.OrderNumber,
                ProviderReference = paymentIntent.ProviderReference,
                Outcome = "Succeeded"
            },
            TestContext.Current.CancellationToken);

        completePaymentResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var completedPayment = await completePaymentResponse.Content.ReadFromJsonAsync<CompleteStorefrontPaymentResponse>(
            cancellationToken: TestContext.Current.CancellationToken);
        completedPayment.Should().NotBeNull();
        completedPayment!.OrderStatus.Should().Be("Created");
        completedPayment.PaymentStatus.Should().Be("Pending");

        var confirmation = await client.GetFromJsonAsync<StorefrontOrderConfirmationResponse>(
            $"/api/v1/public/checkout/orders/{placedOrder.OrderId:D}/confirmation?orderNumber={Uri.EscapeDataString(placedOrder.OrderNumber)}",
            TestContext.Current.CancellationToken);

        confirmation.Should().NotBeNull();
        confirmation!.OrderId.Should().Be(placedOrder.OrderId);
        confirmation.OrderNumber.Should().Be(placedOrder.OrderNumber);
        confirmation.Lines.Should().NotBeEmpty();
        confirmation.Payments.Should().Contain(payment => payment.Id == paymentIntent.PaymentId && payment.Status == "Pending");
    }

    private static CheckoutAddress CreateCheckoutAddress() => new()
    {
        FullName = "Storefront Smoke Tester",
        Street1 = "Teststraße 1",
        PostalCode = "30159",
        City = "Hannover",
        CountryCode = "DE",
        PhoneE164 = "+495111234567"
    };
}


