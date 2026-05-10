using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace Darwin.Infrastructure.Notifications.Brevo;

/// <summary>
/// Validates Brevo options only when Brevo is the active email transport.
/// </summary>
public sealed class BrevoEmailOptionsValidator : IValidateOptions<BrevoEmailOptions>
{
    private readonly IOptions<EmailDeliveryOptions> _emailDeliveryOptions;

    public BrevoEmailOptionsValidator(IOptions<EmailDeliveryOptions> emailDeliveryOptions)
    {
        _emailDeliveryOptions = emailDeliveryOptions ?? throw new ArgumentNullException(nameof(emailDeliveryOptions));
    }

    public ValidateOptionsResult Validate(string? name, BrevoEmailOptions options)
    {
        if (EmailProviderNames.Normalize(_emailDeliveryOptions.Value.Provider) != EmailProviderNames.Brevo)
        {
            return ValidateOptionsResult.Skip;
        }

        var failures = new List<string>();
        if (!IsHttpsAbsoluteUrl(options.BaseUrl))
        {
            failures.Add("Email:Brevo:BaseUrl must be an absolute HTTPS URL when Email:Provider is Brevo.");
        }

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            failures.Add("Email:Brevo:ApiKey is required when Email:Provider is Brevo.");
        }

        if (string.IsNullOrWhiteSpace(options.SenderEmail))
        {
            failures.Add("Email:Brevo:SenderEmail is required when Email:Provider is Brevo.");
        }
        else if (!IsEmailAddress(options.SenderEmail))
        {
            failures.Add("Email:Brevo:SenderEmail must be a valid email address.");
        }

        if (!string.IsNullOrWhiteSpace(options.ReplyToEmail) && !IsEmailAddress(options.ReplyToEmail))
        {
            failures.Add("Email:Brevo:ReplyToEmail must be a valid email address.");
        }

        if (string.IsNullOrWhiteSpace(options.WebhookUsername))
        {
            failures.Add("Email:Brevo:WebhookUsername is required when Email:Provider is Brevo.");
        }

        if (string.IsNullOrWhiteSpace(options.WebhookPassword))
        {
            failures.Add("Email:Brevo:WebhookPassword is required when Email:Provider is Brevo.");
        }

        if (options.TimeoutSeconds is < 5 or > 120)
        {
            failures.Add("Email:Brevo:TimeoutSeconds must be between 5 and 120.");
        }

        foreach (var pair in options.TemplateIds)
        {
            if (string.IsNullOrWhiteSpace(pair.Key))
            {
                failures.Add("Email:Brevo:TemplateIds keys must not be empty.");
            }

            if (pair.Value <= 0)
            {
                failures.Add($"Email:Brevo:TemplateIds:{pair.Key} must be a positive Brevo template id.");
            }
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static bool IsHttpsAbsoluteUrl(string? value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
               string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsEmailAddress(string value)
    {
        try
        {
            var address = new MailAddress(value.Trim());
            return string.Equals(address.Address, value.Trim(), StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
