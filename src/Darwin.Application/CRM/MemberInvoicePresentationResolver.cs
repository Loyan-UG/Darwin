using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Darwin.Domain.Enums;

namespace Darwin.Application.CRM;

internal static partial class MemberInvoicePresentationResolver
{
    public static string ResolveLineDescription(string description, string? culture)
    {
        if (!IsEnglish(culture))
        {
            return description;
        }

        var match = CrmOfferPackageRegex().Match(description);
        return match.Success
            ? $"CRM offer package {match.Groups["number"].Value}"
            : description;
    }

    public static string BuildPaymentSummary(string provider, string currency, long amountMinor, PaymentStatus status, string? culture)
    {
        var amount = (amountMinor / 100.0M).ToString("0.00", ResolveCulture(culture));
        return $"{provider} | {currency} {amount} | {ResolvePaymentStatus(status, culture)}";
    }

    private static string ResolvePaymentStatus(PaymentStatus status, string? culture)
    {
        if (IsEnglish(culture))
        {
            return status switch
            {
                PaymentStatus.Pending => "Pending",
                PaymentStatus.Authorized => "Authorized",
                PaymentStatus.Captured => "Captured",
                PaymentStatus.Completed => "Completed",
                PaymentStatus.Failed => "Failed",
                PaymentStatus.Refunded => "Refunded",
                PaymentStatus.Voided => "Voided",
                _ => status.ToString()
            };
        }

        return status switch
        {
            PaymentStatus.Pending => "Ausstehend",
            PaymentStatus.Authorized => "Autorisiert",
            PaymentStatus.Captured => "Erfasst",
            PaymentStatus.Completed => "Abgeschlossen",
            PaymentStatus.Failed => "Fehlgeschlagen",
            PaymentStatus.Refunded => "Erstattet",
            PaymentStatus.Voided => "Storniert",
            _ => status.ToString()
        };
    }

    private static CultureInfo ResolveCulture(string? culture)
    {
        try
        {
            return string.IsNullOrWhiteSpace(culture)
                ? CultureInfo.GetCultureInfo("de-DE")
                : CultureInfo.GetCultureInfo(culture.Trim());
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.GetCultureInfo("de-DE");
        }
    }

    private static bool IsEnglish(string? culture)
        => !string.IsNullOrWhiteSpace(culture)
           && culture.StartsWith("en", StringComparison.OrdinalIgnoreCase);

    [GeneratedRegex(@"^CRM-Angebotspaket\s+(?<number>\d{2})$", RegexOptions.CultureInvariant)]
    private static partial Regex CrmOfferPackageRegex();
}
