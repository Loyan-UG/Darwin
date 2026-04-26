using System;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Validators;
using FluentAssertions;

namespace Darwin.Tests.Unit.Identity;

/// <summary>
/// Unit tests for address validators:
/// <see cref="AddressCreateValidator"/>, <see cref="AddressEditValidator"/>,
/// and <see cref="AddressDeleteValidator"/>.
/// </summary>
public sealed class AddressValidatorsTests
{
    // ─── AddressCreateValidator ──────────────────────────────────────────────

    [Fact]
    public void AddressCreate_Should_Pass_For_Minimal_Valid_Dto()
    {
        var dto = new AddressCreateDto
        {
            UserId = Guid.NewGuid(),
            Street1 = "Hauptstraße 1",
            PostalCode = "10115",
            City = "Berlin",
            CountryCode = "DE"
        };

        var result = new AddressCreateValidator().Validate(dto);

        result.IsValid.Should().BeTrue("all required fields are provided and within length limits");
    }

    [Fact]
    public void AddressCreate_Should_Pass_With_All_Optional_Fields()
    {
        var dto = new AddressCreateDto
        {
            UserId = Guid.NewGuid(),
            FullName = "Max Mustermann",
            Company = "Acme GmbH",
            Street1 = "Hauptstraße 1",
            Street2 = "3. OG",
            PostalCode = "10115",
            City = "Berlin",
            State = "Berlin",
            CountryCode = "DE",
            PhoneE164 = "+491701234567",
            IsDefaultBilling = true,
            IsDefaultShipping = true
        };

        var result = new AddressCreateValidator().Validate(dto);

        result.IsValid.Should().BeTrue("all optional fields are within their respective maximum lengths");
    }

    [Fact]
    public void AddressCreate_Should_Fail_When_UserId_Empty()
    {
        var dto = new AddressCreateDto
        {
            UserId = Guid.Empty,
            Street1 = "Hauptstraße 1",
            PostalCode = "10115",
            City = "Berlin",
            CountryCode = "DE"
        };

        var result = new AddressCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("an empty UserId must be rejected");
    }

    [Fact]
    public void AddressCreate_Should_Fail_When_Street1_Empty()
    {
        var dto = new AddressCreateDto
        {
            UserId = Guid.NewGuid(),
            Street1 = string.Empty,
            PostalCode = "10115",
            City = "Berlin",
            CountryCode = "DE"
        };

        var result = new AddressCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("an empty Street1 must be rejected");
    }

    [Fact]
    public void AddressCreate_Should_Fail_When_Street1_Exceeds_MaxLength()
    {
        var dto = new AddressCreateDto
        {
            UserId = Guid.NewGuid(),
            Street1 = new string('A', 301),
            PostalCode = "10115",
            City = "Berlin",
            CountryCode = "DE"
        };

        var result = new AddressCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Street1 exceeding 300 characters must be rejected");
    }

    [Fact]
    public void AddressCreate_Should_Fail_When_PostalCode_Empty()
    {
        var dto = new AddressCreateDto
        {
            UserId = Guid.NewGuid(),
            Street1 = "Hauptstraße 1",
            PostalCode = string.Empty,
            City = "Berlin",
            CountryCode = "DE"
        };

        var result = new AddressCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("an empty PostalCode must be rejected");
    }

    [Fact]
    public void AddressCreate_Should_Fail_When_City_Empty()
    {
        var dto = new AddressCreateDto
        {
            UserId = Guid.NewGuid(),
            Street1 = "Hauptstraße 1",
            PostalCode = "10115",
            City = string.Empty,
            CountryCode = "DE"
        };

        var result = new AddressCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("an empty City must be rejected");
    }

