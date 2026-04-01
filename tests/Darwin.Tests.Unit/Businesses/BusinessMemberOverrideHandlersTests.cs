using System;
using System.Threading.Tasks;
using Darwin.Application;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Businesses.Commands;
using Darwin.Application.Businesses.DTOs;
using Darwin.Application.Businesses.Validators;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.Businesses;

/// <summary>
/// Covers controlled override behavior for the "last active owner" protection rule.
/// </summary>
public sealed class BusinessMemberOverrideHandlersTests
{
    [Fact]
    public async Task UpdateBusinessMember_Should_CreateAudit_When_LastOwnerOverride_IsAllowed()
    {
        await using var db = BusinessMemberOverrideTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var membership = CreateOwnerMembership(businessId);

        db.Set<BusinessMember>().Add(membership);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateBusinessMemberHandler(db, new BusinessMemberEditDtoValidator(), new TestStringLocalizer());

        await handler.HandleAsync(new BusinessMemberEditDto
        {
            Id = membership.Id,
            BusinessId = membership.BusinessId,
            UserId = membership.UserId,
            Role = BusinessMemberRole.Manager,
            IsActive = false,
            AllowLastOwnerOverride = true,
            OverrideReason = "Support recovered ownership through a replacement account.",
            OverrideActorDisplayName = "Full Admin",
            RowVersion = membership.RowVersion
        }, TestContext.Current.CancellationToken);

        var persisted = await db.Set<BusinessMember>().AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        var audit = await db.Set<BusinessOwnerOverrideAudit>().AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);

        persisted.Role.Should().Be(BusinessMemberRole.Manager);
        persisted.IsActive.Should().BeFalse();
        audit.BusinessId.Should().Be(businessId);
        audit.BusinessMemberId.Should().Be(membership.Id);
        audit.ActionKind.Should().Be(BusinessOwnerOverrideActionKind.DemoteOrDeactivate);
        audit.Reason.Should().Be("Support recovered ownership through a replacement account.");
        audit.ActorDisplayName.Should().Be("Full Admin");
    }

    [Fact]
    public async Task DeleteBusinessMember_Should_CreateAudit_When_LastOwnerOverride_IsAllowed()
    {
        await using var db = BusinessMemberOverrideTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var membership = CreateOwnerMembership(businessId);

        db.Set<BusinessMember>().Add(membership);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new DeleteBusinessMemberHandler(db, new BusinessMemberDeleteDtoValidator(), new TestStringLocalizer());

        await handler.HandleAsync(new BusinessMemberDeleteDto
        {
            Id = membership.Id,
            RowVersion = membership.RowVersion,
            AllowLastOwnerOverride = true,
            OverrideReason = "Emergency business closure requested by support.",
            OverrideActorDisplayName = "Full Admin"
        }, TestContext.Current.CancellationToken);

        (await db.Set<BusinessMember>().CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
        var audit = await db.Set<BusinessOwnerOverrideAudit>().AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        audit.ActionKind.Should().Be(BusinessOwnerOverrideActionKind.ForceRemove);
        audit.Reason.Should().Be("Emergency business closure requested by support.");
    }

    private static BusinessMember CreateOwnerMembership(Guid businessId)
    {
        return new BusinessMember
        {
            BusinessId = businessId,
            UserId = Guid.NewGuid(),
            Role = BusinessMemberRole.Owner,
            IsActive = true,
            RowVersion = [1, 2, 3]
        };
    }

    private sealed class BusinessMemberOverrideTestDbContext : DbContext, IAppDbContext
    {
        private BusinessMemberOverrideTestDbContext(DbContextOptions<BusinessMemberOverrideTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static BusinessMemberOverrideTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<BusinessMemberOverrideTestDbContext>()
                .UseInMemoryDatabase($"darwin_business_member_override_tests_{Guid.NewGuid()}")
                .Options;

            return new BusinessMemberOverrideTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Ignore<GeoCoordinate>();

            modelBuilder.Entity<BusinessMember>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<BusinessOwnerOverrideAudit>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Reason).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });
        }
    }

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
