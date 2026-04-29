namespace Darwin.Application.Shipping
{
    internal static class ShippingMethodConventions
    {
        public const string DhlCarrier = "DHL";

        public static string NormalizeCarrier(string carrier)
        {
            var normalized = carrier.Trim();
            return string.Equals(normalized, DhlCarrier, System.StringComparison.OrdinalIgnoreCase)
                ? DhlCarrier
                : normalized;
        }
    }
}
