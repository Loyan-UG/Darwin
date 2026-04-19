namespace Darwin.Domain.Common;

/// <summary>
/// Shared domain-model defaults for locale, timezone, currency, and country.
/// </summary>
public static class DomainDefaults
{
    public const string DefaultCulture = "de-DE";
    public const string DefaultTimezone = "Europe/Berlin";
    public const string DefaultCurrency = "EUR";
    public const string DefaultCountryCode = "DE";
    public const string SupportedCulturesCsv = "de-DE,en-US";
}
