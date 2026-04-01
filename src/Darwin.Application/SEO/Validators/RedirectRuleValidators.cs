using Darwin.Application.SEO.DTOs;
using FluentValidation;
using Microsoft.Extensions.Localization;
using System.Text.RegularExpressions;

namespace Darwin.Application.SEO.Validators
{
    /// <summary>
    /// Basic validation for paths and destinations. Uniqueness is enforced by a separate validator.
    /// </summary>
    public sealed class RedirectRuleCreateValidator : AbstractValidator<RedirectRuleCreateDto>
    {
        public RedirectRuleCreateValidator(IStringLocalizer<ValidationResource> localizer)
        {
            RuleFor(x => x.FromPath)
                .NotEmpty()
                .MaximumLength(2048)
                .Must(p => p.StartsWith("/"))
                .WithMessage(localizer["RedirectRuleFromPathMustBeAppRelative"]);

            RuleFor(x => x.To)
                .NotEmpty()
                .MaximumLength(2048);
        }
    }

    public sealed class RedirectRuleEditValidator : AbstractValidator<RedirectRuleEditDto>
    {
        public RedirectRuleEditValidator(IStringLocalizer<ValidationResource> localizer)
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.RowVersion).NotNull();

            RuleFor(x => x.FromPath)
                .NotEmpty()
                .MaximumLength(2048)
                .Must(p => p.StartsWith("/"))
                .WithMessage(localizer["RedirectRuleFromPathMustBeAppRelative"]);

            RuleFor(x => x.To)
                .NotEmpty()
                .MaximumLength(2048);
        }
    }
}
