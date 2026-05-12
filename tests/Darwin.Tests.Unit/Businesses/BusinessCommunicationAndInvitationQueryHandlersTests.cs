using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Businesses.DTOs;
using Darwin.Application.Businesses.Queries;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Integration;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.Businesses;

/// <summary>
/// Covers business communication/operations/support query handlers:
/// <see cref="GetBusinessCommunicationOpsSummaryHandler"/>,
/// <see cref="GetBusinessSupportSummaryHandler"/>,
/// <see cref="GetBusinessInvitationsPageHandler"/>.
/// </summary>
public sealed class BusinessCommunicationAndInvitationQueryHandlersTests
{
    // ─── GetBusinessCommunicationOpsSummaryHandler ────────────────────────────

    [Fact]
    public async Task GetBusinessCommunicationOpsSummary_Should_ReturnZeroCounts_WhenNoBusinesses()
    {
        await using var db = BizCommTestDbContext.Create();
        var handler = new GetBusinessCommunicationOpsSummaryHandler(db);

        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.BusinessesWithCustomerEmailNotificationsEnabledCount.Should().Be(0);
        result.BusinessesWithMarketingEmailsEnabledCount.Should().Be(0);
        result.BusinessesWithOperationalAlertEmailsEnabledCount.Should().Be(0);
        result.BusinessesMissingSupportEmailCount.Should().Be(0);
        result.BusinessesMissingSenderIdentityCount.Should().Be(0);
        result.BusinessesRequiringEmailSetupCount.Should().Be(0);
        result.FailedInvitationCount.Should().Be(0);
        result.FailedActivationCount.Should().Be(0);
        result.FailedPasswordResetCount.Should().Be(0);
        result.FailedAdminTestCount.Should().Be(0);
    }

    [Fact]
    public async Task GetBusinessCommunicationOpsSummary_Should_CountEmailEnabledBusinesses()
    {
        await using var db = BizCommTestDbContext.Create();
        db.Set<Business>().AddRange(
            new Business { Id = Guid.NewGuid(), Name = "Biz 1", CustomerEmailNotificationsEnabled = true, CustomerMarketingEmailsEnabled = true, OperationalAlertEmailsEnabled = true },
            new Business { Id = Guid.NewGuid(), Name = "Biz 2", CustomerEmailNotificationsEnabled = false, CustomerMarketingEmailsEnabled = false, OperationalAlertEmailsEnabled = false },
            new Business { Id = Guid.NewGuid(), Name = "Biz Deleted", CustomerEmailNotificationsEnabled = true, IsDeleted = true });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessCommunicationOpsSummaryHandler(db);
        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.BusinessesWithCustomerEmailNotificationsEnabledCount.Should().Be(1);
        result.BusinessesWithMarketingEmailsEnabledCount.Should().Be(1);
        result.BusinessesWithOperationalAlertEmailsEnabledCount.Should().Be(1);
    }

