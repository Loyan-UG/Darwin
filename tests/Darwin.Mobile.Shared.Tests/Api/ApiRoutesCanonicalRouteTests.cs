using Darwin.Mobile.Shared.Api;
using FluentAssertions;

namespace Darwin.Mobile.Shared.Tests.Api;

/// <summary>
/// Guards the canonical audience-first route constants used by the mobile shared layer.
/// These tests reduce drift between the mobile apps and the WebApi route organization.
/// </summary>
public sealed class ApiRoutesCanonicalRouteTests
{
    /// <summary>
    /// Verifies Normalize trims whitespace/leading slashes while preserving relative route shape.
    /// </summary>
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData("/api/v1/member/profile/me", "api/v1/member/profile/me")]
    [InlineData(" ///api/v1/meta/health ", "api/v1/meta/health")]
    [InlineData("api/v1/member/orders", "api/v1/member/orders")]
    public void Normalize_Should_ReturnExpectedRelativeRoute(string? input, string expected)
    {
        // Act
        var normalized = ApiRoutes.Normalize(input!);

        // Assert
        normalized.Should().Be(expected);
    }

    /// <summary>
    /// Verifies that member-authenticated routes use the member audience prefix.
    /// </summary>
    [Fact]
    public void MemberRoutes_Should_UseAudienceFirstCanonicalPrefixes()
    {
        ApiRoutes.Auth.Login.Should().StartWith("api/v1/member/auth/");
        ApiRoutes.Auth.RequestEmailConfirmation.Should().Be("api/v1/member/auth/email/request-confirmation");
        ApiRoutes.Profile.GetMe.Should().Be("api/v1/member/profile/me");
        ApiRoutes.Profile.GetAddresses.Should().Be("api/v1/member/profile/addresses");
        ApiRoutes.Orders.GetMyOrders.Should().Be("api/v1/member/orders");
        ApiRoutes.Invoices.GetMyInvoices.Should().Be("api/v1/member/invoices");
        ApiRoutes.Notifications.RegisterDevice.Should().Be("api/v1/member/notifications/devices/register");
        ApiRoutes.Loyalty.GetMyAccounts.Should().Be("api/v1/member/loyalty/my/accounts");
        ApiRoutes.Loyalty.GetMyBusinesses.Should().Be("api/v1/member/loyalty/my/businesses");
    }

    /// <summary>
    /// Verifies that public and business routes use the correct audience prefixes.
    /// </summary>
    [Fact]
    public void PublicAndBusinessRoutes_Should_UseAudienceFirstCanonicalPrefixes()
    {
        ApiRoutes.BusinessAuth.PreviewInvitation.Should().Be("api/v1/business/auth/invitations/preview");
        ApiRoutes.BusinessAuth.AcceptInvitation.Should().Be("api/v1/business/auth/invitations/accept");
        ApiRoutes.BusinessAccount.GetAccessState.Should().Be("api/v1/business/account/access-state");
        ApiRoutes.Businesses.List.Should().Be("api/v1/public/businesses/list");
        ApiRoutes.Businesses.Map.Should().Be("api/v1/public/businesses/map");
        ApiRoutes.Businesses.CategoryKinds.Should().Be("api/v1/public/businesses/category-kinds");
        ApiRoutes.Billing.GetCurrentBusinessSubscription.Should().Be("api/v1/business/billing/subscription/current");
        ApiRoutes.Loyalty.ProcessScanSessionForBusiness.Should().Be("api/v1/business/loyalty/scan/process");
        ApiRoutes.Loyalty.GetBusinessCampaigns.Should().Be("api/v1/business/loyalty/campaigns");
    }

    /// <summary>
    /// Verifies dynamic route builders use deterministic lowercase <c>D</c>-format guids.
    /// </summary>
    [Fact]
    public void DynamicRouteBuilders_Should_EmbedGuidUsingDeterministicDFormat()
    {
        // Arrange
        var id = Guid.Parse("01234567-89AB-CDEF-0123-456789ABCDEF");
        const string expected = "01234567-89ab-cdef-0123-456789abcdef";

        // Act
        var profileUpdateRoute = ApiRoutes.Profile.UpdateAddress(id);
        var orderRoute = ApiRoutes.Orders.GetById(id);
        var businessRoute = ApiRoutes.Businesses.GetById(id);
        var loyaltyRoute = ApiRoutes.Loyalty.GetRewardsForBusiness(id);

        // Assert
        profileUpdateRoute.Should().EndWith(expected);
        orderRoute.Should().EndWith(expected);
        businessRoute.Should().EndWith(expected);
        loyaltyRoute.Should().Contain($"/{expected}/");
    }
}
