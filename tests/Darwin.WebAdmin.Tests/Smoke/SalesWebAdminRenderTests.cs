using Darwin.WebAdmin.Tests.TestInfrastructure;
using FluentAssertions;
using System.Net;

namespace Darwin.WebAdmin.Tests.Smoke;

public sealed class SalesWebAdminRenderTests : IClassFixture<WebAdminTestFactory>
{
    private readonly WebAdminTestFactory _factory;

    public SalesWebAdminRenderTests(WebAdminTestFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/Sales")]
    [InlineData("/Sales/Quotes")]
    [InlineData("/Sales/Orders")]
    [InlineData("/Sales/Invoices")]
    [InlineData("/Sales/DeliveryNotes")]
    [InlineData("/Sales/ReturnOrders")]
    public async Task Sales_Workspace_Pages_Should_Render_Against_Test_Database(string path)
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();

        using var response = await client.GetAsync(path, TestContext.Current.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("Darwin Admin");
        html.Should().Contain("/js/admin-core.js");
        html.Should().Contain("Sales");
        html.Should().NotContain("https://cdn.jsdelivr.net");
        if (path is not "/Sales/DeliveryNotes" and not "/Sales/ReturnOrders")
        {
            html.Should().NotContain("hx-post=");
            html.Should().NotContain("method=\"post\" action=\"/Sales");
        }
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("default-src 'self'");
        response.Headers.TryGetValues("X-Content-Type-Options", out var contentTypeOptions).Should().BeTrue();
        contentTypeOptions!.Single().Should().Be("nosniff");
    }
}
