using System;

namespace Darwin.Application.Abstractions.Auth
{
    /// <summary>
    ///     Provides the identity (UserId) of the current actor executing the use case.
    ///     This service is consumed by the persistence layer for auditing and by application logic that needs user context.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Implementations in Web can extract the user id from the authenticated principal (ASP.NET Identity or JWT).
    ///         At design-time or in background processes, a system/fallback identity may be returned.
    ///     </para>
    ///     <para>
    ///         Avoid exposing the entire claims principal here; prefer small, explicit surface area to keep the Application clean.
    ///     </para>
    /// </remarks>
    public interface ICurrentUserService
    {
        Guid GetCurrentUserId();
    }
}
