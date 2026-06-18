using Darwin.Application.Businesses.DTOs;
using Darwin.Application.CRM.DTOs;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Orders.DTOs;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Identity;

namespace Darwin.Application.Common.Addresses;

/// <summary>
/// Internal canonical address shape used to keep address mapping consistent without changing public contracts.
/// </summary>
public sealed class CanonicalAddress
{
    public string FullName { get; set; } = string.Empty;
    public string? Company { get; set; }
    public string Street1 { get; set; } = string.Empty;
    public string? Street2 { get; set; }
    public string PostalCode { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? StateOrRegion { get; set; }
    public string CountryCode { get; set; } = string.Empty;
    public string? PhoneE164 { get; set; }
}

/// <summary>
/// Normalization rules shared by address mappers. Validation remains in the existing validators.
/// </summary>
public static class CanonicalAddressNormalizer
{
    public static CanonicalAddress Normalize(CanonicalAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);

        return new CanonicalAddress
        {
            FullName = Required(address.FullName),
            Company = Optional(address.Company),
            Street1 = Required(address.Street1),
            Street2 = Optional(address.Street2),
            PostalCode = Required(address.PostalCode),
            City = Required(address.City),
            StateOrRegion = Optional(address.StateOrRegion),
            CountryCode = CountryCode(address.CountryCode),
            PhoneE164 = Optional(address.PhoneE164)
        };
    }

    public static string Required(string? value) => value?.Trim() ?? string.Empty;

    public static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static string CountryCode(string? value) => Required(value).ToUpperInvariant();
}

/// <summary>
/// Converts between Darwin's current address-bearing records and the internal canonical shape.
/// Public contract property names remain unchanged.
/// </summary>
public static class CanonicalAddressMapper
{
    public static CanonicalAddress FromAddress(Address address)
    {
        ArgumentNullException.ThrowIfNull(address);

        return CanonicalAddressNormalizer.Normalize(new CanonicalAddress
        {
            FullName = address.FullName,
            Company = address.Company,
            Street1 = address.Street1,
            Street2 = address.Street2,
            PostalCode = address.PostalCode,
            City = address.City,
            StateOrRegion = address.State,
            CountryCode = address.CountryCode,
            PhoneE164 = address.PhoneE164
        });
    }

    public static CanonicalAddress FromAddressDto(AddressCreateDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return CanonicalAddressNormalizer.Normalize(new CanonicalAddress
        {
            FullName = dto.FullName,
            Company = dto.Company,
            Street1 = dto.Street1,
            Street2 = dto.Street2,
            PostalCode = dto.PostalCode,
            City = dto.City,
            StateOrRegion = dto.State,
            CountryCode = dto.CountryCode,
            PhoneE164 = dto.PhoneE164
        });
    }

    public static CanonicalAddress FromAddressDto(AddressEditDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return CanonicalAddressNormalizer.Normalize(new CanonicalAddress
        {
            FullName = dto.FullName,
            Company = dto.Company,
            Street1 = dto.Street1,
            Street2 = dto.Street2,
            PostalCode = dto.PostalCode,
            City = dto.City,
            StateOrRegion = dto.State,
            CountryCode = dto.CountryCode,
            PhoneE164 = dto.PhoneE164
        });
    }

    public static AddressListItemDto ToAddressListItemDto(Address address)
    {
        var canonical = FromAddress(address);

        return new AddressListItemDto
        {
            Id = address.Id,
            RowVersion = address.RowVersion,
            FullName = canonical.FullName,
            Company = canonical.Company,
            Street1 = canonical.Street1,
            Street2 = canonical.Street2,
            PostalCode = canonical.PostalCode,
            City = canonical.City,
            State = canonical.StateOrRegion,
            CountryCode = canonical.CountryCode,
            PhoneE164 = canonical.PhoneE164,
            IsDefaultBilling = address.IsDefaultBilling,
            IsDefaultShipping = address.IsDefaultShipping
        };
    }

