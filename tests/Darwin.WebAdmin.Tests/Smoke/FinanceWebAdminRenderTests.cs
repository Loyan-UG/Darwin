using Darwin.WebAdmin.Tests.TestInfrastructure;
using FluentAssertions;
using System.Net;

namespace Darwin.WebAdmin.Tests.Smoke;

public sealed class FinanceWebAdminRenderTests : IClassFixture<WebAdminTestFactory>
{
    private readonly WebAdminTestFactory _factory;

    public FinanceWebAdminRenderTests(WebAdminTestFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/Finance")]
    [InlineData("/Finance/Receivables")]
    [InlineData("/Finance/Postings")]
    [InlineData("/Finance/AccountMappings")]
    [InlineData("/Finance/Exports")]
    [InlineData("/Finance/SupplierInvoices")]
    [InlineData("/Finance/SupplierPayments")]
    public async Task Finance_Workspace_Pages_Should_Render_Against_Test_Database(string path)
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();

        using var response = await client.GetAsync(path, TestContext.Current.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("Darwin Admin");
        html.Should().Contain("/js/admin-core.js");
        html.Should().Contain("Finance");
        html.Should().NotContain("https://cdn.jsdelivr.net");
        if (path is not "/Finance/AccountMappings" and not "/Finance/Exports" and not "/Finance/SupplierInvoices" and not "/Finance/SupplierPayments")
        {
            html.Should().NotContain("hx-post=");
            html.Should().NotContain("method=\"post\" action=\"/Finance");
        }
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("default-src 'self'");
        response.Headers.TryGetValues("X-Content-Type-Options", out var contentTypeOptions).Should().BeTrue();
        contentTypeOptions!.Single().Should().Be("nosniff");
    }
}
