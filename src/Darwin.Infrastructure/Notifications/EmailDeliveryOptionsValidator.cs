using Microsoft.Extensions.Options;

namespace Darwin.Infrastructure.Notifications;

/// <summary>
/// Validates the active transactional email provider selection.
/// </summary>
public sealed class EmailDeliveryOptionsValidator : IValidateOptions<EmailDeliveryOptions>
{
    public ValidateOptionsResult Validate(string? name, EmailDeliveryOptions options)
    {
        var provider = EmailProviderNames.Normalize(options.Provider);
        return provider is EmailProviderNames.Smtp or EmailProviderNames.Brevo
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail($"Email:Provider '{provider}' is not supported. Use '{EmailProviderNames.Smtp}' or '{EmailProviderNames.Brevo}'.");
    }
}
