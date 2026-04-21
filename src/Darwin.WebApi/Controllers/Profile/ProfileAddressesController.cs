using Darwin.Application;
using Darwin.Application.CRM.Queries;
using Darwin.Application.Identity.Commands;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Queries;
using Darwin.Contracts.Profile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Darwin.WebApi.Controllers.Profile;

/// <summary>
/// Member-profile endpoints for reusable address-book management and CRM linkage visibility.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/member/profile")]
public sealed class ProfileAddressesController : ApiControllerBase
{
    private readonly GetCurrentUserAddressesHandler _getCurrentUserAddressesHandler;
    private readonly CreateCurrentUserAddressHandler _createCurrentUserAddressHandler;
    private readonly UpdateCurrentUserAddressHandler _updateCurrentUserAddressHandler;
    private readonly DeleteCurrentUserAddressHandler _deleteCurrentUserAddressHandler;
    private readonly SetCurrentUserDefaultAddressHandler _setCurrentUserDefaultAddressHandler;
    private readonly GetCurrentMemberCustomerProfileHandler _getCurrentMemberCustomerProfileHandler;
    private readonly GetCurrentMemberCustomerContextHandler _getCurrentMemberCustomerContextHandler;
    private readonly IStringLocalizer<ValidationResource> _validationLocalizer;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileAddressesController"/> class.
    /// </summary>
    public ProfileAddressesController(
        GetCurrentUserAddressesHandler getCurrentUserAddressesHandler,
        CreateCurrentUserAddressHandler createCurrentUserAddressHandler,
        UpdateCurrentUserAddressHandler updateCurrentUserAddressHandler,
        DeleteCurrentUserAddressHandler deleteCurrentUserAddressHandler,
        SetCurrentUserDefaultAddressHandler setCurrentUserDefaultAddressHandler,
        GetCurrentMemberCustomerProfileHandler getCurrentMemberCustomerProfileHandler,
        GetCurrentMemberCustomerContextHandler getCurrentMemberCustomerContextHandler,
        IStringLocalizer<ValidationResource> validationLocalizer)
    {
        _getCurrentUserAddressesHandler = getCurrentUserAddressesHandler ?? throw new ArgumentNullException(nameof(getCurrentUserAddressesHandler));
        _createCurrentUserAddressHandler = createCurrentUserAddressHandler ?? throw new ArgumentNullException(nameof(createCurrentUserAddressHandler));
        _updateCurrentUserAddressHandler = updateCurrentUserAddressHandler ?? throw new ArgumentNullException(nameof(updateCurrentUserAddressHandler));
        _deleteCurrentUserAddressHandler = deleteCurrentUserAddressHandler ?? throw new ArgumentNullException(nameof(deleteCurrentUserAddressHandler));
        _setCurrentUserDefaultAddressHandler = setCurrentUserDefaultAddressHandler ?? throw new ArgumentNullException(nameof(setCurrentUserDefaultAddressHandler));
        _getCurrentMemberCustomerProfileHandler = getCurrentMemberCustomerProfileHandler ?? throw new ArgumentNullException(nameof(getCurrentMemberCustomerProfileHandler));
        _getCurrentMemberCustomerContextHandler = getCurrentMemberCustomerContextHandler ?? throw new ArgumentNullException(nameof(getCurrentMemberCustomerContextHandler));
        _validationLocalizer = validationLocalizer ?? throw new ArgumentNullException(nameof(validationLocalizer));
    }

