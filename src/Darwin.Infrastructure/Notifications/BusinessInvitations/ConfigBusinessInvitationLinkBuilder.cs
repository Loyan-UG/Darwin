using System;
using Darwin.Application.Abstractions.Services;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;

namespace Darwin.Infrastructure.Notifications.BusinessInvitations;

/// <summary>
/// Configuration-driven builder for business invitation links.
/// </summary>
public sealed class ConfigBusinessInvitationLinkBuilder : IBusinessInvitationLinkBuilder
{
    private readonly IOptions<BusinessInvitationLinkOptions> _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigBusinessInvitationLinkBuilder"/> class.
    /// </summary>
    public ConfigBusinessInvitationLinkBuilder(IOptions<BusinessInvitationLinkOptions> options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public string? BuildAcceptanceLink(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Invitation token is required.", nameof(token));
        }

        var baseUrl = _options.Value.BaseUrl?.Trim();
        if (string.IsNullOrWhiteSpace(baseUrl) || !Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
        {
            return null;
        }

        var queryBuilder = new QueryBuilder
        {
            { "token", token.Trim() }
        };

        return new UriBuilder(baseUri)
        {
            Query = queryBuilder.ToQueryString().Value?.TrimStart('?')
        }.Uri.AbsoluteUri;
    }
}
