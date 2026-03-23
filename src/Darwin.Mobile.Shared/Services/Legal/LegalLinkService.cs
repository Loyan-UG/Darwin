using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Mobile.Shared.Configuration;
using Darwin.Shared.Results;
using Microsoft.Maui.ApplicationModel;

namespace Darwin.Mobile.Shared.Services.Legal;

/// <summary>
/// Default implementation that resolves legal URLs from configuration and opens them through MAUI browser abstractions.
/// </summary>
public sealed class LegalLinkService : ILegalLinkService
{
    private readonly LegalLinksOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="LegalLinkService"/> class.
    /// </summary>
    /// <param name="options">The validated legal-link configuration snapshot.</param>
    public LegalLinkService(LegalLinksOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public Result<Uri> ResolveUri(LegalLinkKind linkKind)
    {
        var rawValue = linkKind switch
        {
            LegalLinkKind.Impressum => _options.ImpressumUrl,
            LegalLinkKind.PrivacyPolicy => _options.PrivacyPolicyUrl,
            LegalLinkKind.ConsumerTerms => _options.ConsumerTermsUrl,
            LegalLinkKind.BusinessTerms => _options.BusinessTermsUrl,
            LegalLinkKind.AccountDeletion => _options.AccountDeletionUrl,
            LegalLinkKind.PrivacyChoices => _options.PrivacyChoicesUrl,
            LegalLinkKind.ConsumerPreContractInfo => _options.ConsumerPreContractInfoUrl,
            LegalLinkKind.BusinessLegalInfo => _options.BusinessLegalInfoUrl,
            _ => null
        };

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return Result<Uri>.Fail($"The configured legal link '{linkKind}' is missing.");
        }

        if (!Uri.TryCreate(rawValue.Trim(), UriKind.Absolute, out var uri))
        {
            return Result<Uri>.Fail($"The configured legal link '{linkKind}' is not a valid absolute URL.");
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return Result<Uri>.Fail($"The configured legal link '{linkKind}' must use HTTPS.");
        }

        return Result<Uri>.Ok(uri);
    }

    /// <inheritdoc />
    public async Task<Result> OpenAsync(LegalLinkKind linkKind, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolvedUri = ResolveUri(linkKind);
        if (!resolvedUri.Succeeded || resolvedUri.Value is null)
        {
            return Result.Fail(resolvedUri.Error ?? "The requested legal page is not configured.");
        }

        try
        {
            await Browser.Default.OpenAsync(resolvedUri.Value, BrowserLaunchMode.SystemPreferred).ConfigureAwait(false);
            return Result.Ok();
        }
        catch (Exception browserException)
        {
            Debug.WriteLine($"Legal link browser launch failed for '{linkKind}': {browserException}");

            try
            {
                await Launcher.Default.OpenAsync(resolvedUri.Value).ConfigureAwait(false);
                return Result.Ok();
            }
            catch (Exception launcherException)
            {
                Debug.WriteLine($"Legal link launcher fallback failed for '{linkKind}': {launcherException}");
                return Result.Fail("The legal page could not be opened right now. Please try again shortly.");
            }
        }
    }

    /// <summary>
    /// Validates the configured legal links and returns any human-readable validation errors.
    /// </summary>
    /// <param name="options">The options instance to validate.</param>
    /// <returns>A sequence of validation errors. The sequence is empty when configuration is valid.</returns>
    public static IReadOnlyList<string> ValidateConfiguration(LegalLinksOptions options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        var errors = new List<string>();
        foreach (var pair in options.GetRequiredLinks())
        {
            if (string.IsNullOrWhiteSpace(pair.Value))
            {
                errors.Add($"Required legal link '{pair.Key}' is missing.");
                continue;
            }

            if (!Uri.TryCreate(pair.Value.Trim(), UriKind.Absolute, out var uri))
            {
                errors.Add($"Required legal link '{pair.Key}' is not a valid absolute URL.");
                continue;
            }

            if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"Required legal link '{pair.Key}' must use HTTPS.");
            }
        }

        return errors;
    }
}
