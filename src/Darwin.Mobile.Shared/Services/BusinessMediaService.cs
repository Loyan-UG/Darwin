using Darwin.Contracts.Businesses;
using Darwin.Mobile.Shared.Api;
using Darwin.Shared.Results;

namespace Darwin.Mobile.Shared.Services;

public interface IBusinessMediaService
{
    Task<Result<BusinessMediaLibrary>> GetAsync(CancellationToken ct);
    Task<Result<BusinessImageUploadResponse>> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken ct);
    Task<Result> SetProfileImageAsync(string? url, CancellationToken ct);
    Task<Result<BusinessMediaItem>> CreateGalleryImageAsync(CreateBusinessMediaRequest request, CancellationToken ct);
    Task<Result> UpdateGalleryImageAsync(Guid id, UpdateBusinessMediaRequest request, CancellationToken ct);
    Task<Result> DeleteGalleryImageAsync(Guid id, DeleteBusinessMediaRequest request, CancellationToken ct);
}

public sealed class BusinessMediaService : IBusinessMediaService
{
    private readonly IApiClient _api;

    public BusinessMediaService(IApiClient api)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
    }

    public Task<Result<BusinessMediaLibrary>> GetAsync(CancellationToken ct)
        => _api.GetResultAsync<BusinessMediaLibrary>(ApiRoutes.BusinessAccount.GetMedia, ct);

    public Task<Result<BusinessImageUploadResponse>> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken ct)
        => _api.PostFileResultAsync<BusinessImageUploadResponse>(ApiRoutes.BusinessAccount.UploadMedia, stream, "file", fileName, contentType, ct);

    public Task<Result> SetProfileImageAsync(string? url, CancellationToken ct)
        => _api.PostNoContentAsync(ApiRoutes.BusinessAccount.SetProfileImage, new SetBusinessProfileImageRequest { ProfileImageUrl = url }, ct);

    public Task<Result<BusinessMediaItem>> CreateGalleryImageAsync(CreateBusinessMediaRequest request, CancellationToken ct)
        => _api.PostResultAsync<CreateBusinessMediaRequest, BusinessMediaItem>(ApiRoutes.BusinessAccount.CreateGalleryImage, request, ct);

    public Task<Result> UpdateGalleryImageAsync(Guid id, UpdateBusinessMediaRequest request, CancellationToken ct)
        => _api.PutNoContentAsync(ApiRoutes.BusinessAccount.UpdateGalleryImage(id), request, ct);

    public Task<Result> DeleteGalleryImageAsync(Guid id, DeleteBusinessMediaRequest request, CancellationToken ct)
        => _api.PostNoContentAsync(ApiRoutes.BusinessAccount.DeleteGalleryImage(id), request, ct);
}
