using System;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Entities.Settings;

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

        public static string BuildProviderShipmentReference(Shipment shipment)
        {
            if (shipment.Id == Guid.Empty)
            {
                shipment.Id = Guid.NewGuid();
            }

            return $"dhl-ship-{shipment.Id:N}";
        }

        public static string BuildTrackingNumber(SiteSetting settings, Shipment shipment)
        {
            var account = string.IsNullOrWhiteSpace(settings.DhlAccountNumber)
                ? "DHL"
                : settings.DhlAccountNumber.Trim().ToUpperInvariant();

            if (shipment.Id == Guid.Empty)
            {
                shipment.Id = Guid.NewGuid();
            }

            var suffix = shipment.Id.ToString("N")[..12].ToUpperInvariant();
            return $"{account}-{suffix}";
        }

        public static string BuildLabelUrl(string baseUrl, string providerShipmentReference)
        {
            var normalizedBaseUrl = baseUrl.Trim().TrimEnd('/') + "/";
            var baseUri = new Uri(normalizedBaseUrl, UriKind.Absolute);
            var relative = $"shipments/{Uri.EscapeDataString(providerShipmentReference)}/label";
            return new Uri(baseUri, relative).ToString();
        }
    }
}
