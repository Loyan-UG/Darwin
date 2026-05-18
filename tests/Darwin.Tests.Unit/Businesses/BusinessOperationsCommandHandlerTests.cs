using System;
using System.Threading.Tasks;
using Darwin.Application;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Businesses.Commands;
using Darwin.Application.Businesses.DTOs;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Integration;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.Businesses;

/// <summary>
/// Unit tests for business operations command handlers:
/// <see cref="ProvisionBusinessOnboardingHandler"/>,
/// <see cref="UpdateProviderCallbackInboxMessageHandler"/>, and
/// <see cref="CancelCommunicationDispatchOperationHandler"/>.
/// </summary>
public sealed class BusinessOperationsCommandHandlerTests
{
    // ─── Shared infrastructure ────────────────────────────────────────────────

    private static TestStringLocalizer CreateLocalizer() => new();

    private static IClock CreateClock() =>
        new FixedClock(new DateTime(2030, 6, 1, 12, 0, 0, DateTimeKind.Utc));

    // ─────────────────────────────────────────────────────────────────────────
    // ProvisionBusinessOnboardingHandler
    // ─────────────────────────────────────────────────────────────────────────

    private static ProvisionBusinessOnboardingHandler CreateProvisionHandler(BusinessOpsTestDbContext db) =>
        new(db, CreateClock(), CreateLocalizer());

