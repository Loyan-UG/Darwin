using Darwin.Application;
using Darwin.Application.Orders.DTOs;
using Darwin.WebApi.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using System.Globalization;

namespace Darwin.WebApi.Tests.Services;

public sealed class StorefrontCheckoutUrlBuilderTests
{
    [Fact]
    public void BuildFrontOfficeConfirmationUrl_Should_Throw_WhenFrontOfficeBaseUrlIsMissing()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var sut = new StorefrontCheckoutUrlBuilder(configuration, new KeyLocalizer());

        // Act
        Action act = () => sut.BuildFrontOfficeConfirmationUrl(Guid.NewGuid(), "ORD-1", cancelled: false);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("StorefrontFrontOfficeBaseUrlNotConfigured");
    }

    [Fact]
    public void BuildFrontOfficeConfirmationUrl_Should_BuildExpectedPathAndQuery()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["StorefrontCheckout:FrontOfficeBaseUrl"] = "https://shop.example"
            })
            .Build();
        var sut = new StorefrontCheckoutUrlBuilder(configuration, new KeyLocalizer());

        // Act
        var url = sut.BuildFrontOfficeConfirmationUrl(orderId, "  ORD-999  ", cancelled: true);

        // Assert
        url.Should().Be($"https://shop.example/checkout/orders/{orderId:D}/confirmation?orderNumber=ORD-999&cancelled=true");
    }

    [Fact]
    public void BuildStripeCheckoutUrl_Should_Throw_WhenProviderIsNotStripe()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["StorefrontCheckout:StripeCheckoutBaseUrl"] = "https://pay.example/checkout"
            })
            .Build();
        var sut = new StorefrontCheckoutUrlBuilder(configuration, new KeyLocalizer());
        var result = new StorefrontPaymentIntentResultDto
        {
            Provider = "Other",
            OrderId = Guid.NewGuid(),
            PaymentId = Guid.NewGuid(),
            ProviderReference = "pi_ref"
        };

        // Act
        Action act = () => sut.BuildStripeCheckoutUrl(result, "https://shop.example/return", "https://shop.example/cancel");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("StorefrontPaymentProviderNotSupported");
    }

    [Fact]
    public void BuildStripeCheckoutUrl_Should_Throw_WhenStripeBaseUrlIsMissing()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var sut = new StorefrontCheckoutUrlBuilder(configuration, new KeyLocalizer());
        var result = new StorefrontPaymentIntentResultDto
        {
            Provider = "Stripe",
            OrderId = Guid.NewGuid(),
            PaymentId = Guid.NewGuid(),
            ProviderReference = "pi_ref"
        };

        // Act
        Action act = () => sut.BuildStripeCheckoutUrl(result, "https://shop.example/return", "https://shop.example/cancel");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("StorefrontStripeCheckoutBaseUrlNotConfigured");
    }

    [Fact]
    public void BuildStripeCheckoutUrl_Should_UseCheckoutSessionAndOptionalPaymentIntentId()
    {
        // Arrange
        var result = new StorefrontPaymentIntentResultDto
        {
            Provider = "Stripe",
            OrderId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            PaymentId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            ProviderReference = "pi_fallback",
            ProviderCheckoutSessionReference = "cs_live_123",
            ProviderPaymentIntentReference = "pi_live_123"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["StorefrontCheckout:StripeCheckoutBaseUrl"] = "https://pay.example/checkout"
            })
            .Build();
        var sut = new StorefrontCheckoutUrlBuilder(configuration, new KeyLocalizer());

        // Act
        var url = sut.BuildStripeCheckoutUrl(result, "https://shop.example/return", "https://shop.example/cancel");

        // Assert
        url.Should().Contain("https://pay.example/checkout?");
        url.Should().Contain("orderId=11111111-1111-1111-1111-111111111111");
        url.Should().Contain("paymentId=22222222-2222-2222-2222-222222222222");
        url.Should().Contain("provider=Stripe");
        url.Should().Contain("checkoutSessionId=cs_live_123");
        url.Should().Contain("paymentIntentId=pi_live_123");
        url.Should().Contain("returnUrl=https%3A%2F%2Fshop.example%2Freturn");
        url.Should().Contain("cancelUrl=https%3A%2F%2Fshop.example%2Fcancel");
    }

    [Fact]
    public void BuildStripeCheckoutUrl_Should_FallbackToProviderReference_WhenCheckoutSessionIsMissing()
    {
        // Arrange
        var result = new StorefrontPaymentIntentResultDto
        {
            Provider = "stripe",
            OrderId = Guid.NewGuid(),
            PaymentId = Guid.NewGuid(),
            ProviderReference = "pi_fallback_only",
            ProviderCheckoutSessionReference = null,
            ProviderPaymentIntentReference = null
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["StorefrontCheckout:StripeCheckoutBaseUrl"] = "https://pay.example/checkout"
            })
            .Build();
        var sut = new StorefrontCheckoutUrlBuilder(configuration, new KeyLocalizer());

        // Act
        var url = sut.BuildStripeCheckoutUrl(result, "https://shop.example/return", "https://shop.example/cancel");

        // Assert
        url.Should().Contain("checkoutSessionId=pi_fallback_only");
        url.Should().NotContain("paymentIntentId=");
    }

    private sealed class KeyLocalizer : IStringLocalizer<ValidationResource>
    {
        public LocalizedString this[string name] => new(name, name);

        public LocalizedString this[string name, params object[] arguments] => new(name, name);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];

        public IStringLocalizer WithCulture(CultureInfo culture) => this;
    }
}
