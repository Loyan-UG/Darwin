using Darwin.Application.Common.DTOs;
using Darwin.Contracts.Common;
using Darwin.Domain.Enums;
using System.Security.Claims;

namespace Darwin.WebApi.Controllers.Businesses;

/// <summary>
/// Shared validation and mapping helpers for business-facing WebApi controllers.
/// </summary>
internal static class BusinessControllerConventions
{
    public static string? NormalizeNullable(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static bool TryGetCurrentUserId(ClaimsPrincipal user, out Guid userId)
    {
        userId = Guid.Empty;

        if (user?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var id =
            user.FindFirstValue(ClaimTypes.NameIdentifier) ??
            user.FindFirstValue("sub") ??
            user.FindFirstValue("uid");

        if (!Guid.TryParse(id, out var parsed))
        {
            return false;
        }

        userId = parsed;
        return true;
    }

    public static bool TryParseBusinessCategoryKind(
        string? category,
        out BusinessCategoryKind? kind,
        out string error)
    {
        kind = null;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(category))
        {
            return true;
        }

        if (Enum.TryParse<BusinessCategoryKind>(category.Trim(), ignoreCase: true, out var parsed))
        {
            kind = parsed;
            return true;
        }

        error = "Invalid category value. It must match a known BusinessCategoryKind enum name.";
        return false;
    }

    public static (double? Value, string? Error) TryNormalizeMinRating(double? minRating)
    {
        if (!minRating.HasValue)
        {
            return (null, null);
        }

        if (double.IsNaN(minRating.Value) || double.IsInfinity(minRating.Value))
        {
            return (null, "MinRating must be a finite number between 0 and 5.");
        }

        if (minRating.Value < 0 || minRating.Value > 5)
        {
            return (null, "MinRating must be between 0 and 5.");
        }

        return (minRating.Value, null);
    }

    public static (GeoCoordinateDto? Coordinate, double? RadiusKm, string? Error) TryMapProximity(
        GeoCoordinateModel? near,
        int? radiusMeters)
    {
        if (near is null && radiusMeters is null)
        {
            return (null, null, null);
        }

        if (near is null)
        {
            return (null, null, "Near must be provided when RadiusMeters is provided.");
        }

        if (radiusMeters.HasValue && radiusMeters.Value < 0)
        {
            return (null, null, "RadiusMeters must be zero or a positive integer.");
        }

        var coordinate = new GeoCoordinateDto
        {
            Latitude = near.Latitude,
            Longitude = near.Longitude,
            AltitudeMeters = near.AltitudeMeters
        };

        var radiusKm = radiusMeters.HasValue
            ? radiusMeters.Value / 1000.0
            : (double?)null;

        return (coordinate, radiusKm, null);
    }
}
