using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Businesses.DTOs;
using Darwin.Domain.Entities.Businesses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Businesses.Commands;

public sealed class UpdateBusinessProfileImageHandler
{
    private readonly IAppDbContext _db;
    private readonly IStringLocalizer<ValidationResource> _localizer;

    public UpdateBusinessProfileImageHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    }

    public async Task HandleAsync(BusinessProfileImageEditDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (dto.BusinessId == Guid.Empty)
        {
            throw new InvalidOperationException(_localizer["BusinessIdRequired"]);
        }

        var business = await _db.Set<Business>()
            .FirstOrDefaultAsync(x => x.Id == dto.BusinessId && !x.IsDeleted, ct)
            .ConfigureAwait(false);

        if (business is null)
        {
            throw new InvalidOperationException(_localizer["BusinessNotFound"]);
        }

        var normalized = string.IsNullOrWhiteSpace(dto.ProfileImageUrl) ? null : dto.ProfileImageUrl.Trim();
        if (normalized is { Length: > 1000 })
        {
            throw new InvalidOperationException(_localizer["UrlMaximumLengthExceeded"]);
        }

        business.BrandLogoUrl = normalized;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
