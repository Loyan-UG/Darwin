using System;
using System.Threading.Tasks;

namespace Darwin.Mobile.Shared.Security;

/// <summary>
/// Abstraction over secure storage for access/refresh tokens.
/// </summary>
public interface ITokenStore
{
    Task SaveAsync(string accessToken, DateTime accessExpiresUtc, string refreshToken, DateTime refreshExpiresUtc);
    Task<(string? AccessToken, DateTime? AccessExpiresUtc)> GetAccessAsync();
    Task<(string? RefreshToken, DateTime? RefreshExpiresUtc)> GetRefreshAsync();
    Task ClearAsync();
}
