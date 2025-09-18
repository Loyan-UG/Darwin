using Darwin.Application.SEO.DTOs;
using FluentValidation;
using System.Text.RegularExpressions;

namespace Darwin.Application.SEO.Validators
{
    /// <summary>
    /// Basic validation for paths and destinations. Uniqueness is enforced by a separate validator.
    /// </summary>
    public sealed class RedirectRuleCreateValidator : AbstractValidator<RedirectRuleCreateDto>
    {
        public RedirectRuleCreateValidator()
        {
            RuleFor(x => x.FromPath)
                .NotEmpty()
                .MaximumLength(2048)
                .Must(p => p.StartsWith("/"))
                .WithMessage("FromPath must be app-relative and start with '/'.");

            RuleFor(x => x.To)
                .NotEmpty()
                .MaximumLength(2048);
        }
    }

    public sealed class RedirectRuleEditValidator : AbstractValidator<RedirectRuleEditDto>
    {
        public RedirectRuleEditValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.RowVersion).NotNull();

            RuleFor(x => x.FromPath)
                .NotEmpty()
                .MaximumLength(2048)
                .Must(p => p.StartsWith("/"))
                .WithMessage("FromPath must be app-relative and start with '/'.");

            RuleFor(x => x.To)
                .NotEmpty()
                .MaximumLength(2048);
        }
    }
}
