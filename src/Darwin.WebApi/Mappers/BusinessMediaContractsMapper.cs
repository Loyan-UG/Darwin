using Darwin.Application.Businesses.DTOs;
using Darwin.Contracts.Businesses;

namespace Darwin.WebApi.Mappers;

public static class BusinessMediaContractsMapper
{
    public static BusinessMediaLibrary ToContract(BusinessMediaLibraryDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        return new BusinessMediaLibrary
        {
            BusinessId = dto.BusinessId,
            ProfileImageUrl = dto.ProfileImageUrl,
            Gallery = dto.Gallery.Select(ToContract).ToList()
        };
    }

    public static BusinessMediaItem ToContract(BusinessMediaItemDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        return new BusinessMediaItem
        {
            Id = dto.Id,
            BusinessId = dto.BusinessId,
            BusinessLocationId = dto.BusinessLocationId,
            Url = dto.Url,
            Caption = dto.Caption,
            SortOrder = dto.SortOrder,
            IsPrimary = dto.IsPrimary,
            RowVersion = dto.RowVersion ?? Array.Empty<byte>()
        };
    }
}
