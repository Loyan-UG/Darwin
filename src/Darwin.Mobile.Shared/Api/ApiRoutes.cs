using System;

namespace Darwin.Mobile.Shared.Api
{
    /// <summary>
    /// Centralized API route catalog for the mobile clients.
    /// Keeping routes in one place prevents drift between services and reduces
    /// the risk of subtle 404/route mismatches across apps (Consumer/Business).
    /// </summary>
    public static class ApiRoutes
    {
        /// <summary>
        /// Normalizes a relative route so that it can be safely used with HttpClient.BaseAddress.
        /// </summary>
        /// <remarks>
        /// This method intentionally trims leading slashes to avoid turning a relative URI into
        /// an absolute-path URI that overrides BaseAddress path segments.
        /// </remarks>
        public static string Normalize(string route)
            => string.IsNullOrWhiteSpace(route)
                ? string.Empty
                : route.Trim().TrimStart('/');

        /// <summary>
        /// Auth endpoints (WebApi).
        /// </summary>
        public static class Auth
        {
            public const string LoginWithPassword = "api/auth/login/password";
            public const string Refresh = "api/auth/refresh";
            public const string Logout = "api/auth/logout";
        }

        /// <summary>
        /// Businesses discovery endpoints (WebApi).
        /// </summary>
        public static class Businesses
        {
            public const string List = "api/businesses/list";
            public static string GetById(Guid id) => $"api/businesses/{id:D}";
        }

        /// <summary>
        /// Loyalty endpoints (WebApi).
        /// </summary>
        public static class Loyalty
        {
            public const string PrepareScanSession = "api/loyalty/scan/prepare";
            public const string ProcessScanSessionForBusiness = "api/loyalty/scan/process-for-business";
            public const string ConfirmAccrual = "api/loyalty/scan/confirm-accrual";
            public const string ConfirmRedemption = "api/loyalty/scan/confirm-redemption";

            public static string GetAvailableRewards(Guid businessId)
                => $"api/loyalty/businesses/{businessId:D}/rewards/available";
        }
    }
}
