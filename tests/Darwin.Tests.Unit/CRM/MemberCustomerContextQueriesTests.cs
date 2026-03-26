using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.CRM.Queries;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Tests.Unit.CRM;

/// <summary>
/// Covers member-facing CRM customer context projections.
/// </summary>
public sealed class MemberCustomerContextQueriesTests
{
    [Fact]
    public async Task GetCurrentMemberCustomerContext_Should_ReturnSegmentsConsentsAndRecentInteractions()
    {
        await using var db = MemberCustomerContextDbContext.Create();
        var userId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var vipSegmentId = Guid.NewGuid();
        var interactionId = Guid.NewGuid();

        db.Set<User>().Add(CreateUser(userId));
        db.Set<Customer>().Add(new Customer
        {
            Id = customerId,
            UserId = userId,
            FirstName = "Max",
            LastName = "Mustermann",
            Email = "max@example.de",
            Phone = "+491701234567",
            CompanyName = "Darwin GmbH",
            Notes = "Prefers invoice delivery."
        });
        db.Set<CustomerSegment>().Add(new CustomerSegment
        {
            Id = vipSegmentId,
            Name = "VIP",
            Description = "High-value customers"
        });
        db.Set<CustomerSegmentMembership>().Add(new CustomerSegmentMembership
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            CustomerSegmentId = vipSegmentId
        });
        db.Set<Consent>().Add(new Consent
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Type = ConsentType.MarketingEmail,
            Granted = true,
            GrantedAtUtc = new DateTime(2030, 1, 1, 8, 0, 0, DateTimeKind.Utc)
        });
        db.Set<Interaction>().Add(new Interaction
        {
            Id = interactionId,
            CustomerId = customerId,
            Type = InteractionType.Support,
            Channel = InteractionChannel.Email,
            Subject = "Delivery update",
            Content = new string('A', 200)
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetCurrentMemberCustomerContextHandler(db, new StubCurrentUserService(userId));

        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.Id.Should().Be(customerId);
        result.UserId.Should().Be(userId);
        result.DisplayName.Should().Be("Max Mustermann");
        result.Segments.Should().ContainSingle(x => x.Name == "VIP");
        result.Consents.Should().ContainSingle(x => x.Type == nameof(ConsentType.MarketingEmail) && x.Granted);
        result.RecentInteractions.Should().ContainSingle();
        result.RecentInteractions[0].Id.Should().Be(interactionId);
        result.RecentInteractions[0].ContentPreview.Should().NotBeNull();
        result.RecentInteractions[0].ContentPreview!.Length.Should().Be(160);
        result.InteractionCount.Should().Be(1);
        result.LastInteractionAtUtc.Should().NotBeNull();
    }

    private static User CreateUser(Guid userId)
    {
        return new User("max@example.de", "hashed-password", "security-stamp")
        {
            Id = userId,
            FirstName = "Max",
            LastName = "Mustermann",
            PhoneE164 = "+491701234567",
            RowVersion = [1, 2, 3, 4]
        };
    }

    private sealed class StubCurrentUserService : ICurrentUserService
    {
        private readonly Guid _userId;

        public StubCurrentUserService(Guid userId)
        {
            _userId = userId;
        }

        public Guid GetCurrentUserId() => _userId;
    }

    private sealed class MemberCustomerContextDbContext : DbContext, IAppDbContext
    {
        private MemberCustomerContextDbContext(DbContextOptions<MemberCustomerContextDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static MemberCustomerContextDbContext Create()
        {
            var options = new DbContextOptionsBuilder<MemberCustomerContextDbContext>()
                .UseInMemoryDatabase($"darwin_member_customer_context_tests_{Guid.NewGuid()}")
                .Options;

            return new MemberCustomerContextDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Ignore<GeoCoordinate>();

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
            });

            modelBuilder.Entity<Customer>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.FirstName).IsRequired();
                builder.Property(x => x.LastName).IsRequired();
                builder.Property(x => x.Email).IsRequired();
                builder.Property(x => x.Phone).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<CustomerSegment>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Name).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<CustomerSegmentMembership>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<Consent>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<Interaction>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.RowVersion).IsRequired();
            });
        }
    }
}
