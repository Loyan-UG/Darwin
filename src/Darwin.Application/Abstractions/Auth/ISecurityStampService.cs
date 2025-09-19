using System;

namespace Darwin.Application.Abstractions.Auth
{
    /// <summary>
    /// Issues and validates a per-user security stamp that invalidates existing sessions
    /// when critical changes happen (password change, 2FA toggled, permissions changed).
    /// Application triggers new stamps; Infrastructure stores them on User entity.
    /// </summary>
    public interface ISecurityStampService
    {
        string NewStamp();
        bool Equals(string? currentStamp, string? presentedStamp);
    }
}
