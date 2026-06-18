using Darwin.Application;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.CRM.Commands;
using Darwin.Application.CRM.DTOs;
using Darwin.Application.CRM.Queries;
using Darwin.Application.CRM.Services;
using Darwin.Application.CRM.Validators;
using Darwin.Application.Foundation;
using Darwin.Application.Integration;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Foundation;
using Darwin.Domain.Entities.Integration;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.CRM;

public sealed class CrmFoundationPrimitiveIntegrationTests
{
    private static readonly DateTime FixedNow = new(2031, 2, 3, 10, 15, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CrmFoundationPrimitiveService_Should_WrapReferencesTimelineAndDocuments()
    {
        await using var db = CrmFoundationDbContext.Create();
        var foundation = CreateFoundation(db);
        var externalSystemId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        db.Set<ExternalSystem>().Add(new ExternalSystem
        {
            Id = externalSystemId,
            Code = "CRM-IMPORT",
            Name = "CRM Import",
            Kind = ExternalSystemKind.Crm,
            IsActive = true
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var reference = await foundation.UpsertExternalReferenceAsync(
            externalSystemId,
            CrmFoundationPrimitiveService.EntityTypes.Customer,
            customerId,
            ExternalReferenceKind.Primary,
            "  C-100  ",
            " Customer 100 ",
            SourceOfTruth.Shared,
            isPrimary: true,
            metadataJson: """{"source":"import"}""",
            ct: TestContext.Current.CancellationToken);

        reference.Succeeded.Should().BeTrue();

        var duplicate = await foundation.UpsertExternalReferenceAsync(
            externalSystemId,
            CrmFoundationPrimitiveService.EntityTypes.Customer,
            customerId,
            ExternalReferenceKind.Primary,
            "C-100",
            "Customer 100",
            SourceOfTruth.Shared,
            isPrimary: true,
            ct: TestContext.Current.CancellationToken);

        duplicate.Value.Should().Be(reference.Value);
        (await foundation.GetExternalReferencesAsync(CrmFoundationPrimitiveService.EntityTypes.Customer, customerId, TestContext.Current.CancellationToken))
            .Should().ContainSingle(x => x.ExternalId == "C-100" && x.IsPrimary);

        (await foundation.AddNoteAsync(CrmFoundationPrimitiveService.EntityTypes.Customer, customerId, "  Important account  ", ct: TestContext.Current.CancellationToken))
            .Succeeded.Should().BeTrue();
        (await foundation.AddActivityAsync(CrmFoundationPrimitiveService.EntityTypes.Customer, customerId, "crm.customer.reviewed", FixedNow, null, "Reviewed", metadataJson: """{"k":"v"}""", ct: TestContext.Current.CancellationToken))
            .Succeeded.Should().BeTrue();
        (await foundation.AddActivityAsync(CrmFoundationPrimitiveService.EntityTypes.Customer, customerId, "crm.customer.reviewed", FixedNow, null, "Reviewed", metadataJson: """{"k":"v"}""", ct: TestContext.Current.CancellationToken))
            .Succeeded.Should().BeTrue();
        (await foundation.RegisterDocumentAsync(
            CrmFoundationPrimitiveService.EntityTypes.Customer,
            customerId,
            DocumentRecordKind.Attachment,
            "Contract",
            "contract.pdf",
            "application/pdf",
            1234,
            null,
            "object-store",
            "crm",
            "customers/contract.pdf",
            ct: TestContext.Current.CancellationToken)).Succeeded.Should().BeTrue();

        (await foundation.GetNotesAsync(CrmFoundationPrimitiveService.EntityTypes.Customer, customerId, ct: TestContext.Current.CancellationToken))
            .Should().ContainSingle(x => x.Body == "Important account");
        (await foundation.GetActivitiesAsync(CrmFoundationPrimitiveService.EntityTypes.Customer, customerId, ct: TestContext.Current.CancellationToken))
            .Should().ContainSingle(x => x.ActivityType == "crm.customer.reviewed");
        (await foundation.GetDocumentsAsync(CrmFoundationPrimitiveService.EntityTypes.Customer, customerId, ct: TestContext.Current.CancellationToken))
            .Should().ContainSingle(x => x.FileName == "contract.pdf");
    }

    [Fact]
    public async Task ConvertLeadToCustomer_Should_RecordIdempotentFoundationEvidence()
    {
        await using var db = CrmFoundationDbContext.Create();
        var foundation = CreateFoundation(db);
        var leadId = Guid.NewGuid();
        var rowVersion = new byte[] { 1, 2, 3, 4 };

        db.Set<Lead>().Add(new Lead
        {
            Id = leadId,
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "ada@example.test",
            Phone = "+4917012345678",
            Status = LeadStatus.Qualified,
            RowVersion = rowVersion
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ConvertLeadToCustomerHandler(
            db,
            new ConvertLeadToCustomerValidator(),
            new TestStringLocalizer(),
            foundation);

        var customerId = await handler.HandleAsync(new ConvertLeadToCustomerDto
        {
            LeadId = leadId,
            RowVersion = rowVersion
        }, TestContext.Current.CancellationToken);
        await handler.HandleAsync(new ConvertLeadToCustomerDto
        {
            LeadId = leadId,
            RowVersion = rowVersion
        }, TestContext.Current.CancellationToken);

        customerId.Should().NotBeEmpty();
        (await db.Set<BusinessEvent>().CountAsync(TestContext.Current.CancellationToken)).Should().Be(1);
        (await db.Set<AuditTrail>().CountAsync(TestContext.Current.CancellationToken)).Should().Be(1);
        (await db.Set<Activity>().CountAsync(TestContext.Current.CancellationToken)).Should().Be(2);
    }

    [Fact]
    public async Task LeadOpportunityAndConsentLifecycle_Should_RecordFoundationEvidence()
    {
        await using var db = CrmFoundationDbContext.Create();
        var foundation = CreateFoundation(db);
        var customerId = Guid.NewGuid();
        var leadId = Guid.NewGuid();
        var opportunityId = Guid.NewGuid();

        db.Set<Customer>().Add(new Customer { Id = customerId, FirstName = "CRM", LastName = "Customer", Email = "crm@example.test" });
        db.Set<Lead>().Add(new Lead
        {
            Id = leadId,
            FirstName = "CRM",
            LastName = "Lead",
            Email = "lead@example.test",
            Phone = "+4917012345678",
            Status = LeadStatus.New,
            RowVersion = [1]
        });
        db.Set<Opportunity>().Add(new Opportunity
        {
            Id = opportunityId,
            CustomerId = customerId,
            Title = "Expansion",
            Stage = OpportunityStage.Qualification,
            RowVersion = [2]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var leadHandler = new UpdateLeadLifecycleHandler(db, new TestStringLocalizer(), new FixedClock(FixedNow), foundation);
        var leadResult = await leadHandler.HandleAsync(new UpdateLeadLifecycleDto
        {
            Id = leadId,
            RowVersion = [1],
            Action = "QUALIFY"
        }, TestContext.Current.CancellationToken);

        var opportunityHandler = new UpdateOpportunityLifecycleHandler(db, new FixedClock(FixedNow), new TestStringLocalizer(), foundation);
        var opportunityResult = await opportunityHandler.HandleAsync(new UpdateOpportunityLifecycleDto
        {
            Id = opportunityId,
            RowVersion = [2],
            Action = "CLOSEWON",
            CloseReason = "Signed"
        }, TestContext.Current.CancellationToken);

        var consentHandler = new CreateConsentHandler(db, new ConsentCreateValidator(new TestStringLocalizer()), new TestStringLocalizer(), new FixedClock(FixedNow), foundation);
        var consentId = await consentHandler.HandleAsync(new ConsentCreateDto
        {
            CustomerId = customerId,
            Type = ConsentType.MarketingEmail,
            Granted = true,
            Source = "web",
            PolicyVersion = "v1",
            EvidenceJson = """{"ip":"127.0.0.1"}"""
        }, TestContext.Current.CancellationToken);

        leadResult.Succeeded.Should().BeTrue();
        opportunityResult.Succeeded.Should().BeTrue();
        consentId.Should().NotBeEmpty();
        (await db.Set<BusinessEvent>().CountAsync(TestContext.Current.CancellationToken)).Should().Be(3);
        (await db.Set<AuditTrail>().CountAsync(TestContext.Current.CancellationToken)).Should().Be(3);
        (await db.Set<Activity>().CountAsync(TestContext.Current.CancellationToken)).Should().Be(3);
    }

    [Fact]
    public async Task GetCrmFoundationPanelHandler_Should_ReturnFoundationEvidence_WithStableOrdering()
    {
        await using var db = CrmFoundationDbContext.Create();
        var foundation = CreateFoundation(db);
        var handler = new GetCrmFoundationPanelHandler(foundation);
        var externalSystemId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        db.Set<ExternalSystem>().Add(new ExternalSystem
        {
            Id = externalSystemId,
            Code = "CRM-UI",
            Name = "CRM UI",
            Kind = ExternalSystemKind.Crm,
            IsActive = true
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        (await foundation.UpsertExternalReferenceAsync(
            externalSystemId,
            CrmFoundationPrimitiveService.EntityTypes.Customer,
            customerId,
            ExternalReferenceKind.Alternate,
            "ALT-1",
            null,
            SourceOfTruth.External,
            ct: TestContext.Current.CancellationToken)).Succeeded.Should().BeTrue();
        (await foundation.AddNoteAsync(CrmFoundationPrimitiveService.EntityTypes.Customer, customerId, "First note", ct: TestContext.Current.CancellationToken))
            .Succeeded.Should().BeTrue();
        (await foundation.AddActivityAsync(
            CrmFoundationPrimitiveService.EntityTypes.Customer,
            customerId,
            "crm.customer.reviewed",
            FixedNow.AddMinutes(2),
            null,
            "Reviewed",
            ct: TestContext.Current.CancellationToken)).Succeeded.Should().BeTrue();
        (await foundation.RegisterDocumentAsync(
            CrmFoundationPrimitiveService.EntityTypes.Customer,
            customerId,
            DocumentRecordKind.Attachment,
            "Profile document",
            "profile.pdf",
            "application/pdf",
            10,
            null,
            "object-store",
            "crm",
            "profile.pdf",
            ct: TestContext.Current.CancellationToken)).Succeeded.Should().BeTrue();
        (await foundation.RecordLifecycleEventAsync(
            CrmFoundationPrimitiveService.EntityTypes.Customer,
            customerId,
            "crm.customer.reviewed",
            $"crm.customer.reviewed:{customerId:N}",
            FixedNow.AddMinutes(3),
            null,
            "Reviewed",
            ct: TestContext.Current.CancellationToken)).Succeeded.Should().BeTrue();

        var result = await handler.HandleAsync(" customer ", customerId, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.EntityType.Should().Be(CrmFoundationPrimitiveService.EntityTypes.Customer);
        result.Value.ExternalReferences.Should().ContainSingle(x => x.ExternalId == "ALT-1");
        result.Value.Notes.Should().ContainSingle(x => x.Body == "First note");
        result.Value.Activities.Should().ContainSingle(x => x.Title == "Reviewed");
        result.Value.Documents.Should().ContainSingle(x => x.FileName == "profile.pdf");
        result.Value.Events.Should().ContainSingle(x => x.EventType == "crm.customer.reviewed");
        result.Value.AuditTrail.Should().ContainSingle(x => x.CorrelationId == $"crm.customer.reviewed:{customerId:N}");
    }

    [Fact]
    public async Task CrmFoundationPanelHandlers_Should_RejectUnsupportedEntityAndAddInternalNotesOnlyForWhitelist()
    {
        await using var db = CrmFoundationDbContext.Create();
        var foundation = CreateFoundation(db);
        var panelHandler = new GetCrmFoundationPanelHandler(foundation);
        var noteHandler = new AddCrmFoundationNoteHandler(foundation);
        var customerId = Guid.NewGuid();

        (await panelHandler.HandleAsync("Invoice", customerId, TestContext.Current.CancellationToken))
            .Succeeded.Should().BeFalse();
        (await panelHandler.HandleAsync("Customer", Guid.Empty, TestContext.Current.CancellationToken))
            .Succeeded.Should().BeFalse();

        var invalidNote = await noteHandler.HandleAsync(
            new AddCrmFoundationNoteCommand("Customer", customerId, "  "),
            TestContext.Current.CancellationToken);
        invalidNote.Succeeded.Should().BeFalse();

        var unsupportedNote = await noteHandler.HandleAsync(
            new AddCrmFoundationNoteCommand("Invoice", customerId, "Internal note"),
            TestContext.Current.CancellationToken);
        unsupportedNote.Succeeded.Should().BeFalse();

        var note = await noteHandler.HandleAsync(
            new AddCrmFoundationNoteCommand("Lead", customerId, "  Internal note  "),
            TestContext.Current.CancellationToken);

        note.Succeeded.Should().BeTrue();
        var notes = await foundation.GetNotesAsync(
            CrmFoundationPrimitiveService.EntityTypes.Lead,
            customerId,
            FoundationVisibility.Internal,
            TestContext.Current.CancellationToken);
        notes.Should().ContainSingle(x => x.Body == "Internal note" && x.Visibility == FoundationVisibility.Internal);
    }

    private static CrmFoundationPrimitiveService CreateFoundation(CrmFoundationDbContext db)
    {
        var clock = new FixedClock(FixedNow);
        return new CrmFoundationPrimitiveService(
            db,
            new ExternalSystemReferenceService(db),
            new EntityTimelineService(db),
            new DocumentRecordService(db),
            new BusinessEventService(db, clock));
    }

    private sealed class CrmFoundationDbContext : DbContext, IAppDbContext
    {
        private CrmFoundationDbContext(DbContextOptions<CrmFoundationDbContext> options)
            : base(options)
        {
        }

        public static CrmFoundationDbContext Create()
        {
            var options = new DbContextOptionsBuilder<CrmFoundationDbContext>()
                .UseInMemoryDatabase($"darwin_crm_foundation_tests_{Guid.NewGuid()}")
                .Options;
            return new CrmFoundationDbContext(options);
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Lead> Leads => Set<Lead>();
        public DbSet<Opportunity> Opportunities => Set<Opportunity>();
        public DbSet<Consent> Consents => Set<Consent>();
        public DbSet<ExternalSystem> ExternalSystems => Set<ExternalSystem>();
        public DbSet<ExternalReference> ExternalReferences => Set<ExternalReference>();
        public DbSet<Activity> Activities => Set<Activity>();
        public DbSet<Note> Notes => Set<Note>();
        public DbSet<DocumentRecord> DocumentRecords => Set<DocumentRecord>();
        public DbSet<BusinessEvent> BusinessEvents => Set<BusinessEvent>();
        public DbSet<AuditTrail> AuditTrails => Set<AuditTrail>();
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTime utcNow) => UtcNow = utcNow;

        public DateTime UtcNow { get; }
    }

    private sealed class TestStringLocalizer : IStringLocalizer<ValidationResource>
    {
        public LocalizedString this[string name] => new(name, name, resourceNotFound: false);

        public LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(name, arguments), resourceNotFound: false);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            Array.Empty<LocalizedString>();
    }
}
