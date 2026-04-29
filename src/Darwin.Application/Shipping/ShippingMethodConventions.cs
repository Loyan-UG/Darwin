namespace Darwin.Application.Shipping
{
    internal static class ShippingMethodConventions
    {
        public const string DhlCarrier = "DHL";
        public const int NameMaxLength = 200;
        public const int CarrierMaxLength = 50;
        public const int ServiceMaxLength = 50;
        public const int CountriesCsvMaxLength = 255;

        public static string NormalizeCarrier(string carrier)
        {
            var normalized = carrier.Trim();
            return string.Equals(normalized, DhlCarrier, System.StringComparison.OrdinalIgnoreCase)
                ? DhlCarrier
                : normalized;
        }
    }
}
