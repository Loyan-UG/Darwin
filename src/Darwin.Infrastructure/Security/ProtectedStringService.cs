using System;
using Darwin.Application.Abstractions.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;

namespace Darwin.Infrastructure.Security;

public sealed class ProtectedStringService : IProtectedStringService
{
    private const string Prefix = "dp:";
    private readonly IDataProtector _protector;
    private readonly ILogger<ProtectedStringService> _logger;

    public ProtectedStringService(IDataProtectionProvider dataProtectionProvider, ILogger<ProtectedStringService> logger)
    {
        _protector = (dataProtectionProvider ?? throw new ArgumentNullException(nameof(dataProtectionProvider)))
            .CreateProtector("Darwin.PushTokens.v1");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string? Protect(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return trimmed;
        }

        return Prefix + _protector.Protect(trimmed);
    }

    public string? Unprotect(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (!trimmed.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return trimmed;
        }

        try
        {
            return _protector.Unprotect(trimmed[Prefix.Length..]);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Protected string could not be unprotected.");
            return null;
        }
    }
}
