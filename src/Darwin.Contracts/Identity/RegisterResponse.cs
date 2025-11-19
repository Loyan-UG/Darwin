using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Darwin.Contracts.Identity
{
    /// <summary>
    /// Registration response minimal payload.
    /// </summary>
    public sealed class RegisterResponse
    {
        /// <summary>Display name for immediate UI use.</summary>
        public string DisplayName { get; init; } = default!;

        /// <summary>Indicates whether email confirmation was sent.</summary>
        public bool ConfirmationEmailSent { get; init; }
    }
}
