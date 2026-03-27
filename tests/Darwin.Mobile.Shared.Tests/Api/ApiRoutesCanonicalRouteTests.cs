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
}
