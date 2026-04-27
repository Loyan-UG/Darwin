using System;
using System.Collections.Generic;
using Darwin.Application.Orders.DTOs;

namespace Darwin.Application.Orders.Queries;

internal static class ShipmentTrackingPresentation
{
    private const string DhlTrackingBaseUrl = "https://www.dhl.com/global-en/home/tracking/tracking-express.html";
    private const string ResolutionEventKey = "shipment.exception_resolved";

    public static string? ResolveTrackingUrl(string? carrier, string? trackingNumber)
    {
        var normalizedTrackingNumber = trackingNumber?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedTrackingNumber))
        {
            return null;
        }

        if (!string.Equals(carrier?.Trim(), "DHL", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return $"{DhlTrackingBaseUrl}?submit=1&tracking-id={Uri.EscapeDataString(normalizedTrackingNumber)}";
    }

    public static void Enrich(IList<ShipmentListItemDto> items, DateTime nowUtc)
    {
        foreach (var item in items)
        {
            item.TrackingUrl = ResolveTrackingUrl(item.Carrier, item.TrackingNumber);
            item.HasRefundablePayment = item.DefaultRefundPaymentId.HasValue;
            item.OpenAgeHours = Math.Max(0, (int)Math.Floor((nowUtc - item.CreatedAtUtc).TotalHours));
            if (item.ShippedAtUtc.HasValue && !item.DeliveredAtUtc.HasValue)
            {
                item.InTransitAgeHours = Math.Max(0, (int)Math.Floor((nowUtc - item.ShippedAtUtc.Value).TotalHours));
            }

            item.TrackingState = ResolveTrackingState(item);
            item.ExceptionNote = ResolveExceptionNote(item);
            item.NeedsReturnFollowUp =
                item.Status == Domain.Enums.ShipmentStatus.Returned &&
                (!item.HasRefundablePayment ||
                 item.NeedsCarrierReview ||
                 string.IsNullOrWhiteSpace(item.TrackingNumber));
        }
    }

    private static string ResolveTrackingState(ShipmentListItemDto item)
    {
        if (!string.IsNullOrWhiteSpace(item.TrackingNumber))
        {
            return item.Status switch
            {
                Domain.Enums.ShipmentStatus.Delivered => "Tracking linked and delivered",
                Domain.Enums.ShipmentStatus.Shipped => "Tracking linked and in transit",
                _ => "Tracking linked before handoff completion"
            };
        }

        if (item.Status == Domain.Enums.ShipmentStatus.Returned)
        {
            return "Return recorded without active tracking handoff";
        }

        if (item.TrackingOverdue)
        {
            return "Tracking overdue beyond grace window";
        }

        if (item.Status == Domain.Enums.ShipmentStatus.Shipped || item.Status == Domain.Enums.ShipmentStatus.Delivered)
        {
            return "Carrier handoff recorded without tracking";
        }

        return "No carrier tracking linked yet";
    }

    private static string ResolveExceptionNote(ShipmentListItemDto item)
    {
        var latestResolution = item.RecentCarrierEvents
            .Where(x => string.Equals(x.CarrierEventKey, ResolutionEventKey, StringComparison.OrdinalIgnoreCase))
            .Select(x => (DateTime?)x.OccurredAtUtc)
            .FirstOrDefault();

        var latestCarrierException = item.RecentCarrierEvents
            .FirstOrDefault(x =>
                (!latestResolution.HasValue || x.OccurredAtUtc > latestResolution.Value) &&
                (!string.IsNullOrWhiteSpace(x.ExceptionMessage) || !string.IsNullOrWhiteSpace(x.ExceptionCode)));

        if (latestCarrierException is not null)
        {
            if (!string.IsNullOrWhiteSpace(latestCarrierException.ExceptionCode) &&
                !string.IsNullOrWhiteSpace(latestCarrierException.ExceptionMessage))
            {
                return $"Carrier exception {latestCarrierException.ExceptionCode}: {latestCarrierException.ExceptionMessage}";
            }

            if (!string.IsNullOrWhiteSpace(latestCarrierException.ExceptionMessage))
            {
                return latestCarrierException.ExceptionMessage!;
            }

            return $"Carrier exception {latestCarrierException.ExceptionCode}";
        }

        if (item.Status == Domain.Enums.ShipmentStatus.Returned)
        {
            return "Returned shipment requires carrier or support follow-up.";
        }

        if (string.IsNullOrWhiteSpace(item.Service))
        {
            return "Carrier service is missing.";
        }

        if (item.TrackingOverdue)
        {
            return $"Tracking missing beyond {item.TrackingGraceHours} h grace.";
        }

        if (item.AwaitingHandoff)
        {
            return $"Still open after {item.AttentionDelayHours} h attention threshold.";
        }

        if ((item.Status == Domain.Enums.ShipmentStatus.Shipped || item.Status == Domain.Enums.ShipmentStatus.Delivered) &&
            string.IsNullOrWhiteSpace(item.TrackingNumber))
        {
            return "Shipment is marked handed off but no tracking number is present.";
        }

        return string.Empty;
    }
}
