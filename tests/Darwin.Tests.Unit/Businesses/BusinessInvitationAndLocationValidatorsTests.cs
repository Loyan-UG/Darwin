using System;
using Darwin.Application.Businesses.DTOs;
using Darwin.Application.Businesses.Validators;
using Darwin.Application.Common.DTOs;
using FluentAssertions;

namespace Darwin.Tests.Unit.Businesses;

public sealed class BusinessInvitationAndLocationValidatorsTests
{
    [Fact]
    public void InvitationCreate_Should_Pass_ForValidDto()
    {
        var dto = new BusinessInvitationCreateDto
        {
            BusinessId = Guid.NewGuid(),
            Email = "staff@darwin.test",
            ExpiresInDays = 14,
            Note = "Please complete onboarding this week."
        };

        var result = new BusinessInvitationCreateDtoValidator().Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", 7)]
    [InlineData("not-an-email", 7)]
    [InlineData("staff@darwin.test", 0)]
    [InlineData("staff@darwin.test", 31)]
    public void InvitationCreate_Should_Fail_ForInvalidEmailOrExpiry(string email, int expiresInDays)
    {
        var dto = new BusinessInvitationCreateDto
        {
            BusinessId = Guid.NewGuid(),
            Email = email,
            ExpiresInDays = expiresInDays
        };

        var result = new BusinessInvitationCreateDtoValidator().Validate(dto);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void InvitationCreate_Should_Fail_WhenNoteIsTooLong()
    {
        var dto = new BusinessInvitationCreateDto
        {
            BusinessId = Guid.NewGuid(),
            Email = "staff@darwin.test",
            ExpiresInDays = 7,
            Note = new string('n', 2001)
        };

        var result = new BusinessInvitationCreateDtoValidator().Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Note));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(31)]
    public void InvitationResend_Should_Fail_WhenExpiryIsOutOfRange(int expiresInDays)
    {
        var dto = new BusinessInvitationResendDto
        {
            Id = Guid.NewGuid(),
            ExpiresInDays = expiresInDays
        };

        var result = new BusinessInvitationResendDtoValidator().Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.ExpiresInDays));
    }

    [Fact]
    public void InvitationRevoke_Should_Fail_WhenIdMissing()
    {
        var dto = new BusinessInvitationRevokeDto
        {
            Id = Guid.Empty
        };

        var result = new BusinessInvitationRevokeDtoValidator().Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Id));
    }

    [Fact]
    public void InvitationRevoke_Should_Fail_WhenNoteTooLong()
    {
        var dto = new BusinessInvitationRevokeDto
        {
            Id = Guid.NewGuid(),
            Note = new string('r', 2001)
        };

        var result = new BusinessInvitationRevokeDtoValidator().Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Note));
    }

    [Fact]
    public void InvitationAccept_Should_Pass_WithoutPassword()
    {
        var dto = new BusinessInvitationAcceptDto
        {
            Token = "valid-token-123"
        };

        var result = new BusinessInvitationAcceptDtoValidator().Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void InvitationAccept_Should_Fail_WhenTokenTooShort_AndPasswordTooShort()
    {
        var dto = new BusinessInvitationAcceptDto
        {
            Token = "short",
            Password = "tiny"
        };

        var result = new BusinessInvitationAcceptDtoValidator().Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Token));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Password));
    }

    [Fact]
    public void LocationCreate_Should_Pass_ForValidCoordinateBoundaries()
    {
        var dto = new BusinessLocationCreateDto
        {
            BusinessId = Guid.NewGuid(),
            Name = "Downtown Branch",
            Coordinate = new GeoCoordinateDto
            {
                Latitude = 90,
                Longitude = -180
            }
        };

        var result = new BusinessLocationCreateDtoValidator().Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void LocationCreate_Should_Fail_WhenCoordinateOutsideAllowedRange()
    {
        var dto = new BusinessLocationCreateDto
        {
            BusinessId = Guid.NewGuid(),
            Name = "Downtown Branch",
            Coordinate = new GeoCoordinateDto
            {
                Latitude = 91,
                Longitude = -181
            }
        };

        var result = new BusinessLocationCreateDtoValidator().Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Coordinate.Latitude");
        result.Errors.Should().Contain(e => e.PropertyName == "Coordinate.Longitude");
    }

    [Fact]
    public void LocationEdit_Should_Fail_WhenRequiredFieldsAreMissing()
    {
        var dto = new BusinessLocationEditDto
        {
            Id = Guid.Empty,
            BusinessId = Guid.Empty,
            Name = string.Empty,
            RowVersion = []
        };

        var result = new BusinessLocationEditDtoValidator().Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Id));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.BusinessId));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Name));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.RowVersion));
    }

    [Fact]
    public void LocationDelete_Should_Fail_WhenConcurrencyTokenMissing()
    {
        var dto = new BusinessLocationDeleteDto
        {
            Id = Guid.Empty,
            RowVersion = []
        };

        var result = new BusinessLocationDeleteDtoValidator().Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Id));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.RowVersion));
    }
}
