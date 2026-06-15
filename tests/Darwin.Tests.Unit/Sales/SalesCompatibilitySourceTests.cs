using FluentAssertions;

namespace Darwin.Tests.Unit.Sales
{
    public sealed class SalesCompatibilitySourceTests
    {
        [Fact]
        public void Sales_Core_Projection_Should_Not_Add_Parallel_Domain_Entities_Or_Tables()
        {
            var root = FindRepositoryRoot();
            var domainSources = ReadAllSources(Path.Combine(root, "src", "Darwin.Domain"));
            var infrastructureSources = ReadAllSources(Path.Combine(root, "src", "Darwin.Infrastructure", "Persistence", "Configurations"));
            var salesQuerySource = File.ReadAllText(Path.Combine(root, "src", "Darwin.Application", "Sales", "Queries", "SalesDocumentQueries.cs"));

            domainSources.Should().NotContain("class SalesOrder");
            domainSources.Should().NotContain("class SalesInvoice");
            domainSources.Should().NotContain("class FinanceInvoice");
            infrastructureSources.Should().NotContain("ToTable(\"SalesOrders\"");
            infrastructureSources.Should().NotContain("ToTable(\"SalesInvoices\"");
            infrastructureSources.Should().NotContain("ToTable(\"FinanceInvoices\"");
            salesQuerySource.Should().Contain("Set<Order>()");
            salesQuerySource.Should().Contain("Set<Invoice>()");
        }

        [Fact]
        public void Member_Commerce_Mobile_Routes_Should_Remain_On_Current_ApiRoutes()
        {
            var root = FindRepositoryRoot();
            var apiRoutes = File.ReadAllText(Path.Combine(root, "src", "Darwin.Mobile.Shared", "Api", "ApiRoutes.cs"));
            var service = File.ReadAllText(Path.Combine(root, "src", "Darwin.Mobile.Shared", "Services", "Commerce", "MemberCommerceService.cs"));

            apiRoutes.Should().Contain("public const string GetMyOrders = \"api/v1/member/orders\";");
            apiRoutes.Should().Contain("public static string GetById(Guid id) => $\"api/v1/member/orders/{id:D}\";");
            apiRoutes.Should().Contain("public static string CreatePaymentIntent(Guid id) => $\"api/v1/member/orders/{id:D}/payment-intent\";");
            apiRoutes.Should().Contain("public static string DownloadDocument(Guid id) => $\"api/v1/member/orders/{id:D}/document\";");
            apiRoutes.Should().Contain("public const string GetMyInvoices = \"api/v1/member/invoices\";");
            apiRoutes.Should().Contain("public static string GetById(Guid id) => $\"api/v1/member/invoices/{id:D}\";");
            apiRoutes.Should().Contain("public static string CreatePaymentIntent(Guid id) => $\"api/v1/member/invoices/{id:D}/payment-intent\";");
            apiRoutes.Should().Contain("public static string DownloadArchiveDocument(Guid id) => $\"api/v1/member/invoices/{id:D}/archive-document\";");
            apiRoutes.Should().Contain("public static string DownloadStructuredData(Guid id) => $\"api/v1/member/invoices/{id:D}/structured-data\";");
            apiRoutes.Should().Contain("public static string DownloadStructuredXml(Guid id) => $\"api/v1/member/invoices/{id:D}/structured-xml\";");

            service.Should().Contain("ApiRoutes.Orders.GetMyOrders");
            service.Should().Contain("ApiRoutes.Orders.GetById(orderId)");
            service.Should().Contain("ApiRoutes.Orders.CreatePaymentIntent(orderId)");
            service.Should().Contain("ApiRoutes.Orders.DownloadDocument(orderId)");
            service.Should().Contain("ApiRoutes.Invoices.GetMyInvoices");
            service.Should().Contain("ApiRoutes.Invoices.GetById(invoiceId)");
            service.Should().Contain("ApiRoutes.Invoices.CreatePaymentIntent(invoiceId)");
            service.Should().Contain("ApiRoutes.Invoices.DownloadArchiveDocument(invoiceId)");
            service.Should().Contain("ApiRoutes.Invoices.DownloadStructuredData(invoiceId)");
            service.Should().Contain("ApiRoutes.Invoices.DownloadStructuredXml(invoiceId)");
        }

        [Fact]
        public void Storefront_Checkout_And_Payment_Finalization_Routes_Should_Remain_Web_First()
        {
            var root = FindRepositoryRoot();
            var publicCheckoutController = File.ReadAllText(Path.Combine(root, "src", "Darwin.WebApi", "Controllers", "Public", "PublicCheckoutController.cs"));
            var storefrontCheckoutApi = File.ReadAllText(Path.Combine(root, "src", "Darwin.Web", "src", "features", "checkout", "api", "public-checkout.ts"));
            var finalizeRoute = File.ReadAllText(Path.Combine(root, "src", "Darwin.Web", "src", "app", "checkout", "orders", "[orderId]", "confirmation", "finalize", "route.ts"));

            publicCheckoutController.Should().Contain("[HttpPost(\"/api/v1/checkout/orders\")]");
            publicCheckoutController.Should().Contain("[HttpPost(\"/api/v1/checkout/orders/{orderId:guid}/payment-intent\")]");
            publicCheckoutController.Should().Contain("[HttpPost(\"/api/v1/checkout/orders/{orderId:guid}/payments/{paymentId:guid}/complete\")]");
            storefrontCheckoutApi.Should().Contain("\"/api/v1/public/checkout/orders\"");
            storefrontCheckoutApi.Should().Contain("`/api/v1/public/checkout/orders/${encodeURIComponent(input.orderId)}/payment-intent`");
            storefrontCheckoutApi.Should().Contain("`/api/v1/public/checkout/orders/${encodeURIComponent(input.orderId)}/payments/${encodeURIComponent(input.paymentId)}/complete`");
            finalizeRoute.Should().Contain("/confirmation");
        }

        private static string ReadAllSources(string directory)
        {
            return string.Join(
                Environment.NewLine,
                Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories)
                    .OrderBy(x => x)
                    .Select(File.ReadAllText));
        }

        private static string FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "Darwin.sln")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new InvalidOperationException("Could not find Darwin repository root.");
        }
    }
}
