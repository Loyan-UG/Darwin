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
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            failures.Add("Email:Brevo:ApiKey is required when Email:Provider is Brevo.");
        }

        if (string.IsNullOrWhiteSpace(options.SenderEmail))
        {
            failures.Add("Email:Brevo:SenderEmail is required when Email:Provider is Brevo.");
        }

        if (options.TimeoutSeconds is < 5 or > 120)
        {
            failures.Add("Email:Brevo:TimeoutSeconds must be between 5 and 120.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
