using System;
using System.Threading.Tasks;
using Darwin.Application;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Businesses.Commands;
using Darwin.Application.Businesses.DTOs;
using Darwin.Application.Businesses.Validators;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.Businesses;

/// <summary>
/// Unit tests for business-member command handlers:
/// <see cref="CreateBusinessMemberHandler"/>, <see cref="UpdateBusinessMemberHandler"/>,
/// and <see cref="DeleteBusinessMemberHandler"/>.
/// </summary>
public sealed class BusinessMemberHandlersTests
{
    // ─── CreateBusinessMemberHandler ─────────────────────────────────────────

    [Fact]
    public async Task CreateMember_Should_PersistMember_WhenBusinessAndUserExist()
    {
        await using var db = MemberTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        db.Set<Business>().Add(new Business { Id = businessId, Name = "Cafe Aurora" });
        db.Set<User>().Add(CreateUser(userId, "member@darwin.de"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateBusinessMemberHandler(db, new BusinessMemberCreateDtoValidator(), new TestLocalizer());

        var id = await handler.HandleAsync(new BusinessMemberCreateDto
        {
            BusinessId = businessId,
            UserId = userId,
            Role = BusinessMemberRole.Manager,
            IsActive = true
        }, TestContext.Current.CancellationToken);

        id.Should().NotBeEmpty();

        var persisted = await db.Set<BusinessMember>().AsNoTracking()
            .SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        persisted.BusinessId.Should().Be(businessId);
        persisted.UserId.Should().Be(userId);
        persisted.Role.Should().Be(BusinessMemberRole.Manager);
        persisted.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateMember_Should_Throw_WhenBusinessNotFound()
    {
        await using var db = MemberTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<User>().Add(CreateUser(userId, "user@darwin.de"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateBusinessMemberHandler(db, new BusinessMemberCreateDtoValidator(), new TestLocalizer());

        var act = async () => await handler.HandleAsync(new BusinessMemberCreateDto
        {
            BusinessId = Guid.NewGuid(),
            UserId = userId
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*BusinessNotFound*");
    }

    [Fact]
    public async Task CreateMember_Should_Throw_WhenUserNotFound()
    {
        await using var db = MemberTestDbContext.Create();
        var businessId = Guid.NewGuid();
        db.Set<Business>().Add(new Business { Id = businessId, Name = "Baeckerei Stern" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateBusinessMemberHandler(db, new BusinessMemberCreateDtoValidator(), new TestLocalizer());

        var act = async () => await handler.HandleAsync(new BusinessMemberCreateDto
        {
            BusinessId = businessId,
            UserId = Guid.NewGuid()
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*UserNotFound*");
    }

    [Fact]
    public async Task CreateMember_Should_Throw_WhenDuplicateMemberExists()
    {
        await using var db = MemberTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        db.Set<Business>().Add(new Business { Id = businessId, Name = "Wurst & Co" });
        db.Set<User>().Add(CreateUser(userId, "dup@darwin.de"));
        db.Set<BusinessMember>().Add(new BusinessMember { BusinessId = businessId, UserId = userId });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateBusinessMemberHandler(db, new BusinessMemberCreateDtoValidator(), new TestLocalizer());

        var act = async () => await handler.HandleAsync(new BusinessMemberCreateDto
        {
            BusinessId = businessId,
            UserId = userId
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*BusinessMemberUserAlreadyAssignedToSelectedBusiness*");
    }

    // ─── UpdateBusinessMemberHandler ─────────────────────────────────────────

    [Fact]
    public async Task UpdateMember_Should_UpdateRoleAndActive()
    {
        await using var db = MemberTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var member = CreateMember(businessId, Guid.NewGuid(), BusinessMemberRole.Staff, rowVersion: [1, 2, 3]);
        db.Set<Business>().Add(new Business { Id = businessId, Name = "Test Business" });
        db.Set<BusinessMember>().Add(member);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateBusinessMemberHandler(db, new BusinessMemberEditDtoValidator(), new TestLocalizer());

        await handler.HandleAsync(new BusinessMemberEditDto
        {
            Id = member.Id,
            BusinessId = businessId,
            UserId = member.UserId,
            Role = BusinessMemberRole.Manager,
            IsActive = false,
            RowVersion = [1, 2, 3]
        }, TestContext.Current.CancellationToken);

        var persisted = await db.Set<BusinessMember>().AsNoTracking()
            .SingleAsync(x => x.Id == member.Id, TestContext.Current.CancellationToken);
        persisted.Role.Should().Be(BusinessMemberRole.Manager);
        persisted.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateMember_Should_Throw_WhenMemberNotFound()
    {
        await using var db = MemberTestDbContext.Create();
        var handler = new UpdateBusinessMemberHandler(db, new BusinessMemberEditDtoValidator(), new TestLocalizer());

        var act = async () => await handler.HandleAsync(new BusinessMemberEditDto
        {
            Id = Guid.NewGuid(),
            BusinessId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            RowVersion = [1, 2, 3]
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*BusinessMemberNotFound*");
    }

    [Fact]
    public async Task UpdateMember_Should_Throw_WhenConcurrencyConflict()
    {
        await using var db = MemberTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var member = CreateMember(businessId, Guid.NewGuid(), BusinessMemberRole.Staff, rowVersion: [1, 2, 3]);
        db.Set<Business>().Add(new Business { Id = businessId, Name = "Concurrent Business" });
        db.Set<BusinessMember>().Add(member);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateBusinessMemberHandler(db, new BusinessMemberEditDtoValidator(), new TestLocalizer());

        var act = async () => await handler.HandleAsync(new BusinessMemberEditDto
        {
            Id = member.Id,
            BusinessId = businessId,
            UserId = member.UserId,
            Role = BusinessMemberRole.Manager,
            IsActive = true,
            RowVersion = [9, 9, 9] // wrong version
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    [Fact]
    public async Task UpdateMember_Should_Throw_WhenDemotingLastOwner_WithoutOverride()
    {
        await using var db = MemberTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var member = CreateMember(businessId, Guid.NewGuid(), BusinessMemberRole.Owner, rowVersion: [1, 2, 3]);
        db.Set<Business>().Add(new Business { Id = businessId, Name = "Single Owner Business" });
        db.Set<BusinessMember>().Add(member);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateBusinessMemberHandler(db, new BusinessMemberEditDtoValidator(), new TestLocalizer());

        var act = async () => await handler.HandleAsync(new BusinessMemberEditDto
        {
            Id = member.Id,
            BusinessId = businessId,
            UserId = member.UserId,
            Role = BusinessMemberRole.Staff, // demoting from Owner
            IsActive = true,
            AllowLastOwnerOverride = false,
            RowVersion = [1, 2, 3]
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*AtLeastOneActiveOwnerMustRemainAssignedToBusiness*");
    }

    [Fact]
    public async Task UpdateMember_Should_CreateAuditRecord_WhenDemotingLastOwner_WithOverride()
    {
        await using var db = MemberTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var member = CreateMember(businessId, Guid.NewGuid(), BusinessMemberRole.Owner, rowVersion: [1, 2, 3]);
        db.Set<Business>().Add(new Business { Id = businessId, Name = "Override Business" });
        db.Set<BusinessMember>().Add(member);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateBusinessMemberHandler(db, new BusinessMemberEditDtoValidator(), new TestLocalizer());

        await handler.HandleAsync(new BusinessMemberEditDto
        {
            Id = member.Id,
            BusinessId = businessId,
            UserId = member.UserId,
            Role = BusinessMemberRole.Staff,
            IsActive = true,
            AllowLastOwnerOverride = true,
            OverrideReason = "Admin override for restructuring",
            RowVersion = [1, 2, 3]
        }, TestContext.Current.CancellationToken);

        var audit = await db.Set<BusinessOwnerOverrideAudit>().AsNoTracking()
            .SingleAsync(TestContext.Current.CancellationToken);
        audit.BusinessId.Should().Be(businessId);
        audit.AffectedUserId.Should().Be(member.UserId);
        audit.ActionKind.Should().Be(BusinessOwnerOverrideActionKind.DemoteOrDeactivate);
        audit.Reason.Should().Be("Admin override for restructuring");
    }

    // ─── DeleteBusinessMemberHandler ─────────────────────────────────────────

    [Fact]
    public async Task DeleteMember_Should_RemoveMember()
    {
        await using var db = MemberTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var member = CreateMember(businessId, Guid.NewGuid(), BusinessMemberRole.Staff, rowVersion: [1, 2, 3]);
        db.Set<BusinessMember>().Add(member);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new DeleteBusinessMemberHandler(db, new BusinessMemberDeleteDtoValidator(), new TestLocalizer());

        await handler.HandleAsync(new BusinessMemberDeleteDto
        {
            Id = member.Id,
            RowVersion = [1, 2, 3]
        }, TestContext.Current.CancellationToken);

        var count = await db.Set<BusinessMember>().AsNoTracking().CountAsync(TestContext.Current.CancellationToken);
        count.Should().Be(0);
    }

    [Fact]
    public async Task DeleteMember_Should_BeNoOp_WhenMemberNotFound()
    {
        await using var db = MemberTestDbContext.Create();
        var handler = new DeleteBusinessMemberHandler(db, new BusinessMemberDeleteDtoValidator(), new TestLocalizer());

        var act = async () => await handler.HandleAsync(new BusinessMemberDeleteDto
        {
            Id = Guid.NewGuid(),
            RowVersion = [1, 2, 3]
        }, TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DeleteMember_Should_Throw_WhenDeletingLastOwner_WithoutOverride()
    {
        await using var db = MemberTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var member = CreateMember(businessId, Guid.NewGuid(), BusinessMemberRole.Owner, rowVersion: [1, 2, 3]);
        db.Set<BusinessMember>().Add(member);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new DeleteBusinessMemberHandler(db, new BusinessMemberDeleteDtoValidator(), new TestLocalizer());

        var act = async () => await handler.HandleAsync(new BusinessMemberDeleteDto
        {
            Id = member.Id,
            AllowLastOwnerOverride = false,
            RowVersion = [1, 2, 3]
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*AtLeastOneActiveOwnerMustRemainAssignedToBusiness*");
    }

    [Fact]
    public async Task DeleteMember_Should_CreateAuditAndRemove_WhenDeletingLastOwner_WithOverride()
    {
        await using var db = MemberTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var member = CreateMember(businessId, Guid.NewGuid(), BusinessMemberRole.Owner, rowVersion: [1, 2, 3]);
        db.Set<BusinessMember>().Add(member);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new DeleteBusinessMemberHandler(db, new BusinessMemberDeleteDtoValidator(), new TestLocalizer());

        await handler.HandleAsync(new BusinessMemberDeleteDto
        {
            Id = member.Id,
            AllowLastOwnerOverride = true,
            OverrideReason = "Final offboarding",
            RowVersion = [1, 2, 3]
        }, TestContext.Current.CancellationToken);

        var memberCount = await db.Set<BusinessMember>().AsNoTracking().CountAsync(TestContext.Current.CancellationToken);
        memberCount.Should().Be(0);

        var audit = await db.Set<BusinessOwnerOverrideAudit>().AsNoTracking()
            .SingleAsync(TestContext.Current.CancellationToken);
        audit.BusinessId.Should().Be(businessId);
        audit.ActionKind.Should().Be(BusinessOwnerOverrideActionKind.ForceRemove);
        audit.Reason.Should().Be("Final offboarding");
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static User CreateUser(Guid id, string email) =>
        new(email, "hash", "stamp")
        {
            Id = id,
            FirstName = "Test",
            LastName = "User",
            IsActive = true,
            Locale = "de-DE",
            Currency = "EUR",
            Timezone = "Europe/Berlin",
            ChannelsOptInJson = "{}",
            FirstTouchUtmJson = "{}",
            LastTouchUtmJson = "{}",
            ExternalIdsJson = "{}",
            RowVersion = [1, 2, 3]
        };

    private static BusinessMember CreateMember(Guid businessId, Guid userId, BusinessMemberRole role, byte[] rowVersion) =>
        new()
        {
            BusinessId = businessId,
            UserId = userId,
            Role = role,
            IsActive = true,
            RowVersion = rowVersion
        };

    private sealed class TestLocalizer : IStringLocalizer<ValidationResource>
    {
        public LocalizedString this[string name] => new(name, name, resourceNotFound: false);
        public LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(name, arguments), resourceNotFound: false);
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            Array.Empty<LocalizedString>();
        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
    }

    private sealed class MemberTestDbContext : DbContext, IAppDbContext
    {
        private MemberTestDbContext(DbContextOptions<MemberTestDbContext> options) : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static MemberTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<MemberTestDbContext>()
                .UseInMemoryDatabase($"darwin_business_member_tests_{Guid.NewGuid()}")
                .Options;
            return new MemberTestDbContext(options);
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
                builder.Property(x => x.DefaultTimeZoneId).IsRequired();
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

            modelBuilder.Entity<BusinessMember>(builder =>
            {
                builder.HasKey(x => x.Id);
            });

            modelBuilder.Entity<BusinessOwnerOverrideAudit>(builder =>
            {
                builder.HasKey(x => x.Id);
            });
        }
    }
}