    public static IdentityAddressSummaryDto ToIdentityAddressSummaryDto(Address address)
    {
        var canonical = FromAddress(address);

        return new IdentityAddressSummaryDto
        {
            Id = address.Id,
            FullName = canonical.FullName,
            Street1 = canonical.Street1,
            Street2 = canonical.Street2,
            PostalCode = canonical.PostalCode,
            City = canonical.City,
            State = canonical.StateOrRegion,
            CountryCode = canonical.CountryCode,
            PhoneE164 = canonical.PhoneE164,
            IsDefaultBilling = address.IsDefaultBilling,
            IsDefaultShipping = address.IsDefaultShipping
        };
    }

    public static void ApplyToAddress(Address address, CanonicalAddress canonical)
    {
        ArgumentNullException.ThrowIfNull(address);
        canonical = CanonicalAddressNormalizer.Normalize(canonical);

        address.FullName = canonical.FullName;
        address.Company = canonical.Company;
        address.Street1 = canonical.Street1;
        address.Street2 = canonical.Street2;
        address.PostalCode = canonical.PostalCode;
        address.City = canonical.City;
        address.State = canonical.StateOrRegion;
        address.CountryCode = canonical.CountryCode;
        address.PhoneE164 = canonical.PhoneE164;
    }

    public static CanonicalAddress FromCustomerAddress(CustomerAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);

