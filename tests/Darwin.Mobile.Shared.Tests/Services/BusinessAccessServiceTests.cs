using Darwin.Contracts.Businesses;
using Darwin.Mobile.Shared.Api;
using Darwin.Mobile.Shared.Services;
using Darwin.Shared.Results;
using FluentAssertions;

namespace Darwin.Mobile.Shared.Tests.Services;

/// <summary>
/// Covers current-business access-state retrieval used by the Business mobile soft-gate policy.
/// </summary>
public sealed class BusinessAccessServiceTests
{
    [Fact]
    public async Task GetCurrentAccessStateAsync_Should_UseCanonicalBusinessAccountRoute()
    {
        var api = new FakeApiClient
        {
            OnGetResultAsync = route =>
            {
                route.Should().Be(ApiRoutes.BusinessAccount.GetAccessState);
                return Result<BusinessAccessStateResponse>.Ok(new BusinessAccessStateResponse
                {
                    BusinessId = Guid.NewGuid(),
                    BusinessName = "Cafe Wintergarten",
                    OperationalStatus = "PendingApproval",
                    IsActive = false,
                    IsOperationsAllowed = false,
                    IsSetupComplete = false
                });
            }
        };

        var service = new BusinessAccessService(api);

        var result = await service.GetCurrentAccessStateAsync(TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.OperationalStatus.Should().Be("PendingApproval");
    }

    private sealed class FakeApiClient : IApiClient
    {
        public Func<string, object?>? OnGetResultAsync { get; init; }

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
            => throw new NotSupportedException();

        public Task<Result<TResponse>> PostResultAsync<TRequest, TResponse>(string route, TRequest request, CancellationToken ct)
            => throw new NotSupportedException();

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
