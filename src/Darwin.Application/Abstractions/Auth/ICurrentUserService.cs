using System;

namespace Darwin.Application.Abstractions.Auth
{
    /// <summary>
    /// Provides the current user's identifier for auditing.
    /// </summary>
    public interface ICurrentUserService
    {
        Guid GetCurrentUserId();
    }
}