        return CanonicalAddressNormalizer.Normalize(new CanonicalAddress
        {
            Street1 = address.Line1,
            Street2 = address.Line2,
            PostalCode = address.PostalCode,
            City = address.City,
            StateOrRegion = address.State,
            CountryCode = address.Country
        });
    }

    public static CanonicalAddress FromCustomerAddressDto(CustomerAddressDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return CanonicalAddressNormalizer.Normalize(new CanonicalAddress
        {
            Street1 = dto.Line1,
            Street2 = dto.Line2,
            PostalCode = dto.PostalCode,
            City = dto.City,
            StateOrRegion = dto.State,
            CountryCode = dto.Country
        });
    }

    public static CustomerAddress ToCustomerAddress(CustomerAddressDto dto)
    {
        var canonical = FromCustomerAddressDto(dto);

        return new CustomerAddress
        {
            AddressId = dto.AddressId,
            Line1 = canonical.Street1,
            Line2 = canonical.Street2,
            City = canonical.City,
            State = canonical.StateOrRegion,
            PostalCode = canonical.PostalCode,
            Country = canonical.CountryCode,
            IsDefaultBilling = dto.IsDefaultBilling,
            IsDefaultShipping = dto.IsDefaultShipping
        };
    }

    public static CustomerAddressDto ToCustomerAddressDto(CustomerAddress address)
    {
        var canonical = FromCustomerAddress(address);

        return new CustomerAddressDto
        {
            Id = address.Id,
            AddressId = address.AddressId,
            Line1 = canonical.Street1,
            Line2 = canonical.Street2,
            City = canonical.City,
            State = canonical.StateOrRegion,
            PostalCode = canonical.PostalCode,
            Country = canonical.CountryCode,
            IsDefaultBilling = address.IsDefaultBilling,
            IsDefaultShipping = address.IsDefaultShipping
        };
    }

    public static void ApplyToCustomerAddress(CustomerAddress address, CustomerAddressDto dto)
    {
        ArgumentNullException.ThrowIfNull(address);
        var canonical = FromCustomerAddressDto(dto);

        address.AddressId = dto.AddressId;
        address.Line1 = canonical.Street1;
        address.Line2 = canonical.Street2;
        address.City = canonical.City;
        address.State = canonical.StateOrRegion;
        address.PostalCode = canonical.PostalCode;
        address.Country = canonical.CountryCode;
        address.IsDefaultBilling = dto.IsDefaultBilling;
        address.IsDefaultShipping = dto.IsDefaultShipping;
    }

    public static CanonicalAddress FromBusinessLocation(BusinessLocation location)
    {
        ArgumentNullException.ThrowIfNull(location);

        return CanonicalAddressNormalizer.Normalize(new CanonicalAddress
        {
            Street1 = location.AddressLine1 ?? string.Empty,
            Street2 = location.AddressLine2,
            PostalCode = location.PostalCode ?? string.Empty,
            City = location.City ?? string.Empty,
            StateOrRegion = location.Region,
            CountryCode = location.CountryCode ?? string.Empty
        });
    }

    public static CanonicalAddress FromBusinessLocationDto(BusinessLocationCreateDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return CanonicalAddressNormalizer.Normalize(new CanonicalAddress
        {
            Street1 = dto.AddressLine1 ?? string.Empty,
            Street2 = dto.AddressLine2,
            PostalCode = dto.PostalCode ?? string.Empty,
            City = dto.City ?? string.Empty,
            StateOrRegion = dto.Region,
            CountryCode = dto.CountryCode ?? string.Empty
        });
    }

    public static CanonicalAddress FromBusinessLocationDto(BusinessLocationEditDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return CanonicalAddressNormalizer.Normalize(new CanonicalAddress
        {
            Street1 = dto.AddressLine1 ?? string.Empty,
            Street2 = dto.AddressLine2,
            PostalCode = dto.PostalCode ?? string.Empty,
            City = dto.City ?? string.Empty,
            StateOrRegion = dto.Region,
            CountryCode = dto.CountryCode ?? string.Empty
        });
    }

    public static void ApplyToBusinessLocation(BusinessLocation location, CanonicalAddress canonical)
    {
        ArgumentNullException.ThrowIfNull(location);
        canonical = CanonicalAddressNormalizer.Normalize(canonical);

        location.AddressLine1 = canonical.Street1.Length == 0 ? null : canonical.Street1;
        location.AddressLine2 = canonical.Street2;
        location.PostalCode = canonical.PostalCode.Length == 0 ? null : canonical.PostalCode;
        location.City = canonical.City.Length == 0 ? null : canonical.City;
        location.Region = canonical.StateOrRegion;
        location.CountryCode = canonical.CountryCode.Length == 0 ? null : canonical.CountryCode;
    }

    public static CanonicalAddress FromBusinessPublicLocationDto(BusinessPublicLocationDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return CanonicalAddressNormalizer.Normalize(new CanonicalAddress
        {
            Street1 = dto.AddressLine1 ?? string.Empty,
            Street2 = dto.AddressLine2,
            PostalCode = dto.PostalCode ?? string.Empty,
            City = dto.City ?? string.Empty,
            StateOrRegion = dto.Region,
            CountryCode = dto.CountryCode ?? string.Empty
        });
    }

    public static void ApplyToBusinessPublicLocationDto(BusinessPublicLocationDto dto, CanonicalAddress canonical)
    {
        ArgumentNullException.ThrowIfNull(dto);
        canonical = CanonicalAddressNormalizer.Normalize(canonical);

        dto.AddressLine1 = canonical.Street1.Length == 0 ? null : canonical.Street1;
        dto.AddressLine2 = canonical.Street2;
        dto.PostalCode = canonical.PostalCode.Length == 0 ? null : canonical.PostalCode;
        dto.City = canonical.City.Length == 0 ? null : canonical.City;
        dto.Region = canonical.StateOrRegion;
        dto.CountryCode = canonical.CountryCode.Length == 0 ? null : canonical.CountryCode;
    }

    public static CanonicalAddress FromCheckoutAddress(CheckoutAddressDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return CanonicalAddressNormalizer.Normalize(new CanonicalAddress
        {
            FullName = dto.FullName,
            Company = dto.Company,
            Street1 = dto.Street1,
            Street2 = dto.Street2,
            PostalCode = dto.PostalCode,
            City = dto.City,
            StateOrRegion = dto.State,
            CountryCode = dto.CountryCode,
            PhoneE164 = dto.PhoneE164
        });
    }

    public static CheckoutAddressDto ToCheckoutAddressDto(CanonicalAddress canonical)
    {
        canonical = CanonicalAddressNormalizer.Normalize(canonical);

        return new CheckoutAddressDto
        {
            FullName = canonical.FullName,
            Company = canonical.Company,
            Street1 = canonical.Street1,
            Street2 = canonical.Street2,
            PostalCode = canonical.PostalCode,
            City = canonical.City,
            State = canonical.StateOrRegion,
            CountryCode = canonical.CountryCode,
            PhoneE164 = canonical.PhoneE164
        };
    }
}
