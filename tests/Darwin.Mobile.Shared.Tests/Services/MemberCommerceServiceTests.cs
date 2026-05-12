using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Common;
using Darwin.Contracts.Invoices;
using Darwin.Contracts.Orders;
using Darwin.Mobile.Shared.Api;
using Darwin.Mobile.Shared.Caching;
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

    [Fact]
    public async Task GetOrderAsync_Should_UseCanonicalMemberRoute()
    {
        var apiClient = new FakeApiClient
        {
            OnGetResultAsync = route =>
            {
                route.Should().Be("api/v1/member/orders/11111111-2222-3333-4444-555555555555");
                return Result<MemberOrderDetail>.Ok(new MemberOrderDetail
                {
                    Id = Guid.Parse("11111111-2222-3333-4444-555555555555"),
                    OrderNumber = "ORD-1001",
                    Currency = "EUR"
                });
            }
        };
        var service = CreateService(apiClient);

        var result = await service.GetOrderAsync(Guid.Parse("11111111-2222-3333-4444-555555555555"), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.OrderNumber.Should().Be("ORD-1001");
    }

    [Fact]
    public async Task CreateInvoicePaymentIntentAsync_Should_UseCanonicalMemberRoute()
    {
        var apiClient = new FakeApiClient
        {
            OnPostResultAsync = (route, request) =>
            {
                route.Should().Be("api/v1/member/invoices/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee/payment-intent");
                request.Should().BeOfType<CreateStorefrontPaymentIntentRequest>();
                return Result<CreateStorefrontPaymentIntentResponse>.Ok(new CreateStorefrontPaymentIntentResponse
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
                });
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
    public async Task GetMyInvoicesAsync_Should_ReturnPagedResponse()
    {
        var apiClient = new FakeApiClient
        {
            OnGetResultAsync = route =>
            {
                route.Should().Be("api/v1/member/invoices?page=1&pageSize=20");
                return Result<PagedResponse<MemberInvoiceSummary>>.Ok(new PagedResponse<MemberInvoiceSummary>
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
                });
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
            OnGetStringResultAsync = route =>
            {
                route.Should().Be("api/v1/member/orders/11111111-2222-3333-4444-555555555555/document");
                return Result<string>.Ok("Order: ORD-1001");
            }
        };
        var service = CreateService(apiClient);

        var result = await service.DownloadOrderDocumentAsync(
            Guid.Parse("11111111-2222-3333-4444-555555555555"),
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().Be("Order: ORD-1001");
    }

    private static MemberCommerceService CreateService(FakeApiClient apiClient)
        => new(apiClient, new FakeMobileCacheService(), new FakeTokenStore());

    private sealed class FakeMobileCacheService : IMobileCacheService
    {
        public Task ClearAsync(CancellationToken ct) => Task.CompletedTask;

        public Task<T?> GetFreshAsync<T>(string cacheKey, CancellationToken ct) => Task.FromResult<T?>(default);

        public Task<T?> GetUsableAsync<T>(string cacheKey, TimeSpan maxAge, CancellationToken ct) => Task.FromResult<T?>(default);

        public Task RemoveAsync(string cacheKey, CancellationToken ct) => Task.CompletedTask;

        public Task SetAsync<T>(string cacheKey, T value, TimeSpan ttl, CancellationToken ct) => Task.CompletedTask;
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
        public Func<string, object?>? OnGetResultAsync { get; init; }

        public Func<string, object?>? OnGetStringResultAsync { get; init; }

        public Func<string, object, object?>? OnPostResultAsync { get; init; }

        public void SetBearerToken(string? accessToken)
        {
        }

        public Task<Result<TResponse>> GetResultAsync<TResponse>(string route, CancellationToken ct)
        {
            if (OnGetResultAsync is null)
            {
                return Task.FromResult(Result<TResponse>.Fail("No GET handler configured."));
            }

            var response = OnGetResultAsync(route);
            return Task.FromResult(response as Result<TResponse> ?? Result<TResponse>.Fail("Unexpected GET result type."));
        }

        public Task<Result<string>> GetStringResultAsync(string route, CancellationToken ct)
        {
            if (OnGetStringResultAsync is null)
            {
                return Task.FromResult(Result<string>.Fail("No text GET handler configured."));
            }

            var response = OnGetStringResultAsync(route);
            return Task.FromResult(response as Result<string> ?? Result<string>.Fail("Unexpected text GET result type."));
        }

        public Task<Result<TResponse>> PostResultAsync<TRequest, TResponse>(string route, TRequest request, CancellationToken ct)
        {
            if (OnPostResultAsync is null)
            {
                return Task.FromResult(Result<TResponse>.Fail("No POST handler configured."));
            }

            var response = OnPostResultAsync(route, request!);
            return Task.FromResult(response as Result<TResponse> ?? Result<TResponse>.Fail("Unexpected POST result type."));
        }

        public Task<Result<TResponse>> GetEnvelopeResultAsync<TResponse>(string route, CancellationToken ct)
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
