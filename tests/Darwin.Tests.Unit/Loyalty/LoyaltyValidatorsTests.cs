using System;
using System.Collections.Generic;
using Darwin.Application;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Application.Loyalty.Validators;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Localization;
using Moq;

namespace Darwin.Tests.Unit.Loyalty;

/// <summary>
/// Unit tests for all Loyalty FluentValidation validators:
/// <see cref="LoyaltyProgramCreateValidator"/>, <see cref="LoyaltyProgramEditValidator"/>,
/// <see cref="LoyaltyProgramDeleteValidator"/>, <see cref="LoyaltyRewardTierCreateValidator"/>,
/// <see cref="LoyaltyRewardTierEditValidator"/>, <see cref="LoyaltyRewardTierDeleteValidator"/>,
/// <see cref="AdjustLoyaltyPointsValidator"/>, <see cref="SuspendLoyaltyAccountValidator"/>,
/// <see cref="ActivateLoyaltyAccountValidator"/>, <see cref="ConfirmLoyaltyRewardRedemptionValidator"/>,
/// <see cref="ConfirmAccrualFromSessionDtoValidator"/>, <see cref="ConfirmRedemptionFromSessionDtoValidator"/>,
/// and <see cref="PrepareScanSessionDtoValidator"/>.
/// </summary>
public sealed class LoyaltyValidatorsTests
{
    private static IStringLocalizer<ValidationResource> CreateLocalizer()
    {
        var mock = new Mock<IStringLocalizer<ValidationResource>>();
        mock.Setup(l => l[It.IsAny<string>()])
            .Returns<string>(name => new LocalizedString(name, name));
        return mock.Object;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // LoyaltyProgramCreateValidator
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ProgramCreate_Should_Pass_For_ValidPerVisitDto()
    {
        var dto = new LoyaltyProgramCreateDto
        {
            BusinessId = Guid.NewGuid(),
            Name = "My Rewards",
            AccrualMode = LoyaltyAccrualMode.PerVisit
        };

        var result = new LoyaltyProgramCreateValidator().Validate(dto);

        result.IsValid.Should().BeTrue("a per-visit program with a name and business id should pass");
    }

    [Fact]
    public void ProgramCreate_Should_Pass_For_AmountBasedWithPositivePointsPerUnit()
    {
        var dto = new LoyaltyProgramCreateDto
        {
            BusinessId = Guid.NewGuid(),
            Name = "Spend & Earn",
            AccrualMode = LoyaltyAccrualMode.AmountBased,
            PointsPerCurrencyUnit = 1.5m
        };

        var result = new LoyaltyProgramCreateValidator().Validate(dto);

        result.IsValid.Should().BeTrue("an amount-based program with a positive PointsPerCurrencyUnit should pass");
    }

    [Fact]
    public void ProgramCreate_Should_Fail_When_BusinessId_Empty()
    {
        var dto = new LoyaltyProgramCreateDto
        {
            BusinessId = Guid.Empty,
            Name = "Valid Name",
            AccrualMode = LoyaltyAccrualMode.PerVisit
        };

        var result = new LoyaltyProgramCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("BusinessId is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.BusinessId));
    }

    [Fact]
    public void ProgramCreate_Should_Fail_When_Name_Empty()
    {
        var dto = new LoyaltyProgramCreateDto
        {
            BusinessId = Guid.NewGuid(),
            Name = "",
            AccrualMode = LoyaltyAccrualMode.PerVisit
        };

        var result = new LoyaltyProgramCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Name is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Name));
    }

    [Fact]
    public void ProgramCreate_Should_Fail_When_Name_TooLong()
    {
        var dto = new LoyaltyProgramCreateDto
        {
            BusinessId = Guid.NewGuid(),
            Name = new string('A', 201),
            AccrualMode = LoyaltyAccrualMode.PerVisit
        };

        var result = new LoyaltyProgramCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Name must not exceed 200 characters");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Name));
    }

