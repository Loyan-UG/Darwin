using Darwin.Application.Abstractions.Persistence;
using Darwin.Application;
using Darwin.Application.CRM.Commands;
using Darwin.Application.CRM.DTOs;
using Darwin.Application.CRM.Validators;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.CRM;

/// <summary>
/// Covers CRM engagement handler behaviors: interactions, consents, and customer segments.
/// </summary>
public sealed class CrmEngagementHandlersTests
{
    // ─── CreateInteractionHandler ─────────────────────────────────────────────

    [Fact]
    public async Task CreateInteraction_Should_PersistInteraction_WhenLinkedToCustomer()
    {
        await using var db = CrmEngagementDbContext.Create();
        var customerId = Guid.NewGuid();

        db.Set<Customer>().Add(MakeCustomer(customerId));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateInteractionHandler(
            db,
            new InteractionCreateValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var id = await handler.HandleAsync(new InteractionCreateDto
        {
            CustomerId = customerId,
            Type = InteractionType.Email,
            Channel = InteractionChannel.Email,
            Subject = "Follow up",
            Content = "Please review the proposal."
        }, TestContext.Current.CancellationToken);

        id.Should().NotBeEmpty();

        var interaction = await db.Set<Interaction>().SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        interaction.CustomerId.Should().Be(customerId);
        interaction.Subject.Should().Be("Follow up");
        interaction.Content.Should().Be("Please review the proposal.");
    }

    [Fact]
    public async Task CreateInteraction_Should_NormalizeSubjectAndContent()
    {
        await using var db = CrmEngagementDbContext.Create();
        var customerId = Guid.NewGuid();

        db.Set<Customer>().Add(MakeCustomer(customerId));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateInteractionHandler(
            db,
            new InteractionCreateValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var id = await handler.HandleAsync(new InteractionCreateDto
        {
            CustomerId = customerId,
            Type = InteractionType.Call,
            Channel = InteractionChannel.Phone,
            Subject = "  Lead call  ",
            Content = null
        }, TestContext.Current.CancellationToken);

        var interaction = await db.Set<Interaction>().SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        interaction.Subject.Should().Be("Lead call");
        interaction.Content.Should().BeNull();
    }

    [Fact]
    public async Task CreateInteraction_Should_Throw_WhenCustomerNotFound()
    {
        await using var db = CrmEngagementDbContext.Create();

        var handler = new CreateInteractionHandler(
            db,
            new InteractionCreateValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var act = () => handler.HandleAsync(new InteractionCreateDto
        {
            CustomerId = Guid.NewGuid(),
            Type = InteractionType.Email,
            Channel = InteractionChannel.Email
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CustomerNotFound");
    }

    [Fact]
    public async Task CreateInteraction_Should_Throw_WhenLeadNotFound()
    {
        await using var db = CrmEngagementDbContext.Create();

        var handler = new CreateInteractionHandler(
            db,
            new InteractionCreateValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var act = () => handler.HandleAsync(new InteractionCreateDto
        {
            LeadId = Guid.NewGuid(),
            Type = InteractionType.Meeting,
            Channel = InteractionChannel.InPerson
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("LeadNotFound");
    }

    // ─── CreateConsentHandler ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateConsent_Should_PersistGrantedConsent()
    {
        await using var db = CrmEngagementDbContext.Create();
        var customerId = Guid.NewGuid();

        db.Set<Customer>().Add(MakeCustomer(customerId));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateConsentHandler(
            db,
            new ConsentCreateValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var grantedAt = new DateTime(2030, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var id = await handler.HandleAsync(new ConsentCreateDto
        {
            CustomerId = customerId,
            Type = ConsentType.MarketingEmail,
            Granted = true,
            GrantedAtUtc = grantedAt
        }, TestContext.Current.CancellationToken);

        id.Should().NotBeEmpty();

        var consent = await db.Set<Consent>().SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        consent.CustomerId.Should().Be(customerId);
        consent.Granted.Should().BeTrue();
        consent.GrantedAtUtc.Should().Be(grantedAt);
        consent.RevokedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task CreateConsent_Should_SetRevokedAt_WhenConsentIsRevoked()
    {
        await using var db = CrmEngagementDbContext.Create();
        var customerId = Guid.NewGuid();

        db.Set<Customer>().Add(MakeCustomer(customerId));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateConsentHandler(
            db,
            new ConsentCreateValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var grantedAt = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var revokedAt = new DateTime(2030, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var id = await handler.HandleAsync(new ConsentCreateDto
        {
            CustomerId = customerId,
            Type = ConsentType.MarketingEmail,
            Granted = false,
            GrantedAtUtc = grantedAt,
            RevokedAtUtc = revokedAt
        }, TestContext.Current.CancellationToken);

        var consent = await db.Set<Consent>().SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        consent.Granted.Should().BeFalse();
        consent.RevokedAtUtc.Should().Be(revokedAt);
    }

    [Fact]
    public async Task CreateConsent_Should_Throw_WhenCustomerNotFound()
    {
        await using var db = CrmEngagementDbContext.Create();

        var handler = new CreateConsentHandler(
            db,
            new ConsentCreateValidator(new TestStringLocalizer()),
            new TestStringLocalizer());

        var act = () => handler.HandleAsync(new ConsentCreateDto
        {
            CustomerId = Guid.NewGuid(),
            Type = ConsentType.MarketingEmail,
            Granted = true,
            GrantedAtUtc = DateTime.UtcNow
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CustomerNotFound");
    }

    // ─── CreateCustomerSegmentHandler ────────────────────────────────────────

    [Fact]
    public async Task CreateCustomerSegment_Should_PersistSegment_WhenNameIsUnique()
    {
        await using var db = CrmEngagementDbContext.Create();

        var handler = new CreateCustomerSegmentHandler(
            db,
            new CustomerSegmentEditValidator(),
            new TestStringLocalizer());

        var id = await handler.HandleAsync(new CustomerSegmentEditDto
        {
            Name = "VIP Customers",
            Description = "High-value frequent buyers"
        }, TestContext.Current.CancellationToken);

        id.Should().NotBeEmpty();

        var segment = await db.Set<CustomerSegment>().SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        segment.Name.Should().Be("VIP Customers");
        segment.Description.Should().Be("High-value frequent buyers");
    }

    [Fact]
    public async Task CreateCustomerSegment_Should_Throw_WhenNameAlreadyExists()
    {
        await using var db = CrmEngagementDbContext.Create();

        db.Set<CustomerSegment>().Add(new CustomerSegment
        {
            Id = Guid.NewGuid(),
            Name = "Existing Segment"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateCustomerSegmentHandler(
            db,
            new CustomerSegmentEditValidator(),
            new TestStringLocalizer());

        var act = () => handler.HandleAsync(new CustomerSegmentEditDto
        {
            Name = "Existing Segment"
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CustomerSegmentNameAlreadyExists");
    }

    // ─── UpdateCustomerSegmentHandler ────────────────────────────────────────

    [Fact]
    public async Task UpdateCustomerSegment_Should_PersistChanges_WhenSegmentExists()
    {
        await using var db = CrmEngagementDbContext.Create();
        var segmentId = Guid.NewGuid();
        var rowVersion = new byte[] { 1, 2, 3, 4 };

        db.Set<CustomerSegment>().Add(new CustomerSegment
        {
            Id = segmentId,
            Name = "Old Name",
            RowVersion = rowVersion.ToArray()
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateCustomerSegmentHandler(
            db,
            new CustomerSegmentEditValidator(),
            new TestStringLocalizer());

        await handler.HandleAsync(new CustomerSegmentEditDto
        {
            Id = segmentId,
            RowVersion = rowVersion,
            Name = "New Name",
            Description = "Updated description"
        }, TestContext.Current.CancellationToken);

        var segment = await db.Set<CustomerSegment>().SingleAsync(x => x.Id == segmentId, TestContext.Current.CancellationToken);
        segment.Name.Should().Be("New Name");
        segment.Description.Should().Be("Updated description");
    }

    [Fact]
    public async Task UpdateCustomerSegment_Should_Throw_WhenSegmentNotFound()
    {
        await using var db = CrmEngagementDbContext.Create();

        var handler = new UpdateCustomerSegmentHandler(
            db,
            new CustomerSegmentEditValidator(),
            new TestStringLocalizer());

        var act = () => handler.HandleAsync(new CustomerSegmentEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1 },
            Name = "Does Not Exist"
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CustomerSegmentNotFound");
    }

    [Fact]
    public async Task UpdateCustomerSegment_Should_Throw_WhenRowVersionMismatches()
    {
        await using var db = CrmEngagementDbContext.Create();
        var segmentId = Guid.NewGuid();

        db.Set<CustomerSegment>().Add(new CustomerSegment
        {
            Id = segmentId,
            Name = "Segment A",
            RowVersion = [1, 2, 3, 4]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateCustomerSegmentHandler(
            db,
            new CustomerSegmentEditValidator(),
            new TestStringLocalizer());

        var act = () => handler.HandleAsync(new CustomerSegmentEditDto
        {
            Id = segmentId,
            RowVersion = [9, 9, 9, 9],
            Name = "Segment A Updated"
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    [Fact]
    public async Task UpdateCustomerSegment_Should_Throw_WhenRowVersionIsEmpty()
    {
        await using var db = CrmEngagementDbContext.Create();
        var segmentId = Guid.NewGuid();

        db.Set<CustomerSegment>().Add(new CustomerSegment
        {
            Id = segmentId,
            Name = "Segment Empty RV",
            RowVersion = [1, 2, 3, 4]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateCustomerSegmentHandler(
            db,
            new CustomerSegmentEditValidator(),
            new TestStringLocalizer());

        var act = () => handler.HandleAsync(new CustomerSegmentEditDto
        {
            Id = segmentId,
            RowVersion = Array.Empty<byte>(), // empty row version
            Name = "Segment Empty RV Updated"
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>("empty RowVersion must be treated as a concurrency conflict");
    }

    [Fact]
    public async Task UpdateCustomerSegment_Should_Throw_WhenNameConflictsWithAnotherSegment()
    {
        await using var db = CrmEngagementDbContext.Create();
        var segmentId = Guid.NewGuid();
        var rowVersion = new byte[] { 1, 2, 3, 4 };

        db.Set<CustomerSegment>().AddRange(
            new CustomerSegment
            {
                Id = segmentId,
                Name = "Segment One",
                RowVersion = rowVersion.ToArray()
            },
            new CustomerSegment
            {
                Id = Guid.NewGuid(),
                Name = "Segment Two"
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateCustomerSegmentHandler(
            db,
            new CustomerSegmentEditValidator(),
            new TestStringLocalizer());

        var act = () => handler.HandleAsync(new CustomerSegmentEditDto
        {
            Id = segmentId,
            RowVersion = rowVersion,
            Name = "Segment Two"
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CustomerSegmentNameAlreadyExists");
    }

    // ─── AssignCustomerSegmentHandler ────────────────────────────────────────

    [Fact]
    public async Task AssignCustomerSegment_Should_CreateMembership_WhenNotAlreadyAssigned()
    {
        await using var db = CrmEngagementDbContext.Create();
        var customerId = Guid.NewGuid();
        var segmentId = Guid.NewGuid();

        db.Set<Customer>().Add(MakeCustomer(customerId));
        db.Set<CustomerSegment>().Add(new CustomerSegment { Id = segmentId, Name = "Loyal" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new AssignCustomerSegmentHandler(
            db,
            new AssignCustomerSegmentValidator(),
            new TestStringLocalizer());

        var membershipId = await handler.HandleAsync(new AssignCustomerSegmentDto
        {
            CustomerId = customerId,
            CustomerSegmentId = segmentId
        }, TestContext.Current.CancellationToken);

        membershipId.Should().NotBeEmpty();

        var membership = await db.Set<CustomerSegmentMembership>()
            .SingleAsync(x => x.Id == membershipId, TestContext.Current.CancellationToken);
        membership.CustomerId.Should().Be(customerId);
        membership.CustomerSegmentId.Should().Be(segmentId);
    }

    [Fact]
    public async Task AssignCustomerSegment_Should_Throw_WhenCustomerAlreadyAssigned()
    {
        await using var db = CrmEngagementDbContext.Create();
        var customerId = Guid.NewGuid();
        var segmentId = Guid.NewGuid();

        db.Set<Customer>().Add(MakeCustomer(customerId));
        db.Set<CustomerSegment>().Add(new CustomerSegment { Id = segmentId, Name = "Loyal" });
        db.Set<CustomerSegmentMembership>().Add(new CustomerSegmentMembership
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            CustomerSegmentId = segmentId
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new AssignCustomerSegmentHandler(
            db,
            new AssignCustomerSegmentValidator(),
            new TestStringLocalizer());

        var act = () => handler.HandleAsync(new AssignCustomerSegmentDto
        {
            CustomerId = customerId,
            CustomerSegmentId = segmentId
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CustomerAlreadyAssignedToSegment");
    }

    [Fact]
    public async Task AssignCustomerSegment_Should_Throw_WhenCustomerNotFound()
    {
        await using var db = CrmEngagementDbContext.Create();
        var segmentId = Guid.NewGuid();

        db.Set<CustomerSegment>().Add(new CustomerSegment { Id = segmentId, Name = "Loyal" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new AssignCustomerSegmentHandler(
            db,
            new AssignCustomerSegmentValidator(),
            new TestStringLocalizer());

        var act = () => handler.HandleAsync(new AssignCustomerSegmentDto
        {
            CustomerId = Guid.NewGuid(),
            CustomerSegmentId = segmentId
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CustomerNotFound");
    }

    [Fact]
    public async Task AssignCustomerSegment_Should_Throw_WhenSegmentNotFound()
    {
        await using var db = CrmEngagementDbContext.Create();
        var customerId = Guid.NewGuid();

        db.Set<Customer>().Add(MakeCustomer(customerId));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new AssignCustomerSegmentHandler(
            db,
            new AssignCustomerSegmentValidator(),
            new TestStringLocalizer());

        var act = () => handler.HandleAsync(new AssignCustomerSegmentDto
        {
            CustomerId = customerId,
            CustomerSegmentId = Guid.NewGuid()
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CustomerSegmentNotFound");
    }

    // ─── RemoveCustomerSegmentMembershipHandler ───────────────────────────────

    [Fact]
    public async Task RemoveCustomerSegmentMembership_Should_RemoveMembership_WhenFound()
    {
        await using var db = CrmEngagementDbContext.Create();
        var membershipId = Guid.NewGuid();

        db.Set<CustomerSegmentMembership>().Add(new CustomerSegmentMembership
        {
            Id = membershipId,
            CustomerId = Guid.NewGuid(),
            CustomerSegmentId = Guid.NewGuid()
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new RemoveCustomerSegmentMembershipHandler(db, new TestStringLocalizer());

        await handler.HandleAsync(membershipId, TestContext.Current.CancellationToken);

        var exists = await db.Set<CustomerSegmentMembership>()
            .AnyAsync(x => x.Id == membershipId, TestContext.Current.CancellationToken);
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveCustomerSegmentMembership_Should_Throw_WhenMembershipNotFound()
    {
        await using var db = CrmEngagementDbContext.Create();

        var handler = new RemoveCustomerSegmentMembershipHandler(db, new TestStringLocalizer());

        var act = () => handler.HandleAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CustomerSegmentMembershipNotFound");
    }

    // ─── Shared helpers ───────────────────────────────────────────────────────

    private static Customer MakeCustomer(Guid id) => new()
    {
        Id = id,
        FirstName = "Test",
        LastName = "User",
        Email = "test@example.com",
        Phone = "+4917012345"
    };

    private sealed class CrmEngagementDbContext : DbContext, IAppDbContext
    {
        private CrmEngagementDbContext(DbContextOptions<CrmEngagementDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static CrmEngagementDbContext Create()
        {
            var options = new DbContextOptionsBuilder<CrmEngagementDbContext>()
                .UseInMemoryDatabase($"darwin_crm_engagement_tests_{Guid.NewGuid()}")
                .Options;
            return new CrmEngagementDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Customer>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.FirstName).IsRequired();
                b.Property(x => x.LastName).IsRequired();
                b.Property(x => x.Email).IsRequired();
                b.Property(x => x.Phone).IsRequired();
                b.Property(x => x.RowVersion).IsRequired();
                b.Ignore(x => x.CustomerSegments);
                b.Ignore(x => x.Addresses);
                b.Ignore(x => x.Interactions);
                b.Ignore(x => x.Consents);
                b.Ignore(x => x.Opportunities);
                b.Ignore(x => x.Invoices);
            });

            modelBuilder.Entity<CustomerSegment>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Name).IsRequired();
                b.Property(x => x.RowVersion).IsRequired();
                b.Ignore(x => x.Memberships);
            });

            modelBuilder.Entity<CustomerSegmentMembership>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<Interaction>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<Consent>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<Lead>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.FirstName).IsRequired();
                b.Property(x => x.LastName).IsRequired();
                b.Property(x => x.Email).IsRequired();
                b.Property(x => x.Phone).IsRequired();
                b.Property(x => x.RowVersion).IsRequired();
                b.Ignore(x => x.Interactions);
            });

            modelBuilder.Entity<Opportunity>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Title).IsRequired();
                b.Property(x => x.RowVersion).IsRequired();
                b.Ignore(x => x.Items);
                b.Ignore(x => x.Interactions);
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
