using Darwin.Application;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.CRM.Commands;
using Darwin.Application.CRM.DTOs;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.CRM;

/// <summary>
/// Unit tests for <see cref="UpdateLeadLifecycleHandler"/> and
/// <see cref="UpdateOpportunityLifecycleHandler"/>, verifying lifecycle transitions,
/// RowVersion guards, and closed-opportunity advancement rejection.
/// </summary>
public sealed class CrmPipelineLifecycleHandlerTests
{
    // ─── UpdateLeadLifecycleHandler ───────────────────────────────────────────

    [Fact]
    public async Task UpdateLeadLifecycle_Should_ReturnFail_WhenIdIsEmpty()
    {
        await using var db = PipelineTestDbContext.Create();
        var handler = new UpdateLeadLifecycleHandler(db, Loc());

        var result = await handler.HandleAsync(new UpdateLeadLifecycleDto
        {
            Id = Guid.Empty,
            RowVersion = [1],
            Action = "QUALIFY"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("empty Id must be rejected");
    }

    [Fact]
    public async Task UpdateLeadLifecycle_Should_ReturnFail_WhenRowVersionIsEmpty()
    {
        await using var db = PipelineTestDbContext.Create();
        var handler = new UpdateLeadLifecycleHandler(db, Loc());

        var result = await handler.HandleAsync(new UpdateLeadLifecycleDto
        {
            Id = Guid.NewGuid(),
            RowVersion = [],
            Action = "QUALIFY"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("empty RowVersion must be rejected");
    }

    [Fact]
    public async Task UpdateLeadLifecycle_Should_ReturnFail_WhenLeadNotFound()
    {
        await using var db = PipelineTestDbContext.Create();
        var handler = new UpdateLeadLifecycleHandler(db, Loc());

        var result = await handler.HandleAsync(new UpdateLeadLifecycleDto
        {
            Id = Guid.NewGuid(),
            RowVersion = [1],
            Action = "QUALIFY"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("missing lead must return not-found failure");
    }

    [Fact]
    public async Task UpdateLeadLifecycle_Should_ReturnFail_WhenRowVersionIsStale()
    {
        await using var db = PipelineTestDbContext.Create();
        var lead = MakeLead(LeadStatus.New);
        db.Set<Lead>().Add(lead);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateLeadLifecycleHandler(db, Loc());

        var result = await handler.HandleAsync(new UpdateLeadLifecycleDto
        {
            Id = lead.Id,
            RowVersion = [9, 9, 9],   // stale
            Action = "QUALIFY"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("stale RowVersion must be rejected");
    }

    [Fact]
    public async Task UpdateLeadLifecycle_Should_ReturnFail_WhenActionIsUnsupported()
    {
        await using var db = PipelineTestDbContext.Create();
        var lead = MakeLead(LeadStatus.New);
        db.Set<Lead>().Add(lead);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateLeadLifecycleHandler(db, Loc());

        var result = await handler.HandleAsync(new UpdateLeadLifecycleDto
        {
            Id = lead.Id,
            RowVersion = lead.RowVersion,
            Action = "INVALIDACTION"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("unknown action must return unsupported-action failure");
    }

    [Theory]
    [InlineData("QUALIFY", LeadStatus.Qualified)]
    [InlineData("qualify", LeadStatus.Qualified)]    // case-insensitive
    [InlineData("DISQUALIFY", LeadStatus.Disqualified)]
    [InlineData("REOPEN", LeadStatus.New)]
    public async Task UpdateLeadLifecycle_Should_TransitionStatus_WhenActionIsValid(
        string action, LeadStatus expectedStatus)
    {
        await using var db = PipelineTestDbContext.Create();
        var lead = MakeLead(LeadStatus.New);
        db.Set<Lead>().Add(lead);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateLeadLifecycleHandler(db, Loc());

        var result = await handler.HandleAsync(new UpdateLeadLifecycleDto
        {
            Id = lead.Id,
            RowVersion = lead.RowVersion,
            Action = action
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue($"action '{action}' should succeed from New status");
        var updated = await db.Set<Lead>().SingleAsync(x => x.Id == lead.Id, TestContext.Current.CancellationToken);
        updated.Status.Should().Be(expectedStatus);
    }

    [Theory]
    [InlineData("QUALIFY")]
    [InlineData("DISQUALIFY")]
    [InlineData("REOPEN")]
    public async Task UpdateLeadLifecycle_Should_ReturnFail_WhenLeadIsConverted(string action)
    {
        await using var db = PipelineTestDbContext.Create();
        var lead = MakeLead(LeadStatus.Converted);
        db.Set<Lead>().Add(lead);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateLeadLifecycleHandler(db, Loc());

        var result = await handler.HandleAsync(new UpdateLeadLifecycleDto
        {
            Id = lead.Id,
            RowVersion = lead.RowVersion,
            Action = action
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("converted leads must not be modified by lifecycle actions");
    }

    // ─── UpdateOpportunityLifecycleHandler ────────────────────────────────────

    [Fact]
    public async Task UpdateOpportunityLifecycle_Should_ReturnFail_WhenIdIsEmpty()
    {
        await using var db = PipelineTestDbContext.Create();
        var handler = new UpdateOpportunityLifecycleHandler(db, new FixedClock(DateTime.UtcNow), Loc());

        var result = await handler.HandleAsync(new UpdateOpportunityLifecycleDto
        {
            Id = Guid.Empty,
            RowVersion = [1],
            Action = "ADVANCE"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("empty Id must be rejected");
    }

    [Fact]
    public async Task UpdateOpportunityLifecycle_Should_ReturnFail_WhenRowVersionIsEmpty()
    {
        await using var db = PipelineTestDbContext.Create();
        var handler = new UpdateOpportunityLifecycleHandler(db, new FixedClock(DateTime.UtcNow), Loc());

        var result = await handler.HandleAsync(new UpdateOpportunityLifecycleDto
        {
            Id = Guid.NewGuid(),
            RowVersion = [],
            Action = "ADVANCE"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("empty RowVersion must be rejected");
    }

    [Fact]
    public async Task UpdateOpportunityLifecycle_Should_ReturnFail_WhenOpportunityNotFound()
    {
        await using var db = PipelineTestDbContext.Create();
        var handler = new UpdateOpportunityLifecycleHandler(db, new FixedClock(DateTime.UtcNow), Loc());

        var result = await handler.HandleAsync(new UpdateOpportunityLifecycleDto
        {
            Id = Guid.NewGuid(),
            RowVersion = [1],
            Action = "ADVANCE"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("missing opportunity must return not-found failure");
    }

    [Fact]
    public async Task UpdateOpportunityLifecycle_Should_ReturnFail_WhenRowVersionIsStale()
    {
        await using var db = PipelineTestDbContext.Create();
        var opp = MakeOpportunity(OpportunityStage.Qualification);
        db.Set<Opportunity>().Add(opp);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateOpportunityLifecycleHandler(db, new FixedClock(DateTime.UtcNow), Loc());

        var result = await handler.HandleAsync(new UpdateOpportunityLifecycleDto
        {
            Id = opp.Id,
            RowVersion = [9, 9, 9],  // stale
            Action = "ADVANCE"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("stale RowVersion must be rejected");
    }

    [Fact]
    public async Task UpdateOpportunityLifecycle_Should_ReturnFail_WhenClosedWonAndAdvance()
    {
        await using var db = PipelineTestDbContext.Create();
        var opp = MakeOpportunity(OpportunityStage.ClosedWon);
        db.Set<Opportunity>().Add(opp);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateOpportunityLifecycleHandler(db, new FixedClock(DateTime.UtcNow), Loc());

        var result = await handler.HandleAsync(new UpdateOpportunityLifecycleDto
        {
            Id = opp.Id,
            RowVersion = opp.RowVersion,
            Action = "ADVANCE"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("closed (won) opportunities cannot be advanced");
    }

    [Fact]
    public async Task UpdateOpportunityLifecycle_Should_ReturnFail_WhenClosedLostAndAdvance()
    {
        await using var db = PipelineTestDbContext.Create();
        var opp = MakeOpportunity(OpportunityStage.ClosedLost);
        db.Set<Opportunity>().Add(opp);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateOpportunityLifecycleHandler(db, new FixedClock(DateTime.UtcNow), Loc());

        var result = await handler.HandleAsync(new UpdateOpportunityLifecycleDto
        {
            Id = opp.Id,
            RowVersion = opp.RowVersion,
            Action = "ADVANCE"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("closed (lost) opportunities cannot be advanced");
    }

    [Theory]
    [InlineData(OpportunityStage.Qualification, "ADVANCE", OpportunityStage.Proposal)]
    [InlineData(OpportunityStage.Proposal, "ADVANCE", OpportunityStage.Negotiation)]
    [InlineData(OpportunityStage.Negotiation, "ADVANCE", OpportunityStage.ClosedWon)]
    [InlineData(OpportunityStage.Qualification, "CLOSEWON", OpportunityStage.ClosedWon)]
    [InlineData(OpportunityStage.Proposal, "CLOSELOST", OpportunityStage.ClosedLost)]
    [InlineData(OpportunityStage.ClosedWon, "REOPEN", OpportunityStage.Qualification)]
    [InlineData(OpportunityStage.ClosedLost, "REOPEN", OpportunityStage.Qualification)]
    public async Task UpdateOpportunityLifecycle_Should_TransitionStage_WhenActionIsValid(
        OpportunityStage startStage, string action, OpportunityStage expectedStage)
    {
        await using var db = PipelineTestDbContext.Create();
        var opp = MakeOpportunity(startStage);
        db.Set<Opportunity>().Add(opp);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateOpportunityLifecycleHandler(db, new FixedClock(DateTime.UtcNow), Loc());

        var result = await handler.HandleAsync(new UpdateOpportunityLifecycleDto
        {
            Id = opp.Id,
            RowVersion = opp.RowVersion,
            Action = action
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue($"action '{action}' from {startStage} should succeed");
        var updated = await db.Set<Opportunity>().SingleAsync(x => x.Id == opp.Id, TestContext.Current.CancellationToken);
        updated.Stage.Should().Be(expectedStage);
    }

    [Fact]
    public async Task UpdateOpportunityLifecycle_Should_SetExpectedCloseDateUtc_WhenCloseWon()
    {
        await using var db = PipelineTestDbContext.Create();
        var now = new DateTime(2026, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var opp = MakeOpportunity(OpportunityStage.Qualification);
        db.Set<Opportunity>().Add(opp);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateOpportunityLifecycleHandler(db, new FixedClock(now), Loc());

        await handler.HandleAsync(new UpdateOpportunityLifecycleDto
        {
            Id = opp.Id,
            RowVersion = opp.RowVersion,
            Action = "CLOSEWON"
        }, TestContext.Current.CancellationToken);

        var updated = await db.Set<Opportunity>().SingleAsync(x => x.Id == opp.Id, TestContext.Current.CancellationToken);
        updated.ExpectedCloseDateUtc.Should().Be(now.Date, "close date must default to clock.UtcNow.Date when not already set");
    }

    [Fact]
    public async Task UpdateOpportunityLifecycle_Should_PreserveExistingCloseDateUtc_WhenCloseWon()
    {
        await using var db = PipelineTestDbContext.Create();
        var existingCloseDate = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var opp = MakeOpportunity(OpportunityStage.Qualification);
        opp.ExpectedCloseDateUtc = existingCloseDate;
        db.Set<Opportunity>().Add(opp);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateOpportunityLifecycleHandler(
            db, new FixedClock(new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc)), Loc());

        await handler.HandleAsync(new UpdateOpportunityLifecycleDto
        {
            Id = opp.Id,
            RowVersion = opp.RowVersion,
            Action = "CLOSEWON"
        }, TestContext.Current.CancellationToken);

        var updated = await db.Set<Opportunity>().SingleAsync(x => x.Id == opp.Id, TestContext.Current.CancellationToken);
        updated.ExpectedCloseDateUtc.Should().Be(existingCloseDate, "existing close date must not be overwritten");
    }

    [Fact]
    public async Task UpdateOpportunityLifecycle_Should_ReturnFail_WhenActionIsUnsupported()
    {
        await using var db = PipelineTestDbContext.Create();
        var opp = MakeOpportunity(OpportunityStage.Qualification);
        db.Set<Opportunity>().Add(opp);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateOpportunityLifecycleHandler(db, new FixedClock(DateTime.UtcNow), Loc());

        var result = await handler.HandleAsync(new UpdateOpportunityLifecycleDto
        {
            Id = opp.Id,
            RowVersion = opp.RowVersion,
            Action = "UNKNOWN"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("unknown action must return unsupported-action failure");
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static Lead MakeLead(LeadStatus status)
    {
        return new Lead
        {
            FirstName = "Test",
            LastName = "Lead",
            Email = "lead@test.com",
            Status = status,
            RowVersion = [1, 2, 3]
        };
    }

    private static Opportunity MakeOpportunity(OpportunityStage stage)
    {
        return new Opportunity
        {
            CustomerId = Guid.NewGuid(),
            Title = "Test Opportunity",
            Stage = stage,
            RowVersion = [1, 2, 3]
        };
    }

    private static IStringLocalizer<ValidationResource> Loc()
    {
        var mock = new Moq.Mock<IStringLocalizer<ValidationResource>>();
        mock.Setup(l => l[Moq.It.IsAny<string>()])
            .Returns<string>(n => new LocalizedString(n, n));
        mock.Setup(l => l[Moq.It.IsAny<string>(), Moq.It.IsAny<object[]>()])
            .Returns<string, object[]>((n, _) => new LocalizedString(n, n));
        return mock.Object;
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTime utcNow) => UtcNow = utcNow;
        public DateTime UtcNow { get; }
    }

    private sealed class PipelineTestDbContext : DbContext, IAppDbContext
    {
        private PipelineTestDbContext(DbContextOptions<PipelineTestDbContext> opts)
            : base(opts)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static PipelineTestDbContext Create()
        {
            var opts = new DbContextOptionsBuilder<PipelineTestDbContext>()
                .UseInMemoryDatabase($"darwin_crm_pipeline_tests_{Guid.NewGuid()}")
                .Options;
            return new PipelineTestDbContext(opts);
        }

        protected override void OnModelCreating(ModelBuilder mb)
        {
            base.OnModelCreating(mb);
            mb.Ignore<GeoCoordinate>();

            mb.Entity<Lead>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.FirstName).IsRequired();
                b.Property(x => x.LastName).IsRequired();
                b.Property(x => x.Email).IsRequired();
                b.Property(x => x.RowVersion).IsRequired();
                b.Ignore(x => x.Interactions);
            });

            mb.Entity<Opportunity>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Title).IsRequired();
                b.Property(x => x.RowVersion).IsRequired();
                b.Ignore(x => x.Interactions);
            });
        }
    }
}
