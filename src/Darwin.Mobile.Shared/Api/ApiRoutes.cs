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
        /// Provides constant values for well-known API metadata endpoint paths.
        /// </summary>
        /// <remarks>Use the fields of this class to reference standard metadata endpoints in a consistent
        /// and type-safe manner throughout the application. This class cannot be instantiated.</remarks>
        public static class Meta
        {
            public const string Health = "api/v1/meta/health";
            public const string Info = "api/v1/meta/info";
            public const string Bootstrap = "api/v1/meta/bootstrap";
        }

        /// <summary>
        /// Auth endpoints (WebApi).
        /// </summary>
        public static class Auth
        {
            public const string Login = "api/v1/auth/login";
            public const string Refresh = "api/v1/auth/refresh";
            public const string Logout = "api/v1/auth/logout";
            public const string LogoutAll = "api/v1/auth/logout-all";
            public const string ChangePassword = "api/v1/auth/password/change";
            public const string RequestPasswordReset = "api/v1/auth/password/request-reset";
            public const string ResetPassword = "api/v1/auth/password/reset";
            public const string Register = "api/v1/auth/register";
        }

        /// <summary>
        /// Provides API endpoint paths for profile-related operations.
        /// </summary>
        /// <remarks>This class contains constant string values representing the relative paths for
        /// profile API endpoints. Use these constants to avoid hardcoding endpoint URLs in your code and to ensure
        /// consistency when making requests to the profile API.</remarks>
        public static class Profile
        {
            public const string GetMe = "api/v1/profile/me";
            public const string UpdateMe = "api/v1/profile/me";
        }

        /// <summary>
        /// Businesses discovery endpoints (WebApi).
        /// </summary>
        public static class Businesses
        {
            public const string List = "api/v1/businesses/list";
            public const string Map = "api/v1/businesses/map";
            public static string GetById(Guid id) => $"api/v1/businesses/{id:D}";
            public static string GetWithMyAccount(Guid id) => $"api/v1/businesses/{id:D}/with-my-account";
            public const string CategoryKinds = "api/v1/businesses/category-kinds";
        }

        /// <summary>
        /// Loyalty endpoints (WebApi).
        /// </summary>
        public static class Loyalty
        {
            public const string PrepareScanSession = "api/v1/loyalty/scan/prepare";
            public const string ProcessScanSessionForBusiness = "api/v1/loyalty/scan/process";
            public const string ConfirmAccrual = "api/v1/loyalty/scan/confirm-accrual";
            public const string ConfirmRedemption = "api/v1/loyalty/scan/confirm-redemption";

            public const string GetMyAccounts = "api/v1/loyalty/my/accounts";
            public static string GetMyHistory(Guid businessId) => $"api/v1/loyalty/my/history/{businessId:D}";
            public static string GetAccountForBusiness(Guid businessId) => $"api/v1/loyalty/account/{businessId:D}";
            public static string GetRewardsForBusiness(Guid businessId) => $"api/v1/loyalty/business/{businessId:D}/rewards";
            public const string GetMyBusinesses = "api/v1/loyalty/my/businesses"; // GET with query
            public const string GetMyTimeline = "api/v1/loyalty/my/timeline";     // POST
            public static string Join(Guid businessId) => $"api/v1/loyalty/account/{businessId:D}/join";
            public static string GetNextReward(Guid businessId) => $"api/v1/loyalty/account/{businessId:D}/next-reward";
        }
    }
}
