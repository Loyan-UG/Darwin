using Microsoft.AspNetCore.Http;

namespace Darwin.WebAdmin.Localization
{
    /// <summary>
    /// Request-scoped storage keys and helpers for admin text override snapshots.
    /// </summary>
    public static class AdminTextOverrideRequestContext
    {
        public static readonly object PlatformOverridesItemKey = typeof(AdminTextOverrideCatalog);
        public const string BusinessOverridesItemKey = "AdminTextLocalizer.BusinessOverrides";

        public static Guid? TryResolveCurrentBusinessId(HttpContext httpContext, IFormCollection? form = null)
        {
            if (TryParseGuid(httpContext.Request.RouteValues["businessId"]?.ToString(), out var routeBusinessId))
            {
                return routeBusinessId;
            }

            var controller = httpContext.Request.RouteValues["controller"]?.ToString();
            if (string.Equals(controller, "Businesses", StringComparison.OrdinalIgnoreCase) &&
                TryParseGuid(httpContext.Request.RouteValues["id"]?.ToString(), out var routeId))
            {
                return routeId;
            }

            if (TryParseGuid(httpContext.Request.Query["businessId"].ToString(), out var queryBusinessId))
            {
                return queryBusinessId;
            }

            if (string.Equals(controller, "Businesses", StringComparison.OrdinalIgnoreCase) &&
                TryParseGuid(httpContext.Request.Query["id"].ToString(), out var queryId))
            {
                return queryId;
            }

            if (form is not null)
            {
                if (TryParseGuid(form["BusinessId"].ToString(), out var formBusinessId))
                {
                    return formBusinessId;
                }

                if (string.Equals(controller, "Businesses", StringComparison.OrdinalIgnoreCase) &&
                    TryParseGuid(form["Id"].ToString(), out var formId))
                {
                    return formId;
                }
            }

            return null;
        }

        public static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> GetOverrides(
            IDictionary<object, object?> items,
            object key)
        {
            return items.TryGetValue(key, out var value) &&
                   value is IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> overrides
                ? overrides
                : AdminTextOverrideCatalog.Empty;
        }

        private static bool TryParseGuid(string? value, out Guid id)
        {
            return Guid.TryParse(value, out id) && id != Guid.Empty;
        }
    }
}
