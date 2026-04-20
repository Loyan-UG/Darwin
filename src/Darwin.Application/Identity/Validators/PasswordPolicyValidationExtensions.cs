using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Identity.Validators
{
    internal static class PasswordPolicyValidationExtensions
    {
        public static IRuleBuilderOptions<T, string> ApplyPasswordPolicy<T>(
            this IRuleBuilder<T, string> rule,
            IStringLocalizer<ValidationResource> localizer)
        {
            return rule
                .NotEmpty()
                .MinimumLength(8).WithMessage(localizer["PasswordMinLength"])
                .Matches("[A-Z]").WithMessage(localizer["PasswordUppercaseRequired"])
                .Matches("[a-z]").WithMessage(localizer["PasswordLowercaseRequired"])
                .Matches("[0-9]").WithMessage(localizer["PasswordDigitRequired"]);
        }
    }
}
