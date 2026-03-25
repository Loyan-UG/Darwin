using Darwin.Application.CRM.DTOs;
using FluentValidation;

namespace Darwin.Application.CRM.Validators
{
    public sealed class CustomerAddressValidator : AbstractValidator<CustomerAddressDto>
    {
        public CustomerAddressValidator()
        {
            RuleFor(x => x.Line1).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Line2).MaximumLength(200);
            RuleFor(x => x.City).NotEmpty().MaximumLength(120);
            RuleFor(x => x.State).MaximumLength(120);
            RuleFor(x => x.PostalCode).NotEmpty().MaximumLength(32);
            RuleFor(x => x.Country).NotEmpty().MaximumLength(8);
        }
    }

    public sealed class CustomerCreateValidator : AbstractValidator<CustomerCreateDto>
    {
        public CustomerCreateValidator()
        {
            RuleFor(x => x.FirstName).MaximumLength(120);
            RuleFor(x => x.LastName).MaximumLength(120);
            RuleFor(x => x.Email).MaximumLength(256).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
            RuleFor(x => x.Phone).MaximumLength(50);
            RuleFor(x => x.CompanyName).MaximumLength(200);
            RuleFor(x => x.Notes).MaximumLength(2000);
            RuleForEach(x => x.Addresses).SetValidator(new CustomerAddressValidator());

            When(x => !x.UserId.HasValue, () =>
            {
                RuleFor(x => x.FirstName).NotEmpty();
                RuleFor(x => x.LastName).NotEmpty();
                RuleFor(x => x.Email).NotEmpty();
            });
        }
    }

    public sealed class CustomerEditValidator : AbstractValidator<CustomerEditDto>
    {
        public CustomerEditValidator()
        {
            Include(new CustomerCreateValidator());
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.RowVersion).NotEmpty();
        }
    }

    public sealed class LeadCreateValidator : AbstractValidator<LeadCreateDto>
    {
        public LeadCreateValidator()
        {
            RuleFor(x => x.FirstName).NotEmpty().MaximumLength(120);
            RuleFor(x => x.LastName).NotEmpty().MaximumLength(120);
            RuleFor(x => x.CompanyName).MaximumLength(200);
            RuleFor(x => x.Email).NotEmpty().MaximumLength(256).EmailAddress();
            RuleFor(x => x.Phone).NotEmpty().MaximumLength(50);
            RuleFor(x => x.Source).MaximumLength(120);
            RuleFor(x => x.Notes).MaximumLength(2000);
            RuleFor(x => x.Status).IsInEnum();
        }
    }

    public sealed class LeadEditValidator : AbstractValidator<LeadEditDto>
    {
        public LeadEditValidator()
        {
            Include(new LeadCreateValidator());
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.RowVersion).NotEmpty();
        }
    }

    public sealed class OpportunityItemValidator : AbstractValidator<OpportunityItemDto>
    {
        public OpportunityItemValidator()
        {
            RuleFor(x => x.ProductVariantId).NotEmpty();
            RuleFor(x => x.Quantity).GreaterThan(0);
            RuleFor(x => x.UnitPriceMinor).GreaterThanOrEqualTo(0);
        }
    }

    public sealed class OpportunityCreateValidator : AbstractValidator<OpportunityCreateDto>
    {
        public OpportunityCreateValidator()
        {
            RuleFor(x => x.CustomerId).NotEmpty();
            RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
            RuleFor(x => x.EstimatedValueMinor).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Stage).IsInEnum();
            RuleForEach(x => x.Items).SetValidator(new OpportunityItemValidator());
        }
    }

    public sealed class OpportunityEditValidator : AbstractValidator<OpportunityEditDto>
    {
        public OpportunityEditValidator()
        {
            Include(new OpportunityCreateValidator());
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.RowVersion).NotEmpty();
        }
    }
}
