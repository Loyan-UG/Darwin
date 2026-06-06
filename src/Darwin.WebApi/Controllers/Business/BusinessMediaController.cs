using Darwin.Application;
using Darwin.Application.Businesses.Commands;
using Darwin.Application.Businesses.DTOs;
using Darwin.Application.Businesses.Queries;
using Darwin.Contracts.Businesses;
using Darwin.Infrastructure.Media;
using Darwin.WebApi.Controllers.Businesses;
using Darwin.WebApi.Mappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Darwin.WebApi.Controllers.Business;

[ApiController]
[Authorize]
[Route("api/v1/business/account/media")]
public sealed class BusinessMediaController : ApiControllerBase
{
    private const long MaxUploadBytes = 5 * 1024 * 1024;

    private readonly GetBusinessMediaLibraryHandler _getMedia;
    private readonly UpdateBusinessProfileImageHandler _updateProfileImage;
    private readonly CreateBusinessMediaHandler _createMedia;
    private readonly UpdateBusinessMediaHandler _updateMedia;
    private readonly DeleteBusinessMediaHandler _deleteMedia;
    private readonly IUploadedImageStorageService _uploads;
    private readonly IStringLocalizer<ValidationResource> _localizer;

    public BusinessMediaController(
        GetBusinessMediaLibraryHandler getMedia,
        UpdateBusinessProfileImageHandler updateProfileImage,
        CreateBusinessMediaHandler createMedia,
        UpdateBusinessMediaHandler updateMedia,
        DeleteBusinessMediaHandler deleteMedia,
        IUploadedImageStorageService uploads,
        IStringLocalizer<ValidationResource> localizer)
    {
        _getMedia = getMedia;
        _updateProfileImage = updateProfileImage;
        _createMedia = createMedia;
        _updateMedia = updateMedia;
        _deleteMedia = deleteMedia;
        _uploads = uploads;
        _localizer = localizer;
    }

    [HttpGet]
    [ProducesResponseType(typeof(BusinessMediaLibrary), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync(CancellationToken ct = default)
    {
        if (!BusinessControllerConventions.TryGetCurrentBusinessId(User, out var businessId))
        {
            return BadRequestProblem(_localizer["BusinessRequired"]);
        }

        var dto = await _getMedia.HandleAsync(businessId, ct).ConfigureAwait(false);
        return dto is null
            ? NotFoundProblem(_localizer["BusinessNotFound"])
            : Ok(BusinessMediaContractsMapper.ToContract(dto));
    }

    [HttpPost("profile-image")]
    public async Task<IActionResult> SetProfileImageAsync([FromBody] SetBusinessProfileImageRequest? request, CancellationToken ct = default)
    {
        if (request is null)
        {
            return BadRequestProblem(_localizer["RequestPayloadRequired"]);
        }

        if (!BusinessControllerConventions.TryGetCurrentBusinessId(User, out var businessId))
        {
            return BadRequestProblem(_localizer["BusinessRequired"]);
        }

        await _updateProfileImage.HandleAsync(new BusinessProfileImageEditDto
        {
            BusinessId = businessId,
            ProfileImageUrl = request.ProfileImageUrl
        }, ct).ConfigureAwait(false);

        return NoContent();
    }

    [HttpPost("gallery")]
    [ProducesResponseType(typeof(BusinessMediaItem), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateGalleryImageAsync([FromBody] CreateBusinessMediaRequest? request, CancellationToken ct = default)
    {
        if (request is null)
        {
            return BadRequestProblem(_localizer["RequestPayloadRequired"]);
        }

        if (!BusinessControllerConventions.TryGetCurrentBusinessId(User, out var businessId))
        {
            return BadRequestProblem(_localizer["BusinessRequired"]);
        }

        var id = await _createMedia.HandleAsync(new BusinessMediaCreateDto
        {
            BusinessId = businessId,
            BusinessLocationId = request.BusinessLocationId,
            Url = request.Url,
            Caption = request.Caption,
            SortOrder = request.SortOrder.GetValueOrDefault(),
            IsPrimary = request.IsPrimary
        }, ct).ConfigureAwait(false);

        var library = await _getMedia.HandleAsync(businessId, ct).ConfigureAwait(false);
        var item = library?.Gallery.FirstOrDefault(x => x.Id == id);
        return item is null ? NotFoundProblem(_localizer["BusinessMediaNotFound"]) : Ok(BusinessMediaContractsMapper.ToContract(item));
    }

    [HttpPut("gallery/{id:guid}")]
    public async Task<IActionResult> UpdateGalleryImageAsync([FromRoute] Guid id, [FromBody] UpdateBusinessMediaRequest? request, CancellationToken ct = default)
    {
        if (request is null)
        {
            return BadRequestProblem(_localizer["RequestPayloadRequired"]);
        }

        if (!BusinessControllerConventions.TryGetCurrentBusinessId(User, out var businessId))
        {
            return BadRequestProblem(_localizer["BusinessRequired"]);
        }

        await _updateMedia.HandleAsync(new BusinessMediaEditDto
        {
            Id = id,
            BusinessId = businessId,
            BusinessLocationId = request.BusinessLocationId,
            Url = request.Url,
            Caption = request.Caption,
            SortOrder = request.SortOrder,
            IsPrimary = request.IsPrimary,
            RowVersion = request.RowVersion
        }, ct).ConfigureAwait(false);

        return NoContent();
    }

    [HttpPost("gallery/{id:guid}/delete")]
    public async Task<IActionResult> DeleteGalleryImageAsync([FromRoute] Guid id, [FromBody] DeleteBusinessMediaRequest? request, CancellationToken ct = default)
    {
        if (request is null)
        {
            return BadRequestProblem(_localizer["RequestPayloadRequired"]);
        }

        var result = await _deleteMedia.HandleAsync(new BusinessMediaDeleteDto
        {
            Id = id,
            RowVersion = request.RowVersion
        }, ct).ConfigureAwait(false);

        return result.Succeeded ? NoContent() : BadRequestProblem(result.Error ?? _localizer["InvalidDeleteRequest"]);
    }

    [HttpPost("upload")]
    [RequestSizeLimit(MaxUploadBytes)]
    [ProducesResponseType(typeof(BusinessImageUploadResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadAsync(IFormFile? file, CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequestProblem(_localizer["MediaUploadFileRequired"]);
        }

        if (file.Length > MaxUploadBytes)
        {
            return BadRequestProblem(_localizer["MediaUploadFileTooLarge"]);
        }

        await using var stream = file.OpenReadStream();
        var stored = await _uploads.SaveAsync(stream, file.FileName, file.ContentType, ct).ConfigureAwait(false);
        return Ok(new BusinessImageUploadResponse { Url = stored.PublicUrl });
    }
}