    [Fact]
    public async Task GetBusinessCommunicationOpsSummary_Should_CountMissingSupportEmailAndSenderIdentity()
    {
        await using var db = BizCommTestDbContext.Create();
        db.Set<Business>().AddRange(
            new Business { Id = Guid.NewGuid(), Name = "Complete Biz", SupportEmail = "support@biz.de", CommunicationSenderName = "Biz Team", CommunicationReplyToEmail = "noreply@biz.de" },
            new Business { Id = Guid.NewGuid(), Name = "No Support Email" },
            new Business { Id = Guid.NewGuid(), Name = "No Sender Name", SupportEmail = "support@biz2.de" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessCommunicationOpsSummaryHandler(db);
        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        // "No Support Email" and "No Sender Name" are missing sender identity (SenderName or ReplyToEmail)
        result.BusinessesMissingSenderIdentityCount.Should().Be(2);
        // Only "No Support Email" is missing the support email
        result.BusinessesMissingSupportEmailCount.Should().Be(1);
    }

    [Fact]
    public async Task GetBusinessCommunicationOpsSummary_Should_CountRequiringEmailSetup()
    {
        await using var db = BizCommTestDbContext.Create();
        db.Set<Business>().AddRange(
            // Email enabled but missing support config - requires setup
            new Business { Id = Guid.NewGuid(), Name = "Needs Setup", CustomerEmailNotificationsEnabled = true, CustomerMarketingEmailsEnabled = false, OperationalAlertEmailsEnabled = false },
            // Email enabled and fully configured - does NOT require setup
            new Business { Id = Guid.NewGuid(), Name = "Fully Configured", CustomerEmailNotificationsEnabled = true, CustomerMarketingEmailsEnabled = false, OperationalAlertEmailsEnabled = false, SupportEmail = "s@biz.de", CommunicationSenderName = "Biz", CommunicationReplyToEmail = "r@biz.de" },
            // No email notifications enabled at all - does NOT require setup
            new Business { Id = Guid.NewGuid(), Name = "No Email", CustomerEmailNotificationsEnabled = false, CustomerMarketingEmailsEnabled = false, OperationalAlertEmailsEnabled = false });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessCommunicationOpsSummaryHandler(db);
        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.BusinessesRequiringEmailSetupCount.Should().Be(1);
    }

    [Fact]
    public async Task GetBusinessCommunicationOpsSummary_Should_CountFailedEmailAudits_ByFlowKey()
    {
        await using var db = BizCommTestDbContext.Create();
        db.Set<EmailDispatchAudit>().AddRange(
            new EmailDispatchAudit { RecipientEmail = "a@test.de", Subject = "Invite", FlowKey = "BusinessInvitation", Status = "Failed", AttemptedAtUtc = DateTime.UtcNow, RowVersion = [1] },
            new EmailDispatchAudit { RecipientEmail = "b@test.de", Subject = "Activate", FlowKey = "AccountActivation", Status = "Failed", AttemptedAtUtc = DateTime.UtcNow, RowVersion = [1] },
            new EmailDispatchAudit { RecipientEmail = "c@test.de", Subject = "Reset", FlowKey = "PasswordReset", Status = "Failed", AttemptedAtUtc = DateTime.UtcNow, RowVersion = [1] },
            new EmailDispatchAudit { RecipientEmail = "d@test.de", Subject = "Test", FlowKey = "AdminCommunicationTest", Status = "Failed", AttemptedAtUtc = DateTime.UtcNow, RowVersion = [1] },
            // Sent (not failed) - should not count
            new EmailDispatchAudit { RecipientEmail = "e@test.de", Subject = "OK", FlowKey = "BusinessInvitation", Status = "Sent", AttemptedAtUtc = DateTime.UtcNow, RowVersion = [1] },
            // Deleted - should not count
            new EmailDispatchAudit { RecipientEmail = "f@test.de", Subject = "Deleted", FlowKey = "BusinessInvitation", Status = "Failed", IsDeleted = true, AttemptedAtUtc = DateTime.UtcNow, RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessCommunicationOpsSummaryHandler(db);
        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.FailedInvitationCount.Should().Be(1);
        result.FailedActivationCount.Should().Be(1);
        result.FailedPasswordResetCount.Should().Be(1);
        result.FailedAdminTestCount.Should().Be(1);
    }

    // ─── GetBusinessSupportSummaryHandler ────────────────────────────────────

    [Fact]
    public async Task GetBusinessSupportSummary_Should_ReturnZeroCounts_WhenNoBusinesses()
    {
        await using var db = BizCommTestDbContext.Create();
        var handler = new GetBusinessSupportSummaryHandler(db);

        var result = await handler.HandleAsync(ct: TestContext.Current.CancellationToken);

        result.PendingApprovalBusinessCount.Should().Be(0);
        result.SuspendedBusinessCount.Should().Be(0);
        result.ApprovedInactiveBusinessCount.Should().Be(0);
        result.MissingOwnerBusinessCount.Should().Be(0);
        result.MissingPrimaryLocationBusinessCount.Should().Be(0);
        result.PendingInvitationCount.Should().Be(0);
    }

    [Fact]
    public async Task GetBusinessSupportSummary_Should_CountPendingApprovalAndSuspended()
    {
        await using var db = BizCommTestDbContext.Create();
        db.Set<Business>().AddRange(
            new Business { Id = Guid.NewGuid(), Name = "Biz1", OperationalStatus = BusinessOperationalStatus.PendingApproval },
            new Business { Id = Guid.NewGuid(), Name = "Biz2", OperationalStatus = BusinessOperationalStatus.Suspended },
            new Business { Id = Guid.NewGuid(), Name = "Biz3", OperationalStatus = BusinessOperationalStatus.Approved, IsActive = true },
            new Business { Id = Guid.NewGuid(), Name = "Biz Deleted", OperationalStatus = BusinessOperationalStatus.PendingApproval, IsDeleted = true });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessSupportSummaryHandler(db);
        var result = await handler.HandleAsync(ct: TestContext.Current.CancellationToken);

        result.PendingApprovalBusinessCount.Should().Be(1);
        result.SuspendedBusinessCount.Should().Be(1);
    }

    [Fact]
    public async Task GetBusinessSupportSummary_Should_CountApprovedButInactive()
    {
        await using var db = BizCommTestDbContext.Create();
        db.Set<Business>().AddRange(
            new Business { Id = Guid.NewGuid(), Name = "Approved Active", OperationalStatus = BusinessOperationalStatus.Approved, IsActive = true },
            new Business { Id = Guid.NewGuid(), Name = "Approved Inactive", OperationalStatus = BusinessOperationalStatus.Approved, IsActive = false });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessSupportSummaryHandler(db);
        var result = await handler.HandleAsync(ct: TestContext.Current.CancellationToken);

        result.ApprovedInactiveBusinessCount.Should().Be(1);
    }

    [Fact]
    public async Task GetBusinessSupportSummary_Should_CountMissingContactEmailAndLegalName()
    {
        await using var db = BizCommTestDbContext.Create();
        db.Set<Business>().AddRange(
            new Business { Id = Guid.NewGuid(), Name = "Full Biz", ContactEmail = "contact@biz.de", LegalName = "Biz GmbH" },
            new Business { Id = Guid.NewGuid(), Name = "No Contact" },
            new Business { Id = Guid.NewGuid(), Name = "No Legal", ContactEmail = "contact@biz2.de" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessSupportSummaryHandler(db);
        var result = await handler.HandleAsync(ct: TestContext.Current.CancellationToken);

        // "No Contact" lacks ContactEmail, "No Legal" has it
        result.MissingContactEmailBusinessCount.Should().Be(1);
        // "No Contact" and "No Legal" both lack LegalName
        result.MissingLegalNameBusinessCount.Should().Be(2);
    }

    [Fact]
    public async Task GetBusinessSupportSummary_Should_CountPendingInvitations()
    {
        await using var db = BizCommTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var inviterId = Guid.NewGuid();
        db.Set<Business>().Add(new Business { Id = businessId, Name = "Test Biz" });
        db.Set<BusinessInvitation>().AddRange(
            new BusinessInvitation { Id = Guid.NewGuid(), BusinessId = businessId, InvitedByUserId = inviterId, Email = "a@t.de", Token = "tok1", Status = BusinessInvitationStatus.Pending, ExpiresAtUtc = DateTime.UtcNow.AddDays(7), RowVersion = [1] },
            new BusinessInvitation { Id = Guid.NewGuid(), BusinessId = businessId, InvitedByUserId = inviterId, Email = "b@t.de", Token = "tok2", Status = BusinessInvitationStatus.Accepted, ExpiresAtUtc = DateTime.UtcNow.AddDays(7), RowVersion = [1] },
            new BusinessInvitation { Id = Guid.NewGuid(), BusinessId = businessId, InvitedByUserId = inviterId, Email = "c@t.de", Token = "tok3", Status = BusinessInvitationStatus.Pending, IsDeleted = true, ExpiresAtUtc = DateTime.UtcNow.AddDays(7), RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessSupportSummaryHandler(db);
        var result = await handler.HandleAsync(ct: TestContext.Current.CancellationToken);

        result.PendingInvitationCount.Should().Be(1);
        result.OpenInvitationCount.Should().Be(1);
    }

    [Fact]
    public async Task GetBusinessSupportSummary_Should_IncludeSelectedBusinessCounts_WhenBusinessIdProvided()
    {
        await using var db = BizCommTestDbContext.Create();
        var selectedBizId = Guid.NewGuid();
        var otherBizId = Guid.NewGuid();
        var inviterId = Guid.NewGuid();
        db.Set<Business>().AddRange(
            new Business { Id = selectedBizId, Name = "Selected Biz" },
            new Business { Id = otherBizId, Name = "Other Biz" });
        db.Set<BusinessInvitation>().AddRange(
            new BusinessInvitation { Id = Guid.NewGuid(), BusinessId = selectedBizId, InvitedByUserId = inviterId, Email = "a@t.de", Token = "tok1", Status = BusinessInvitationStatus.Pending, ExpiresAtUtc = DateTime.UtcNow.AddDays(7), RowVersion = [1] },
            new BusinessInvitation { Id = Guid.NewGuid(), BusinessId = otherBizId, InvitedByUserId = inviterId, Email = "b@t.de", Token = "tok2", Status = BusinessInvitationStatus.Pending, ExpiresAtUtc = DateTime.UtcNow.AddDays(7), RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessSupportSummaryHandler(db);
        var result = await handler.HandleAsync(selectedBizId, TestContext.Current.CancellationToken);

        result.SelectedBusinessPendingInvitationCount.Should().Be(1);
        result.SelectedBusinessOpenInvitationCount.Should().Be(1);
        // Global count still spans both
        result.PendingInvitationCount.Should().Be(2);
    }

    // ─── GetBusinessInvitationsPageHandler ───────────────────────────────────

    [Fact]
    public async Task GetBusinessInvitationsPage_Should_ReturnPaginatedInvitations()
    {
        await using var db = BizCommTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var inviterId = Guid.NewGuid();
        db.Set<User>().Add(CreateUser(inviterId, "inviter@test.de", "Jane", "Doe"));
        db.Set<Business>().Add(new Business { Id = businessId, Name = "Biz" });
        for (var i = 0; i < 5; i++)
        {
            db.Set<BusinessInvitation>().Add(new BusinessInvitation
            {
                Id = Guid.NewGuid(),
                BusinessId = businessId,
                InvitedByUserId = inviterId,
                Email = $"staff{i}@test.de",
                Token = $"tok{i}",
                Role = BusinessMemberRole.Staff,
                Status = BusinessInvitationStatus.Pending,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
                CreatedAtUtc = DateTime.UtcNow.AddDays(-i),
                RowVersion = [1]
            });
        }
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessInvitationsPageHandler(db);
        var (items, total) = await handler.HandleAsync(businessId, 1, 3, ct: TestContext.Current.CancellationToken);

        total.Should().Be(5);
        items.Should().HaveCount(3);
        items.Should().AllSatisfy(x => x.InvitedByDisplayName.Should().Be("Jane Doe"));
    }

    [Fact]
    public async Task GetBusinessInvitationsPage_Should_ExcludeDeletedInvitations()
    {
        await using var db = BizCommTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var inviterId = Guid.NewGuid();
        db.Set<User>().Add(CreateUser(inviterId, "inviter@test.de"));
        db.Set<Business>().Add(new Business { Id = businessId, Name = "Biz" });
        db.Set<BusinessInvitation>().AddRange(
            new BusinessInvitation { Id = Guid.NewGuid(), BusinessId = businessId, InvitedByUserId = inviterId, Email = "a@t.de", Token = "tok1", Status = BusinessInvitationStatus.Pending, ExpiresAtUtc = DateTime.UtcNow.AddDays(7), RowVersion = [1] },
            new BusinessInvitation { Id = Guid.NewGuid(), BusinessId = businessId, InvitedByUserId = inviterId, Email = "b@t.de", Token = "tok2", Status = BusinessInvitationStatus.Pending, IsDeleted = true, ExpiresAtUtc = DateTime.UtcNow.AddDays(7), RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessInvitationsPageHandler(db);
        var (items, total) = await handler.HandleAsync(businessId, 1, 10, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Should().HaveCount(1);
        items[0].Email.Should().Be("a@t.de");
    }

    [Fact]
    public async Task GetBusinessInvitationsPage_Should_NotReturnInvitationsForOtherBusinesses()
    {
        await using var db = BizCommTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var otherBizId = Guid.NewGuid();
        var inviterId = Guid.NewGuid();
        db.Set<User>().Add(CreateUser(inviterId, "inv@test.de"));
        db.Set<BusinessInvitation>().Add(
            new BusinessInvitation { Id = Guid.NewGuid(), BusinessId = otherBizId, InvitedByUserId = inviterId, Email = "other@t.de", Token = "tok", Status = BusinessInvitationStatus.Pending, ExpiresAtUtc = DateTime.UtcNow.AddDays(7), RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessInvitationsPageHandler(db);
        var (items, total) = await handler.HandleAsync(businessId, 1, 10, ct: TestContext.Current.CancellationToken);

        total.Should().Be(0);
        items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBusinessInvitationsPage_Should_FilterByPending()
    {
        await using var db = BizCommTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var inviterId = Guid.NewGuid();
        db.Set<User>().Add(CreateUser(inviterId, "inv@test.de"));
        db.Set<BusinessInvitation>().AddRange(
            new BusinessInvitation { Id = Guid.NewGuid(), BusinessId = businessId, InvitedByUserId = inviterId, Email = "pending@t.de", Token = "tok1", Status = BusinessInvitationStatus.Pending, ExpiresAtUtc = DateTime.UtcNow.AddDays(7), RowVersion = [1] },
            new BusinessInvitation { Id = Guid.NewGuid(), BusinessId = businessId, InvitedByUserId = inviterId, Email = "accepted@t.de", Token = "tok2", Status = BusinessInvitationStatus.Accepted, ExpiresAtUtc = DateTime.UtcNow.AddDays(7), RowVersion = [1] },
            new BusinessInvitation { Id = Guid.NewGuid(), BusinessId = businessId, InvitedByUserId = inviterId, Email = "revoked@t.de", Token = "tok3", Status = BusinessInvitationStatus.Revoked, ExpiresAtUtc = DateTime.UtcNow.AddDays(7), RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessInvitationsPageHandler(db);
        var (items, total) = await handler.HandleAsync(businessId, 1, 10, filter: BusinessInvitationQueueFilter.Pending, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items[0].Email.Should().Be("pending@t.de");
    }

    [Fact]
    public async Task GetBusinessInvitationsPage_Should_FilterByAccepted()
    {
        await using var db = BizCommTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var inviterId = Guid.NewGuid();
        db.Set<User>().Add(CreateUser(inviterId, "inv@test.de"));
        db.Set<BusinessInvitation>().AddRange(
            new BusinessInvitation { Id = Guid.NewGuid(), BusinessId = businessId, InvitedByUserId = inviterId, Email = "pending@t.de", Token = "tok1", Status = BusinessInvitationStatus.Pending, ExpiresAtUtc = DateTime.UtcNow.AddDays(7), RowVersion = [1] },
            new BusinessInvitation { Id = Guid.NewGuid(), BusinessId = businessId, InvitedByUserId = inviterId, Email = "accepted@t.de", Token = "tok2", Status = BusinessInvitationStatus.Accepted, ExpiresAtUtc = DateTime.UtcNow.AddDays(7), RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessInvitationsPageHandler(db);
        var (items, total) = await handler.HandleAsync(businessId, 1, 10, filter: BusinessInvitationQueueFilter.Accepted, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items[0].Email.Should().Be("accepted@t.de");
    }

    [Fact]
    public async Task GetBusinessInvitationsPage_Should_FilterByRevoked()
    {
        await using var db = BizCommTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var inviterId = Guid.NewGuid();
        db.Set<User>().Add(CreateUser(inviterId, "inv@test.de"));
        db.Set<BusinessInvitation>().AddRange(
            new BusinessInvitation { Id = Guid.NewGuid(), BusinessId = businessId, InvitedByUserId = inviterId, Email = "pending@t.de", Token = "tok1", Status = BusinessInvitationStatus.Pending, ExpiresAtUtc = DateTime.UtcNow.AddDays(7), RowVersion = [1] },
            new BusinessInvitation { Id = Guid.NewGuid(), BusinessId = businessId, InvitedByUserId = inviterId, Email = "revoked@t.de", Token = "tok2", Status = BusinessInvitationStatus.Revoked, ExpiresAtUtc = DateTime.UtcNow.AddDays(7), RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessInvitationsPageHandler(db);
        var (items, total) = await handler.HandleAsync(businessId, 1, 10, filter: BusinessInvitationQueueFilter.Revoked, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items[0].Email.Should().Be("revoked@t.de");
    }

    [Fact]
    public async Task GetBusinessInvitationsPage_Should_UseEmailAsInviterDisplayName_WhenNameIsEmpty()
    {
        await using var db = BizCommTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var inviterId = Guid.NewGuid();
        // User with no first/last name
        db.Set<User>().Add(CreateUser(inviterId, "namer@test.de"));
        db.Set<BusinessInvitation>().Add(
            new BusinessInvitation { Id = Guid.NewGuid(), BusinessId = businessId, InvitedByUserId = inviterId, Email = "staff@t.de", Token = "tok", Status = BusinessInvitationStatus.Pending, ExpiresAtUtc = DateTime.UtcNow.AddDays(7), RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessInvitationsPageHandler(db);
        var (items, _) = await handler.HandleAsync(businessId, 1, 10, ct: TestContext.Current.CancellationToken);

        items[0].InvitedByDisplayName.Should().Be("namer@test.de");
    }

    [Fact]
    public async Task GetBusinessInvitationsPage_Should_ShowUnknownAdmin_WhenInviterIdDoesNotExistInUserTable()
    {
        await using var db = BizCommTestDbContext.Create();
        var businessId = Guid.NewGuid();
        // Inviter user ID is not present in the User table → left join yields null → "Unknown admin"
        var missingInviterId = Guid.NewGuid();
        db.Set<BusinessInvitation>().Add(
            new BusinessInvitation { Id = Guid.NewGuid(), BusinessId = businessId, InvitedByUserId = missingInviterId, Email = "staff@t.de", Token = "tok", Status = BusinessInvitationStatus.Pending, ExpiresAtUtc = DateTime.UtcNow.AddDays(1), RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessInvitationsPageHandler(db);
        var (items, _) = await handler.HandleAsync(businessId, 1, 10, ct: TestContext.Current.CancellationToken);

        items[0].InvitedByDisplayName.Should().Be("Unknown admin");
    }

    [Fact]
    public async Task GetBusinessInvitationsPage_Should_ProjectExpiredStatus_WhenPendingAndExpired()
    {
        await using var db = BizCommTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var inviterId = Guid.NewGuid();
        db.Set<User>().Add(CreateUser(inviterId, "inv@test.de"));
        db.Set<BusinessInvitation>().Add(
            new BusinessInvitation
            {
                Id = Guid.NewGuid(),
                BusinessId = businessId,
                InvitedByUserId = inviterId,
                Email = "expired@t.de",
                Token = "tok",
                Status = BusinessInvitationStatus.Pending,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(-1), // already past
                RowVersion = [1]
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessInvitationsPageHandler(db);
        var (items, _) = await handler.HandleAsync(businessId, 1, 10, ct: TestContext.Current.CancellationToken);

        items[0].Status.Should().Be(BusinessInvitationStatus.Expired);
    }

    [Fact]
    public async Task GetBusinessInvitationsPage_Should_FilterByExpired_WhenExpiredFilterApplied()
    {
        await using var db = BizCommTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var inviterId = Guid.NewGuid();
        db.Set<User>().Add(CreateUser(inviterId, "inv@test.de"));
        db.Set<BusinessInvitation>().AddRange(
            new BusinessInvitation { Id = Guid.NewGuid(), BusinessId = businessId, InvitedByUserId = inviterId, Email = "active@t.de", Token = "tok1", Status = BusinessInvitationStatus.Pending, ExpiresAtUtc = DateTime.UtcNow.AddDays(7), RowVersion = [1] },
            new BusinessInvitation { Id = Guid.NewGuid(), BusinessId = businessId, InvitedByUserId = inviterId, Email = "expired@t.de", Token = "tok2", Status = BusinessInvitationStatus.Pending, ExpiresAtUtc = DateTime.UtcNow.AddDays(-1), RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessInvitationsPageHandler(db);
        var (items, total) = await handler.HandleAsync(businessId, 1, 10, filter: BusinessInvitationQueueFilter.Expired, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items[0].Email.Should().Be("expired@t.de");
    }

    [Fact]
    public async Task GetBusinessInvitationsPage_Should_FilterByOpen_WhenOpenFilterApplied()
    {
        await using var db = BizCommTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var inviterId = Guid.NewGuid();
        db.Set<User>().Add(CreateUser(inviterId, "inv@test.de"));
        db.Set<BusinessInvitation>().AddRange(
            new BusinessInvitation { Id = Guid.NewGuid(), BusinessId = businessId, InvitedByUserId = inviterId, Email = "pending@t.de", Token = "tok1", Status = BusinessInvitationStatus.Pending, ExpiresAtUtc = DateTime.UtcNow.AddDays(7), RowVersion = [1] },
            new BusinessInvitation { Id = Guid.NewGuid(), BusinessId = businessId, InvitedByUserId = inviterId, Email = "expired@t.de", Token = "tok2", Status = BusinessInvitationStatus.Pending, ExpiresAtUtc = DateTime.UtcNow.AddDays(-1), RowVersion = [1] },
            new BusinessInvitation { Id = Guid.NewGuid(), BusinessId = businessId, InvitedByUserId = inviterId, Email = "accepted@t.de", Token = "tok3", Status = BusinessInvitationStatus.Accepted, ExpiresAtUtc = DateTime.UtcNow.AddDays(7), RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessInvitationsPageHandler(db);
        var (items, total) = await handler.HandleAsync(businessId, 1, 10, filter: BusinessInvitationQueueFilter.Open, ct: TestContext.Current.CancellationToken);

        // "Open" means Pending + Expired
        total.Should().Be(2);
        items.Select(x => x.Email).Should().BeEquivalentTo(["pending@t.de", "expired@t.de"]);
    }

    [Fact]
    public async Task GetBusinessInvitationsPage_Should_FilterByEmailSearchQuery()
    {
        await using var db = BizCommTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var inviterId = Guid.NewGuid();
        db.Set<User>().Add(CreateUser(inviterId, "inv@test.de"));
        db.Set<BusinessInvitation>().AddRange(
            new BusinessInvitation { Id = Guid.NewGuid(), BusinessId = businessId, InvitedByUserId = inviterId, Email = "alice@example.de", Token = "tok1", Status = BusinessInvitationStatus.Pending, ExpiresAtUtc = DateTime.UtcNow.AddDays(7), RowVersion = [1] },
            new BusinessInvitation { Id = Guid.NewGuid(), BusinessId = businessId, InvitedByUserId = inviterId, Email = "bob@example.de", Token = "tok2", Status = BusinessInvitationStatus.Pending, ExpiresAtUtc = DateTime.UtcNow.AddDays(7), RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessInvitationsPageHandler(db);
        var (items, total) = await handler.HandleAsync(businessId, 1, 10, query: "alice", ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items[0].Email.Should().Be("alice@example.de");
    }

    // ─── GetBusinessInvitationsPage — exact ExpiresAtUtc boundary ────────────

    [Fact]
    public async Task GetBusinessInvitationsPage_Should_ProjectExpiredStatus_WhenExpiresAtIsExactlyNow()
    {
        // The handler uses <=, so a pending invitation with ExpiresAtUtc == utcNow must show as Expired.
        var utcNow = new DateTime(2030, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        var clock = new FixedClock(utcNow);

        await using var db = BizCommTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var inviterId = Guid.NewGuid();
        db.Set<User>().Add(CreateUser(inviterId, "inv@test.de"));
        db.Set<BusinessInvitation>().Add(new BusinessInvitation
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            InvitedByUserId = inviterId,
            Email = "boundary@test.de",
            Token = "tok-boundary-exact",
            Status = BusinessInvitationStatus.Pending,
            ExpiresAtUtc = utcNow, // exactly at the clock
            RowVersion = [1]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessInvitationsPageHandler(db, clock);
        var (items, _) = await handler.HandleAsync(businessId, 1, 10, ct: TestContext.Current.CancellationToken);

        items.Should().HaveCount(1);
        items[0].Status.Should().Be(BusinessInvitationStatus.Expired,
            "ExpiresAtUtc <= utcNow is true when they are equal, so the invitation must show as Expired");
    }

    [Fact]
    public async Task GetBusinessInvitationsPage_Should_ProjectPendingStatus_WhenExpiresAtIsOneSecondInFuture()
    {
        var utcNow = new DateTime(2030, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        var clock = new FixedClock(utcNow);

        await using var db = BizCommTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var inviterId = Guid.NewGuid();
        db.Set<User>().Add(CreateUser(inviterId, "inv@test.de"));
        db.Set<BusinessInvitation>().Add(new BusinessInvitation
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            InvitedByUserId = inviterId,
            Email = "almost-expired@test.de",
            Token = "tok-almost-expired",
            Status = BusinessInvitationStatus.Pending,
            ExpiresAtUtc = utcNow.AddSeconds(1), // one second in the future
            RowVersion = [1]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessInvitationsPageHandler(db, clock);
        var (items, _) = await handler.HandleAsync(businessId, 1, 10, ct: TestContext.Current.CancellationToken);

        items.Should().HaveCount(1);
        items[0].Status.Should().Be(BusinessInvitationStatus.Pending,
            "ExpiresAtUtc > utcNow means the invitation is still active");
    }

    [Fact]
    public async Task GetBusinessInvitationsPage_FilterExpired_Should_IncludeInvitation_WhenExpiresAtIsExactlyNow()
    {
        var utcNow = new DateTime(2030, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        var clock = new FixedClock(utcNow);

        await using var db = BizCommTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var inviterId = Guid.NewGuid();
        db.Set<User>().Add(CreateUser(inviterId, "inv@test.de"));
        db.Set<BusinessInvitation>().Add(new BusinessInvitation
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            InvitedByUserId = inviterId,
            Email = "exact@test.de",
            Token = "tok-exact-filter",
            Status = BusinessInvitationStatus.Pending,
            ExpiresAtUtc = utcNow, // at the boundary
            RowVersion = [1]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessInvitationsPageHandler(db, clock);
        var (items, total) = await handler.HandleAsync(
            businessId, 1, 10,
            filter: BusinessInvitationQueueFilter.Expired,
            ct: TestContext.Current.CancellationToken);

        total.Should().Be(1, "an invitation at exactly ExpiresAtUtc is expired and must appear in the Expired filter");
        items[0].Email.Should().Be("exact@test.de");
    }

    [Fact]
    public async Task GetBusinessInvitationsPage_FilterPending_Should_ExcludeInvitation_WhenExpiresAtIsExactlyNow()
    {
        var utcNow = new DateTime(2030, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        var clock = new FixedClock(utcNow);

        await using var db = BizCommTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var inviterId = Guid.NewGuid();
        db.Set<User>().Add(CreateUser(inviterId, "inv@test.de"));
        db.Set<BusinessInvitation>().Add(new BusinessInvitation
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            InvitedByUserId = inviterId,
            Email = "boundary-pending@test.de",
            Token = "tok-boundary-pending",
            Status = BusinessInvitationStatus.Pending,
            ExpiresAtUtc = utcNow,
            RowVersion = [1]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessInvitationsPageHandler(db, clock);
        var (_, total) = await handler.HandleAsync(
            businessId, 1, 10,
            filter: BusinessInvitationQueueFilter.Pending,
            ct: TestContext.Current.CancellationToken);

        total.Should().Be(0, "an invitation at exactly ExpiresAtUtc is expired and must NOT appear in the Pending filter");
    }

    [Fact]
    public async Task GetBusinessInvitationsPage_FilterOpen_Should_IncludeInvitation_WhenExpiresAtIsExactlyNow()
    {
        // "Open" includes both Pending and Expired, so exact boundary invitations must still appear.
        var utcNow = new DateTime(2030, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        var clock = new FixedClock(utcNow);

        await using var db = BizCommTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var inviterId = Guid.NewGuid();
        db.Set<User>().Add(CreateUser(inviterId, "inv@test.de"));
        db.Set<BusinessInvitation>().Add(new BusinessInvitation
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            InvitedByUserId = inviterId,
            Email = "open-boundary@test.de",
            Token = "tok-open-boundary",
            Status = BusinessInvitationStatus.Pending,
            ExpiresAtUtc = utcNow,
            RowVersion = [1]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessInvitationsPageHandler(db, clock);
        var (_, total) = await handler.HandleAsync(
            businessId, 1, 10,
            filter: BusinessInvitationQueueFilter.Open,
            ct: TestContext.Current.CancellationToken);

        total.Should().Be(1, "an expired invitation (boundary) still belongs to the Open bucket");
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private sealed class FixedClock : IClock
    {
        private readonly DateTime _utcNow;
        public FixedClock(DateTime utcNow) => _utcNow = utcNow;
        public DateTime UtcNow => _utcNow;
    }

    private static User CreateUser(Guid id, string email, string? firstName = null, string? lastName = null)
    {
        return new User(email, "hashed:pw", Guid.NewGuid().ToString("N"))
        {
            Id = id,
            FirstName = firstName,
            LastName = lastName,
            RowVersion = [1]
        };
    }

    private sealed class BizCommTestDbContext : DbContext, IAppDbContext
    {
        private BizCommTestDbContext(DbContextOptions<BizCommTestDbContext> options) : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static BizCommTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<BizCommTestDbContext>()
                .UseInMemoryDatabase($"darwin_biz_comm_inv_tests_{Guid.NewGuid()}")
                .Options;
            return new BizCommTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Ignore<GeoCoordinate>();

            modelBuilder.Entity<Business>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Name).IsRequired();
                builder.Property(x => x.DefaultCurrency).IsRequired();
                builder.Property(x => x.DefaultCulture).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
                builder.Ignore(x => x.Members);
                builder.Ignore(x => x.Locations);
                builder.Ignore(x => x.Favorites);
                builder.Ignore(x => x.Likes);
                builder.Ignore(x => x.Reviews);
                builder.Ignore(x => x.EngagementStats);
                builder.Ignore(x => x.Invitations);
                builder.Ignore(x => x.StaffQrCodes);
                builder.Ignore(x => x.Subscriptions);
                builder.Ignore(x => x.AnalyticsExportJobs);
            });

            modelBuilder.Entity<BusinessInvitation>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Email).IsRequired();
                builder.Property(x => x.Token).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<BusinessMember>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<BusinessLocation>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Name).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
                builder.Ignore(x => x.Coordinate);
            });

            modelBuilder.Entity<EmailDispatchAudit>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.RecipientEmail).IsRequired();
                builder.Property(x => x.Subject).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<User>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Email).IsRequired();
                builder.Property(x => x.NormalizedEmail).IsRequired();
                builder.Property(x => x.UserName).IsRequired();
                builder.Property(x => x.NormalizedUserName).IsRequired();
                builder.Property(x => x.PasswordHash).IsRequired();
                builder.Property(x => x.SecurityStamp).IsRequired();
                builder.Property(x => x.Locale).IsRequired();
                builder.Property(x => x.Currency).IsRequired();
                builder.Property(x => x.Timezone).IsRequired();
                builder.Property(x => x.ChannelsOptInJson).IsRequired();
                builder.Property(x => x.FirstTouchUtmJson).IsRequired();
                builder.Property(x => x.LastTouchUtmJson).IsRequired();
                builder.Property(x => x.ExternalIdsJson).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
                builder.Ignore(x => x.UserRoles);
                builder.Ignore(x => x.Logins);
                builder.Ignore(x => x.Tokens);
                builder.Ignore(x => x.TwoFactorSecrets);
                builder.Ignore(x => x.Devices);
                builder.Ignore(x => x.BusinessFavorites);
                builder.Ignore(x => x.BusinessLikes);
                builder.Ignore(x => x.BusinessReviews);
                builder.Ignore(x => x.EngagementSnapshot);
            });
        }
    }
}