    [Theory]
    [InlineData("")]         // empty
    [InlineData("D")]        // 1 char (need exactly 2)
    [InlineData("DEU")]      // 3 chars (too long)
    public void AddressCreate_Should_Fail_When_CountryCode_Not_Two_Characters(string countryCode)
    {
        var dto = new AddressCreateDto
        {
            UserId = Guid.NewGuid(),
            Street1 = "Hauptstraße 1",
            PostalCode = "10115",
            City = "Berlin",
            CountryCode = countryCode
        };

        var result = new AddressCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse($"country code '{countryCode}' does not match the required exact length of 2");
    }

    [Fact]
    public void AddressCreate_Should_Fail_When_FullName_Exceeds_MaxLength()
    {
        var dto = new AddressCreateDto
        {
            UserId = Guid.NewGuid(),
            Street1 = "Hauptstraße 1",
            PostalCode = "10115",
            City = "Berlin",
            CountryCode = "DE",
            FullName = new string('A', 201)
        };

        var result = new AddressCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("FullName exceeding 200 characters must be rejected");
    }

    // ─── AddressEditValidator ────────────────────────────────────────────────

    [Fact]
    public void AddressEdit_Should_Pass_For_Minimal_Valid_Dto()
    {
        var dto = new AddressEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1, 2, 3 },
            Street1 = "Neustraße 5",
            PostalCode = "80331",
            City = "München",
            CountryCode = "DE"
        };

        var result = new AddressEditValidator().Validate(dto);

        result.IsValid.Should().BeTrue("all required fields for editing are provided and valid");
    }

    [Fact]
    public void AddressEdit_Should_Fail_When_Id_Empty()
    {
        var dto = new AddressEditDto
        {
            Id = Guid.Empty,
            RowVersion = new byte[] { 1 },
            Street1 = "Neustraße 5",
            PostalCode = "80331",
            City = "München",
            CountryCode = "DE"
        };

        var result = new AddressEditValidator().Validate(dto);

        result.IsValid.Should().BeFalse("an empty Guid for Id must be rejected");
    }

    [Fact]
    public void AddressEdit_Should_Fail_When_RowVersion_Null()
    {
        var dto = new AddressEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = null!,
            Street1 = "Neustraße 5",
            PostalCode = "80331",
            City = "München",
            CountryCode = "DE"
        };

        var result = new AddressEditValidator().Validate(dto);

        result.IsValid.Should().BeFalse("a null RowVersion must be rejected to enforce optimistic concurrency");
    }

    [Fact]
    public void AddressEdit_Should_Fail_When_City_Exceeds_MaxLength()
    {
        var dto = new AddressEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1 },
            Street1 = "Neustraße 5",
            PostalCode = "80331",
            City = new string('X', 151),
            CountryCode = "DE"
        };

        var result = new AddressEditValidator().Validate(dto);

        result.IsValid.Should().BeFalse("a City name exceeding 150 characters must be rejected");
    }

    // ─── AddressDeleteValidator ──────────────────────────────────────────────

    [Fact]
    public void AddressDelete_Should_Pass_For_Valid_Dto()
    {
        var dto = new AddressDeleteDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1, 2, 3 }
        };

        var result = new AddressDeleteValidator().Validate(dto);

        result.IsValid.Should().BeTrue("a valid Id and RowVersion are the only requirements for deletion");
    }

    [Fact]
    public void AddressDelete_Should_Fail_When_Id_Empty()
    {
        var dto = new AddressDeleteDto
        {
            Id = Guid.Empty,
            RowVersion = new byte[] { 1 }
        };

        var result = new AddressDeleteValidator().Validate(dto);

        result.IsValid.Should().BeFalse("an empty Guid for Id must be rejected");
    }

    [Fact]
    public void AddressDelete_Should_Fail_When_RowVersion_Null()
    {
        var dto = new AddressDeleteDto
        {
            Id = Guid.NewGuid(),
            RowVersion = null!
        };

        var result = new AddressDeleteValidator().Validate(dto);

        result.IsValid.Should().BeFalse("a null RowVersion must be rejected to prevent stale deletes");
    }
}
