using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Darwin.Contracts.Identity
{
    /// <summary>
    /// Logout request to revoke refresh token.
    /// </summary>
    public sealed class LogoutRequest
    {
        /// <summary>
        /// Refresh token to revoke.
        /// </summary>
        public string RefreshToken { get; init; } = default!;
    }
}
