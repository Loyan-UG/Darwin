using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;

namespace Darwin.Mobile.Shared.Security;

/// <summary>
/// Centralizes best-effort JWT claim reads used by mobile clients.
/// Tokens are still validated by the server; this helper only parses local UI/cache hints.
/// </summary>
public static class JwtClaimReader
{
    public const string BusinessIdClaim = "business_id";

    public static JwtSecurityToken? TryReadToken(string? accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }

        try
        {
            return new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    public static string? GetSubject(string? accessToken)
        => GetSubject(TryReadToken(accessToken));

    public static string? GetSubject(JwtSecurityToken? token)
        => GetClaimValue(token?.Claims, JwtRegisteredClaimNames.Sub, "sub");

    public static Guid? GetBusinessId(string? accessToken)
        => GetBusinessId(TryReadToken(accessToken));

    public static Guid? GetBusinessId(JwtSecurityToken? token)
    {
        var value = GetClaimValue(token?.Claims, BusinessIdClaim);
        return Guid.TryParse(value, out var businessId) && businessId != Guid.Empty
            ? businessId
            : null;
    }

    public static string? GetEmailOrSubject(JwtSecurityToken? token)
        => GetClaimValue(token?.Claims, JwtRegisteredClaimNames.Email, ClaimTypes.Email, "email")
           ?? GetSubject(token);

    public static string? BuildBusinessOperatorScope(string? accessToken)
    {
        var token = TryReadToken(accessToken);
        var businessId = GetBusinessId(token)?.ToString("D");
        var subject = GetSubject(token);

        if (!string.IsNullOrWhiteSpace(businessId) && !string.IsNullOrWhiteSpace(subject))
        {
            return $"{businessId}:{subject.Trim()}";
        }

        return !string.IsNullOrWhiteSpace(businessId) ? businessId : subject?.Trim();
    }

    public static HashSet<string> ReadScopeSet(JwtSecurityToken? token)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (token is null)
        {
            return set;
        }

        foreach (var claim in token.Claims)
        {
            if (!string.Equals(claim.Type, "scope", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(claim.Type, "scp", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(claim.Type, "permissions", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(claim.Type, "permission", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var value in claim.Value.Split(new[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                set.Add(value.Trim());
            }
        }

        return set;
    }

    private static string? GetClaimValue(IEnumerable<Claim>? claims, params string[] claimTypes)
    {
        if (claims is null)
        {
            return null;
        }

        foreach (var claimType in claimTypes)
        {
            var value = claims.FirstOrDefault(claim =>
                string.Equals(claim.Type, claimType, StringComparison.OrdinalIgnoreCase))?.Value;

            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }
}
