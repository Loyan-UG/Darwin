using Darwin.Contracts.Businesses;
using Darwin.Mobile.Shared.Api;
using Darwin.Mobile.Shared.Services;
using Darwin.Shared.Results;
using FluentAssertions;

namespace Darwin.Mobile.Shared.Tests.Services;

/// <summary>
/// Covers business media/profile routes used by the Business mobile setup surface.
/// </summary>
public sealed class BusinessMediaServiceTests
{
    [Fact]
    public async Task GetAsync_Should_UseCanonicalBusinessAccountMediaRoute()
    {
        var api = new FakeApiClient
        {
            OnGetResultAsync = route =>
            {
                route.Should().Be(ApiRoutes.BusinessAccount.GetMedia);
                return Result<BusinessMediaLibrary>.Ok(new BusinessMediaLibrary
                {
                    BusinessId = Guid.Parse("11111111-2222-3333-4444-555555555555"),
                    ProfileImageUrl = "/media/profile.png"
                });
            }
        };
        var service = new BusinessMediaService(api);

        var result = await service.GetAsync(TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.ProfileImageUrl.Should().Be("/media/profile.png");
    }

    [Fact]
    public async Task UploadAsync_Should_UseCanonicalBusinessAccountUploadRoute()
    {
        var api = new FakeApiClient
        {
            OnPostFileResultAsync = (route, formFieldName, fileName, contentType) =>
            {
                route.Should().Be(ApiRoutes.BusinessAccount.UploadMedia);
                formFieldName.Should().Be("file");
                fileName.Should().Be("profile.png");
                contentType.Should().Be("image/png");
                return Result<BusinessImageUploadResponse>.Ok(new BusinessImageUploadResponse { Url = "/media/profile.png" });
            }
        };
        var service = new BusinessMediaService(api);

        await using var stream = new MemoryStream([1, 2, 3]);
        var result = await service.UploadAsync(stream, "profile.png", "image/png", TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Url.Should().Be("/media/profile.png");
    }

    [Fact]
    public async Task SetProfileImageAsync_Should_UseCanonicalBusinessAccountProfileImageRoute()
    {
        var api = new FakeApiClient
        {
            OnPostNoContentAsync = (route, request) =>
            {
                route.Should().Be(ApiRoutes.BusinessAccount.SetProfileImage);
                request.Should().BeOfType<SetBusinessProfileImageRequest>();
                ((SetBusinessProfileImageRequest)request).ProfileImageUrl.Should().Be("/media/profile.png");
                return Result.Ok();
            }
        };
        var service = new BusinessMediaService(api);

        var result = await service.SetProfileImageAsync("/media/profile.png", TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task GalleryMutations_Should_UseCanonicalBusinessAccountRoutes()
    {
        var id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var seenRoutes = new List<string>();
        var api = new FakeApiClient
        {
            OnPostResultAsync = (route, request) =>
            {
                seenRoutes.Add(route);
                request.Should().BeOfType<CreateBusinessMediaRequest>();
                return Result<BusinessMediaItem>.Ok(new BusinessMediaItem
                {
                    Id = id,
                    BusinessId = Guid.Parse("11111111-2222-3333-4444-555555555555"),
                    Url = "/media/gallery.png",
                    RowVersion = [1, 2, 3]
                });
            },
            OnPutNoContentAsync = (route, request) =>
            {
                seenRoutes.Add(route);
                request.Should().BeOfType<UpdateBusinessMediaRequest>();
                return Result.Ok();
            },
            OnPostNoContentAsync = (route, request) =>
            {
                seenRoutes.Add(route);
                request.Should().BeOfType<DeleteBusinessMediaRequest>();
                return Result.Ok();
            }
        };
        var service = new BusinessMediaService(api);

        var createResult = await service.CreateGalleryImageAsync(
            new CreateBusinessMediaRequest { Url = "/media/gallery.png" },
            TestContext.Current.CancellationToken);
        var updateResult = await service.UpdateGalleryImageAsync(
            id,
            new UpdateBusinessMediaRequest { Url = "/media/gallery.png", RowVersion = [1, 2, 3] },
            TestContext.Current.CancellationToken);
        var deleteResult = await service.DeleteGalleryImageAsync(
            id,
            new DeleteBusinessMediaRequest { RowVersion = [1, 2, 3] },
            TestContext.Current.CancellationToken);

        createResult.Succeeded.Should().BeTrue();
        updateResult.Succeeded.Should().BeTrue();
        deleteResult.Succeeded.Should().BeTrue();
        seenRoutes.Should().Equal(
            ApiRoutes.BusinessAccount.CreateGalleryImage,
            ApiRoutes.BusinessAccount.UpdateGalleryImage(id),
            ApiRoutes.BusinessAccount.DeleteGalleryImage(id));
    }

    private sealed class FakeApiClient : IApiClient
    {
        public Func<string, object?>? OnGetResultAsync { get; init; }
        public Func<string, string, string, string, object?>? OnPostFileResultAsync { get; init; }
        public Func<string, object, object?>? OnPostResultAsync { get; init; }
        public Func<string, object, Result>? OnPutNoContentAsync { get; init; }
        public Func<string, object, Result>? OnPostNoContentAsync { get; init; }

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

        public Task<Result<TResponse>> PostFileResultAsync<TResponse>(
            string route,
            Stream fileStream,
            string formFieldName,
            string fileName,
            string contentType,
            CancellationToken ct)
        {
            if (OnPostFileResultAsync is null)
            {
                return Task.FromResult(Result<TResponse>.Fail("No POST file handler configured."));
            }

            var response = OnPostFileResultAsync(route, formFieldName, fileName, contentType);
            return Task.FromResult(response as Result<TResponse> ?? Result<TResponse>.Fail("Unexpected POST file result type."));
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

        public Task<Result> PutNoContentAsync<TRequest>(string route, TRequest request, CancellationToken ct)
            => Task.FromResult(OnPutNoContentAsync?.Invoke(route, request!) ?? Result.Fail("No PUT handler configured."));

        public Task<Result> PostNoContentAsync<TRequest>(string route, TRequest request, CancellationToken ct)
            => Task.FromResult(OnPostNoContentAsync?.Invoke(route, request!) ?? Result.Fail("No POST no-content handler configured."));

        public Task<Result<string>> GetStringResultAsync(string route, CancellationToken ct)
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
    }
}
