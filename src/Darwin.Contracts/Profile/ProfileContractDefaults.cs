using Darwin.Contracts.Common;

namespace Darwin.Contracts.Profile;

/// <summary>
/// Shared contract-level defaults for member profile and address preferences.
/// </summary>
public static class ProfileContractDefaults
{
    public const string DefaultLocale = ContractDefaults.DefaultLocale;
    public const string DefaultTimezone = ContractDefaults.DefaultTimezone;
    public const string DefaultCurrency = ContractDefaults.DefaultCurrency;
    public const string DefaultCountryCode = ContractDefaults.DefaultCountryCode;
}