    /// <summary>
    /// Returns the current member's reusable address book.
    /// </summary>
    [HttpGet("addresses")]
    [HttpGet("/api/v1/profile/me/addresses")]
    [ProducesResponseType(typeof(IReadOnlyList<MemberAddress>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAddressesAsync(CancellationToken ct = default)
    {
        var result = await _getCurrentUserAddressesHandler.HandleAsync(ct).ConfigureAwait(false);
        if (!result.Succeeded || result.Value is null)
        {
            return ProblemFromResult(result);
        }

        return Ok(result.Value.Select(MapAddress).ToList());
    }

    /// <summary>
    /// Creates a new address in the current member's reusable address book.
    /// </summary>
    [HttpPost("addresses")]
    [HttpPost("/api/v1/profile/me/addresses")]
    [ProducesResponseType(typeof(MemberAddress), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAddressAsync([FromBody] CreateMemberAddressRequest? request, CancellationToken ct = default)
    {
        if (request is null)
        {
            return BadRequestProblem(_validationLocalizer["RequestPayloadRequired"]);
        }

        var createResult = await _createCurrentUserAddressHandler.HandleAsync(new AddressCreateDto
        {
            FullName = request.FullName,
            Company = request.Company,
            Street1 = request.Street1,
            Street2 = request.Street2,
            PostalCode = request.PostalCode,
            City = request.City,
            State = request.State,
            CountryCode = request.CountryCode,
            PhoneE164 = request.PhoneE164,
            IsDefaultBilling = request.IsDefaultBilling,
            IsDefaultShipping = request.IsDefaultShipping
        }, ct).ConfigureAwait(false);

        if (!createResult.Succeeded || createResult.Value == Guid.Empty)
        {
            return ProblemFromResult(createResult);
        }

        return await GetAddressByIdAsync(createResult.Value, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Updates an address owned by the current member.
    /// </summary>
    [HttpPut("addresses/{id:guid}")]
    [HttpPut("/api/v1/profile/me/addresses/{id:guid}")]
    [ProducesResponseType(typeof(MemberAddress), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAddressAsync(Guid id, [FromBody] UpdateMemberAddressRequest? request, CancellationToken ct = default)
    {
        if (request is null)
        {
            return BadRequestProblem(_validationLocalizer["RequestPayloadRequired"]);
        }

        if (id == Guid.Empty)
        {
            return BadRequestProblem(_validationLocalizer["AddressIdRequired"]);
        }

        var result = await _updateCurrentUserAddressHandler.HandleAsync(new AddressEditDto
        {
            Id = id,
            RowVersion = request.RowVersion,
            FullName = request.FullName,
            Company = request.Company,
            Street1 = request.Street1,
            Street2 = request.Street2,
            PostalCode = request.PostalCode,
            City = request.City,
            State = request.State,
            CountryCode = request.CountryCode,
            PhoneE164 = request.PhoneE164,
            IsDefaultBilling = request.IsDefaultBilling,
            IsDefaultShipping = request.IsDefaultShipping
        }, ct).ConfigureAwait(false);

        if (!result.Succeeded)
        {
            return ProblemFromResult(result);
        }

        return await GetAddressByIdAsync(id, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Soft-deletes an address owned by the current member.
    /// </summary>
    [HttpPost("addresses/{id:guid}/delete")]
    [HttpPost("/api/v1/profile/me/addresses/{id:guid}/delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteAddressAsync(Guid id, [FromBody] DeleteMemberAddressRequest? request, CancellationToken ct = default)
    {
        if (request is null)
        {
            return BadRequestProblem(_validationLocalizer["RequestPayloadRequired"]);
        }

        var result = await _deleteCurrentUserAddressHandler.HandleAsync(new AddressDeleteDto
        {
            Id = id,
            RowVersion = request.RowVersion
        }, ct).ConfigureAwait(false);

        return result.Succeeded ? NoContent() : ProblemFromResult(result);
    }

    /// <summary>
    /// Sets billing and/or shipping defaults for an address owned by the current member.
    /// </summary>
    [HttpPost("addresses/{id:guid}/default")]
    [HttpPost("/api/v1/profile/me/addresses/{id:guid}/default")]
    [ProducesResponseType(typeof(MemberAddress), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetDefaultAddressAsync(Guid id, [FromBody] SetMemberDefaultAddressRequest? request, CancellationToken ct = default)
    {
        if (request is null)
        {
            return BadRequestProblem(_validationLocalizer["RequestPayloadRequired"]);
        }

        var result = await _setCurrentUserDefaultAddressHandler
            .HandleAsync(id, request.AsBilling, request.AsShipping, ct)
            .ConfigureAwait(false);

        if (!result.Succeeded)
        {
            return ProblemFromResult(result);
        }

        return await GetAddressByIdAsync(id, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Returns the CRM customer profile linked to the current member identity, when one exists.
    /// </summary>
    [HttpGet("customer")]
    [HttpGet("/api/v1/profile/me/customer")]
    [ProducesResponseType(typeof(LinkedCustomerProfile), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLinkedCustomerAsync(CancellationToken ct = default)
    {
        var dto = await _getCurrentMemberCustomerProfileHandler.HandleAsync(ct).ConfigureAwait(false);
        return dto is null ? NotFoundProblem(_validationLocalizer["LinkedCustomerNotFound"]) : Ok(MapCustomer(dto));
    }

    /// <summary>
    /// Returns richer CRM customer context for the current member identity, including segments, consents, and recent interactions.
    /// </summary>
    [HttpGet("customer/context")]
    [HttpGet("/api/v1/profile/me/customer/context")]
    [ProducesResponseType(typeof(MemberCustomerContext), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLinkedCustomerContextAsync(CancellationToken ct = default)
    {
        var dto = await _getCurrentMemberCustomerContextHandler.HandleAsync(ct).ConfigureAwait(false);
        return dto is null ? NotFoundProblem(_validationLocalizer["LinkedCustomerContextNotFound"]) : Ok(MapCustomerContext(dto));
    }

    private async Task<IActionResult> GetAddressByIdAsync(Guid id, CancellationToken ct)
    {
        var result = await _getCurrentUserAddressesHandler.HandleAsync(ct).ConfigureAwait(false);
        if (!result.Succeeded || result.Value is null)
        {
            return ProblemFromResult(result);
        }

        var address = result.Value.FirstOrDefault(x => x.Id == id);
        return address is null ? NotFoundProblem(_validationLocalizer["AddressNotFound"]) : Ok(MapAddress(address));
    }

    private static MemberAddress MapAddress(AddressListItemDto dto)
        => new()
        {
            Id = dto.Id,
            RowVersion = dto.RowVersion,
            FullName = dto.FullName,
            Company = dto.Company,
            Street1 = dto.Street1,
            Street2 = dto.Street2,
            PostalCode = dto.PostalCode,
            City = dto.City,
            State = dto.State,
            CountryCode = dto.CountryCode,
            PhoneE164 = dto.PhoneE164,
            IsDefaultBilling = dto.IsDefaultBilling,
            IsDefaultShipping = dto.IsDefaultShipping
        };

    private static LinkedCustomerProfile MapCustomer(Darwin.Application.CRM.DTOs.MemberCustomerProfileDto dto)
        => new()
        {
            Id = dto.Id,
            UserId = dto.UserId,
            DisplayName = dto.DisplayName,
            Email = dto.Email,
            Phone = dto.Phone,
            CompanyName = dto.CompanyName,
            CreatedAtUtc = dto.CreatedAtUtc
        };

    private static MemberCustomerContext MapCustomerContext(Darwin.Application.CRM.DTOs.MemberCustomerContextDto dto)
        => new()
        {
            Id = dto.Id,
            UserId = dto.UserId,
            DisplayName = dto.DisplayName,
            Email = dto.Email,
            Phone = dto.Phone,
            CompanyName = dto.CompanyName,
            Notes = dto.Notes,
            CreatedAtUtc = dto.CreatedAtUtc,
            LastInteractionAtUtc = dto.LastInteractionAtUtc,
            InteractionCount = dto.InteractionCount,
            Segments = dto.Segments.Select(x => new MemberCustomerSegment
            {
                SegmentId = x.SegmentId,
                Name = x.Name,
                Description = x.Description
            }).ToList(),
            Consents = dto.Consents.Select(x => new MemberCustomerConsent
            {
                Id = x.Id,
                Type = x.Type,
                Granted = x.Granted,
                GrantedAtUtc = x.GrantedAtUtc,
                RevokedAtUtc = x.RevokedAtUtc
            }).ToList(),
            RecentInteractions = dto.RecentInteractions.Select(x => new MemberCustomerInteraction
            {
                Id = x.Id,
                Type = x.Type,
                Channel = x.Channel,
                Subject = x.Subject,
                ContentPreview = x.ContentPreview,
                CreatedAtUtc = x.CreatedAtUtc
            }).ToList()
        };
}
