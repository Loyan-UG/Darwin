using System;
using System.Threading.Tasks;
using Darwin.Application;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Loyalty.Commands;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Application.Loyalty.Validators;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Loyalty;
using Darwin.Domain.Enums;
using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.Loyalty;

/// <summary>
/// Unit tests for loyalty command handlers:
/// <see cref="AdjustLoyaltyPointsHandler"/>,
/// <see cref="ActivateLoyaltyAccountHandler"/>,
/// <see cref="SuspendLoyaltyAccountHandler"/>,
/// <see cref="CreateLoyaltyProgramHandler"/>,
/// <see cref="UpdateLoyaltyProgramHandler"/>,
/// <see cref="CreateLoyaltyRewardTierHandler"/>,
/// and <see cref="SoftDeleteLoyaltyProgramHandler"/>.
/// </summary>
public sealed class LoyaltyHandlerTests
{
    // ─── AdjustLoyaltyPointsHandler ──────────────────────────────────────────

    [Fact]
    public async Task AdjustLoyaltyPoints_Should_IncreaseBalance_WhenDeltaIsPositive()
    {
        await using var db = LoyaltyTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        db.Set<LoyaltyAccount>().Add(new LoyaltyAccount
        {
            Id = accountId,
            BusinessId = businessId,
            UserId = Guid.NewGuid(),
            Status = LoyaltyAccountStatus.Active,
            PointsBalance = 100,
            LifetimePoints = 100
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateAdjustHandler(db);

        var result = await handler.HandleAsync(new AdjustLoyaltyPointsDto
        {
            LoyaltyAccountId = accountId,
            BusinessId = businessId,
            PointsDelta = 50
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.NewPointsBalance.Should().Be(150);
        result.Value.NewLifetimePoints.Should().Be(150);
    }

    [Fact]
    public async Task AdjustLoyaltyPoints_Should_DecreaseBalance_WhenDeltaIsNegative()
    {
        await using var db = LoyaltyTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        db.Set<LoyaltyAccount>().Add(new LoyaltyAccount
        {
            Id = accountId,
            BusinessId = businessId,
            UserId = Guid.NewGuid(),
            Status = LoyaltyAccountStatus.Active,
            PointsBalance = 200,
            LifetimePoints = 200
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateAdjustHandler(db);

        var result = await handler.HandleAsync(new AdjustLoyaltyPointsDto
        {
            LoyaltyAccountId = accountId,
            BusinessId = businessId,
            PointsDelta = -75,
            Reason = "Correction"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.NewPointsBalance.Should().Be(125);
        result.Value.NewLifetimePoints.Should().Be(125);
    }

    [Fact]
    public async Task AdjustLoyaltyPoints_Should_RecordTransaction_WhenSuccessful()
    {
        await using var db = LoyaltyTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        db.Set<LoyaltyAccount>().Add(new LoyaltyAccount
        {
            Id = accountId,
            BusinessId = businessId,
            UserId = Guid.NewGuid(),
            Status = LoyaltyAccountStatus.Active,
            PointsBalance = 50,
            LifetimePoints = 50
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateAdjustHandler(db);

        var result = await handler.HandleAsync(new AdjustLoyaltyPointsDto
        {
            LoyaltyAccountId = accountId,
            BusinessId = businessId,
            PointsDelta = 25
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();

        var transaction = await db.Set<LoyaltyPointsTransaction>()
            .SingleAsync(TestContext.Current.CancellationToken);
        transaction.LoyaltyAccountId.Should().Be(accountId);
        transaction.BusinessId.Should().Be(businessId);
        transaction.PointsDelta.Should().Be(25);
        transaction.Type.Should().Be(LoyaltyPointsTransactionType.Adjustment);
        transaction.Id.Should().Be(result.Value!.TransactionId);
    }

    [Fact]
    public async Task AdjustLoyaltyPoints_Should_UpdateLastAccrualAt_WhenDeltaIsPositive()
    {
        await using var db = LoyaltyTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        db.Set<LoyaltyAccount>().Add(new LoyaltyAccount
        {
            Id = accountId,
            BusinessId = businessId,
            UserId = Guid.NewGuid(),
            Status = LoyaltyAccountStatus.Active,
            PointsBalance = 10,
            LifetimePoints = 10
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateAdjustHandler(db);

        await handler.HandleAsync(new AdjustLoyaltyPointsDto
        {
            LoyaltyAccountId = accountId,
            BusinessId = businessId,
            PointsDelta = 10
        }, TestContext.Current.CancellationToken);

        var account = await db.Set<LoyaltyAccount>().SingleAsync(TestContext.Current.CancellationToken);
        account.LastAccrualAtUtc.Should().NotBeNull("positive adjustments should update the accrual timestamp");
    }

    [Fact]
    public async Task AdjustLoyaltyPoints_Should_Fail_WhenAccountNotFound()
    {
        await using var db = LoyaltyTestDbContext.Create();
        var handler = CreateAdjustHandler(db);

        var result = await handler.HandleAsync(new AdjustLoyaltyPointsDto
        {
            LoyaltyAccountId = Guid.NewGuid(),
            BusinessId = Guid.NewGuid(),
            PointsDelta = 10
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("LoyaltyAccountNotFound");
    }

    [Fact]
    public async Task AdjustLoyaltyPoints_Should_Fail_WhenBusinessMismatch()
    {
        await using var db = LoyaltyTestDbContext.Create();
        var accountId = Guid.NewGuid();

        db.Set<LoyaltyAccount>().Add(new LoyaltyAccount
        {
            Id = accountId,
            BusinessId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Status = LoyaltyAccountStatus.Active
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateAdjustHandler(db);

        var result = await handler.HandleAsync(new AdjustLoyaltyPointsDto
        {
            LoyaltyAccountId = accountId,
            BusinessId = Guid.NewGuid(), // different business
            PointsDelta = 10
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("BusinessMismatchForLoyaltyAccount");
    }

    [Fact]
    public async Task AdjustLoyaltyPoints_Should_Fail_WhenAccountIsNotActive()
    {
        await using var db = LoyaltyTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        db.Set<LoyaltyAccount>().Add(new LoyaltyAccount
        {
            Id = accountId,
            BusinessId = businessId,
            UserId = Guid.NewGuid(),
            Status = LoyaltyAccountStatus.Suspended
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateAdjustHandler(db);

        var result = await handler.HandleAsync(new AdjustLoyaltyPointsDto
        {
            LoyaltyAccountId = accountId,
            BusinessId = businessId,
            PointsDelta = 10
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("LoyaltyAccountMustBeActiveForAdjustment");
    }

    [Fact]
    public async Task AdjustLoyaltyPoints_Should_Fail_WhenAdjustmentWouldCauseNegativeBalance()
    {
        await using var db = LoyaltyTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        db.Set<LoyaltyAccount>().Add(new LoyaltyAccount
        {
            Id = accountId,
            BusinessId = businessId,
            UserId = Guid.NewGuid(),
            Status = LoyaltyAccountStatus.Active,
            PointsBalance = 30,
            LifetimePoints = 30
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateAdjustHandler(db);

        var result = await handler.HandleAsync(new AdjustLoyaltyPointsDto
        {
            LoyaltyAccountId = accountId,
            BusinessId = businessId,
            PointsDelta = -50, // would result in -20
            Reason = "Test"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("LoyaltyAdjustmentWouldResultInNegativeBalance");
    }

    [Fact]
    public async Task AdjustLoyaltyPoints_Should_Fail_WhenRowVersionMismatches()
    {
        await using var db = LoyaltyTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        db.Set<LoyaltyAccount>().Add(new LoyaltyAccount
        {
            Id = accountId,
            BusinessId = businessId,
            UserId = Guid.NewGuid(),
            Status = LoyaltyAccountStatus.Active
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateAdjustHandler(db);

        var result = await handler.HandleAsync(new AdjustLoyaltyPointsDto
        {
            LoyaltyAccountId = accountId,
            BusinessId = businessId,
            PointsDelta = 10,
            RowVersion = new byte[] { 99, 88, 77 } // stale row version
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("ConcurrencyConflictLoyaltyAccountModified");
    }

    // ─── ActivateLoyaltyAccountHandler ───────────────────────────────────────

    [Fact]
    public async Task ActivateLoyaltyAccount_Should_SetStatusToActive_WhenSuspended()
    {
        await using var db = LoyaltyTestDbContext.Create();
        var accountId = Guid.NewGuid();

        db.Set<LoyaltyAccount>().Add(new LoyaltyAccount
        {
            Id = accountId,
            BusinessId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Status = LoyaltyAccountStatus.Suspended
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ActivateLoyaltyAccountHandler(db, new TestStringLocalizer());

        var result = await handler.HandleAsync(
            new ActivateLoyaltyAccountDto { Id = accountId },
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();

        var account = await db.Set<LoyaltyAccount>().SingleAsync(TestContext.Current.CancellationToken);
        account.Status.Should().Be(LoyaltyAccountStatus.Active);
    }

    [Fact]
    public async Task ActivateLoyaltyAccount_Should_BeIdempotent_WhenAlreadyActive()
    {
        await using var db = LoyaltyTestDbContext.Create();
        var accountId = Guid.NewGuid();

        db.Set<LoyaltyAccount>().Add(new LoyaltyAccount
        {
            Id = accountId,
            BusinessId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Status = LoyaltyAccountStatus.Active
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ActivateLoyaltyAccountHandler(db, new TestStringLocalizer());

        var result = await handler.HandleAsync(
            new ActivateLoyaltyAccountDto { Id = accountId },
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue("activating an already-active account should succeed idempotently");
    }

    [Fact]
    public async Task ActivateLoyaltyAccount_Should_Fail_WhenAccountNotFound()
    {
        await using var db = LoyaltyTestDbContext.Create();
        var handler = new ActivateLoyaltyAccountHandler(db, new TestStringLocalizer());

        var result = await handler.HandleAsync(
            new ActivateLoyaltyAccountDto { Id = Guid.NewGuid() },
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("LoyaltyAccountNotFound");
    }

    [Fact]
    public async Task ActivateLoyaltyAccount_Should_Fail_WhenRowVersionMismatches()
    {
        await using var db = LoyaltyTestDbContext.Create();
        var accountId = Guid.NewGuid();

        db.Set<LoyaltyAccount>().Add(new LoyaltyAccount
        {
            Id = accountId,
            BusinessId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Status = LoyaltyAccountStatus.Suspended
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ActivateLoyaltyAccountHandler(db, new TestStringLocalizer());

        var result = await handler.HandleAsync(
            new ActivateLoyaltyAccountDto { Id = accountId, RowVersion = new byte[] { 0xFF } },
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("LoyaltyAccountConcurrencyConflict");
    }

    // ─── SuspendLoyaltyAccountHandler ────────────────────────────────────────

    [Fact]
    public async Task SuspendLoyaltyAccount_Should_SetStatusToSuspended_WhenActive()
    {
        await using var db = LoyaltyTestDbContext.Create();
        var accountId = Guid.NewGuid();

        db.Set<LoyaltyAccount>().Add(new LoyaltyAccount
        {
            Id = accountId,
            BusinessId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Status = LoyaltyAccountStatus.Active
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SuspendLoyaltyAccountHandler(db, new TestStringLocalizer());

        var result = await handler.HandleAsync(
            new SuspendLoyaltyAccountDto { Id = accountId },
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();

        var account = await db.Set<LoyaltyAccount>().SingleAsync(TestContext.Current.CancellationToken);
        account.Status.Should().Be(LoyaltyAccountStatus.Suspended);
    }

    [Fact]
    public async Task SuspendLoyaltyAccount_Should_BeIdempotent_WhenAlreadySuspended()
    {
        await using var db = LoyaltyTestDbContext.Create();
        var accountId = Guid.NewGuid();

        db.Set<LoyaltyAccount>().Add(new LoyaltyAccount
        {
            Id = accountId,
            BusinessId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Status = LoyaltyAccountStatus.Suspended
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SuspendLoyaltyAccountHandler(db, new TestStringLocalizer());

        var result = await handler.HandleAsync(
            new SuspendLoyaltyAccountDto { Id = accountId },
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue("suspending an already-suspended account should succeed idempotently");
    }

    [Fact]
    public async Task SuspendLoyaltyAccount_Should_Fail_WhenAccountNotFound()
    {
        await using var db = LoyaltyTestDbContext.Create();
        var handler = new SuspendLoyaltyAccountHandler(db, new TestStringLocalizer());

        var result = await handler.HandleAsync(
            new SuspendLoyaltyAccountDto { Id = Guid.NewGuid() },
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("LoyaltyAccountNotFound");
    }

    [Fact]
    public async Task SuspendLoyaltyAccount_Should_Fail_WhenRowVersionMismatches()
    {
        await using var db = LoyaltyTestDbContext.Create();
        var accountId = Guid.NewGuid();

        db.Set<LoyaltyAccount>().Add(new LoyaltyAccount
        {
            Id = accountId,
            BusinessId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Status = LoyaltyAccountStatus.Active
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SuspendLoyaltyAccountHandler(db, new TestStringLocalizer());

        var result = await handler.HandleAsync(
            new SuspendLoyaltyAccountDto { Id = accountId, RowVersion = new byte[] { 0xAB } },
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("LoyaltyAccountConcurrencyConflict");
    }

    // ─── CreateLoyaltyProgramHandler ─────────────────────────────────────────

    [Fact]
    public async Task CreateLoyaltyProgram_Should_PersistProgram_WhenValid()
    {
        await using var db = LoyaltyTestDbContext.Create();
        var businessId = Guid.NewGuid();

        var handler = new CreateLoyaltyProgramHandler(
            db,
            new LoyaltyProgramCreateValidator(),
            new TestStringLocalizer());

        var id = await handler.HandleAsync(new LoyaltyProgramCreateDto
        {
            BusinessId = businessId,
            Name = "VIP Rewards",
            AccrualMode = LoyaltyAccrualMode.PerVisit,
            IsActive = true
        }, TestContext.Current.CancellationToken);

        id.Should().NotBeEmpty();

        var program = await db.Set<LoyaltyProgram>().SingleAsync(TestContext.Current.CancellationToken);
        program.BusinessId.Should().Be(businessId);
        program.Name.Should().Be("VIP Rewards");
        program.AccrualMode.Should().Be(LoyaltyAccrualMode.PerVisit);
        program.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateLoyaltyProgram_Should_TrimName_WhenCreated()
    {
        await using var db = LoyaltyTestDbContext.Create();
        var businessId = Guid.NewGuid();

        var handler = new CreateLoyaltyProgramHandler(
            db,
            new LoyaltyProgramCreateValidator(),
            new TestStringLocalizer());

        await handler.HandleAsync(new LoyaltyProgramCreateDto
        {
            BusinessId = businessId,
            Name = "  Gold Program  ",
            AccrualMode = LoyaltyAccrualMode.PerVisit
        }, TestContext.Current.CancellationToken);

        var program = await db.Set<LoyaltyProgram>().SingleAsync(TestContext.Current.CancellationToken);
        program.Name.Should().Be("Gold Program", "program name should be trimmed on creation");
    }

    [Fact]
    public async Task CreateLoyaltyProgram_Should_Throw_WhenProgramAlreadyExistsForBusiness()
    {
        await using var db = LoyaltyTestDbContext.Create();
        var businessId = Guid.NewGuid();

        db.Set<LoyaltyProgram>().Add(new LoyaltyProgram
        {
            BusinessId = businessId,
            Name = "Existing Program"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateLoyaltyProgramHandler(
            db,
            new LoyaltyProgramCreateValidator(),
            new TestStringLocalizer());

        var act = () => handler.HandleAsync(new LoyaltyProgramCreateDto
        {
            BusinessId = businessId,
            Name = "Another Program",
            AccrualMode = LoyaltyAccrualMode.PerVisit
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>(
            "one program per business is enforced as an MVP constraint");
    }

    // ─── UpdateLoyaltyProgramHandler ─────────────────────────────────────────

    [Fact]
    public async Task UpdateLoyaltyProgram_Should_PersistChanges_WhenProgramExists()
    {
        await using var db = LoyaltyTestDbContext.Create();
        var programId = Guid.NewGuid();

        var entity = new LoyaltyProgram
        {
            Id = programId,
            BusinessId = Guid.NewGuid(),
            Name = "Old Name",
            AccrualMode = LoyaltyAccrualMode.PerVisit,
            IsActive = true
        };
        db.Set<LoyaltyProgram>().Add(entity);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateLoyaltyProgramHandler(
            db,
            new LoyaltyProgramEditValidator(),
            new TestStringLocalizer());

        await handler.HandleAsync(new LoyaltyProgramEditDto
        {
            Id = programId,
            BusinessId = entity.BusinessId,
            Name = "New Name",
            AccrualMode = LoyaltyAccrualMode.PerCurrencyUnit,
            PointsPerCurrencyUnit = 2.5m,
            IsActive = false,
            RowVersion = entity.RowVersion
        }, TestContext.Current.CancellationToken);

        var updated = await db.Set<LoyaltyProgram>().SingleAsync(TestContext.Current.CancellationToken);
        updated.Name.Should().Be("New Name");
        updated.AccrualMode.Should().Be(LoyaltyAccrualMode.PerCurrencyUnit);
        updated.PointsPerCurrencyUnit.Should().Be(2.5m);
        updated.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateLoyaltyProgram_Should_Throw_WhenProgramNotFound()
    {
        await using var db = LoyaltyTestDbContext.Create();
        var handler = new UpdateLoyaltyProgramHandler(
            db,
            new LoyaltyProgramEditValidator(),
            new TestStringLocalizer());

        var act = () => handler.HandleAsync(new LoyaltyProgramEditDto
        {
            Id = Guid.NewGuid(),
            BusinessId = Guid.NewGuid(),
            Name = "Does Not Matter",
            RowVersion = Array.Empty<byte>()
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task UpdateLoyaltyProgram_Should_Throw_WhenProgramIsSoftDeleted()
    {
        await using var db = LoyaltyTestDbContext.Create();
        var programId = Guid.NewGuid();

        var entity = new LoyaltyProgram
        {
            Id = programId,
            BusinessId = Guid.NewGuid(),
            Name = "Deleted Program",
            IsDeleted = true
        };
        db.Set<LoyaltyProgram>().Add(entity);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateLoyaltyProgramHandler(
            db,
            new LoyaltyProgramEditValidator(),
            new TestStringLocalizer());

        var act = () => handler.HandleAsync(new LoyaltyProgramEditDto
        {
            Id = programId,
            BusinessId = entity.BusinessId,
            Name = "New Name",
            RowVersion = entity.RowVersion
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("deleted programs cannot be updated");
    }

    [Fact]
    public async Task UpdateLoyaltyProgram_Should_Throw_WhenRowVersionMismatches()
    {
        await using var db = LoyaltyTestDbContext.Create();
        var programId = Guid.NewGuid();

        var entity = new LoyaltyProgram
        {
            Id = programId,
            BusinessId = Guid.NewGuid(),
            Name = "Program"
        };
        db.Set<LoyaltyProgram>().Add(entity);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateLoyaltyProgramHandler(
            db,
            new LoyaltyProgramEditValidator(),
            new TestStringLocalizer());

        var act = () => handler.HandleAsync(new LoyaltyProgramEditDto
        {
            Id = programId,
            BusinessId = entity.BusinessId,
            Name = "New Name",
            RowVersion = new byte[] { 0xDE, 0xAD } // stale row version
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("concurrency conflict must block the update");
    }

    // ─── CreateLoyaltyRewardTierHandler ──────────────────────────────────────

    [Fact]
    public async Task CreateLoyaltyRewardTier_Should_PersistTier_WhenProgramExists()
    {
        await using var db = LoyaltyTestDbContext.Create();
        var programId = Guid.NewGuid();

        db.Set<LoyaltyProgram>().Add(new LoyaltyProgram
        {
            Id = programId,
            BusinessId = Guid.NewGuid(),
            Name = "Rewards"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateLoyaltyRewardTierHandler(
            db,
            new LoyaltyRewardTierCreateValidator(),
            new TestStringLocalizer());

        var tierId = await handler.HandleAsync(new LoyaltyRewardTierCreateDto
        {
            LoyaltyProgramId = programId,
            PointsRequired = 500,
            RewardType = LoyaltyRewardType.PercentDiscount,
            RewardValue = 10m,
            Description = "10% off any purchase",
            AllowSelfRedemption = true
        }, TestContext.Current.CancellationToken);

        tierId.Should().NotBeEmpty();

        var tier = await db.Set<LoyaltyRewardTier>().SingleAsync(TestContext.Current.CancellationToken);
        tier.LoyaltyProgramId.Should().Be(programId);
        tier.PointsRequired.Should().Be(500);
        tier.RewardType.Should().Be(LoyaltyRewardType.PercentDiscount);
        tier.AllowSelfRedemption.Should().BeTrue();
    }

    [Fact]
    public async Task CreateLoyaltyRewardTier_Should_Throw_WhenProgramNotFound()
    {
        await using var db = LoyaltyTestDbContext.Create();
        var handler = new CreateLoyaltyRewardTierHandler(
            db,
            new LoyaltyRewardTierCreateValidator(),
            new TestStringLocalizer());

        var act = () => handler.HandleAsync(new LoyaltyRewardTierCreateDto
        {
            LoyaltyProgramId = Guid.NewGuid(),
            PointsRequired = 100,
            RewardType = LoyaltyRewardType.FreeItem
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("reward tier requires an existing program");
    }

    [Fact]
    public async Task CreateLoyaltyRewardTier_Should_Throw_WhenProgramIsSoftDeleted()
    {
        await using var db = LoyaltyTestDbContext.Create();
        var programId = Guid.NewGuid();

        db.Set<LoyaltyProgram>().Add(new LoyaltyProgram
        {
            Id = programId,
            BusinessId = Guid.NewGuid(),
            Name = "Deleted Program",
            IsDeleted = true
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateLoyaltyRewardTierHandler(
            db,
            new LoyaltyRewardTierCreateValidator(),
            new TestStringLocalizer());

        var act = () => handler.HandleAsync(new LoyaltyRewardTierCreateDto
        {
            LoyaltyProgramId = programId,
            PointsRequired = 100,
            RewardType = LoyaltyRewardType.FreeItem
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("tiers cannot be added to deleted programs");
    }

    // ─── SoftDeleteLoyaltyProgramHandler ─────────────────────────────────────

    [Fact]
    public async Task SoftDeleteLoyaltyProgram_Should_MarkAsDeleted_WhenProgramExists()
    {
        await using var db = LoyaltyTestDbContext.Create();
        var programId = Guid.NewGuid();

        var entity = new LoyaltyProgram
        {
            Id = programId,
            BusinessId = Guid.NewGuid(),
            Name = "Program To Delete"
        };
        db.Set<LoyaltyProgram>().Add(entity);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SoftDeleteLoyaltyProgramHandler(db, new TestStringLocalizer());

        var result = await handler.HandleAsync(new LoyaltyProgramDeleteDto
        {
            Id = programId,
            RowVersion = entity.RowVersion
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();

        var deleted = await db.Set<LoyaltyProgram>().SingleAsync(TestContext.Current.CancellationToken);
        deleted.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task SoftDeleteLoyaltyProgram_Should_BeIdempotent_WhenAlreadyDeleted()
    {
        await using var db = LoyaltyTestDbContext.Create();
        var programId = Guid.NewGuid();

        db.Set<LoyaltyProgram>().Add(new LoyaltyProgram
        {
            Id = programId,
            BusinessId = Guid.NewGuid(),
            Name = "Already Deleted",
            IsDeleted = true
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SoftDeleteLoyaltyProgramHandler(db, new TestStringLocalizer());

        var result = await handler.HandleAsync(new LoyaltyProgramDeleteDto
        {
            Id = programId
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue("deleting an already-deleted program should succeed idempotently");
    }

    [Fact]
    public async Task SoftDeleteLoyaltyProgram_Should_Fail_WhenProgramNotFound()
    {
        await using var db = LoyaltyTestDbContext.Create();
        var handler = new SoftDeleteLoyaltyProgramHandler(db, new TestStringLocalizer());

        var result = await handler.HandleAsync(new LoyaltyProgramDeleteDto
        {
            Id = Guid.NewGuid()
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("LoyaltyProgramNotFound");
    }

    [Fact]
    public async Task SoftDeleteLoyaltyProgram_Should_Fail_WhenRowVersionMismatches()
    {
        await using var db = LoyaltyTestDbContext.Create();
        var programId = Guid.NewGuid();

        var entity = new LoyaltyProgram
        {
            Id = programId,
            BusinessId = Guid.NewGuid(),
            Name = "Concurrency Test"
        };
        db.Set<LoyaltyProgram>().Add(entity);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SoftDeleteLoyaltyProgramHandler(db, new TestStringLocalizer());

        var result = await handler.HandleAsync(new LoyaltyProgramDeleteDto
        {
            Id = programId,
            RowVersion = new byte[] { 0xBA, 0xAD } // stale row version
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("LoyaltyProgramConcurrencyConflict");
    }

    // ─── Shared infrastructure ────────────────────────────────────────────────

    private static AdjustLoyaltyPointsHandler CreateAdjustHandler(LoyaltyTestDbContext db)
        => new(db, new StubClock(), new AdjustLoyaltyPointsValidator(new TestStringLocalizer()), new TestStringLocalizer());

    private sealed class StubClock : IClock
    {
        public DateTime UtcNow => new(2030, 6, 1, 12, 0, 0, DateTimeKind.Utc);
    }

    private sealed class TestStringLocalizer : IStringLocalizer<ValidationResource>
    {
        public LocalizedString this[string name] => new(name, name, resourceNotFound: false);

        public LocalizedString this[string name, params object[] arguments]
            => new(name, string.Format(name, arguments), resourceNotFound: false);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => Array.Empty<LocalizedString>();

        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
    }

    private sealed class LoyaltyTestDbContext : DbContext, IAppDbContext
    {
        private LoyaltyTestDbContext(DbContextOptions<LoyaltyTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static LoyaltyTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<LoyaltyTestDbContext>()
                .UseInMemoryDatabase($"darwin_loyalty_handler_tests_{Guid.NewGuid()}")
                .Options;
            return new LoyaltyTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Ignore<GeoCoordinate>();

            modelBuilder.Entity<LoyaltyAccount>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.RowVersion).IsRequired();
                builder.Ignore(x => x.Transactions);
                builder.Ignore(x => x.Redemptions);
            });

            modelBuilder.Entity<LoyaltyPointsTransaction>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<LoyaltyProgram>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Name).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
                builder.Ignore(x => x.RewardTiers);
            });

            modelBuilder.Entity<LoyaltyRewardTier>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.RowVersion).IsRequired();
            });
        }
    }
}
