using System;
using Darwin.Domain.Enums;

namespace Darwin.Application.Orders;

public static class MemberOrderPresentationResolver
{
    public static string ResolveLineName(string name, string? culture)
    {
        if (IsEnglish(culture))
        {
            return name.Replace("CRM-Angebotspaket", "CRM offer package", StringComparison.OrdinalIgnoreCase);
        }

        return name;
    }

    public static string ResolveOrderStatus(OrderStatus status, string? culture)
    {
        if (IsEnglish(culture))
        {
            return status.ToString();
        }

        return status switch
        {
            OrderStatus.Created => "Erstellt",
            OrderStatus.Confirmed => "Bestaetigt",
            OrderStatus.Paid => "Bezahlt",
            OrderStatus.PartiallyShipped => "Teilweise versendet",
            OrderStatus.Shipped => "Versendet",
            OrderStatus.Delivered => "Zugestellt",
            OrderStatus.Cancelled => "Storniert",
            OrderStatus.Refunded => "Erstattet",
            OrderStatus.PartiallyRefunded => "Teilweise erstattet",
            OrderStatus.Completed => "Abgeschlossen",
            _ => status.ToString()
        };
    }

    public static string ResolvePaymentStatus(PaymentStatus status, string? culture)
    {
        if (IsEnglish(culture))
        {
            return status.ToString();
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

    public static string ResolveShipmentStatus(ShipmentStatus status, string? culture)
    {
        if (IsEnglish(culture))
        {
            return status.ToString();
        }

        return status switch
        {
            ShipmentStatus.Pending => "Ausstehend",
            ShipmentStatus.Packed => "Gepackt",
            ShipmentStatus.Shipped => "Versendet",
            ShipmentStatus.Delivered => "Zugestellt",
            ShipmentStatus.Returned => "Retourniert",
            _ => status.ToString()
        };
    }

    public static string ResolveInvoiceStatus(InvoiceStatus status, string? culture)
    {
        if (IsEnglish(culture))
        {
            return status.ToString();
        }

        return status switch
        {
            InvoiceStatus.Draft => "Entwurf",
            InvoiceStatus.Open => "Offen",
            InvoiceStatus.Paid => "Bezahlt",
            InvoiceStatus.Cancelled => "Storniert",
            _ => status.ToString()
        };
    }

    private static bool IsEnglish(string? culture)
        => !string.IsNullOrWhiteSpace(culture)
           && culture.StartsWith("en", StringComparison.OrdinalIgnoreCase);
}
