using Darwin.Application;
using Darwin.Application.CRM.DTOs;
using Darwin.Application.CRM.Validators;
using Darwin.Domain.Enums;
using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.CRM;

/// <summary>
/// Unit tests for CRM input validators covering field constraints and conditional rules.
/// </summary>
public sealed class CrmValidatorsTests
{
    // ─── CustomerCreateValidator ──────────────────────────────────────────────

    [Fact]
    public void CustomerCreate_Should_Pass_WhenAllRequiredFieldsPresent()
    {
        var validator = new CustomerCreateValidator(new TestStringLocalizer());
        var dto = new CustomerCreateDto
        {
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "ada@example.com",
            TaxProfileType = CustomerTaxProfileType.Consumer
        };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CustomerCreate_Should_Fail_WhenFirstNameMissingAndNoUserId()
    {
        var validator = new CustomerCreateValidator(new TestStringLocalizer());
        var dto = new CustomerCreateDto
        {
            FirstName = string.Empty,
            LastName = "Lovelace",
            Email = "ada@example.com"
        };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CustomerCreateDto.FirstName));
    }

    [Fact]
    public void CustomerCreate_Should_Fail_WhenEmailIsInvalid()
    {
        var validator = new CustomerCreateValidator(new TestStringLocalizer());
        var dto = new CustomerCreateDto
        {
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "not-an-email",
            TaxProfileType = CustomerTaxProfileType.Consumer
        };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CustomerCreateDto.Email));
    }

    [Fact]
    public void CustomerCreate_Should_Fail_WhenBusinessCustomerHasNoCompanyName()
    {
        var validator = new CustomerCreateValidator(new TestStringLocalizer());
        var dto = new CustomerCreateDto
        {
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "ada@example.com",
            TaxProfileType = CustomerTaxProfileType.Business,
            CompanyName = null
        };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CustomerCreateDto.CompanyName));
    }

    [Fact]
    public void CustomerCreate_Should_Pass_WhenUserIdIsSet_AndFieldsMissing()
    {
        // When UserId is provided, FirstName/LastName/Email are not required.
        var validator = new CustomerCreateValidator(new TestStringLocalizer());
        var dto = new CustomerCreateDto
        {
            UserId = Guid.NewGuid(),
            FirstName = string.Empty,
            LastName = string.Empty,
            Email = string.Empty,
            TaxProfileType = CustomerTaxProfileType.Consumer
        };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CustomerCreate_Should_Fail_WhenAddressIsMissingRequiredFields()
    {
        var validator = new CustomerCreateValidator(new TestStringLocalizer());
        var dto = new CustomerCreateDto
        {
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "ada@example.com",
            Addresses = new List<CustomerAddressDto>
            {
                new() { Line1 = string.Empty, City = string.Empty, PostalCode = string.Empty, Country = string.Empty }
            }
        };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
    }

    // ─── InteractionCreateValidator ───────────────────────────────────────────

    [Fact]
    public void InteractionCreate_Should_Pass_WhenExactlyOneTargetIsSet()
    {
        var validator = new InteractionCreateValidator(new TestStringLocalizer());
        var dto = new InteractionCreateDto
        {
            CustomerId = Guid.NewGuid(),
            Type = InteractionType.Email,
            Channel = InteractionChannel.Email
        };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void InteractionCreate_Should_Fail_WhenNoTargetIsSet()
    {
        var validator = new InteractionCreateValidator(new TestStringLocalizer());
        var dto = new InteractionCreateDto
        {
            CustomerId = null,
            LeadId = null,
            OpportunityId = null,
            Type = InteractionType.Email,
            Channel = InteractionChannel.Email
        };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void InteractionCreate_Should_Fail_WhenMultipleTargetsAreSet()
    {
        var validator = new InteractionCreateValidator(new TestStringLocalizer());
        var dto = new InteractionCreateDto
        {
            CustomerId = Guid.NewGuid(),
            LeadId = Guid.NewGuid(),
            Type = InteractionType.Call,
            Channel = InteractionChannel.Phone
        };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void InteractionCreate_Should_Fail_WhenSubjectExceedsMaxLength()
    {
        var validator = new InteractionCreateValidator(new TestStringLocalizer());
        var dto = new InteractionCreateDto
        {
            CustomerId = Guid.NewGuid(),
            Type = InteractionType.Email,
            Channel = InteractionChannel.Email,
            Subject = new string('X', 301)
        };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(InteractionCreateDto.Subject));
    }

    // ─── ConsentCreateValidator ───────────────────────────────────────────────

    [Fact]
    public void ConsentCreate_Should_Pass_WhenGranted()
    {
        var validator = new ConsentCreateValidator(new TestStringLocalizer());
        var dto = new ConsentCreateDto
        {
            CustomerId = Guid.NewGuid(),
            Type = ConsentType.MarketingEmail,
            Granted = true,
            GrantedAtUtc = DateTime.UtcNow
        };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ConsentCreate_Should_Pass_WhenRevokedWithTimestamp()
    {
        var validator = new ConsentCreateValidator(new TestStringLocalizer());
        var dto = new ConsentCreateDto
        {
            CustomerId = Guid.NewGuid(),
            Type = ConsentType.MarketingEmail,
            Granted = false,
            GrantedAtUtc = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            RevokedAtUtc = new DateTime(2030, 6, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ConsentCreate_Should_Fail_WhenRevokedWithoutTimestamp()
    {
        var validator = new ConsentCreateValidator(new TestStringLocalizer());
        var dto = new ConsentCreateDto
        {
            CustomerId = Guid.NewGuid(),
            Type = ConsentType.MarketingEmail,
            Granted = false,
            GrantedAtUtc = DateTime.UtcNow,
            RevokedAtUtc = null
        };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ConsentCreate_Should_Fail_WhenCustomerIdIsEmpty()
    {
        var validator = new ConsentCreateValidator(new TestStringLocalizer());
        var dto = new ConsentCreateDto
        {
            CustomerId = Guid.Empty,
            Type = ConsentType.MarketingEmail,
            Granted = true,
            GrantedAtUtc = DateTime.UtcNow
        };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ConsentCreateDto.CustomerId));
    }

    // ─── CustomerSegmentEditValidator ─────────────────────────────────────────

    [Fact]
    public void CustomerSegmentEdit_Should_Pass_WhenNameIsValid()
    {
        var validator = new CustomerSegmentEditValidator();
        var dto = new CustomerSegmentEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = [1, 2, 3, 4],
            Name = "Returning Buyers"
        };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CustomerSegmentEdit_Should_Fail_WhenNameIsEmpty()
    {
        var validator = new CustomerSegmentEditValidator();
        var dto = new CustomerSegmentEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = [1, 2, 3, 4],
            Name = string.Empty
        };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CustomerSegmentEditDto.Name));
    }

    [Fact]
    public void CustomerSegmentEdit_Should_Fail_WhenDescriptionExceedsMaxLength()
    {
        var validator = new CustomerSegmentEditValidator();
        var dto = new CustomerSegmentEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = [1, 2, 3, 4],
            Name = "Valid Name",
            Description = new string('D', 2001)
        };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CustomerSegmentEditDto.Description));
    }

    // ─── InvoiceRefundCreateValidator ─────────────────────────────────────────

    [Fact]
    public void InvoiceRefundCreate_Should_Pass_WhenAllFieldsAreValid()
    {
        var validator = new InvoiceRefundCreateValidator();
        var dto = new InvoiceRefundCreateDto
        {
            InvoiceId = Guid.NewGuid(),
            RowVersion = [1, 2, 3, 4],
            AmountMinor = 500,
            Currency = "EUR",
            Reason = "Goodwill refund"
        };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void InvoiceRefundCreate_Should_Fail_WhenAmountIsZeroOrNegative()
    {
        var validator = new InvoiceRefundCreateValidator();

        foreach (var amount in new[] { 0L, -100L })
        {
            var dto = new InvoiceRefundCreateDto
            {
                InvoiceId = Guid.NewGuid(),
                RowVersion = [1, 2, 3, 4],
                AmountMinor = amount,
                Currency = "EUR",
                Reason = "Test"
            };

            var result = validator.Validate(dto);

            result.IsValid.Should().BeFalse($"amount={amount} should fail");
            result.Errors.Should().Contain(e => e.PropertyName == nameof(InvoiceRefundCreateDto.AmountMinor));
        }
    }

    [Fact]
    public void InvoiceRefundCreate_Should_Fail_WhenCurrencyIsNotThreeChars()
    {
        var validator = new InvoiceRefundCreateValidator();
        var dto = new InvoiceRefundCreateDto
        {
            InvoiceId = Guid.NewGuid(),
            RowVersion = [1, 2, 3, 4],
            AmountMinor = 100,
            Currency = "EU",
            Reason = "Test"
        };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(InvoiceRefundCreateDto.Currency));
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private sealed class TestStringLocalizer : IStringLocalizer<ValidationResource>
    {
        public LocalizedString this[string name] => new(name, name, resourceNotFound: false);

        public LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(name, arguments), resourceNotFound: false);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            Array.Empty<LocalizedString>();

        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
    }
}
