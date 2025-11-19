using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Darwin.Contracts.Identity
{
    /// <summary>
    /// Request payload used to exchange a refresh token for a new access token.
    /// </summary>
    public sealed class RefreshTokenRequest
    {
        /// <summary>
        /// Gets or sets the opaque refresh token issued by the server.
        /// </summary>
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional device identifier used when the token
        /// was originally issued. If JwtRequireDeviceBinding is enabled,
        /// the value must match to successfully refresh.
        /// </summary>
        public string? DeviceId { get; set; }
    }
}