    [Fact]
    public void ProgramCreate_Should_Fail_For_AmountBased_When_PointsPerCurrencyUnit_Null()
    {
        var dto = new LoyaltyProgramCreateDto
        {
            BusinessId = Guid.NewGuid(),
            Name = "Amount Based",
            AccrualMode = LoyaltyAccrualMode.AmountBased,
            PointsPerCurrencyUnit = null
        };

        var result = new LoyaltyProgramCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("PointsPerCurrencyUnit must be set for AmountBased mode");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.PointsPerCurrencyUnit));
    }

    [Fact]
    public void ProgramCreate_Should_Fail_For_AmountBased_When_PointsPerCurrencyUnit_Zero()
    {
        var dto = new LoyaltyProgramCreateDto
        {
            BusinessId = Guid.NewGuid(),
            Name = "Amount Based",
            AccrualMode = LoyaltyAccrualMode.AmountBased,
            PointsPerCurrencyUnit = 0m
        };

        var result = new LoyaltyProgramCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("PointsPerCurrencyUnit must be greater than 0");
    }

    [Fact]
    public void ProgramCreate_Should_NotRequirePointsPerUnit_For_PerVisit()
    {
        var dto = new LoyaltyProgramCreateDto
        {
            BusinessId = Guid.NewGuid(),
            Name = "Visit Rewards",
            AccrualMode = LoyaltyAccrualMode.PerVisit,
            PointsPerCurrencyUnit = null
        };

        var result = new LoyaltyProgramCreateValidator().Validate(dto);

        result.IsValid.Should().BeTrue("PointsPerCurrencyUnit is not required for PerVisit accrual mode");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // LoyaltyProgramEditValidator
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ProgramEdit_Should_Pass_For_ValidDto()
    {
        var dto = new LoyaltyProgramEditDto
        {
            Id = Guid.NewGuid(),
            BusinessId = Guid.NewGuid(),
            Name = "Updated Program",
            AccrualMode = LoyaltyAccrualMode.PerVisit
        };

        var result = new LoyaltyProgramEditValidator().Validate(dto);

        result.IsValid.Should().BeTrue("a fully populated edit DTO should pass");
    }

    [Fact]
    public void ProgramEdit_Should_Fail_When_Id_Empty()
    {
        var dto = new LoyaltyProgramEditDto
        {
            Id = Guid.Empty,
            BusinessId = Guid.NewGuid(),
            Name = "Program",
            AccrualMode = LoyaltyAccrualMode.PerVisit
        };

        var result = new LoyaltyProgramEditValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Id is required for edit");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Id));
    }

    [Fact]
    public void ProgramEdit_Should_Fail_When_BusinessId_Empty()
    {
        var dto = new LoyaltyProgramEditDto
        {
            Id = Guid.NewGuid(),
            BusinessId = Guid.Empty,
            Name = "Program",
            AccrualMode = LoyaltyAccrualMode.PerVisit
        };

        var result = new LoyaltyProgramEditValidator().Validate(dto);

        result.IsValid.Should().BeFalse("BusinessId is required for edit");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.BusinessId));
    }

    [Fact]
    public void ProgramEdit_Should_Fail_For_AmountBased_When_PointsPerCurrencyUnit_NotPositive()
    {
        var dto = new LoyaltyProgramEditDto
        {
            Id = Guid.NewGuid(),
            BusinessId = Guid.NewGuid(),
            Name = "Amount Based",
            AccrualMode = LoyaltyAccrualMode.AmountBased,
            PointsPerCurrencyUnit = -1m
        };

        var result = new LoyaltyProgramEditValidator().Validate(dto);

        result.IsValid.Should().BeFalse("PointsPerCurrencyUnit must be greater than 0 for AmountBased");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // LoyaltyProgramDeleteValidator
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ProgramDelete_Should_Pass_For_ValidId()
    {
        var dto = new LoyaltyProgramDeleteDto { Id = Guid.NewGuid() };

        var result = new LoyaltyProgramDeleteValidator().Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ProgramDelete_Should_Fail_When_Id_Empty()
    {
        var dto = new LoyaltyProgramDeleteDto { Id = Guid.Empty };

        var result = new LoyaltyProgramDeleteValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Id is required for deletion");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Id));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // LoyaltyRewardTierCreateValidator
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TierCreate_Should_Pass_For_ValidDto()
    {
        var dto = new LoyaltyRewardTierCreateDto
        {
            LoyaltyProgramId = Guid.NewGuid(),
            PointsRequired = 100,
            RewardType = LoyaltyRewardType.FreeItem
        };

        var result = new LoyaltyRewardTierCreateValidator().Validate(dto);

        result.IsValid.Should().BeTrue("a valid reward tier create DTO should pass");
    }

    [Fact]
    public void TierCreate_Should_Fail_When_LoyaltyProgramId_Empty()
    {
        var dto = new LoyaltyRewardTierCreateDto
        {
            LoyaltyProgramId = Guid.Empty,
            PointsRequired = 100,
            RewardType = LoyaltyRewardType.FreeItem
        };

        var result = new LoyaltyRewardTierCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("LoyaltyProgramId is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.LoyaltyProgramId));
    }

    [Fact]
    public void TierCreate_Should_Fail_When_PointsRequired_Zero()
    {
        var dto = new LoyaltyRewardTierCreateDto
        {
            LoyaltyProgramId = Guid.NewGuid(),
            PointsRequired = 0,
            RewardType = LoyaltyRewardType.FreeItem
        };

        var result = new LoyaltyRewardTierCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("PointsRequired must be greater than 0");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.PointsRequired));
    }

    [Fact]
    public void TierCreate_Should_Fail_When_PointsRequired_Negative()
    {
        var dto = new LoyaltyRewardTierCreateDto
        {
            LoyaltyProgramId = Guid.NewGuid(),
            PointsRequired = -10,
            RewardType = LoyaltyRewardType.FreeItem
        };

        var result = new LoyaltyRewardTierCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("PointsRequired must be positive");
    }

    [Fact]
    public void TierCreate_Should_Fail_When_Description_TooLong()
    {
        var dto = new LoyaltyRewardTierCreateDto
        {
            LoyaltyProgramId = Guid.NewGuid(),
            PointsRequired = 50,
            RewardType = LoyaltyRewardType.FreeItem,
            Description = new string('X', 501)
        };

        var result = new LoyaltyRewardTierCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Description must not exceed 500 characters");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Description));
    }

    [Fact]
    public void TierCreate_Should_Pass_For_MaxLengthDescription()
    {
        var dto = new LoyaltyRewardTierCreateDto
        {
            LoyaltyProgramId = Guid.NewGuid(),
            PointsRequired = 50,
            RewardType = LoyaltyRewardType.PercentDiscount,
            Description = new string('X', 500)
        };

        var result = new LoyaltyRewardTierCreateValidator().Validate(dto);

        result.IsValid.Should().BeTrue("exactly 500-character description is within the limit");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // LoyaltyRewardTierEditValidator
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TierEdit_Should_Pass_For_ValidDto()
    {
        var dto = new LoyaltyRewardTierEditDto
        {
            Id = Guid.NewGuid(),
            LoyaltyProgramId = Guid.NewGuid(),
            PointsRequired = 200,
            RewardType = LoyaltyRewardType.AmountDiscount
        };

        var result = new LoyaltyRewardTierEditValidator().Validate(dto);

        result.IsValid.Should().BeTrue("a valid reward tier edit DTO should pass");
    }

    [Fact]
    public void TierEdit_Should_Fail_When_Id_Empty()
    {
        var dto = new LoyaltyRewardTierEditDto
        {
            Id = Guid.Empty,
            LoyaltyProgramId = Guid.NewGuid(),
            PointsRequired = 100,
            RewardType = LoyaltyRewardType.FreeItem
        };

        var result = new LoyaltyRewardTierEditValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Id is required for edit");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Id));
    }

    [Fact]
    public void TierEdit_Should_Fail_When_PointsRequired_NotPositive()
    {
        var dto = new LoyaltyRewardTierEditDto
        {
            Id = Guid.NewGuid(),
            LoyaltyProgramId = Guid.NewGuid(),
            PointsRequired = 0,
            RewardType = LoyaltyRewardType.FreeItem
        };

        var result = new LoyaltyRewardTierEditValidator().Validate(dto);

        result.IsValid.Should().BeFalse("PointsRequired must be greater than 0 for edit");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // LoyaltyRewardTierDeleteValidator
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TierDelete_Should_Pass_For_ValidId()
    {
        var dto = new LoyaltyRewardTierDeleteDto { Id = Guid.NewGuid() };

        var result = new LoyaltyRewardTierDeleteValidator().Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void TierDelete_Should_Fail_When_Id_Empty()
    {
        var dto = new LoyaltyRewardTierDeleteDto { Id = Guid.Empty };

        var result = new LoyaltyRewardTierDeleteValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Id is required for tier deletion");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Id));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // AdjustLoyaltyPointsValidator
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AdjustPoints_Should_Pass_For_PositiveDeltaWithoutReason()
    {
        var dto = new AdjustLoyaltyPointsDto
        {
            LoyaltyAccountId = Guid.NewGuid(),
            BusinessId = Guid.NewGuid(),
            PointsDelta = 50
        };

        var result = new AdjustLoyaltyPointsValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("a positive points adjustment does not require a reason");
    }

    [Fact]
    public void AdjustPoints_Should_Pass_For_NegativeDeltaWithReason()
    {
        var dto = new AdjustLoyaltyPointsDto
        {
            LoyaltyAccountId = Guid.NewGuid(),
            BusinessId = Guid.NewGuid(),
            PointsDelta = -10,
            Reason = "Customer complaint correction"
        };

        var result = new AdjustLoyaltyPointsValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("a negative delta with a reason should pass");
    }

    [Fact]
    public void AdjustPoints_Should_Fail_When_LoyaltyAccountId_Empty()
    {
        var dto = new AdjustLoyaltyPointsDto
        {
            LoyaltyAccountId = Guid.Empty,
            BusinessId = Guid.NewGuid(),
            PointsDelta = 10
        };

        var result = new AdjustLoyaltyPointsValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("LoyaltyAccountId is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.LoyaltyAccountId));
    }

    [Fact]
    public void AdjustPoints_Should_Fail_When_BusinessId_Empty()
    {
        var dto = new AdjustLoyaltyPointsDto
        {
            LoyaltyAccountId = Guid.NewGuid(),
            BusinessId = Guid.Empty,
            PointsDelta = 10
        };

        var result = new AdjustLoyaltyPointsValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("BusinessId is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.BusinessId));
    }

    [Fact]
    public void AdjustPoints_Should_Fail_When_PointsDelta_IsZero()
    {
        var dto = new AdjustLoyaltyPointsDto
        {
            LoyaltyAccountId = Guid.NewGuid(),
            BusinessId = Guid.NewGuid(),
            PointsDelta = 0
        };

        var result = new AdjustLoyaltyPointsValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("PointsDelta must not be zero");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.PointsDelta));
    }

    [Fact]
    public void AdjustPoints_Should_Fail_For_NegativeDelta_Without_Reason()
    {
        var dto = new AdjustLoyaltyPointsDto
        {
            LoyaltyAccountId = Guid.NewGuid(),
            BusinessId = Guid.NewGuid(),
            PointsDelta = -5,
            Reason = null
        };

        var result = new AdjustLoyaltyPointsValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("Reason is required when subtracting points");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Reason));
    }

    [Fact]
    public void AdjustPoints_Should_Fail_When_Reason_TooLong()
    {
        var dto = new AdjustLoyaltyPointsDto
        {
            LoyaltyAccountId = Guid.NewGuid(),
            BusinessId = Guid.NewGuid(),
            PointsDelta = 10,
            Reason = new string('R', 1001)
        };

        var result = new AdjustLoyaltyPointsValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("Reason must not exceed 1000 characters");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Reason));
    }

    [Fact]
    public void AdjustPoints_Should_Fail_When_Reference_TooLong()
    {
        var dto = new AdjustLoyaltyPointsDto
        {
            LoyaltyAccountId = Guid.NewGuid(),
            BusinessId = Guid.NewGuid(),
            PointsDelta = 10,
            Reference = new string('R', 201)
        };

        var result = new AdjustLoyaltyPointsValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("Reference must not exceed 200 characters");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Reference));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SuspendLoyaltyAccountValidator
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SuspendAccount_Should_Pass_For_ValidId()
    {
        var dto = new SuspendLoyaltyAccountDto { Id = Guid.NewGuid() };

        var result = new SuspendLoyaltyAccountValidator().Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void SuspendAccount_Should_Fail_When_Id_Empty()
    {
        var dto = new SuspendLoyaltyAccountDto { Id = Guid.Empty };

        var result = new SuspendLoyaltyAccountValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Id is required to suspend an account");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Id));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ActivateLoyaltyAccountValidator
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ActivateAccount_Should_Pass_For_ValidId()
    {
        var dto = new ActivateLoyaltyAccountDto { Id = Guid.NewGuid() };

        var result = new ActivateLoyaltyAccountValidator().Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ActivateAccount_Should_Fail_When_Id_Empty()
    {
        var dto = new ActivateLoyaltyAccountDto { Id = Guid.Empty };

        var result = new ActivateLoyaltyAccountValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Id is required to activate an account");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Id));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ConfirmLoyaltyRewardRedemptionValidator
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ConfirmRedemption_Should_Pass_For_ValidDto()
    {
        var dto = new ConfirmLoyaltyRewardRedemptionDto
        {
            RedemptionId = Guid.NewGuid(),
            BusinessId = Guid.NewGuid()
        };

        var result = new ConfirmLoyaltyRewardRedemptionValidator().Validate(dto);

        result.IsValid.Should().BeTrue("a valid confirm redemption DTO should pass");
    }

    [Fact]
    public void ConfirmRedemption_Should_Fail_When_RedemptionId_Empty()
    {
        var dto = new ConfirmLoyaltyRewardRedemptionDto
        {
            RedemptionId = Guid.Empty,
            BusinessId = Guid.NewGuid()
        };

        var result = new ConfirmLoyaltyRewardRedemptionValidator().Validate(dto);

        result.IsValid.Should().BeFalse("RedemptionId is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.RedemptionId));
    }

    [Fact]
    public void ConfirmRedemption_Should_Fail_When_BusinessId_Empty()
    {
        var dto = new ConfirmLoyaltyRewardRedemptionDto
        {
            RedemptionId = Guid.NewGuid(),
            BusinessId = Guid.Empty
        };

        var result = new ConfirmLoyaltyRewardRedemptionValidator().Validate(dto);

        result.IsValid.Should().BeFalse("BusinessId is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.BusinessId));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ConfirmAccrualFromSessionDtoValidator
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ConfirmAccrual_Should_Pass_For_ValidDto()
    {
        var dto = new ConfirmAccrualFromSessionDto
        {
            ScanSessionToken = "valid-scan-token",
            Points = 1
        };

        var result = new ConfirmAccrualFromSessionDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("a valid accrual confirm DTO should pass");
    }

    [Fact]
    public void ConfirmAccrual_Should_Fail_When_Token_Empty()
    {
        var dto = new ConfirmAccrualFromSessionDto
        {
            ScanSessionToken = "",
            Points = 1
        };

        var result = new ConfirmAccrualFromSessionDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("ScanSessionToken is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.ScanSessionToken));
    }

    [Fact]
    public void ConfirmAccrual_Should_Fail_When_Token_TooLong()
    {
        var dto = new ConfirmAccrualFromSessionDto
        {
            ScanSessionToken = new string('T', 4001),
            Points = 1
        };

        var result = new ConfirmAccrualFromSessionDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("ScanSessionToken must not exceed 4000 characters");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.ScanSessionToken));
    }

    [Fact]
    public void ConfirmAccrual_Should_Fail_When_Points_Zero()
    {
        var dto = new ConfirmAccrualFromSessionDto
        {
            ScanSessionToken = "valid-token",
            Points = 0
        };

        var result = new ConfirmAccrualFromSessionDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("Points must be greater than 0");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Points));
    }

    [Fact]
    public void ConfirmAccrual_Should_Fail_When_Points_Negative()
    {
        var dto = new ConfirmAccrualFromSessionDto
        {
            ScanSessionToken = "valid-token",
            Points = -1
        };

        var result = new ConfirmAccrualFromSessionDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("Points must be a positive integer");
    }

    [Fact]
    public void ConfirmAccrual_Should_Fail_When_Note_TooLong()
    {
        var dto = new ConfirmAccrualFromSessionDto
        {
            ScanSessionToken = "valid-token",
            Points = 1,
            Note = new string('N', 501)
        };

        var result = new ConfirmAccrualFromSessionDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("Note must not exceed 500 characters");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Note));
    }

    [Fact]
    public void ConfirmAccrual_Should_Pass_When_Note_Is_Null()
    {
        var dto = new ConfirmAccrualFromSessionDto
        {
            ScanSessionToken = "valid-token",
            Points = 5,
            Note = null
        };

        var result = new ConfirmAccrualFromSessionDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("a null note should be allowed");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ConfirmRedemptionFromSessionDtoValidator
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ConfirmRedemptionFromSession_Should_Pass_For_ValidToken()
    {
        var dto = new ConfirmRedemptionFromSessionDto { ScanSessionToken = "scan-abc-xyz" };

        var result = new ConfirmRedemptionFromSessionDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("a non-empty token within length limits should pass");
    }

    [Fact]
    public void ConfirmRedemptionFromSession_Should_Fail_When_Token_Empty()
    {
        var dto = new ConfirmRedemptionFromSessionDto { ScanSessionToken = "" };

        var result = new ConfirmRedemptionFromSessionDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("ScanSessionToken is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.ScanSessionToken));
    }

    [Fact]
    public void ConfirmRedemptionFromSession_Should_Fail_When_Token_TooLong()
    {
        var dto = new ConfirmRedemptionFromSessionDto
        {
            ScanSessionToken = new string('T', 4001)
        };

        var result = new ConfirmRedemptionFromSessionDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("ScanSessionToken must not exceed 4000 characters");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PrepareScanSessionDtoValidator
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void PrepareScan_Should_Pass_For_AccrualModeWithNoRewards()
    {
        var dto = new PrepareScanSessionDto
        {
            BusinessId = Guid.NewGuid(),
            Mode = LoyaltyScanMode.Accrual
        };

        var result = new PrepareScanSessionDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("an accrual session without reward tiers should pass");
    }

    [Fact]
    public void PrepareScan_Should_Pass_For_RedemptionModeWithRewardTiers()
    {
        var dto = new PrepareScanSessionDto
        {
            BusinessId = Guid.NewGuid(),
            Mode = LoyaltyScanMode.Redemption,
            SelectedRewardTierIds = new List<Guid> { Guid.NewGuid() }
        };

        var result = new PrepareScanSessionDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("a redemption session with at least one reward tier should pass");
    }

    [Fact]
    public void PrepareScan_Should_Fail_When_BusinessId_Empty()
    {
        var dto = new PrepareScanSessionDto
        {
            BusinessId = Guid.Empty,
            Mode = LoyaltyScanMode.Accrual
        };

        var result = new PrepareScanSessionDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("BusinessId is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.BusinessId));
    }

    [Fact]
    public void PrepareScan_Should_Fail_For_RedemptionMode_When_NoRewardTiers()
    {
        var dto = new PrepareScanSessionDto
        {
            BusinessId = Guid.NewGuid(),
            Mode = LoyaltyScanMode.Redemption,
            SelectedRewardTierIds = new List<Guid>()
        };

        var result = new PrepareScanSessionDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("at least one reward tier is required for redemption mode");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.SelectedRewardTierIds));
    }

    [Fact]
    public void PrepareScan_Should_Fail_For_RedemptionMode_When_RewardTierIds_ContainsEmptyGuid()
    {
        var dto = new PrepareScanSessionDto
        {
            BusinessId = Guid.NewGuid(),
            Mode = LoyaltyScanMode.Redemption,
            SelectedRewardTierIds = new List<Guid> { Guid.Empty }
        };

        var result = new PrepareScanSessionDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("reward tier IDs must not be empty GUIDs");
        result.Errors.Should().Contain(e => e.PropertyName.Contains("SelectedRewardTierIds"));
    }

    [Fact]
    public void PrepareScan_Should_Fail_When_DeviceId_TooLong()
    {
        var dto = new PrepareScanSessionDto
        {
            BusinessId = Guid.NewGuid(),
            Mode = LoyaltyScanMode.Accrual,
            DeviceId = new string('D', 201)
        };

        var result = new PrepareScanSessionDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("DeviceId must not exceed 200 characters");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.DeviceId));
    }

    [Fact]
    public void PrepareScan_Should_Pass_When_DeviceId_Is_Null()
    {
        var dto = new PrepareScanSessionDto
        {
            BusinessId = Guid.NewGuid(),
            Mode = LoyaltyScanMode.Accrual,
            DeviceId = null
        };

        var result = new PrepareScanSessionDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("a null DeviceId is allowed");
    }

    [Theory]
    [InlineData(99)]
    [InlineData(-1)]
    [InlineData(100)]
    public void PrepareScan_Should_Fail_WhenModeIsInvalidNumericValue(int modeValue)
    {
        // Proves invalid numeric LoyaltyScanMode values are rejected with ModeInvalid
        // instead of silently defaulting to Accrual (backlog item §7).
        var dto = new PrepareScanSessionDto
        {
            BusinessId = Guid.NewGuid(),
            Mode = (LoyaltyScanMode)modeValue
        };

        var result = new PrepareScanSessionDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("out-of-range Mode values must be rejected");
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(dto.Mode) &&
            e.ErrorMessage == "ModeInvalid");
    }
}
