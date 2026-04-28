using Darwin.WebApi.Extensions;
using Darwin.WebApi.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Darwin.WebApi.Tests.Extensions;

public sealed class LoyaltyServiceCollectionExtensionsTests
{
    [Fact]
    public void AddLoyaltyPresentationServices_Should_RegisterLoyaltyPresentationService()
    {
        var services = new ServiceCollection();
        services.AddMemoryCache();

        var result = services.AddLoyaltyPresentationServices();

        result.Should().BeSameAs(services);
        var descriptor = services.SingleOrDefault(s => s.ServiceType == typeof(ILoyaltyPresentationService));
        descriptor.Should().NotBeNull();
        descriptor!.ServiceType.Should().Be(typeof(ILoyaltyPresentationService));
        descriptor.ImplementationType.Should().Be(typeof(LoyaltyPresentationService));
        descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddLoyaltyPresentationServices_Should_Throw_WhenServicesCollectionIsNull()
    {
        ServiceCollection services = null!;

        Action act = () => services.AddLoyaltyPresentationServices();
        act.Should().Throw<NullReferenceException>();
    }

    [Fact]
    public void AddWebApiComposition_Should_Throw_WhenServiceCollectionIsNull()
    {
        ServiceCollection services = null!;
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection()
            .Build();

        Action act = () => services.AddWebApiComposition(configuration);

        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    [Fact]
    public void AddWebApiComposition_Should_Throw_WhenConfigurationIsNull()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddWebApiComposition(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("configuration");
    }
}
