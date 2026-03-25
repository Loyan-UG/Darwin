using System.Linq;
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

    public sealed class InteractionCreateValidator : AbstractValidator<InteractionCreateDto>
    {
        public InteractionCreateValidator()
        {
            RuleFor(x => x.Type).IsInEnum();
            RuleFor(x => x.Channel).IsInEnum();
            RuleFor(x => x.Subject).MaximumLength(300);
            RuleFor(x => x.Content).MaximumLength(4000);
            RuleFor(x => x)
                .Must(x => new[] { x.CustomerId, x.LeadId, x.OpportunityId }.Count(id => id.HasValue) == 1)
                .WithMessage("Exactly one CRM target must be selected for an interaction.");
        }
    }

    public sealed class ConsentCreateValidator : AbstractValidator<ConsentCreateDto>
    {
        public ConsentCreateValidator()
        {
            RuleFor(x => x.CustomerId).NotEmpty();
            RuleFor(x => x.Type).IsInEnum();
            RuleFor(x => x.GrantedAtUtc).NotEmpty();
            RuleFor(x => x)
                .Must(x => x.Granted || x.RevokedAtUtc.HasValue)
                .WithMessage("Revoked consents must include a revocation timestamp.");
        }
    }

    public sealed class CustomerSegmentEditValidator : AbstractValidator<CustomerSegmentEditDto>
    {
        public CustomerSegmentEditValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).MaximumLength(2000);
        }
    }

    public sealed class AssignCustomerSegmentValidator : AbstractValidator<AssignCustomerSegmentDto>
    {
        public AssignCustomerSegmentValidator()
        {
            RuleFor(x => x.CustomerId).NotEmpty();
            RuleFor(x => x.CustomerSegmentId).NotEmpty();
        }
    }
}
