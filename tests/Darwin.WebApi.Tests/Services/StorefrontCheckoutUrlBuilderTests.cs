using Darwin.Application;
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

    /// <summary>
    ///     Ensures front-office base URL must be a valid absolute URI.
    /// </summary>
    [Fact]
    public void BuildFrontOfficeConfirmationUrl_Should_Throw_WhenBaseUrlIsInvalid()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["StorefrontCheckout:FrontOfficeBaseUrl"] = "not-a-valid-uri"
            })
            .Build();
        var sut = new StorefrontCheckoutUrlBuilder(configuration, new KeyLocalizer());

        // Act
        Action act = () => sut.BuildFrontOfficeConfirmationUrl(Guid.NewGuid(), "ORD-1", cancelled: false);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("StorefrontFrontOfficeBaseUrlNotConfigured");
    }

    /// <summary>
    ///     Ensures empty/whitespace order numbers are intentionally omitted from query.
    /// </summary>
    [Fact]
    public void BuildFrontOfficeConfirmationUrl_Should_OmitOrderNumber_WhenOrderNumberIsWhitespace()
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
        var url = sut.BuildFrontOfficeConfirmationUrl(orderId, "   ", cancelled: false);

        // Assert
        url.Should().Be($"https://shop.example/checkout/orders/{orderId:D}/confirmation");
    }

    /// <summary>
    ///     Ensures constructor null-checks are explicit and deterministic.
    /// </summary>
    [Fact]
    public void Ctor_Should_Throw_WhenDependenciesAreMissing()
    {
        // Act
        Action noConfig = () => new StorefrontCheckoutUrlBuilder(null!, new KeyLocalizer());
        Action noLocalizer = () => new StorefrontCheckoutUrlBuilder(
            new ConfigurationBuilder().AddInMemoryCollection().Build(),
            null!);

        // Assert
        noConfig.Should().Throw<ArgumentNullException>();
        noLocalizer.Should().Throw<ArgumentNullException>();
    }

    private sealed class KeyLocalizer : IStringLocalizer<ValidationResource>
    {
        public LocalizedString this[string name] => new(name, name);

        public LocalizedString this[string name, params object[] arguments] => new(name, name);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];

        public IStringLocalizer WithCulture(CultureInfo culture) => this;
    }
}
