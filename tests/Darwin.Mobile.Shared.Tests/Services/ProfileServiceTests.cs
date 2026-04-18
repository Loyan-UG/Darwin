using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Profile;
using Darwin.Mobile.Shared.Api;
using Darwin.Mobile.Shared.Caching;
using Darwin.Mobile.Shared.Services.Profile;
using Darwin.Mobile.Shared.Security;
using Darwin.Shared.Results;
using FluentAssertions;

namespace Darwin.Mobile.Shared.Tests.Services;

/// <summary>
/// Covers canonical member-profile address-book operations in the shared mobile profile service.
/// </summary>
public sealed class ProfileServiceTests
{
    [Fact]
    public async Task GetAddressesAsync_Should_UseCanonicalMemberRoute()
    {
        var api = new FakeApiClient
        {
            OnGetAsync = route =>
            {
                route.Should().Be("api/v1/member/profile/addresses");
                return new List<MemberAddress>
                {
                    new()
                    {
                        Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                        FullName = "Max Mustermann",
                        Street1 = "Friedrichstraße 1",
                        PostalCode = "10117",
                        City = "Berlin",
                        CountryCode = "DE"
                    }
                };
            }
        };
        var service = new ProfileService(api, new FakeMobileCacheService(), new FakeTokenStore());

        var result = await service.GetAddressesAsync(TestContext.Current.CancellationToken);

        result.Should().HaveCount(1);
        result[0].FullName.Should().Be("Max Mustermann");
    }

    [Fact]
    public async Task UpdateAddressAsync_Should_UseCanonicalMemberRoute()
    {
        var api = new FakeApiClient
        {
            OnPutResultAsync = (route, request) =>
            {
                route.Should().Be("api/v1/member/profile/addresses/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
                request.Should().BeOfType<UpdateMemberAddressRequest>();
                return Result<MemberAddress>.Ok(new MemberAddress
                {
                    Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                    FullName = "Erika Mustermann",
                    Street1 = "Unter den Linden 5",
                    PostalCode = "10117",
                    City = "Berlin",
                    CountryCode = "DE",
                    RowVersion = new byte[] { 1, 2, 3 }
                });
            }
        };
        var service = new ProfileService(api, new FakeMobileCacheService(), new FakeTokenStore());

        var result = await service.UpdateAddressAsync(
            Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            new UpdateMemberAddressRequest
            {
                FullName = "Erika Mustermann",
                Street1 = "Unter den Linden 5",
                PostalCode = "10117",
                City = "Berlin",
                CountryCode = "DE",
                RowVersion = new byte[] { 1, 2, 3 }
            },
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.FullName.Should().Be("Erika Mustermann");
    }

    [Fact]
    public async Task DeleteAddressAsync_Should_Fail_WhenAddressIdIsEmpty()
    {
        var service = new ProfileService(new FakeApiClient(), new FakeMobileCacheService(), new FakeTokenStore());

        var result = await service.DeleteAddressAsync(
            Guid.Empty,
            new DeleteMemberAddressRequest { RowVersion = new byte[] { 1 } },
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("AddressId is required.");
    }

    private sealed class FakeApiClient : IApiClient
    {
        public Func<string, object?>? OnGetAsync { get; init; }
        public Func<string, object, object?>? OnPutResultAsync { get; init; }

        public void SetBearerToken(string? accessToken)
        {
        }

        public Task<Result<TResponse>> GetResultAsync<TResponse>(string route, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<Result<string>> GetStringResultAsync(string route, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<Result<TResponse>> PostResultAsync<TRequest, TResponse>(string route, TRequest request, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<Result<TResponse>> GetEnvelopeResultAsync<TResponse>(string route, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<Result<TResponse>> PostEnvelopeResultAsync<TRequest, TResponse>(string route, TRequest request, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<TResponse?> GetAsync<TResponse>(string route, CancellationToken ct)
        {
            if (OnGetAsync is null)
            {
                return Task.FromResult<TResponse?>(default);
            }

            return Task.FromResult((TResponse?)OnGetAsync(route));
        }

        public Task<TResponse?> PostAsync<TRequest, TResponse>(string route, TRequest request, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<Result<TResponse>> PutResultAsync<TRequest, TResponse>(string route, TRequest request, CancellationToken ct)
        {
            if (OnPutResultAsync is null)
            {
                return Task.FromResult(Result<TResponse>.Fail("No PUT handler configured."));
            }

            var response = OnPutResultAsync(route, request!);
            return Task.FromResult(response as Result<TResponse> ?? Result<TResponse>.Fail("Unexpected PUT result type."));
        }

        public Task<TResponse?> PutAsync<TRequest, TResponse>(string route, TRequest request, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<Result> PutNoContentAsync<TRequest>(string route, TRequest request, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<Result> PostNoContentAsync<TRequest>(string route, TRequest request, CancellationToken ct)
            => throw new NotSupportedException();
    }

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
        public Task SaveAsync(string accessToken, DateTime accessExpiresUtc, string refreshToken, DateTime refreshExpiresUtc) => Task.CompletedTask;

        public Task<(string? AccessToken, DateTime? AccessExpiresUtc)> GetAccessAsync() => Task.FromResult<(string?, DateTime?)>((null, null));

        public Task<(string? RefreshToken, DateTime? RefreshExpiresUtc)> GetRefreshAsync() => Task.FromResult<(string?, DateTime?)>((null, null));

        public Task ClearAsync() => Task.CompletedTask;
    }
}
