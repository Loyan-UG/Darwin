using System.Linq;
using Darwin.Application.CRM.DTOs;
using FluentValidation;
using Microsoft.Extensions.Localization;

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
        public CustomerCreateValidator(IStringLocalizer<ValidationResource> localizer)
        {
            RuleFor(x => x.FirstName).MaximumLength(120);
            RuleFor(x => x.LastName).MaximumLength(120);
            RuleFor(x => x.Email).MaximumLength(256).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
            RuleFor(x => x.Phone).MaximumLength(50);
            RuleFor(x => x.CompanyName).MaximumLength(200);
            RuleFor(x => x.TaxProfileType).IsInEnum();
            RuleFor(x => x.VatId).MaximumLength(64);
            RuleFor(x => x.Notes).MaximumLength(2000);
            RuleForEach(x => x.Addresses).SetValidator(new CustomerAddressValidator());

            When(x => x.TaxProfileType == Darwin.Domain.Enums.CustomerTaxProfileType.Business, () =>
            {
                RuleFor(x => x.CompanyName)
                    .NotEmpty()
                    .WithMessage(localizer["CustomerBusinessRequiresCompanyName"]);
            });

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
        public CustomerEditValidator(IStringLocalizer<ValidationResource> localizer)
        {
            Include(new CustomerCreateValidator(localizer));
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.RowVersion).NotEmpty();
        }
    }

    public sealed class CustomerTaxProfileUpdateValidator : AbstractValidator<CustomerTaxProfileUpdateDto>
    {
        public CustomerTaxProfileUpdateValidator(IStringLocalizer<ValidationResource> localizer)
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.RowVersion).NotEmpty();
            RuleFor(x => x.TaxProfileType).IsInEnum();
            RuleFor(x => x.CompanyName).MaximumLength(200);
            RuleFor(x => x.VatId).MaximumLength(64);

            When(x => x.TaxProfileType == Darwin.Domain.Enums.CustomerTaxProfileType.Business, () =>
            {
                RuleFor(x => x.CompanyName)
                    .NotEmpty()
                    .WithMessage(localizer["CustomerBusinessRequiresCompanyName"]);
                RuleFor(x => x.VatId)
                    .NotEmpty()
                    .WithMessage(localizer["CustomerBusinessRequiresVatIdForTaxCompliance"]);
            });
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

    public sealed class ConvertLeadToCustomerValidator : AbstractValidator<ConvertLeadToCustomerDto>
    {
        public ConvertLeadToCustomerValidator()
        {
            RuleFor(x => x.LeadId).NotEmpty();
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
        public InteractionCreateValidator(IStringLocalizer<ValidationResource> localizer)
        {
            RuleFor(x => x.Type).IsInEnum();
            RuleFor(x => x.Channel).IsInEnum();
            RuleFor(x => x.Subject).MaximumLength(300);
            RuleFor(x => x.Content).MaximumLength(4000);
            RuleFor(x => x)
                .Must(x => new[] { x.CustomerId, x.LeadId, x.OpportunityId }.Count(id => id.HasValue) == 1)
                .WithMessage(localizer["InteractionSingleTargetRequired"]);
        }
    }

    public sealed class ConsentCreateValidator : AbstractValidator<ConsentCreateDto>
    {
        public ConsentCreateValidator(IStringLocalizer<ValidationResource> localizer)
        {
            RuleFor(x => x.CustomerId).NotEmpty();
            RuleFor(x => x.Type).IsInEnum();
            RuleFor(x => x.GrantedAtUtc).NotEmpty();
            RuleFor(x => x)
                .Must(x => x.Granted || x.RevokedAtUtc.HasValue)
                .WithMessage(localizer["ConsentRevocationTimestampRequired"]);
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

    public sealed class InvoiceEditValidator : AbstractValidator<InvoiceEditDto>
    {
        public InvoiceEditValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.RowVersion).NotEmpty();
            RuleFor(x => x.Status).IsInEnum();
            RuleFor(x => x.Currency).NotEmpty().Length(3);
            RuleFor(x => x.TotalNetMinor).GreaterThanOrEqualTo(0);
            RuleFor(x => x.TotalTaxMinor).GreaterThanOrEqualTo(0);
            RuleFor(x => x.TotalGrossMinor).GreaterThanOrEqualTo(0);
            RuleFor(x => x.DueDateUtc).NotEmpty();
            When(x => x.Status == Darwin.Domain.Enums.InvoiceStatus.Paid, () =>
            {
                RuleFor(x => x.PaidAtUtc).NotNull();
            });
        }
    }

    public sealed class InvoiceStatusTransitionValidator : AbstractValidator<InvoiceStatusTransitionDto>
    {
        public InvoiceStatusTransitionValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.RowVersion).NotEmpty();
            RuleFor(x => x.TargetStatus).IsInEnum();
        }
    }

    public sealed class InvoiceRefundCreateValidator : AbstractValidator<InvoiceRefundCreateDto>
    {
        public InvoiceRefundCreateValidator()
        {
            RuleFor(x => x.InvoiceId).NotEmpty();
            RuleFor(x => x.RowVersion).NotEmpty();
            RuleFor(x => x.AmountMinor).GreaterThan(0);
            RuleFor(x => x.Currency).NotEmpty().Length(3);
            RuleFor(x => x.Reason).NotEmpty().MaximumLength(256);
        }
    }
}
