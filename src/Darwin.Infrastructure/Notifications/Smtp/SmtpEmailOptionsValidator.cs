using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace Darwin.Infrastructure.Notifications.Smtp;

/// <summary>
/// Validates SMTP options only when SMTP is the active email transport.
/// </summary>
public sealed class SmtpEmailOptionsValidator : IValidateOptions<SmtpEmailOptions>
{
    private readonly IOptions<EmailDeliveryOptions> _emailDeliveryOptions;

    public SmtpEmailOptionsValidator(IOptions<EmailDeliveryOptions> emailDeliveryOptions)
    {
        _emailDeliveryOptions = emailDeliveryOptions ?? throw new ArgumentNullException(nameof(emailDeliveryOptions));
    }

    public ValidateOptionsResult Validate(string? name, SmtpEmailOptions options)
    {
        if (EmailProviderNames.Normalize(_emailDeliveryOptions.Value.Provider) != EmailProviderNames.Smtp)
        {
            return ValidateOptionsResult.Skip;
        }

        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Host))
        {
            failures.Add("Email:Smtp:Host is required when Email:Provider is SMTP.");
        }

        if (options.Port is < 1 or > 65535)
        {
            failures.Add("Email:Smtp:Port must be between 1 and 65535.");
        }

        if (string.IsNullOrWhiteSpace(options.FromAddress))
        {
            failures.Add("Email:Smtp:FromAddress is required when Email:Provider is SMTP.");
        }
        else if (!IsEmailAddress(options.FromAddress))
        {
            failures.Add("Email:Smtp:FromAddress must be a valid email address.");
        }

        if (string.IsNullOrWhiteSpace(options.FromDisplayName))
        {
            failures.Add("Email:Smtp:FromDisplayName is required when Email:Provider is SMTP.");
        }

        if (!string.IsNullOrWhiteSpace(options.Username) && string.IsNullOrWhiteSpace(options.Password))
        {
            failures.Add("Email:Smtp:Password is required when Email:Smtp:Username is configured.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
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