    [Fact]
    public async Task ProvisionOnboarding_Should_Fail_WhenIdIsEmpty()
    {
        await using var db = BusinessOpsTestDbContext.Create();
        var handler = CreateProvisionHandler(db);

        var result = await handler.HandleAsync(new BusinessLifecycleActionDto
        {
            Id = Guid.Empty,
            RowVersion = [1]
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("empty Id must be rejected");
        result.Error.Should().Be("InvalidDeleteRequest");
    }

    [Fact]
    public async Task ProvisionOnboarding_Should_Fail_WhenRowVersionIsEmpty()
    {
        await using var db = BusinessOpsTestDbContext.Create();
        var handler = CreateProvisionHandler(db);

        var result = await handler.HandleAsync(new BusinessLifecycleActionDto
        {
            Id = Guid.NewGuid(),
            RowVersion = Array.Empty<byte>()
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("empty RowVersion must be rejected");
        result.Error.Should().Be("RowVersionRequired");
    }

    [Fact]
    public async Task ProvisionOnboarding_Should_Fail_WhenRowVersionIsNull()
    {
        await using var db = BusinessOpsTestDbContext.Create();
        var handler = CreateProvisionHandler(db);

        var result = await handler.HandleAsync(new BusinessLifecycleActionDto
        {
            Id = Guid.NewGuid(),
            RowVersion = null
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("null RowVersion is treated as empty");
        result.Error.Should().Be("RowVersionRequired");
    }

    [Fact]
    public async Task ProvisionOnboarding_Should_Fail_WhenBusinessNotFound()
    {
        await using var db = BusinessOpsTestDbContext.Create();
        var handler = CreateProvisionHandler(db);

        var result = await handler.HandleAsync(new BusinessLifecycleActionDto
        {
            Id = Guid.NewGuid(),
            RowVersion = [1, 2, 3]
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("non-existent business must fail");
        result.Error.Should().Be("BusinessNotFound");
    }

    [Fact]
    public async Task ProvisionOnboarding_Should_Fail_WhenRowVersionIsStale()
    {
        await using var db = BusinessOpsTestDbContext.Create();
        var business = CreateReadyBusiness(rowVersion: [1, 2, 3]);
        db.Set<Business>().Add(business);
        SeedProvisionPrerequisites(db, business.Id);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateProvisionHandler(db);

        var result = await handler.HandleAsync(new BusinessLifecycleActionDto
        {
            Id = business.Id,
            RowVersion = [9, 9, 9] // stale
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("a stale RowVersion must be rejected");
        result.Error.Should().Be("ConcurrencyConflictDetected");
    }

    [Fact]
    public async Task ProvisionOnboarding_Should_Fail_WhenMissingOwnerPrerequisite()
    {
        await using var db = BusinessOpsTestDbContext.Create();
        var business = CreateReadyBusiness();
        db.Set<Business>().Add(business);
        // No BusinessMember with Owner role seeded
        db.Set<BusinessLocation>().Add(new BusinessLocation
        {
            BusinessId = business.Id,
            Name = "Main Location",
            IsPrimary = true,
            RowVersion = [1]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateProvisionHandler(db);

        var result = await handler.HandleAsync(new BusinessLifecycleActionDto
        {
            Id = business.Id,
            RowVersion = business.RowVersion
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("missing owner must block provisioning");
        result.Error.Should().Contain("BusinessOnboardingProvisioningBlocked");
    }

    [Fact]
    public async Task ProvisionOnboarding_Should_Fail_WhenMissingPrimaryLocationPrerequisite()
    {
        await using var db = BusinessOpsTestDbContext.Create();
        var business = CreateReadyBusiness();
        db.Set<Business>().Add(business);
        // Seed owner but no primary location
        var userId = Guid.NewGuid();
        db.Set<User>().Add(new User("owner@test.com", "hashed-pw", "security-stamp")
        {
            Id = userId,
            FirstName = "Owner",
            LastName = "User"
        });
        db.Set<BusinessMember>().Add(new BusinessMember
        {
            BusinessId = business.Id,
            UserId = userId,
            Role = BusinessMemberRole.Owner,
            IsActive = true,
            RowVersion = [1]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateProvisionHandler(db);

        var result = await handler.HandleAsync(new BusinessLifecycleActionDto
        {
            Id = business.Id,
            RowVersion = business.RowVersion
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("missing primary location must block provisioning");
        result.Error.Should().Contain("BusinessOnboardingProvisioningBlocked");
    }

    [Fact]
    public async Task ProvisionOnboarding_Should_Succeed_WhenAllPrerequisitesMet()
    {
        await using var db = BusinessOpsTestDbContext.Create();
        var business = CreateReadyBusiness();
        business.OperationalStatus = BusinessOperationalStatus.PendingApproval;
        business.IsActive = false;
        db.Set<Business>().Add(business);
        SeedProvisionPrerequisites(db, business.Id);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateProvisionHandler(db);

        var result = await handler.HandleAsync(new BusinessLifecycleActionDto
        {
            Id = business.Id,
            RowVersion = business.RowVersion
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue("all prerequisites are met");
        result.Value.Should().NotBeNull();
        result.Value!.BusinessId.Should().Be(business.Id);
        result.Value.WasApproved.Should().BeTrue("business was not Approved before provisioning");
        result.Value.WasActivated.Should().BeTrue("business was not active before provisioning");

        var persisted = await db.Set<Business>().AsNoTracking()
            .SingleAsync(x => x.Id == business.Id, TestContext.Current.CancellationToken);
        persisted.OperationalStatus.Should().Be(BusinessOperationalStatus.Approved);
        persisted.IsActive.Should().BeTrue();
        persisted.ApprovedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task ProvisionOnboarding_Should_CreateCustomerProfile_WhenNoneExists()
    {
        await using var db = BusinessOpsTestDbContext.Create();
        var business = CreateReadyBusiness();
        db.Set<Business>().Add(business);
        SeedProvisionPrerequisites(db, business.Id);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateProvisionHandler(db);

        var result = await handler.HandleAsync(new BusinessLifecycleActionDto
        {
            Id = business.Id,
            RowVersion = business.RowVersion
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.CustomerCreated.Should().BeTrue("no existing customer profile existed");
        result.Value.CustomerId.Should().NotBeEmpty();

        var customerCount = await db.Set<Customer>().CountAsync(TestContext.Current.CancellationToken);
        customerCount.Should().Be(1, "exactly one customer should have been created");
    }

    [Fact]
    public async Task ProvisionOnboarding_Should_ReturnWasApprovedFalse_WhenAlreadyApproved()
    {
        await using var db = BusinessOpsTestDbContext.Create();
        var business = CreateReadyBusiness();
        business.OperationalStatus = BusinessOperationalStatus.Approved;
        business.IsActive = true;
        db.Set<Business>().Add(business);
        SeedProvisionPrerequisites(db, business.Id);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateProvisionHandler(db);

        var result = await handler.HandleAsync(new BusinessLifecycleActionDto
        {
            Id = business.Id,
            RowVersion = business.RowVersion
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.WasApproved.Should().BeFalse("business was already Approved");
        result.Value.WasActivated.Should().BeFalse("business was already active");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UpdateProviderCallbackInboxMessageHandler
    // ─────────────────────────────────────────────────────────────────────────

    private static UpdateProviderCallbackInboxMessageHandler CreateInboxHandler(BusinessOpsTestDbContext db) =>
        new(db, CreateClock(), CreateLocalizer());

    [Fact]
    public async Task UpdateInboxMessage_Should_Fail_WhenIdIsEmpty()
    {
        await using var db = BusinessOpsTestDbContext.Create();
        var handler = CreateInboxHandler(db);

        var result = await handler.HandleAsync(new UpdateProviderCallbackInboxMessageDto
        {
            Id = Guid.Empty,
            RowVersion = [1],
            Action = "MARKPROCESSED"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("empty Id must be rejected");
        result.Error.Should().Be("InvalidDeleteRequest");
    }

    [Fact]
    public async Task UpdateInboxMessage_Should_Fail_WhenRowVersionIsEmpty()
    {
        await using var db = BusinessOpsTestDbContext.Create();
        var handler = CreateInboxHandler(db);

        var result = await handler.HandleAsync(new UpdateProviderCallbackInboxMessageDto
        {
            Id = Guid.NewGuid(),
            RowVersion = Array.Empty<byte>(),
            Action = "MARKPROCESSED"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("empty RowVersion must be rejected");
        result.Error.Should().Be("RowVersionRequired");
    }

    [Fact]
    public async Task UpdateInboxMessage_Should_Fail_WhenMessageNotFound()
    {
        await using var db = BusinessOpsTestDbContext.Create();
        var handler = CreateInboxHandler(db);

        var result = await handler.HandleAsync(new UpdateProviderCallbackInboxMessageDto
        {
            Id = Guid.NewGuid(),
            RowVersion = [1],
            Action = "MARKPROCESSED"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("non-existent message must fail with not-found");
        result.Error.Should().Be("ProviderCallbackInboxMessageNotFound");
    }

    [Fact]
    public async Task UpdateInboxMessage_Should_Fail_WhenRowVersionIsStale()
    {
        await using var db = BusinessOpsTestDbContext.Create();
        var message = CreateInboxMessage(rowVersion: [1, 2, 3]);
        db.Set<ProviderCallbackInboxMessage>().Add(message);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateInboxHandler(db);

        var result = await handler.HandleAsync(new UpdateProviderCallbackInboxMessageDto
        {
            Id = message.Id,
            RowVersion = [9, 9, 9], // stale
            Action = "MARKPROCESSED"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("stale RowVersion must be rejected");
        result.Error.Should().Be("ItemConcurrencyConflict");
    }

    [Fact]
    public async Task UpdateInboxMessage_Should_Fail_WhenActionIsUnsupported()
    {
        await using var db = BusinessOpsTestDbContext.Create();
        var message = CreateInboxMessage();
        db.Set<ProviderCallbackInboxMessage>().Add(message);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateInboxHandler(db);

        var result = await handler.HandleAsync(new UpdateProviderCallbackInboxMessageDto
        {
            Id = message.Id,
            RowVersion = message.RowVersion,
            Action = "UNKNOWN_ACTION"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("unsupported action must be rejected");
        result.Error.Should().Be("ProviderCallbackInboxUnsupportedAction");
    }

    [Fact]
    public async Task UpdateInboxMessage_Should_ApplyMarkProcessed_AndSetProcessedAtUtc()
    {
        await using var db = BusinessOpsTestDbContext.Create();
        var message = CreateInboxMessage(status: "Pending");
        db.Set<ProviderCallbackInboxMessage>().Add(message);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateInboxHandler(db);

        var result = await handler.HandleAsync(new UpdateProviderCallbackInboxMessageDto
        {
            Id = message.Id,
            RowVersion = message.RowVersion,
            Action = "MARKPROCESSED"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue("MARKPROCESSED is a valid action");

        var updated = await db.Set<ProviderCallbackInboxMessage>().AsNoTracking()
            .SingleAsync(x => x.Id == message.Id, TestContext.Current.CancellationToken);
        updated.Status.Should().Be("Processed");
        updated.ProcessedAtUtc.Should().NotBeNull();
        updated.FailureReason.Should().BeNull();
    }

    [Fact]
    public async Task UpdateInboxMessage_Should_ApplyMarkFailed_AndSetFailureReason()
    {
        await using var db = BusinessOpsTestDbContext.Create();
        var message = CreateInboxMessage(status: "Pending");
        db.Set<ProviderCallbackInboxMessage>().Add(message);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateInboxHandler(db);

        var result = await handler.HandleAsync(new UpdateProviderCallbackInboxMessageDto
        {
            Id = message.Id,
            RowVersion = message.RowVersion,
            Action = "MARKFAILED",
            FailureReason = "Manual operator failure"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue("MARKFAILED is a valid action");

        var updated = await db.Set<ProviderCallbackInboxMessage>().AsNoTracking()
            .SingleAsync(x => x.Id == message.Id, TestContext.Current.CancellationToken);
        updated.Status.Should().Be("Failed");
        updated.FailureReason.Should().Be("Manual operator failure");
        updated.ProcessedAtUtc.Should().BeNull("MARKFAILED clears ProcessedAtUtc");
    }

    [Fact]
    public async Task UpdateInboxMessage_Should_ApplyMarkFailed_WithDefaultReason_WhenNoReasonProvided()
    {
        await using var db = BusinessOpsTestDbContext.Create();
        var message = CreateInboxMessage(status: "Pending");
        db.Set<ProviderCallbackInboxMessage>().Add(message);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateInboxHandler(db);

        var result = await handler.HandleAsync(new UpdateProviderCallbackInboxMessageDto
        {
            Id = message.Id,
            RowVersion = message.RowVersion,
            Action = "MARKFAILED",
            FailureReason = null
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();

        var updated = await db.Set<ProviderCallbackInboxMessage>().AsNoTracking()
            .SingleAsync(x => x.Id == message.Id, TestContext.Current.CancellationToken);
        updated.FailureReason.Should().NotBeNullOrEmpty("a default failure reason is provided when none is given");
    }

    [Fact]
    public async Task UpdateInboxMessage_Should_ApplyRequeue_AndResetState()
    {
        await using var db = BusinessOpsTestDbContext.Create();
        var message = CreateInboxMessage(status: "Failed");
        message.AttemptCount = 3;
        message.FailureReason = "Previous failure";
        db.Set<ProviderCallbackInboxMessage>().Add(message);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateInboxHandler(db);

        var result = await handler.HandleAsync(new UpdateProviderCallbackInboxMessageDto
        {
            Id = message.Id,
            RowVersion = message.RowVersion,
            Action = "REQUEUE"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue("REQUEUE is a valid action");

        var updated = await db.Set<ProviderCallbackInboxMessage>().AsNoTracking()
            .SingleAsync(x => x.Id == message.Id, TestContext.Current.CancellationToken);
        updated.Status.Should().Be("Pending");
        updated.AttemptCount.Should().Be(0, "REQUEUE resets the attempt count");
        updated.FailureReason.Should().BeNull("REQUEUE clears the failure reason");
        updated.ProcessedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task UpdateInboxMessage_Should_ApplyAction_CaseInsensitive()
    {
        await using var db = BusinessOpsTestDbContext.Create();
        var message = CreateInboxMessage(status: "Pending");
        db.Set<ProviderCallbackInboxMessage>().Add(message);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateInboxHandler(db);

        var result = await handler.HandleAsync(new UpdateProviderCallbackInboxMessageDto
        {
            Id = message.Id,
            RowVersion = message.RowVersion,
            Action = "markProcessed" // mixed case
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue("actions should be normalized to upper-case");

        var updated = await db.Set<ProviderCallbackInboxMessage>().AsNoTracking()
            .SingleAsync(x => x.Id == message.Id, TestContext.Current.CancellationToken);
        updated.Status.Should().Be("Processed");
    }

    [Fact]
    public async Task UpdateInboxMessage_Should_Fail_WhenActionExceedsMaxLength()
    {
        await using var db = BusinessOpsTestDbContext.Create();
        var message = CreateInboxMessage();
        db.Set<ProviderCallbackInboxMessage>().Add(message);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateInboxHandler(db);
        var oversizedAction = new string('A', 65); // > 64 chars

        var result = await handler.HandleAsync(new UpdateProviderCallbackInboxMessageDto
        {
            Id = message.Id,
            RowVersion = message.RowVersion,
            Action = oversizedAction
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("an oversized action is treated as unsupported");
        result.Error.Should().Be("ProviderCallbackInboxUnsupportedAction");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CancelCommunicationDispatchOperationHandler
    // ─────────────────────────────────────────────────────────────────────────

    private static CancelCommunicationDispatchOperationHandler CreateCancelHandler(BusinessOpsTestDbContext db) =>
        new(db, CreateClock(), CreateLocalizer());

    [Fact]
    public async Task CancelDispatch_Should_Fail_WhenIdIsEmpty()
    {
        await using var db = BusinessOpsTestDbContext.Create();
        var handler = CreateCancelHandler(db);

        var result = await handler.HandleAsync(new CancelCommunicationDispatchOperationDto
        {
            Id = Guid.Empty,
            RowVersion = [1],
            Channel = "Email"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("empty Id must be rejected");
        result.Error.Should().Be("InvalidDeleteRequest");
    }

    [Fact]
    public async Task CancelDispatch_Should_Fail_WhenRowVersionIsEmpty()
    {
        await using var db = BusinessOpsTestDbContext.Create();
        var handler = CreateCancelHandler(db);

        var result = await handler.HandleAsync(new CancelCommunicationDispatchOperationDto
        {
            Id = Guid.NewGuid(),
            RowVersion = Array.Empty<byte>(),
            Channel = "Email"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("empty RowVersion must be rejected");
        result.Error.Should().Be("RowVersionRequired");
    }

    [Fact]
    public async Task CancelEmailDispatch_Should_Fail_WhenEmailOperationNotFound()
    {
        await using var db = BusinessOpsTestDbContext.Create();
        var handler = CreateCancelHandler(db);

        var result = await handler.HandleAsync(new CancelCommunicationDispatchOperationDto
        {
            Id = Guid.NewGuid(),
            RowVersion = [1],
            Channel = "Email"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("non-existent email operation must fail");
        result.Error.Should().Be("CommunicationDispatchOperationNotFound");
    }

    [Fact]
    public async Task CancelEmailDispatch_Should_Fail_WhenRowVersionIsStale()
    {
        await using var db = BusinessOpsTestDbContext.Create();
        var op = CreateEmailDispatchOp(rowVersion: [1, 2, 3]);
        db.Set<EmailDispatchOperation>().Add(op);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateCancelHandler(db);

        var result = await handler.HandleAsync(new CancelCommunicationDispatchOperationDto
        {
            Id = op.Id,
            RowVersion = [9, 9, 9], // stale
            Channel = "Email"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("stale RowVersion must be rejected");
        result.Error.Should().Be("ItemConcurrencyConflict");
    }

    [Fact]
    public async Task CancelEmailDispatch_Should_Succeed_AndMarkAsDeleted()
    {
        await using var db = BusinessOpsTestDbContext.Create();
        var op = CreateEmailDispatchOp();
        db.Set<EmailDispatchOperation>().Add(op);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateCancelHandler(db);

        var result = await handler.HandleAsync(new CancelCommunicationDispatchOperationDto
        {
            Id = op.Id,
            RowVersion = op.RowVersion,
            Channel = "Email"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue("a valid pending email dispatch can be cancelled");

        var persisted = await db.Set<EmailDispatchOperation>().AsNoTracking()
            .SingleAsync(x => x.Id == op.Id, TestContext.Current.CancellationToken);
        persisted.IsDeleted.Should().BeTrue("the operation should be soft-deleted after cancellation");
    }

    [Fact]
    public async Task CancelEmailDispatch_Should_ReturnOk_WhenAlreadyCancelled()
    {
        await using var db = BusinessOpsTestDbContext.Create();
        var op = CreateEmailDispatchOp();
        op.IsDeleted = true; // already cancelled
        db.Set<EmailDispatchOperation>().Add(op);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateCancelHandler(db);

        var result = await handler.HandleAsync(new CancelCommunicationDispatchOperationDto
        {
            Id = op.Id,
            RowVersion = op.RowVersion,
            Channel = "Email"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue("cancelling an already-cancelled operation is a no-op success");
    }

    [Fact]
    public async Task CancelEmailDispatch_Should_Fail_WhenRecentAttemptIsInFlight()
    {
        // LastAttemptAtUtc within 2-minute in-flight protection window
        var recentAttempt = new DateTime(2030, 6, 1, 11, 59, 0, DateTimeKind.Utc); // 1 min before FixedClock.UtcNow
        await using var db = BusinessOpsTestDbContext.Create();
        var op = CreateEmailDispatchOp();
        op.LastAttemptAtUtc = recentAttempt;
        db.Set<EmailDispatchOperation>().Add(op);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateCancelHandler(db);

        var result = await handler.HandleAsync(new CancelCommunicationDispatchOperationDto
        {
            Id = op.Id,
            RowVersion = op.RowVersion,
            Channel = "Email"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("a recently attempted operation is in-flight and cannot be cancelled");
        result.Error.Should().Be("CommunicationDispatchOperationInFlight");
    }

    [Fact]
    public async Task CancelChannelDispatch_Should_Fail_WhenChannelOperationNotFound()
    {
        await using var db = BusinessOpsTestDbContext.Create();
        var handler = CreateCancelHandler(db);

        var result = await handler.HandleAsync(new CancelCommunicationDispatchOperationDto
        {
            Id = Guid.NewGuid(),
            RowVersion = [1],
            Channel = "SMS"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("non-existent channel operation must fail");
        result.Error.Should().Be("CommunicationDispatchOperationNotFound");
    }

    [Fact]
    public async Task CancelChannelDispatch_Should_Succeed_AndMarkAsDeleted()
    {
        await using var db = BusinessOpsTestDbContext.Create();
        var op = CreateChannelDispatchOp(channel: "SMS");
        db.Set<ChannelDispatchOperation>().Add(op);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateCancelHandler(db);

        var result = await handler.HandleAsync(new CancelCommunicationDispatchOperationDto
        {
            Id = op.Id,
            RowVersion = op.RowVersion,
            Channel = "SMS"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue("a valid pending channel dispatch can be cancelled");

        var persisted = await db.Set<ChannelDispatchOperation>().AsNoTracking()
            .SingleAsync(x => x.Id == op.Id, TestContext.Current.CancellationToken);
        persisted.IsDeleted.Should().BeTrue("the channel operation should be soft-deleted");
    }

    [Fact]
    public async Task CancelChannelBatch_Should_ReturnZero_WhenNoMatchingOperations()
    {
        await using var db = BusinessOpsTestDbContext.Create();
        var handler = CreateCancelHandler(db);

        var result = await handler.HandleChannelBatchAsync(new CancelChannelDispatchOperationsBatchDto
        {
            Limit = 100
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().Be(0, "no operations in the database means nothing is cancelled");
    }

    [Fact]
    public async Task CancelChannelBatch_Should_CancelPendingOperations_AndRespectLimit()
    {
        await using var db = BusinessOpsTestDbContext.Create();
        var businessId = Guid.NewGuid();

        for (var i = 0; i < 5; i++)
        {
            var op = CreateChannelDispatchOp(channel: "SMS");
            op.BusinessId = businessId;
            op.Status = "Pending";
            db.Set<ChannelDispatchOperation>().Add(op);
        }
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateCancelHandler(db);

        var result = await handler.HandleChannelBatchAsync(new CancelChannelDispatchOperationsBatchDto
        {
            BusinessId = businessId,
            Limit = 3
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().Be(3, "limit of 3 should be respected even when 5 operations are pending");
    }

    [Fact]
    public async Task CancelChannelBatch_Should_Skip_AlreadyDeletedOperations()
    {
        await using var db = BusinessOpsTestDbContext.Create();
        var op1 = CreateChannelDispatchOp(channel: "WhatsApp");
        op1.Status = "Pending";
        var op2 = CreateChannelDispatchOp(channel: "WhatsApp");
        op2.Status = "Pending";
        op2.IsDeleted = true; // already cancelled, should be excluded

        db.Set<ChannelDispatchOperation>().AddRange(op1, op2);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateCancelHandler(db);

        var result = await handler.HandleChannelBatchAsync(new CancelChannelDispatchOperationsBatchDto
        {
            Limit = 100
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().Be(1, "the soft-deleted operation should be excluded from batch cancel");
    }

    [Fact]
    public async Task CancelChannelBatch_Should_Fail_WhenInvalidStatusFilterProvided()
    {
        await using var db = BusinessOpsTestDbContext.Create();
        var handler = CreateCancelHandler(db);

        var result = await handler.HandleChannelBatchAsync(new CancelChannelDispatchOperationsBatchDto
        {
            Status = "Cancelled" // not Pending or Failed
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("only Pending and Failed statuses are accepted as filter values");
        result.Error.Should().Be("InvalidDeleteRequest");
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static Business CreateReadyBusiness(byte[]? rowVersion = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Test Business",
            LegalName = "Test Business GmbH",
            ContactEmail = "contact@test-business.com",
            Category = BusinessCategoryKind.Bakery,
            DefaultCurrency = "EUR",
            DefaultCulture = "de-DE",
            IsActive = true,
            OperationalStatus = BusinessOperationalStatus.PendingApproval,
            RowVersion = rowVersion ?? [1, 2, 3]
        };

    private static void SeedProvisionPrerequisites(BusinessOpsTestDbContext db, Guid businessId)
    {
        var userId = Guid.NewGuid();
        db.Set<User>().Add(new User("owner@test-business.com", "hashed-pw", "security-stamp")
        {
            Id = userId,
            FirstName = "Owner",
            LastName = "User"
        });
        db.Set<BusinessMember>().Add(new BusinessMember
        {
            BusinessId = businessId,
            UserId = userId,
            Role = BusinessMemberRole.Owner,
            IsActive = true,
            RowVersion = [1]
        });
        db.Set<BusinessLocation>().Add(new BusinessLocation
        {
            BusinessId = businessId,
            Name = "Main Location",
            IsPrimary = true,
            RowVersion = [1]
        });
    }

    private static ProviderCallbackInboxMessage CreateInboxMessage(
        byte[]? rowVersion = null,
        string status = "Pending") =>
        new()
        {
            Id = Guid.NewGuid(),
            Provider = "Stripe",
            PayloadJson = "{}",
            Status = status,
            AttemptCount = 0,
            RowVersion = rowVersion ?? [1, 2, 3]
        };

    private static EmailDispatchOperation CreateEmailDispatchOp(byte[]? rowVersion = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            Provider = "SMTP",
            RecipientEmail = "user@example.com",
            Subject = "Test",
            HtmlBody = "<p>Test</p>",
            Status = "Pending",
            RowVersion = rowVersion ?? [1, 2, 3]
        };

    private static ChannelDispatchOperation CreateChannelDispatchOp(string channel = "SMS", byte[]? rowVersion = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            Channel = channel,
            Provider = "Twilio",
            RecipientAddress = "+49123456789",
            MessageText = "Test message",
            Status = "Pending",
            RowVersion = rowVersion ?? [1, 2, 3]
        };

    // ─── Test infrastructure ─────────────────────────────────────────────────

    private sealed class FixedClock : IClock
    {
        private readonly DateTime _utcNow;
        public FixedClock(DateTime utcNow) { _utcNow = utcNow; }
        public DateTime UtcNow => _utcNow;
    }

    private sealed class TestStringLocalizer : IStringLocalizer<ValidationResource>
    {
        public LocalizedString this[string name] => new(name, name, resourceNotFound: false);
        public LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(name, arguments), resourceNotFound: false);
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            Array.Empty<LocalizedString>();
    }

    private sealed class BusinessOpsTestDbContext : DbContext, IAppDbContext
    {
        private BusinessOpsTestDbContext(DbContextOptions<BusinessOpsTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static BusinessOpsTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<BusinessOpsTestDbContext>()
                .UseInMemoryDatabase($"darwin_business_ops_tests_{Guid.NewGuid()}")
                .Options;
            return new BusinessOpsTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Ignore<GeoCoordinate>();

            modelBuilder.Entity<Business>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Name).IsRequired();
                b.Property(x => x.DefaultCurrency).IsRequired();
                b.Property(x => x.DefaultCulture).IsRequired();
                b.Property(x => x.RowVersion).IsRequired();
                b.Ignore(x => x.Locations);
                b.Ignore(x => x.Members);
                b.Ignore(x => x.Invitations);
                b.Ignore(x => x.Subscriptions);
                b.Ignore(x => x.Favorites);
                b.Ignore(x => x.Likes);
                b.Ignore(x => x.Reviews);
                b.Ignore(x => x.StaffQrCodes);
                b.Ignore(x => x.AnalyticsExportJobs);
            });

            modelBuilder.Entity<BusinessMember>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<BusinessLocation>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Name).IsRequired();
                b.Property(x => x.RowVersion).IsRequired();
                b.Ignore(x => x.Coordinate);
            });

            modelBuilder.Entity<User>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Email).IsRequired();
                b.Property(x => x.NormalizedEmail).IsRequired();
                b.Property(x => x.UserName).IsRequired();
                b.Property(x => x.NormalizedUserName).IsRequired();
                b.Ignore(x => x.UserRoles);
                b.Ignore(x => x.Logins);
                b.Ignore(x => x.Tokens);
                b.Ignore(x => x.TwoFactorSecrets);
                b.Ignore(x => x.Devices);
                b.Ignore(x => x.BusinessFavorites);
                b.Ignore(x => x.BusinessLikes);
                b.Ignore(x => x.BusinessReviews);
                b.Ignore(x => x.EngagementSnapshot);
            });

            modelBuilder.Entity<Customer>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.FirstName).IsRequired();
                b.Property(x => x.LastName).IsRequired();
                b.Property(x => x.Email).IsRequired();
                b.Ignore(x => x.Addresses);
            });

            modelBuilder.Entity<ProviderCallbackInboxMessage>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Provider).IsRequired();
                b.Property(x => x.PayloadJson).IsRequired();
                b.Property(x => x.Status).IsRequired();
                b.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<EmailDispatchOperation>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Provider).IsRequired();
                b.Property(x => x.RecipientEmail).IsRequired();
                b.Property(x => x.Subject).IsRequired();
                b.Property(x => x.HtmlBody).IsRequired();
                b.Property(x => x.Status).IsRequired();
                b.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<ChannelDispatchOperation>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Channel).IsRequired();
                b.Property(x => x.Provider).IsRequired();
                b.Property(x => x.RecipientAddress).IsRequired();
                b.Property(x => x.MessageText).IsRequired();
                b.Property(x => x.Status).IsRequired();
                b.Property(x => x.RowVersion).IsRequired();
            });
        }
    }
}
