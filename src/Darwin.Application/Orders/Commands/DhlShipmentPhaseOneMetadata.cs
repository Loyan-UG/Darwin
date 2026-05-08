using System;
using System.Text.Json;
using Darwin.Application.Orders.DTOs;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Entities.Settings;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Orders.Commands
{
    internal static class DhlShipmentPhaseOneMetadata
    {
        public static bool IsDhlCarrier(string? carrier)
        {
            return string.Equals(carrier?.Trim(), "DHL", StringComparison.OrdinalIgnoreCase);
        }

        public static bool HasLabelGenerationReadiness(SiteSetting settings)
        {
            return !string.IsNullOrWhiteSpace(settings.DhlApiBaseUrl) &&
                   !string.IsNullOrWhiteSpace(settings.DhlApiKey) &&
                   !string.IsNullOrWhiteSpace(settings.DhlApiSecret) &&
                   !string.IsNullOrWhiteSpace(settings.DhlAccountNumber) &&
                   !string.IsNullOrWhiteSpace(settings.DhlShipperName) &&
                   !string.IsNullOrWhiteSpace(settings.DhlShipperEmail) &&
                   !string.IsNullOrWhiteSpace(settings.DhlShipperPhoneE164) &&
                   !string.IsNullOrWhiteSpace(settings.DhlShipperStreet) &&
                   !string.IsNullOrWhiteSpace(settings.DhlShipperPostalCode) &&
                   !string.IsNullOrWhiteSpace(settings.DhlShipperCity) &&
                   !string.IsNullOrWhiteSpace(settings.DhlShipperCountry);
        }

        public static CheckoutAddressDto ParseShippingAddress(string? shippingAddressJson, IStringLocalizer<ValidationResource> localizer)
        {
            if (string.IsNullOrWhiteSpace(shippingAddressJson))
            {
                throw new InvalidOperationException(localizer["DhlShipmentShippingAddressRequired"]);
            }

            try
            {
                var address = JsonSerializer.Deserialize<CheckoutAddressDto>(shippingAddressJson);
                if (address is null ||
                    string.IsNullOrWhiteSpace(address.FullName) ||
                    string.IsNullOrWhiteSpace(address.Street1) ||
                    string.IsNullOrWhiteSpace(address.PostalCode) ||
                    string.IsNullOrWhiteSpace(address.City) ||
                    string.IsNullOrWhiteSpace(address.CountryCode))
                {
                    throw new InvalidOperationException(localizer["DhlShipmentShippingAddressRequired"]);
                }

                address.FullName = address.FullName.Trim();
                address.Company = string.IsNullOrWhiteSpace(address.Company) ? null : address.Company.Trim();
                address.Street1 = address.Street1.Trim();
                address.Street2 = string.IsNullOrWhiteSpace(address.Street2) ? null : address.Street2.Trim();
                address.PostalCode = address.PostalCode.Trim();
                address.City = address.City.Trim();
                address.State = string.IsNullOrWhiteSpace(address.State) ? null : address.State.Trim();
                address.CountryCode = address.CountryCode.Trim().ToUpperInvariant();
                address.PhoneE164 = string.IsNullOrWhiteSpace(address.PhoneE164) ? null : address.PhoneE164.Trim();
                return address;
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException(localizer["DhlShipmentShippingAddressRequired"], ex);
            }
        }
    }
}
