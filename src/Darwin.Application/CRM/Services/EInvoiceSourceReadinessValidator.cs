using System.Text.Json;
using Darwin.Application.Abstractions.Invoicing;
using Darwin.Domain.Entities.CRM;

namespace Darwin.Application.CRM.Services;

public sealed class EInvoiceSourceReadinessValidator
{
    public EInvoiceSourceReadinessResult Validate(Invoice invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        if (invoice.ArchivePurgedAtUtc.HasValue || string.IsNullOrWhiteSpace(invoice.IssuedSnapshotJson))
        {
            return new EInvoiceSourceReadinessResult(false, new[] { "issuedSnapshotJson" });
        }

        try
        {
            using var document = JsonDocument.Parse(invoice.IssuedSnapshotJson);
            var root = document.RootElement;
            var missing = new List<string>();

            RequireGuid(root, "invoiceId", missing);
            RequireString(root, "currency", missing);
            RequireDateTime(root, "issuedAtUtc", missing);
            RequirePositiveLong(root, "totalGrossMinor", missing);

            RequireObject(root, "issuer", missing, issuer =>
            {
                RequireAnyString(issuer, new[] { "legalName", "companyName" }, "issuer.legalName", missing);
                RequireAnyString(issuer, new[] { "taxId", "vatId" }, "issuer.taxId", missing);
                RequireString(issuer, "addressLine1", missing, "issuer.addressLine1");
                RequireString(issuer, "postalCode", missing, "issuer.postalCode");
                RequireString(issuer, "city", missing, "issuer.city");
                RequireString(issuer, "country", missing, "issuer.country");
            });

            RequireObject(root, "customer", missing, customer =>
            {
                RequireAnyString(customer, new[] { "legalName", "companyName", "firstName", "lastName" }, "customer.name", missing);
                RequireString(customer, "addressLine1", missing, "customer.addressLine1");
                RequireString(customer, "postalCode", missing, "customer.postalCode");
                RequireString(customer, "city", missing, "customer.city");
                RequireString(customer, "country", missing, "customer.country");
            });

            RequireInvoiceLines(root, missing);

            return missing.Count == 0
                ? EInvoiceSourceReadinessResult.Ready
                : new EInvoiceSourceReadinessResult(false, missing);
        }
        catch (JsonException)
        {
            return new EInvoiceSourceReadinessResult(false, new[] { "issuedSnapshotJson.validJson" });
        }
    }

    private static void RequireInvoiceLines(JsonElement root, List<string> missing)
    {
        if (!root.TryGetProperty("lines", out var lines) || lines.ValueKind != JsonValueKind.Array)
        {
            missing.Add("lines");
            return;
        }

        var index = 0;
        foreach (var line in lines.EnumerateArray())
        {
            index++;
            var prefix = $"lines[{index}]";
            RequireString(line, "description", missing, $"{prefix}.description");
            RequirePositiveInt(line, "quantity", missing, $"{prefix}.quantity");
            RequireNonNegativeLong(line, "unitPriceNetMinor", missing, $"{prefix}.unitPriceNetMinor");
            RequireNonNegativeLong(line, "totalNetMinor", missing, $"{prefix}.totalNetMinor");
            RequireNonNegativeLong(line, "totalGrossMinor", missing, $"{prefix}.totalGrossMinor");
        }

        if (index == 0)
        {
            missing.Add("lines");
        }
    }

    private static void RequireObject(JsonElement element, string propertyName, List<string> missing, Action<JsonElement> validate)
    {
        if (!element.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.Object)
        {
            missing.Add(propertyName);
            return;
        }

        validate(value);
    }

    private static void RequireGuid(JsonElement element, string propertyName, List<string> missing)
    {
        if (!Guid.TryParse(GetString(element, propertyName), out var value) || value == Guid.Empty)
        {
            missing.Add(propertyName);
        }
    }

    private static void RequireDateTime(JsonElement element, string propertyName, List<string> missing)
    {
        if (!element.TryGetProperty(propertyName, out var value) ||
            value.ValueKind != JsonValueKind.String ||
            !value.TryGetDateTime(out var _))
        {
            missing.Add(propertyName);
        }
    }

    private static void RequireString(JsonElement element, string propertyName, List<string> missing, string? fieldName = null)
    {
        if (string.IsNullOrWhiteSpace(GetString(element, propertyName)))
        {
            missing.Add(fieldName ?? propertyName);
        }
    }

    private static void RequireAnyString(JsonElement element, IReadOnlyList<string> propertyNames, string fieldName, List<string> missing)
    {
        if (!propertyNames.Any(name => !string.IsNullOrWhiteSpace(GetString(element, name))))
        {
            missing.Add(fieldName);
        }
    }

    private static void RequirePositiveInt(JsonElement element, string propertyName, List<string> missing, string fieldName)
    {
        if (!element.TryGetProperty(propertyName, out var value) || !value.TryGetInt32(out var result) || result <= 0)
        {
            missing.Add(fieldName);
        }
    }

    private static void RequirePositiveLong(JsonElement element, string propertyName, List<string> missing)
    {
        if (!element.TryGetProperty(propertyName, out var value) || !value.TryGetInt64(out var result) || result <= 0)
        {
            missing.Add(propertyName);
        }
    }

    private static void RequireNonNegativeLong(JsonElement element, string propertyName, List<string> missing, string fieldName)
    {
        if (!element.TryGetProperty(propertyName, out var value) || !value.TryGetInt64(out var result) || result < 0)
        {
            missing.Add(fieldName);
        }
    }

    private static string? GetString(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
