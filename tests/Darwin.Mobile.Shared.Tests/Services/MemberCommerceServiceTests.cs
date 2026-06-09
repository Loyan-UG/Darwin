using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Common;
using Darwin.Contracts.Invoices;
using Darwin.Contracts.Orders;
using Darwin.Mobile.Shared.Api;
using Darwin.Mobile.Shared.Caching;
using Darwin.Mobile.Shared.Common;
using Darwin.Mobile.Shared.Security;
using Darwin.Mobile.Shared.Services.Commerce;
using Darwin.Shared.Results;
using FluentAssertions;

namespace Darwin.Mobile.Shared.Tests.Services;

/// <summary>
/// Covers canonical member-commerce service behavior for order and invoice history flows.
/// </summary>
public sealed class MemberCommerceServiceTests
{
    [Fact]
    public async Task GetMyOrdersAsync_Should_Fail_WhenPageIsInvalid()
    {
        var service = CreateService(new FakeApiClient());

        var result = await service.GetMyOrdersAsync(0, 20, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Page must be a positive integer.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(201)]
    public async Task GetMyOrdersAsync_Should_Fail_WhenPageSizeIsInvalid(int pageSize)
    {
        var service = CreateService(new FakeApiClient());

        var result = await service.GetMyOrdersAsync(1, pageSize, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("PageSize must be between 1 and 200.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(201)]
    public async Task GetMyInvoicesAsync_Should_Fail_WhenPageSizeIsInvalid(int pageSize)
    {
        var service = CreateService(new FakeApiClient());

        var result = await service.GetMyInvoicesAsync(1, pageSize, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("PageSize must be between 1 and 200.");
    }

    [Fact]
    public async Task GetMyInvoicesAsync_Should_Fail_WhenPageIsInvalid()
    {
        var service = CreateService(new FakeApiClient());

        var result = await service.GetMyInvoicesAsync(0, 20, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Page must be a positive integer.");
    }

    [Fact]
    public async Task GetMyOrdersAsync_Should_UseFreshCacheBeforeCallingApi()
    {
        var cache = new FakeMobileCacheService();
        var cachedOrders = new PagedResponse<MemberOrderSummary>
        {
            Total = 1,
            Items =
            [
                new MemberOrderSummary
                {
                    Id = Guid.Parse("11111111-2222-3333-4444-555555555555"),
                    OrderNumber = "ORD-1001",
                    Status = "Completed"
                }
            ],
            Request = new PagedRequest
            {
                Page = 1,
                PageSize = 20
            }
        };

        cache.SetFresh("commerce.orders:1:20:anonymous", cachedOrders);
        var apiClient = new FakeApiClient
        {
            OnGetResultAsync = (_, _) => throw new InvalidOperationException("API should not be called.")
        };
        var service = CreateService(apiClient, cache);

        var result = await service.GetMyOrdersAsync(1, 20, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().BeSameAs(cachedOrders);
        apiClient.GetResultAsyncCallCount.Should().Be(0);
    }

    [Fact]
    public async Task GetMyInvoicesAsync_Should_FallbackToUsableCacheOnFailure()
    {
        var cache = new FakeMobileCacheService();
        var usableInvoices = new PagedResponse<MemberInvoiceSummary>
        {
            Total = 1,
            Items =
            [
                new MemberInvoiceSummary
                {
                    Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                    Currency = "EUR",
                    TotalGrossMinor = 2599,
                    Status = "Open"
                }
            ],
            Request = new PagedRequest
            {
                Page = 1,
                PageSize = 20
            }
        };

        cache.SetUsable("commerce.invoices:1:20:anonymous", usableInvoices);
        var apiClient = new FakeApiClient
        {
            OnGetResultAsync = (_, _) =>
                Task.FromResult<object?>(Result<PagedResponse<MemberInvoiceSummary>>.Fail("network down"))
        };
        var service = CreateService(apiClient, cache);

        var result = await service.GetMyInvoicesAsync(1, 20, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().BeSameAs(usableInvoices);
        apiClient.GetResultAsyncCallCount.Should().Be(1);
    }

    [Fact]
    public async Task GetMyOrdersAsync_Should_FallbackToUsableCacheOnException()
    {
        var cache = new FakeMobileCacheService();
        var usableOrders = new PagedResponse<MemberOrderSummary>
        {
            Total = 1,
            Items =
            [
                new MemberOrderSummary
                {
                    Id = Guid.Parse("11111111-2222-3333-4444-555555555555"),
                    OrderNumber = "ORD-1001",
                    Status = "Completed"
                }
            ],
            Request = new PagedRequest
            {
                Page = 1,
                PageSize = 20
            }
        };

        cache.SetUsable("commerce.orders:1:20:anonymous", usableOrders);
        var apiClient = new FakeApiClient
        {
            OnGetResultAsync = (_, _) => throw new TimeoutException("network down")
        };
        var service = CreateService(apiClient, cache);

        var result = await service.GetMyOrdersAsync(1, 20, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().BeSameAs(usableOrders);
        apiClient.GetResultAsyncCallCount.Should().Be(1);
    }

    [Fact]
    public async Task GetOrderAsync_Should_Fail_WhenOrderIdIsEmpty()
    {
        var service = CreateService(new FakeApiClient());

        var result = await service.GetOrderAsync(Guid.Empty, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("OrderId is required.");
    }

    [Fact]
    public async Task GetOrderAsync_Should_ReturnFailure_WhenApiReturnsFailure()
    {
        var apiClient = new FakeApiClient
        {
            OnGetResultAsync = (_, _) =>
                Task.FromResult<object?>(Result<MemberOrderDetail>.Fail("retrieval unavailable"))
        };
        var service = CreateService(apiClient);

        var result = await service.GetOrderAsync(
            Guid.Parse("11111111-2222-3333-4444-555555555555"),
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("retrieval unavailable");
    }

    [Fact]
    public async Task DownloadOrderDocumentAsync_Should_Fail_WhenOrderIdIsEmpty()
    {
        var service = CreateService(new FakeApiClient());

        var result = await service.DownloadOrderDocumentAsync(Guid.Empty, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("OrderId is required.");
    }

    [Fact]
    public async Task DownloadOrderDocumentAsync_Should_ReturnFailure_WhenApiReturnsFailure()
    {
        var apiClient = new FakeApiClient
        {
            OnGetStringResultAsync = (_, _) =>
                Task.FromResult<object?>(Result<string>.Fail("document unavailable"))
        };
        var service = CreateService(apiClient);

        var result = await service.DownloadOrderDocumentAsync(
            Guid.Parse("11111111-2222-3333-4444-555555555555"),
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("document unavailable");
    }

    [Fact]
    public async Task CreateOrderPaymentIntentAsync_Should_Fail_WhenOrderIdIsEmpty()
    {
        var service = CreateService(new FakeApiClient());

        var result = await service.CreateOrderPaymentIntentAsync(Guid.Empty, new CreateStorefrontPaymentIntentRequest(), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("OrderId is required.");
    }

    [Fact]
    public async Task CreateOrderPaymentIntentAsync_Should_Fail_WhenRequestIsNull()
    {
        var service = CreateService(new FakeApiClient());

        var result = await service.CreateOrderPaymentIntentAsync(Guid.NewGuid(), null!, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Request body is required.");
    }

    [Fact]
    public async Task CreateOrderPaymentIntentAsync_Should_ReturnFailure_WhenApiReturnsFailure()
    {
        var apiClient = new FakeApiClient
        {
            OnPostResultAsync = (_, _, _) =>
                Task.FromResult<object?>(Result<CreateStorefrontPaymentIntentResponse>.Fail("payment intent unavailable"))
        };
        var service = CreateService(apiClient);

        var result = await service.CreateOrderPaymentIntentAsync(
            Guid.Parse("11111111-2222-3333-4444-555555555555"),
            new CreateStorefrontPaymentIntentRequest(),
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("payment intent unavailable");
    }

    [Fact]
    public async Task GetInvoiceAsync_Should_Fail_WhenInvoiceIdIsEmpty()
    {
        var service = CreateService(new FakeApiClient());

        var result = await service.GetInvoiceAsync(Guid.Empty, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("InvoiceId is required.");
    }

    [Fact]
    public async Task GetInvoiceAsync_Should_ReturnFailure_WhenApiReturnsFailure()
    {
        var apiClient = new FakeApiClient
        {
            OnGetResultAsync = (_, _) =>
                Task.FromResult<object?>(Result<MemberInvoiceDetail>.Fail("invoice retrieval unavailable"))
        };
        var service = CreateService(apiClient);

        var result = await service.GetInvoiceAsync(
            Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("invoice retrieval unavailable");
    }

    [Fact]
    public async Task DownloadInvoiceDocumentAsync_Should_Fail_WhenInvoiceIdIsEmpty()
    {
        var service = CreateService(new FakeApiClient());

        var result = await service.DownloadInvoiceDocumentAsync(Guid.Empty, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("InvoiceId is required.");
    }

    [Fact]
    public async Task DownloadInvoiceDocumentAsync_Should_ReturnFailure_WhenApiReturnsFailure()
    {
        var apiClient = new FakeApiClient
        {
            OnGetStringResultAsync = (_, _) =>
                Task.FromResult<object?>(Result<string>.Fail("invoice document unavailable"))
        };
        var service = CreateService(apiClient);

        var result = await service.DownloadInvoiceDocumentAsync(
            Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("invoice document unavailable");
    }

    [Fact]
    public async Task DownloadInvoiceArchiveDocumentAsync_Should_Fail_WhenInvoiceIdIsEmpty()
    {
        var service = CreateService(new FakeApiClient());

        var result = await service.DownloadInvoiceArchiveDocumentAsync(Guid.Empty, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("InvoiceId is required.");
    }

    [Fact]
    public async Task DownloadInvoiceStructuredDataAsync_Should_Fail_WhenInvoiceIdIsEmpty()
    {
        var service = CreateService(new FakeApiClient());

        var result = await service.DownloadInvoiceStructuredDataAsync(Guid.Empty, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("InvoiceId is required.");
    }

    [Fact]
    public async Task DownloadInvoiceStructuredXmlAsync_Should_Fail_WhenInvoiceIdIsEmpty()
    {
        var service = CreateService(new FakeApiClient());

        var result = await service.DownloadInvoiceStructuredXmlAsync(Guid.Empty, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("InvoiceId is required.");
    }

    [Fact]
    public async Task CreateInvoicePaymentIntentAsync_Should_Fail_WhenInvoiceIdIsEmpty()
    {
        var service = CreateService(new FakeApiClient());

        var result = await service.CreateInvoicePaymentIntentAsync(Guid.Empty, new CreateStorefrontPaymentIntentRequest(), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("InvoiceId is required.");
    }

    [Fact]
    public async Task CreateInvoicePaymentIntentAsync_Should_Fail_WhenRequestIsNull()
    {
        var service = CreateService(new FakeApiClient());

        var result = await service.CreateInvoicePaymentIntentAsync(Guid.NewGuid(), null!, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Request body is required.");
    }

    [Fact]
    public async Task CreateInvoicePaymentIntentAsync_Should_ReturnFailure_WhenApiReturnsFailure()
    {
        var apiClient = new FakeApiClient
        {
            OnPostResultAsync = (_, _, _) =>
                Task.FromResult<object?>(Result<CreateStorefrontPaymentIntentResponse>.Fail("invoice intent unavailable"))
        };
        var service = CreateService(apiClient);

        var result = await service.CreateInvoicePaymentIntentAsync(
            Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            new CreateStorefrontPaymentIntentRequest(),
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("invoice intent unavailable");
    }

    [Fact]
    public async Task GetOrderAsync_Should_ReturnNetworkFailure_OnException()
    {
        var apiClient = new FakeApiClient
        {
            OnGetResultAsync = (_, _) => throw new TimeoutException("network failure")
        };
        var service = CreateService(apiClient);

        var result = await service.GetOrderAsync(Guid.Parse("11111111-2222-3333-4444-555555555555"), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(MobileErrorMessages.NetworkFailure("retrieving order detail"));
    }

    [Fact]
    public async Task CreateOrderPaymentIntentAsync_Should_ReturnNetworkFailure_OnException()
    {
        var apiClient = new FakeApiClient
        {
            OnPostResultAsync = (_, _, _) => throw new TimeoutException("network failure")
        };
        var service = CreateService(apiClient);

        var result = await service.CreateOrderPaymentIntentAsync(
            Guid.Parse("11111111-2222-3333-4444-555555555555"),
            new CreateStorefrontPaymentIntentRequest(),
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(MobileErrorMessages.NetworkFailure("creating order payment intent"));
    }

    [Fact]
    public async Task DownloadOrderDocumentAsync_Should_ReturnNetworkFailure_OnException()
    {
        var apiClient = new FakeApiClient
        {
            OnGetStringResultAsync = (_, _) => throw new TimeoutException("network failure")
        };
        var service = CreateService(apiClient);

        var result = await service.DownloadOrderDocumentAsync(Guid.Parse("11111111-2222-3333-4444-555555555555"), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(MobileErrorMessages.NetworkFailure("retrieving order document"));
    }

    [Fact]
    public async Task GetInvoiceAsync_Should_ReturnNetworkFailure_OnException()
    {
        var apiClient = new FakeApiClient
        {
            OnGetResultAsync = (_, _) => throw new TimeoutException("network failure")
        };
        var service = CreateService(apiClient);

        var result = await service.GetInvoiceAsync(
            Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(MobileErrorMessages.NetworkFailure("retrieving invoice detail"));
    }

    [Fact]
    public async Task CreateInvoicePaymentIntentAsync_Should_ReturnNetworkFailure_OnException()
    {
        var apiClient = new FakeApiClient
        {
            OnPostResultAsync = (_, _, _) => throw new TimeoutException("network failure")
        };
        var service = CreateService(apiClient);

        var result = await service.CreateInvoicePaymentIntentAsync(
            Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            new CreateStorefrontPaymentIntentRequest(),
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(MobileErrorMessages.NetworkFailure("creating invoice payment intent"));
    }

    [Fact]
    public async Task DownloadInvoiceDocumentAsync_Should_ReturnNetworkFailure_OnException()
    {
        var apiClient = new FakeApiClient
        {
            OnGetStringResultAsync = (_, _) => throw new TimeoutException("network failure")
        };
        var service = CreateService(apiClient);

        var result = await service.DownloadInvoiceDocumentAsync(
            Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(MobileErrorMessages.NetworkFailure("retrieving invoice document"));
    }

    [Fact]
    public async Task DownloadInvoiceArchiveDocumentAsync_Should_ReturnNetworkFailure_OnException()
    {
        var apiClient = new FakeApiClient
        {
            OnGetStringResultAsync = (_, _) => throw new TimeoutException("network failure")
        };
        var service = CreateService(apiClient);

        var result = await service.DownloadInvoiceArchiveDocumentAsync(
            Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(MobileErrorMessages.NetworkFailure("retrieving invoice archive document"));
    }

    [Fact]
    public async Task DownloadInvoiceStructuredDataAsync_Should_ReturnNetworkFailure_OnException()
    {
        var apiClient = new FakeApiClient
        {
            OnGetStringResultAsync = (_, _) => throw new TimeoutException("network failure")
        };
        var service = CreateService(apiClient);

        var result = await service.DownloadInvoiceStructuredDataAsync(
            Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(MobileErrorMessages.NetworkFailure("retrieving invoice structured data"));
    }

    [Fact]
    public async Task DownloadInvoiceStructuredXmlAsync_Should_ReturnNetworkFailure_OnException()
    {
        var apiClient = new FakeApiClient
        {
            OnGetStringResultAsync = (_, _) => throw new TimeoutException("network failure")
        };
        var service = CreateService(apiClient);

        var result = await service.DownloadInvoiceStructuredXmlAsync(
            Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(MobileErrorMessages.NetworkFailure("retrieving invoice structured XML"));
    }

    [Fact]
    public async Task GetOrderAsync_Should_UseCanonicalMemberRoute()
    {
        var apiClient = new FakeApiClient
        {
            OnGetResultAsync = (route, _) =>
            {
                route.Should().Be("api/v1/member/orders/11111111-2222-3333-4444-555555555555");
                return Task.FromResult<object?>(Result<MemberOrderDetail>.Ok(new MemberOrderDetail
                {
                    Id = Guid.Parse("11111111-2222-3333-4444-555555555555"),
                    OrderNumber = "ORD-1001",
                    Currency = "EUR"
                }));
            }
        };
        var service = CreateService(apiClient);

        var result = await service.GetOrderAsync(Guid.Parse("11111111-2222-3333-4444-555555555555"), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.OrderNumber.Should().Be("ORD-1001");
    }

    [Fact]
    public async Task GetMyOrdersAsync_Should_UseCanonicalMemberRoute()
    {
        var apiClient = new FakeApiClient
        {
            OnGetResultAsync = (route, _) =>
            {
                route.Should().Be("api/v1/member/orders?page=1&pageSize=20");
                return Task.FromResult<object?>(Result<PagedResponse<MemberOrderSummary>>.Ok(new PagedResponse<MemberOrderSummary>
                {
                    Total = 1,
                    Items =
                    [
                        new MemberOrderSummary
                        {
                            Id = Guid.Parse("11111111-2222-3333-4444-555555555555"),
                            OrderNumber = "ORD-1001",
                            Status = "Completed"
                        }
                    ],
                    Request = new PagedRequest
                    {
                        Page = 1,
                        PageSize = 20
                    }
                }));
            }
        };
        var service = CreateService(apiClient);

        var result = await service.GetMyOrdersAsync(1, 20, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateInvoicePaymentIntentAsync_Should_UseCanonicalMemberRoute()
    {
        var apiClient = new FakeApiClient
        {
            OnPostResultAsync = (route, request, _) =>
            {
                route.Should().Be("api/v1/member/invoices/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee/payment-intent");
                request.Should().BeOfType<CreateStorefrontPaymentIntentRequest>();
                return Task.FromResult<object?>(Result<CreateStorefrontPaymentIntentResponse>.Ok(new CreateStorefrontPaymentIntentResponse
                {
                    OrderId = Guid.Parse("99999999-8888-7777-6666-555555555555"),
                    PaymentId = Guid.Parse("12121212-3434-5656-7878-909090909090"),
                    Provider = "Stripe",
                    ProviderReference = "pi_123",
                    Currency = "EUR",
                    AmountMinor = 2599,
                    Status = "Pending",
                    CheckoutUrl = "https://checkout.example/intent",
                    ReturnUrl = "https://app.example/return",
                    CancelUrl = "https://app.example/cancel",
                    ExpiresAtUtc = new DateTime(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc)
                }));
            }
        };
        var service = CreateService(apiClient);

        var result = await service.CreateInvoicePaymentIntentAsync(
            Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            new CreateStorefrontPaymentIntentRequest
            {
                Provider = "Stripe"
            },
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.PaymentId.Should().Be(Guid.Parse("12121212-3434-5656-7878-909090909090"));
    }

    [Fact]
    public async Task GetInvoiceAsync_Should_UseCanonicalMemberRoute()
    {
        var apiClient = new FakeApiClient
        {
            OnGetResultAsync = (route, _) =>
            {
                route.Should().Be("api/v1/member/invoices/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
                return Task.FromResult<object?>(Result<MemberInvoiceDetail>.Ok(new MemberInvoiceDetail
                {
                    Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                    OrderNumber = "INV-1001",
                    Currency = "EUR",
                    TotalGrossMinor = 2599,
                    Status = "Open"
                }));
            }
        };
        var service = CreateService(apiClient);

        var result = await service.GetInvoiceAsync(
            Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.OrderNumber.Should().Be("INV-1001");
    }

    [Fact]
    public async Task GetMyInvoicesAsync_Should_ReturnPagedResponse()
    {
        var apiClient = new FakeApiClient
        {
            OnGetResultAsync = (route, _) =>
            {
                route.Should().Be("api/v1/member/invoices?page=1&pageSize=20");
                return Task.FromResult<object?>(Result<PagedResponse<MemberInvoiceSummary>>.Ok(new PagedResponse<MemberInvoiceSummary>
                {
                    Total = 1,
                    Items =
                    [
                        new MemberInvoiceSummary
                        {
                            Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                            Currency = "EUR",
                            TotalGrossMinor = 2599,
                            Status = "Open"
                        }
                    ],
                    Request = new PagedRequest
                    {
                        Page = 1,
                        PageSize = 20
                    }
                }));
            }
        };
        var service = CreateService(apiClient);

        var result = await service.GetMyInvoicesAsync(1, 20, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Total.Should().Be(1);
        result.Value.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task DownloadOrderDocumentAsync_Should_UseCanonicalMemberRoute()
    {
        var apiClient = new FakeApiClient
        {
            OnGetStringResultAsync = (route, _) =>
            {
                route.Should().Be("api/v1/member/orders/11111111-2222-3333-4444-555555555555/document");
                return Task.FromResult<object?>(Result<string>.Ok("Order: ORD-1001"));
            }
        };
        var service = CreateService(apiClient);

        var result = await service.DownloadOrderDocumentAsync(
            Guid.Parse("11111111-2222-3333-4444-555555555555"),
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().Be("Order: ORD-1001");
    }

    [Fact]
    public async Task DownloadInvoiceDocumentAsync_Should_UseCanonicalMemberRoute()
    {
        var apiClient = new FakeApiClient
        {
            OnGetStringResultAsync = (route, _) =>
            {
                route.Should().Be("api/v1/member/invoices/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee/document");
                return Task.FromResult<object?>(Result<string>.Ok("Invoice: INV-1001"));
            }
        };
        var service = CreateService(apiClient);

        var result = await service.DownloadInvoiceDocumentAsync(
            Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().Be("Invoice: INV-1001");
    }

    [Theory]
    [InlineData("archive-document", "Archive invoice")]
    [InlineData("structured-data", "{\"invoice\":true}")]
    [InlineData("structured-xml", "<invoice />")]
    public async Task InvoiceExportDownloads_Should_UseCanonicalMemberRoutes(string exportRouteSuffix, string payload)
    {
        var invoiceId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var apiClient = new FakeApiClient
        {
            OnGetStringResultAsync = (route, _) =>
            {
                route.Should().Be($"api/v1/member/invoices/{invoiceId:D}/{exportRouteSuffix}");
                return Task.FromResult<object?>(Result<string>.Ok(payload));
            }
        };
        var service = CreateService(apiClient);

        var result = exportRouteSuffix switch
        {
            "archive-document" => await service.DownloadInvoiceArchiveDocumentAsync(invoiceId, TestContext.Current.CancellationToken),
            "structured-data" => await service.DownloadInvoiceStructuredDataAsync(invoiceId, TestContext.Current.CancellationToken),
            "structured-xml" => await service.DownloadInvoiceStructuredXmlAsync(invoiceId, TestContext.Current.CancellationToken),
            _ => throw new InvalidOperationException("Unexpected export route suffix.")
        };

        result.Succeeded.Should().BeTrue();
        result.Value.Should().Be(payload);
    }

    private static MemberCommerceService CreateService(
        FakeApiClient apiClient,
        FakeMobileCacheService? cacheService = null,
        FakeTokenStore? tokenStore = null)
        => new(apiClient, cacheService ?? new FakeMobileCacheService(), tokenStore ?? new FakeTokenStore());

    private sealed class FakeMobileCacheService : IMobileCacheService
    {
        private readonly Dictionary<string, object?> _fresh = [];
        private readonly Dictionary<string, object?> _usable = [];

        public int GetFreshCalls { get; private set; }

        public int GetUsableCalls { get; private set; }

        public int SetCalls { get; private set; }

        public void SetFresh<T>(string cacheKey, T value)
            => _fresh[cacheKey] = value;

        public void SetUsable<T>(string cacheKey, T value)
            => _usable[cacheKey] = value;

        public Task ClearAsync(CancellationToken ct) => Task.CompletedTask;

        public Task<T?> GetFreshAsync<T>(string cacheKey, CancellationToken ct)
        {
            GetFreshCalls++;
            return Task.FromResult(_fresh.TryGetValue(cacheKey, out var value) ? (T?)value : default);
        }

        public Task<T?> GetUsableAsync<T>(string cacheKey, TimeSpan maxAge, CancellationToken ct)
        {
            GetUsableCalls++;
            return Task.FromResult(_usable.TryGetValue(cacheKey, out var value) ? (T?)value : default);
        }

        public Task RemoveAsync(string cacheKey, CancellationToken ct) => Task.CompletedTask;

        public Task SetAsync<T>(string cacheKey, T value, TimeSpan ttl, CancellationToken ct)
        {
            SetCalls++;
            _fresh[cacheKey] = value;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeTokenStore : ITokenStore
    {
        public Task SaveAsync(string accessToken, DateTime accessExpiresUtc, string refreshToken, DateTime refreshExpiresUtc)
            => Task.CompletedTask;

        public Task<(string? AccessToken, DateTime? AccessExpiresUtc)> GetAccessAsync()
            => Task.FromResult<(string?, DateTime?)>((null, null));

        public Task<(string? RefreshToken, DateTime? RefreshExpiresUtc)> GetRefreshAsync()
            => Task.FromResult<(string?, DateTime?)>((null, null));

        public Task ClearAsync() => Task.CompletedTask;
    }

    private sealed class FakeApiClient : IApiClient
    {
        public int GetResultAsyncCallCount { get; private set; }

        public int GetStringResultAsyncCallCount { get; private set; }

        public int PostResultAsyncCallCount { get; private set; }

        public Func<string, CancellationToken, Task<object?>>? OnGetResultAsync { get; init; }

        public Func<string, CancellationToken, Task<object?>>? OnGetStringResultAsync { get; init; }

        public Func<string, object, CancellationToken, Task<object?>>? OnPostResultAsync { get; init; }

        public void SetBearerToken(string? accessToken)
        {
        }

        public Task<Result<TResponse>> GetResultAsync<TResponse>(string route, CancellationToken ct)
        {
            GetResultAsyncCallCount++;
            if (OnGetResultAsync is null)
            {
                return Task.FromResult(Result<TResponse>.Fail("No GET handler configured."));
            }

            var task = OnGetResultAsync(route, ct);
            if (task.IsCompleted)
            {
                var response = task.Result;
                return Task.FromResult(response as Result<TResponse> ?? Result<TResponse>.Fail("Unexpected GET result type."));
            }

            return GetResultCore<TResponse>(task);
        }

        private static async Task<Result<TResponse>> GetResultCore<TResponse>(Task<object?> responseTask)
        {
            var response = await responseTask.ConfigureAwait(false);
            return response as Result<TResponse> ?? Result<TResponse>.Fail("Unexpected GET result type.");
        }

        public Task<Result<string>> GetStringResultAsync(string route, CancellationToken ct)
        {
            GetStringResultAsyncCallCount++;
            if (OnGetStringResultAsync is null)
            {
                return Task.FromResult(Result<string>.Fail("No text GET handler configured."));
            }

            var task = OnGetStringResultAsync(route, ct);
            if (task.IsCompleted)
            {
                var response = task.Result;
                return Task.FromResult(response as Result<string> ?? Result<string>.Fail("Unexpected text GET result type."));
            }

            return GetStringResultCore(task);
        }

        private static async Task<Result<string>> GetStringResultCore(Task<object?> responseTask)
        {
            var response = await responseTask.ConfigureAwait(false);
            return response as Result<string> ?? Result<string>.Fail("Unexpected text GET result type.");
        }

        public Task<Result<TResponse>> PostResultAsync<TRequest, TResponse>(string route, TRequest request, CancellationToken ct)
        {
            PostResultAsyncCallCount++;
            if (OnPostResultAsync is null)
            {
                return Task.FromResult(Result<TResponse>.Fail("No POST handler configured."));
            }

            var task = OnPostResultAsync(route, request!, ct);
            if (task.IsCompleted)
            {
                var response = task.Result;
                return Task.FromResult(response as Result<TResponse> ?? Result<TResponse>.Fail("Unexpected POST result type."));
            }

            return PostResultCore<TResponse>(task);
        }

        private static async Task<Result<TResponse>> PostResultCore<TResponse>(Task<object?> responseTask)
        {
            var response = await responseTask.ConfigureAwait(false);
            return response as Result<TResponse> ?? Result<TResponse>.Fail("Unexpected POST result type.");
        }

        public Task<Result<TResponse>> GetEnvelopeResultAsync<TResponse>(string route, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<Result<TResponse>> PostFileResultAsync<TResponse>(
            string route,
            Stream fileStream,
            string formFieldName,
            string fileName,
            string contentType,
            CancellationToken ct)
            => throw new NotSupportedException();

        public Task<Result<TResponse>> PostEnvelopeResultAsync<TRequest, TResponse>(string route, TRequest request, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<TResponse?> GetAsync<TResponse>(string route, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<TResponse?> PostAsync<TRequest, TResponse>(string route, TRequest request, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<Result<TResponse>> PutResultAsync<TRequest, TResponse>(string route, TRequest request, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<TResponse?> PutAsync<TRequest, TResponse>(string route, TRequest request, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<Result> PutNoContentAsync<TRequest>(string route, TRequest request, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<Result> PostNoContentAsync<TRequest>(string route, TRequest request, CancellationToken ct)
            => throw new NotSupportedException();
    }
}
