using Darwin.Application;
using Darwin.Application.CRM.DTOs;
using Darwin.Application.CRM.Validators;
using Darwin.Domain.Enums;
using FluentAssertions;

namespace Darwin.Tests.Unit.CRM;

/// <summary>
/// Unit tests for CRM lead, opportunity, and customer edit validators.
/// </summary>
public sealed class LeadAndOpportunityValidatorsTests
{
    // ─── LeadCreateValidator ──────────────────────────────────────────────────

    [Fact]
    public void LeadCreate_Should_Pass_WhenAllRequiredFieldsPresent()
    {
        var validator = new LeadCreateValidator();
        var dto = new LeadCreateDto
        {
            FirstName = "Hans",
            LastName = "Müller",
            Email = "hans@example.de",
            Phone = "+4917012345678",
            Status = LeadStatus.New
        };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void LeadCreate_Should_Fail_WhenFirstNameIsEmpty()
    {
        var validator = new LeadCreateValidator();
        var dto = new LeadCreateDto
        {
            FirstName = string.Empty,
            LastName = "Müller",
            Email = "hans@example.de",
            Phone = "+4917012345678"
        };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LeadCreateDto.FirstName));
    }

    [Fact]
    public void LeadCreate_Should_Fail_WhenEmailIsInvalid()
    {
        var validator = new LeadCreateValidator();
        var dto = new LeadCreateDto
        {
            FirstName = "Hans",
            LastName = "Müller",
            Email = "not-an-email",
            Phone = "+4917012345678"
        };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LeadCreateDto.Email));
    }

    [Fact]
    public void LeadCreate_Should_Fail_WhenEmailIsEmpty()
    {
        var validator = new LeadCreateValidator();
        var dto = new LeadCreateDto
        {
            FirstName = "Hans",
            LastName = "Müller",
            Email = string.Empty,
            Phone = "+4917012345678"
        };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LeadCreateDto.Email));
    }

    [Fact]
    public void LeadCreate_Should_Fail_WhenPhoneIsEmpty()
    {
        var validator = new LeadCreateValidator();
        var dto = new LeadCreateDto
        {
            FirstName = "Hans",
            LastName = "Müller",
            Email = "hans@example.de",
            Phone = string.Empty
        };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LeadCreateDto.Phone));
    }

    [Fact]
    public void LeadCreate_Should_Fail_WhenFirstNameExceedsMaxLength()
    {
        var validator = new LeadCreateValidator();
        var dto = new LeadCreateDto
        {
            FirstName = new string('A', 121),
            LastName = "Müller",
            Email = "hans@example.de",
            Phone = "+4917012345678"
        };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LeadCreateDto.FirstName));
    }

    [Fact]
    public void LeadCreate_Should_Fail_WhenNotesExceedMaxLength()
    {
        var validator = new LeadCreateValidator();
        var dto = new LeadCreateDto
        {
            FirstName = "Hans",
            LastName = "Müller",
            Email = "hans@example.de",
            Phone = "+4917012345678",
            Notes = new string('N', 2001)
        };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LeadCreateDto.Notes));
    }

    // ─── LeadEditValidator ────────────────────────────────────────────────────

    [Fact]
    public void LeadEdit_Should_Pass_WhenAllRequiredFieldsPresent()
    {
        var validator = new LeadEditValidator();
        var dto = new LeadEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1, 2, 3, 4 },
            FirstName = "Hans",
            LastName = "Müller",
            Email = "hans@example.de",
            Phone = "+4917012345678",
            Status = LeadStatus.Qualified
        };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void LeadEdit_Should_Fail_WhenIdIsEmpty()
    {
        var validator = new LeadEditValidator();
        var dto = new LeadEditDto
        {
            Id = Guid.Empty,
            RowVersion = new byte[] { 1, 2, 3, 4 },
            FirstName = "Hans",
            LastName = "Müller",
            Email = "hans@example.de",
            Phone = "+4917012345678"
        };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LeadEditDto.Id));
    }

    [Fact]
    public void LeadEdit_Should_Fail_WhenRowVersionIsEmpty()
    {
        var validator = new LeadEditValidator();
        var dto = new LeadEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = Array.Empty<byte>(),
            FirstName = "Hans",
            LastName = "Müller",
            Email = "hans@example.de",
            Phone = "+4917012345678"
        };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LeadEditDto.RowVersion));
    }

    // ─── ConvertLeadToCustomerValidator ──────────────────────────────────────

    [Fact]
    public void ConvertLeadToCustomer_Should_Pass_WhenLeadIdAndRowVersionProvided()
    {
        var validator = new ConvertLeadToCustomerValidator();
        var dto = new ConvertLeadToCustomerDto
        {
            LeadId = Guid.NewGuid(),
            RowVersion = new byte[] { 1, 2, 3, 4 }
        };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ConvertLeadToCustomer_Should_Fail_WhenLeadIdIsEmpty()
    {
        var validator = new ConvertLeadToCustomerValidator();
        var dto = new ConvertLeadToCustomerDto
        {
            LeadId = Guid.Empty,
            RowVersion = new byte[] { 1, 2, 3, 4 }
        };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ConvertLeadToCustomerDto.LeadId));
    }

    [Fact]
    public void ConvertLeadToCustomer_Should_Fail_WhenRowVersionIsEmpty()
    {
        var validator = new ConvertLeadToCustomerValidator();
        var dto = new ConvertLeadToCustomerDto
        {
            LeadId = Guid.NewGuid(),
            RowVersion = Array.Empty<byte>()
        };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ConvertLeadToCustomerDto.RowVersion));
    }

    // ─── OpportunityItemValidator ─────────────────────────────────────────────

    [Fact]
    public void OpportunityItem_Should_Pass_WhenAllFieldsAreValid()
    {
        var validator = new OpportunityItemValidator();
        var dto = new OpportunityItemDto
        {
            ProductVariantId = Guid.NewGuid(),
            Quantity = 3,
            UnitPriceMinor = 5000
        };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void OpportunityItem_Should_Fail_WhenProductVariantIdIsEmpty()
    {
        var validator = new OpportunityItemValidator();
        var dto = new OpportunityItemDto
        {
            ProductVariantId = Guid.Empty,
            Quantity = 1,
            UnitPriceMinor = 1000
        };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(OpportunityItemDto.ProductVariantId));
    }

    [Fact]
    public void OpportunityItem_Should_Fail_WhenQuantityIsZero()
    {
        var validator = new OpportunityItemValidator();
        var dto = new OpportunityItemDto
        {
            ProductVariantId = Guid.NewGuid(),
            Quantity = 0,
            UnitPriceMinor = 1000
        };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(OpportunityItemDto.Quantity));
    }

    [Fact]
    public void OpportunityItem_Should_Fail_WhenQuantityIsNegative()
    {
        var validator = new OpportunityItemValidator();
        var dto = new OpportunityItemDto
        {
            ProductVariantId = Guid.NewGuid(),
            Quantity = -1,
            UnitPriceMinor = 1000
        };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(OpportunityItemDto.Quantity));
    }

    [Fact]
    public void OpportunityItem_Should_Pass_WhenUnitPriceIsZero()
    {
        var validator = new OpportunityItemValidator();
        var dto = new OpportunityItemDto
        {
            ProductVariantId = Guid.NewGuid(),
            Quantity = 1,
            UnitPriceMinor = 0
        };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeTrue("zero-priced items are valid for free products");
    }

    // ─── OpportunityCreateValidator ───────────────────────────────────────────

    [Fact]
    public void OpportunityCreate_Should_Pass_WhenAllRequiredFieldsPresent()
    {
        var validator = new OpportunityCreateValidator();
        var dto = new OpportunityCreateDto
        {
            CustomerId = Guid.NewGuid(),
            Title = "Enterprise Deal",
            EstimatedValueMinor = 100000,
            Stage = OpportunityStage.Qualification
        };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void OpportunityCreate_Should_Fail_WhenCustomerIdIsEmpty()
    {
        var validator = new OpportunityCreateValidator();
        var dto = new OpportunityCreateDto
        {
            CustomerId = Guid.Empty,
            Title = "Enterprise Deal",
            EstimatedValueMinor = 100000,
            Stage = OpportunityStage.Qualification
        };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(OpportunityCreateDto.CustomerId));
    }

    [Fact]
    public void OpportunityCreate_Should_Fail_WhenTitleIsEmpty()
    {
        var validator = new OpportunityCreateValidator();
        var dto = new OpportunityCreateDto
        {
            CustomerId = Guid.NewGuid(),
            Title = string.Empty,
            EstimatedValueMinor = 100000,
            Stage = OpportunityStage.Qualification
        };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(OpportunityCreateDto.Title));
    }

    [Fact]
    public void OpportunityCreate_Should_Fail_WhenTitleExceedsMaxLength()
    {
        var validator = new OpportunityCreateValidator();
        var dto = new OpportunityCreateDto
        {
            CustomerId = Guid.NewGuid(),
            Title = new string('T', 201),
            EstimatedValueMinor = 100000,
            Stage = OpportunityStage.Qualification
        };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(OpportunityCreateDto.Title));
    }

    [Fact]
    public void OpportunityCreate_Should_Fail_WhenEstimatedValueIsNegative()
    {
        var validator = new OpportunityCreateValidator();
        var dto = new OpportunityCreateDto
        {
            CustomerId = Guid.NewGuid(),
            Title = "Deal",
            EstimatedValueMinor = -1,
            Stage = OpportunityStage.Qualification
        };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(OpportunityCreateDto.EstimatedValueMinor));
    }

    [Fact]
    public void OpportunityCreate_Should_Pass_WhenEstimatedValueIsZero()
    {
        var validator = new OpportunityCreateValidator();
        var dto = new OpportunityCreateDto
        {
            CustomerId = Guid.NewGuid(),
            Title = "Exploratory",
            EstimatedValueMinor = 0,
            Stage = OpportunityStage.Qualification
        };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void OpportunityCreate_Should_Fail_WhenItemHasInvalidQuantity()
    {
        var validator = new OpportunityCreateValidator();
        var dto = new OpportunityCreateDto
        {
            CustomerId = Guid.NewGuid(),
            Title = "Deal With Bad Item",
            EstimatedValueMinor = 500,
            Stage = OpportunityStage.Qualification,
            Items = new List<OpportunityItemDto>
            {
                new()
                {
                    ProductVariantId = Guid.NewGuid(),
                    Quantity = 0,
                    UnitPriceMinor = 500
                }
            }
        };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains(nameof(OpportunityItemDto.Quantity)));
    }

    // ─── OpportunityEditValidator ─────────────────────────────────────────────

    [Fact]
    public void OpportunityEdit_Should_Pass_WhenAllRequiredFieldsPresent()
    {
        var validator = new OpportunityEditValidator();
        var dto = new OpportunityEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1, 2, 3, 4 },
            CustomerId = Guid.NewGuid(),
            Title = "Deal",
            EstimatedValueMinor = 5000,
            Stage = OpportunityStage.Proposal
        };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void OpportunityEdit_Should_Fail_WhenIdIsEmpty()
    {
        var validator = new OpportunityEditValidator();
        var dto = new OpportunityEditDto
        {
            Id = Guid.Empty,
            RowVersion = new byte[] { 1, 2, 3, 4 },
            CustomerId = Guid.NewGuid(),
            Title = "Deal",
            EstimatedValueMinor = 5000,
            Stage = OpportunityStage.Proposal
        };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(OpportunityEditDto.Id));
    }

    [Fact]
    public void OpportunityEdit_Should_Fail_WhenRowVersionIsEmpty()
    {
        var validator = new OpportunityEditValidator();
        var dto = new OpportunityEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = Array.Empty<byte>(),
            CustomerId = Guid.NewGuid(),
            Title = "Deal",
            EstimatedValueMinor = 5000,
            Stage = OpportunityStage.Proposal
        };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(OpportunityEditDto.RowVersion));
    }

    // ─── CustomerEditValidator ────────────────────────────────────────────────

    [Fact]
    public void CustomerEdit_Should_Pass_WhenAllRequiredFieldsPresent()
    {
        var validator = new CustomerEditValidator(new TestStringLocalizer());
        var dto = new CustomerEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1, 2, 3, 4 },
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "ada@example.com",
            TaxProfileType = CustomerTaxProfileType.Consumer
        };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CustomerEdit_Should_Fail_WhenIdIsEmpty()
    {
        var validator = new CustomerEditValidator(new TestStringLocalizer());
        var dto = new CustomerEditDto
        {
            Id = Guid.Empty,
            RowVersion = new byte[] { 1, 2, 3, 4 },
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "ada@example.com",
            TaxProfileType = CustomerTaxProfileType.Consumer
        };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CustomerEditDto.Id));
    }

    [Fact]
    public void CustomerEdit_Should_Fail_WhenRowVersionIsEmpty()
    {
        var validator = new CustomerEditValidator(new TestStringLocalizer());
        var dto = new CustomerEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = Array.Empty<byte>(),
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "ada@example.com",
            TaxProfileType = CustomerTaxProfileType.Consumer
        };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CustomerEditDto.RowVersion));
    }

    [Fact]
    public void CustomerEdit_Should_Fail_WhenBusinessCustomerHasNoCompanyName()
    {
        var validator = new CustomerEditValidator(new TestStringLocalizer());
        var dto = new CustomerEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1 },
            FirstName = "Biz",
            LastName = "Owner",
            Email = "biz@example.de",
            TaxProfileType = CustomerTaxProfileType.Business,
            CompanyName = null
        };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CustomerEditDto.CompanyName));
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private sealed class TestStringLocalizer : Microsoft.Extensions.Localization.IStringLocalizer<ValidationResource>
    {
        public Microsoft.Extensions.Localization.LocalizedString this[string name] =>
            new(name, name, resourceNotFound: false);

        public Microsoft.Extensions.Localization.LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(name, arguments), resourceNotFound: false);

        public IEnumerable<Microsoft.Extensions.Localization.LocalizedString> GetAllStrings(bool includeParentCultures) =>
            Array.Empty<Microsoft.Extensions.Localization.LocalizedString>();

        public Microsoft.Extensions.Localization.IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
    }
}
