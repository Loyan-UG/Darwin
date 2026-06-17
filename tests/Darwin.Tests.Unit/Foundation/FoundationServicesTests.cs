using Darwin.Application;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Foundation;
using Darwin.Application.Orders.Commands;
using Darwin.Application.Orders.DTOs;
using Darwin.Application.Orders.Validators;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Foundation;
using Darwin.Domain.Entities.HumanResources;
using Darwin.Domain.Entities.Inventory;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Entities.Sales;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.Text.Json;

namespace Darwin.Tests.Unit.Foundation;

public sealed class FoundationServicesTests
{
    private static readonly DateTime FixedNow = new(2030, 4, 30, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CustomFieldService_Should_NormalizeDefinitionKey_And_RejectDuplicate()
    {
        await using var db = FoundationTestDbContext.Create();
        var service = new CustomFieldService(db);

        var created = await service.CreateDefinitionAsync(new CreateCustomFieldDefinitionCommand(
            Guid.NewGuid(),
            " Customer ",
            " Favorite Color ",
            " Favorite color ",
            CustomFieldDataType.Text));
        var duplicate = await service.CreateDefinitionAsync(new CreateCustomFieldDefinitionCommand(
            db.Set<CustomFieldDefinition>().Single().BusinessId,
            "Customer",
            "favorite color",
            "Favorite color duplicate",
            CustomFieldDataType.Text));

        created.Succeeded.Should().BeTrue();
        duplicate.Succeeded.Should().BeFalse();

        var definition = await db.Set<CustomFieldDefinition>().SingleAsync();
        definition.TargetEntityType.Should().Be("Customer");
        definition.Key.Should().Be("favorite color");
        definition.Label.Should().Be("Favorite color");
        definition.ValidationJson.Should().Be("{}");
        definition.MetadataJson.Should().Be("{}");
    }

    [Fact]
    public async Task CustomFieldService_Should_UpsertTypedValue_And_QueryOnlyActiveDefinitionsForEntity()
    {
        await using var db = FoundationTestDbContext.Create();
        var service = new CustomFieldService(db);
        var customerId = Guid.NewGuid();
        var otherCustomerId = Guid.NewGuid();
        var colorDefinitionId = (await service.CreateDefinitionAsync(new CreateCustomFieldDefinitionCommand(
            null,
            "Customer",
            "favorite-color",
            "Favorite color",
            CustomFieldDataType.Text,
            IsRequired: true,
            Visibility: FoundationVisibility.Staff))).Value;
        var inactiveDefinitionId = (await service.CreateDefinitionAsync(new CreateCustomFieldDefinitionCommand(
            null,
            "Customer",
            "inactive-note",
            "Inactive note",
            CustomFieldDataType.Text,
            IsActive: false))).Value;

        var missingRequired = await service.UpsertValueAsync(new UpsertCustomFieldValueCommand(
            colorDefinitionId,
            "Customer",
            customerId));
        var inactive = await service.UpsertValueAsync(new UpsertCustomFieldValueCommand(
            inactiveDefinitionId,
            "Customer",
            customerId,
            StringValue: "ignored"));
        var first = await service.UpsertValueAsync(new UpsertCustomFieldValueCommand(
            colorDefinitionId,
            " Customer ",
            customerId,
            StringValue: " Blue ",
            MetadataJson: "{\"source\":\"unit\"}"));
        var updated = await service.UpsertValueAsync(new UpsertCustomFieldValueCommand(
            colorDefinitionId,
            "Customer",
            customerId,
            StringValue: "Green"));
        await service.UpsertValueAsync(new UpsertCustomFieldValueCommand(
            colorDefinitionId,
            "Customer",
            otherCustomerId,
            StringValue: "Red"));

        missingRequired.Succeeded.Should().BeFalse();
        inactive.Succeeded.Should().BeFalse();
        first.Succeeded.Should().BeTrue();
        updated.Value.Should().Be(first.Value);

        var values = await service.GetValuesForEntityAsync(" Customer ", customerId);
        values.Should().ContainSingle();
        values[0].StringValue.Should().Be("Green");
        values[0].Visibility.Should().Be(FoundationVisibility.Staff);
        values[0].MetadataJson.Should().Be("{}");
    }

    [Fact]
    public async Task CustomFieldService_Should_RejectSensitiveCustomFieldKeysAndValues()
    {
        await using var db = FoundationTestDbContext.Create();
        var service = new CustomFieldService(db);

        var secretDefinition = await service.CreateDefinitionAsync(new CreateCustomFieldDefinitionCommand(
            null,
            "Customer",
            "api-token",
            "API token",
            CustomFieldDataType.Text));
        var definitionId = (await service.CreateDefinitionAsync(new CreateCustomFieldDefinitionCommand(
            null,
            "Customer",
            "public-note",
            "Public note",
            CustomFieldDataType.Text))).Value;
        var secretValue = await service.UpsertValueAsync(new UpsertCustomFieldValueCommand(
            definitionId,
            "Customer",
            Guid.NewGuid(),
            StringValue: "secret-token-value"));

        secretDefinition.Succeeded.Should().BeFalse();
        secretValue.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task EntityTimelineService_Should_AddAndQueryActivitiesAndNotes_WithVisibilityOrdering()
    {
        await using var db = FoundationTestDbContext.Create();
        var service = new EntityTimelineService(db);
        var entityId = Guid.NewGuid();

        await service.AddActivityAsync(new AddActivityCommand(
            "Customer",
            entityId,
            "status-change",
            new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc),
            null,
            "Internal status",
            Visibility: FoundationVisibility.Internal));
        await service.AddActivityAsync(new AddActivityCommand(
            "Customer",
            entityId,
            "member-visible",
            new DateTime(2026, 1, 2, 10, 0, 0, DateTimeKind.Utc),
            null,
            "Member status",
            Visibility: FoundationVisibility.Member));
        await service.AddNoteAsync(new AddNoteCommand(
            "Customer",
            entityId,
            "Regular note",
            Visibility: FoundationVisibility.Internal));
        await service.AddNoteAsync(new AddNoteCommand(
            "Customer",
            entityId,
            "Pinned staff note",
            Visibility: FoundationVisibility.Staff,
            IsPinned: true));

        var staffActivities = await service.GetActivitiesForEntityAsync("Customer", entityId, FoundationVisibility.Staff);
        var memberActivities = await service.GetActivitiesForEntityAsync("Customer", entityId, FoundationVisibility.Member);
        var notes = await service.GetNotesForEntityAsync(" Customer ", entityId, FoundationVisibility.Staff);

        staffActivities.Should().ContainSingle();
        staffActivities[0].Title.Should().Be("Internal status");
        memberActivities.Should().HaveCount(2);
        memberActivities[0].Title.Should().Be("Member status");
        notes.Should().HaveCount(2);
        notes[0].Body.Should().Be("Pinned staff note");
    }

    [Fact]
    public async Task DocumentRecordService_Should_RegisterMetadataOnly_And_FilterByEntityAndVisibility()
    {
        await using var db = FoundationTestDbContext.Create();
        var service = new DocumentRecordService(db);
        var entityId = Guid.NewGuid();

        var registered = await service.RegisterDocumentAsync(new RegisterDocumentRecordCommand(
            " Invoice ",
            entityId,
            DocumentRecordKind.InvoiceArtifact,
            " Issued invoice ",
            "invoice.pdf",
            "application/pdf",
            128,
            "hash-1",
            "ObjectStorage",
            "invoices",
            "issued/invoice.pdf",
            Visibility: FoundationVisibility.Member,
            MetadataJson: "{\"source\":\"unit\"}"));
        await service.RegisterDocumentAsync(new RegisterDocumentRecordCommand(
            "Invoice",
            Guid.NewGuid(),
            DocumentRecordKind.InvoiceArtifact,
            "Other invoice",
            "other.pdf",
            "application/pdf",
            128,
            null,
            "ObjectStorage",
            "invoices",
            "issued/other.pdf",
            Visibility: FoundationVisibility.Member));
        var negativeSize = await service.RegisterDocumentAsync(new RegisterDocumentRecordCommand(
            "Invoice",
            entityId,
            DocumentRecordKind.InvoiceArtifact,
            "Bad invoice",
            "bad.pdf",
            "application/pdf",
            -1,
            null,
            "ObjectStorage",
            "invoices",
            "issued/bad.pdf"));
        var sensitivePath = await service.RegisterDocumentAsync(new RegisterDocumentRecordCommand(
            "Invoice",
            entityId,
            DocumentRecordKind.InvoiceArtifact,
            "Secret invoice",
            "invoice.pdf",
            "application/pdf",
            128,
            null,
            "ObjectStorage",
            "invoices",
            "token/invoice.pdf"));

        registered.Succeeded.Should().BeTrue();
        negativeSize.Succeeded.Should().BeFalse();
        sensitivePath.Succeeded.Should().BeFalse();

        var documents = await service.GetDocumentsForEntityAsync("Invoice", entityId, FoundationVisibility.Member);
        documents.Should().ContainSingle();
        documents[0].Title.Should().Be("Issued invoice");
        documents[0].StorageKey.Should().Be("issued/invoice.pdf");
        documents[0].MetadataJson.Should().Be("{\"source\":\"unit\"}");
    }

    [Fact]
    public async Task NumberSequenceService_Should_PreviewWithoutIncrement_AndReserveWithPadding()
    {
        await using var db = FoundationTestDbContext.Create();
        var clock = new MutableClock(FixedNow);
        var service = new NumberSequenceService(db, clock);
        var sequenceId = (await service.CreateSequenceAsync(new CreateNumberSequenceCommand(
            null,
            NumberSequenceDocumentType.Order,
            NumberSequenceService.GlobalScopeKey,
            "D-{yyyy}{MM}{dd}-{seq}",
            7,
            5,
            NumberSequenceResetPolicy.Never))).Value;

        var preview = await service.PreviewAsync(new NumberSequenceRequest(null, NumberSequenceDocumentType.Order, NumberSequenceService.GlobalScopeKey));
        var reserved = await service.ReserveNextAsync(new NumberSequenceRequest(null, NumberSequenceDocumentType.Order, NumberSequenceService.GlobalScopeKey));
        var nextPreview = await service.PreviewAsync(new NumberSequenceRequest(null, NumberSequenceDocumentType.Order, NumberSequenceService.GlobalScopeKey));

        preview.Value.Should().Be("D-20300430-00007");
        reserved.Value.Should().Be("D-20300430-00007");
        nextPreview.Value.Should().Be("D-20300430-00008");
        (await db.Set<NumberSequence>().FindAsync([sequenceId], TestContext.Current.CancellationToken))!
            .NextValue.Should().Be(8);
    }

    [Fact]
    public async Task BusinessEventService_Should_AddAndQueryEvents_WithVisibilityOrdering()
    {
        await using var db = FoundationTestDbContext.Create();
        var service = new BusinessEventService(db, new MutableClock(FixedNow));
        var entityId = Guid.NewGuid();

        await service.AddEventAsync(new AddBusinessEventCommand(
            Guid.NewGuid(),
            " Customer ",
            entityId,
            "profile.updated",
            null,
            new DateTime(2030, 4, 29, 9, 0, 0, DateTimeKind.Utc),
            null,
            BusinessEventSource.User,
            BusinessEventSeverity.Info,
            FoundationVisibility.Internal,
            " Internal update ",
            PayloadJson: "{\"field\":\"name\"}"));
        await service.AddEventAsync(new AddBusinessEventCommand(
            null,
            "Customer",
            entityId,
            "profile.visible",
            null,
            new DateTime(2030, 4, 30, 9, 0, 0, DateTimeKind.Utc),
            null,
            BusinessEventSource.System,
            BusinessEventSeverity.Warning,
            FoundationVisibility.Member,
            "Member update"));

        var staffEvents = await service.GetEventsForEntityAsync("Customer", entityId, FoundationVisibility.Staff);
        var memberEvents = await service.GetEventsForEntityAsync("Customer", entityId, FoundationVisibility.Member);

        staffEvents.Should().ContainSingle();
        staffEvents[0].Title.Should().Be("Internal update");
        staffEvents[0].PayloadJson.Should().Be("{\"field\":\"name\"}");
        memberEvents.Should().HaveCount(2);
        memberEvents[0].Title.Should().Be("Member update");
    }

    [Fact]
    public async Task BusinessEventService_Should_UseEventKeyIdempotently_And_QueryCorrelation()
    {
        await using var db = FoundationTestDbContext.Create();
        var service = new BusinessEventService(db, new MutableClock(FixedNow));
        var entityId = Guid.NewGuid();

        var first = await service.AddEventAsync(new AddBusinessEventCommand(
            null,
            "Order",
            entityId,
            "order.placed",
            " order-placed-1 ",
            default,
            null,
            BusinessEventSource.System,
            BusinessEventSeverity.Info,
            FoundationVisibility.Staff,
            "Order placed",
            CorrelationId: " checkout-1 "));
        var duplicate = await service.AddEventAsync(new AddBusinessEventCommand(
            null,
            "Order",
            entityId,
            "order.placed",
            "order-placed-1",
            default,
            null,
            BusinessEventSource.System,
            BusinessEventSeverity.Info,
            FoundationVisibility.Staff,
            "Duplicate order placed",
            CorrelationId: "checkout-1"));

        duplicate.Value.Should().Be(first.Value);
        db.Set<BusinessEvent>().Should().ContainSingle();
        var correlated = await service.GetEventsByCorrelationAsync(" checkout-1 ", FoundationVisibility.Staff);
        correlated.Should().ContainSingle();
        correlated[0].OccurredAtUtc.Should().Be(FixedNow);
    }

    [Fact]
    public async Task BusinessEventService_Should_AddAndQueryAuditTrail_ForEntity()
    {
        await using var db = FoundationTestDbContext.Create();
        var service = new BusinessEventService(db, new MutableClock(FixedNow));
        var entityId = Guid.NewGuid();
        var eventId = (await service.AddEventAsync(new AddBusinessEventCommand(
            null,
            "Customer",
            entityId,
            "customer.updated",
            null,
            FixedNow,
            Guid.NewGuid(),
            BusinessEventSource.User,
            BusinessEventSeverity.Info,
            FoundationVisibility.Internal,
            "Customer updated"))).Value;

        var audit = await service.AddAuditTrailAsync(new AddAuditTrailCommand(
            null,
            " Customer ",
            entityId,
            AuditTrailAction.Updated,
            default,
            BusinessEventId: eventId,
            Reason: " Data correction ",
            CorrelationId: "case-1",
            ChangeSetJson: "{\"name\":{\"from\":\"A\",\"to\":\"B\"}}"));

        audit.Succeeded.Should().BeTrue();
        var audits = await service.GetAuditTrailForEntityAsync("Customer", entityId);
        audits.Should().ContainSingle();
        audits[0].BusinessEventId.Should().Be(eventId);
        audits[0].Reason.Should().Be("Data correction");
        audits[0].OccurredAtUtc.Should().Be(FixedNow);
    }

    [Fact]
    public async Task BusinessEventService_Should_RejectInvalidAndSensitiveInputs()
    {
        await using var db = FoundationTestDbContext.Create();
        var service = new BusinessEventService(db, new MutableClock(FixedNow));

        var missingEventType = await service.AddEventAsync(new AddBusinessEventCommand(
            null,
            "Customer",
            Guid.NewGuid(),
            "",
            null,
            default,
            null,
            BusinessEventSource.System,
            BusinessEventSeverity.Info,
            FoundationVisibility.Internal,
            "Title"));
        var sensitiveEvent = await service.AddEventAsync(new AddBusinessEventCommand(
            null,
            "Customer",
            Guid.NewGuid(),
            "customer.updated",
            null,
            default,
            null,
            BusinessEventSource.System,
            BusinessEventSeverity.Info,
            FoundationVisibility.Internal,
            "Title",
            PayloadJson: "{\"refreshToken\":\"secret\"}"));
        var missingAuditEntity = await service.AddAuditTrailAsync(new AddAuditTrailCommand(
            null,
            "Customer",
            Guid.Empty,
            AuditTrailAction.Updated,
            default));
        var sensitiveAudit = await service.AddAuditTrailAsync(new AddAuditTrailCommand(
            null,
            "Customer",
            Guid.NewGuid(),
            AuditTrailAction.Updated,
            default,
            ChangeSetJson: "{\"api_key\":\"secret\"}"));

        missingEventType.Succeeded.Should().BeFalse();
        sensitiveEvent.Succeeded.Should().BeFalse();
        missingAuditEntity.Succeeded.Should().BeFalse();
        sensitiveAudit.Succeeded.Should().BeFalse();
    }

    [Theory]
    [InlineData(NumberSequenceResetPolicy.Daily, "D-{yyyy}{MM}{dd}-{seq}", "D-20300430-0001", "D-20300501-0001")]
    [InlineData(NumberSequenceResetPolicy.Monthly, "M-{yyyy}{MM}-{seq}", "M-203004-0001", "M-203005-0001")]
    [InlineData(NumberSequenceResetPolicy.Yearly, "Y-{yyyy}-{seq}", "Y-2030-0001", "Y-2031-0001")]
    public async Task NumberSequenceService_Should_ResetByUtcPeriod(
        NumberSequenceResetPolicy resetPolicy,
        string pattern,
        string firstExpected,
        string resetExpected)
    {
        await using var db = FoundationTestDbContext.Create();
        var clock = new MutableClock(FixedNow);
        var service = new NumberSequenceService(db, clock);
        await service.CreateSequenceAsync(new CreateNumberSequenceCommand(
            null,
            NumberSequenceDocumentType.Invoice,
            "GLOBAL",
            pattern,
            1,
            4,
            resetPolicy));

        var first = await service.ReserveNextAsync(new NumberSequenceRequest(null, NumberSequenceDocumentType.Invoice, "global"));
        var second = await service.ReserveNextAsync(new NumberSequenceRequest(null, NumberSequenceDocumentType.Invoice, "GLOBAL"));
        clock.UtcNow = resetPolicy switch
        {
            NumberSequenceResetPolicy.Daily => new DateTime(2030, 5, 1, 0, 5, 0, DateTimeKind.Utc),
            NumberSequenceResetPolicy.Monthly => new DateTime(2030, 5, 1, 0, 5, 0, DateTimeKind.Utc),
            NumberSequenceResetPolicy.Yearly => new DateTime(2031, 1, 1, 0, 5, 0, DateTimeKind.Utc),
            _ => clock.UtcNow
        };
        var reset = await service.ReserveNextAsync(new NumberSequenceRequest(null, NumberSequenceDocumentType.Invoice, "GLOBAL"));

        first.Value.Should().Be(firstExpected);
        second.Value.Should().NotBe(firstExpected);
        reset.Value.Should().Be(resetExpected);
    }

    [Fact]
    public async Task NumberSequenceService_Should_RejectDuplicateGlobalAndScopedSequences()
    {
        await using var db = FoundationTestDbContext.Create();
        var clock = new MutableClock(FixedNow);
        var service = new NumberSequenceService(db, clock);
        var businessId = Guid.NewGuid();

        var global = await service.CreateSequenceAsync(new CreateNumberSequenceCommand(
            null,
            NumberSequenceDocumentType.PurchaseOrder,
            "MAIN",
            "PO-{seq}",
            1,
            3,
            NumberSequenceResetPolicy.Never));
        var duplicateGlobal = await service.CreateSequenceAsync(new CreateNumberSequenceCommand(
            null,
            NumberSequenceDocumentType.PurchaseOrder,
            " main ",
            "PO-{seq}",
            1,
            3,
            NumberSequenceResetPolicy.Never));
        var scoped = await service.CreateSequenceAsync(new CreateNumberSequenceCommand(
            businessId,
            NumberSequenceDocumentType.PurchaseOrder,
            "MAIN",
            "BPO-{seq}",
            1,
            3,
            NumberSequenceResetPolicy.Never));
        var duplicateScoped = await service.CreateSequenceAsync(new CreateNumberSequenceCommand(
            businessId,
            NumberSequenceDocumentType.PurchaseOrder,
            "MAIN",
            "BPO-{seq}",
            1,
            3,
            NumberSequenceResetPolicy.Never));

        global.Succeeded.Should().BeTrue();
        duplicateGlobal.Succeeded.Should().BeFalse();
        scoped.Succeeded.Should().BeTrue();
        duplicateScoped.Succeeded.Should().BeFalse();
    }

    [Theory]
    [InlineData("ORD")]
    [InlineData("")]
    [InlineData(null)]
    public async Task NumberSequenceService_Should_RejectInvalidPatterns(string? pattern)
    {
        await using var db = FoundationTestDbContext.Create();
        var service = new NumberSequenceService(db, new MutableClock(FixedNow));

        var result = await service.CreateSequenceAsync(new CreateNumberSequenceCommand(
            null,
            NumberSequenceDocumentType.Order,
            "GLOBAL",
            pattern,
            1,
            5,
            NumberSequenceResetPolicy.Never));

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task FeatureAreaService_Should_NormalizeCode_And_RejectDuplicate()
    {
        await using var db = FoundationTestDbContext.Create();
        var service = new FeatureAreaService(db, new MutableClock(FixedNow));

        var created = await service.CreateFeatureAreaAsync(new CreateFeatureAreaCommand(
            " Sales & Orders ",
            "Sales and orders",
            FeatureAreaCategory.Sales,
            FeatureAreaVisibilityScope.Staff,
            RequiredPermissionKey: " AccessSales "));
        var duplicate = await service.CreateFeatureAreaAsync(new CreateFeatureAreaCommand(
            "sales-orders",
            "Sales duplicate",
            FeatureAreaCategory.Sales,
            FeatureAreaVisibilityScope.Staff));

        created.Succeeded.Should().BeTrue();
        duplicate.Succeeded.Should().BeFalse();
        var feature = await db.Set<FeatureArea>().SingleAsync();
        feature.Code.Should().Be("sales-orders");
        feature.RequiredPermissionKey.Should().Be("AccessSales");
    }

    [Fact]
    public async Task FeatureAreaService_Should_ResolveDefaultAndBusinessOverrides()
    {
        await using var db = FoundationTestDbContext.Create();
        var clock = new MutableClock(FixedNow);
        var service = new FeatureAreaService(db, clock);
        var businessId = Guid.NewGuid();
        var featureId = (await service.CreateFeatureAreaAsync(new CreateFeatureAreaCommand(
            "inventory",
            "Inventory",
            FeatureAreaCategory.Inventory,
            FeatureAreaVisibilityScope.Business,
            DefaultEnabled: true))).Value;

        var defaultEnabled = await service.IsEnabledAsync(" inventory ", businessId);
        await service.UpsertBusinessOverrideAsync(new UpsertBusinessFeatureOverrideCommand(
            businessId,
            featureId,
            false,
            Reason: "Not contracted"));
        var overriddenDisabled = await service.IsEnabledAsync("inventory", businessId);
        var globalStillEnabled = await service.IsEnabledAsync("inventory");

        defaultEnabled.Should().BeTrue();
        overriddenDisabled.Should().BeFalse();
        globalStillEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task FeatureAreaService_Should_IgnoreInactiveAndOutOfWindowOverrides()
    {
        await using var db = FoundationTestDbContext.Create();
        var clock = new MutableClock(FixedNow);
        var service = new FeatureAreaService(db, clock);
        var businessId = Guid.NewGuid();
        var featureId = (await service.CreateFeatureAreaAsync(new CreateFeatureAreaCommand(
            "finance",
            "Finance",
            FeatureAreaCategory.Finance,
            FeatureAreaVisibilityScope.Staff,
            DefaultEnabled: false))).Value;

        var missing = await service.IsEnabledAsync("missing", businessId);
        await service.UpsertBusinessOverrideAsync(new UpsertBusinessFeatureOverrideCommand(
            businessId,
            featureId,
            true,
            EffectiveFromUtc: FixedNow.AddDays(1),
            EffectiveToUtc: FixedNow.AddDays(2)));
        var beforeWindow = await service.IsEnabledAsync("finance", businessId);
        clock.UtcNow = FixedNow.AddDays(1).AddMinutes(1);
        var insideWindow = await service.IsEnabledAsync("finance", businessId);

        missing.Should().BeFalse();
        beforeWindow.Should().BeFalse();
        insideWindow.Should().BeTrue();
    }

    [Fact]
    public async Task FeatureAreaService_Should_QueryEnabledAreas_WithOrdering()
    {
        await using var db = FoundationTestDbContext.Create();
        var service = new FeatureAreaService(db, new MutableClock(FixedNow));
        var businessId = Guid.NewGuid();
        var crmId = (await service.CreateFeatureAreaAsync(new CreateFeatureAreaCommand(
            "crm",
            "CRM",
            FeatureAreaCategory.CRM,
            FeatureAreaVisibilityScope.Staff,
            SortOrder: 20))).Value;
        var salesId = (await service.CreateFeatureAreaAsync(new CreateFeatureAreaCommand(
            "sales",
            "Sales",
            FeatureAreaCategory.Sales,
            FeatureAreaVisibilityScope.Staff,
            SortOrder: 10))).Value;
        await service.CreateFeatureAreaAsync(new CreateFeatureAreaCommand(
            "finance",
            "Finance",
            FeatureAreaCategory.Finance,
            FeatureAreaVisibilityScope.Staff,
            DefaultEnabled: false));
        await service.UpsertBusinessOverrideAsync(new UpsertBusinessFeatureOverrideCommand(businessId, crmId, false));
        await service.UpsertBusinessOverrideAsync(new UpsertBusinessFeatureOverrideCommand(businessId, salesId, true));

        var global = await service.GetEnabledAreasForBusinessAsync();
        var business = await service.GetEnabledAreasForBusinessAsync(businessId);

        global.Select(x => x.Code).Should().Equal("crm", "sales");
        business.Select(x => x.Code).Should().Equal("sales");
    }

    [Fact]
    public async Task FeatureAreaService_Should_RejectSensitiveMetadata()
    {
        await using var db = FoundationTestDbContext.Create();
        var service = new FeatureAreaService(db, new MutableClock(FixedNow));
        var sensitiveFeature = await service.CreateFeatureAreaAsync(new CreateFeatureAreaCommand(
            "ai",
            "AI",
            FeatureAreaCategory.AI,
            FeatureAreaVisibilityScope.Internal,
            MetadataJson: "{\"apiToken\":\"secret\"}"));
        var featureId = (await service.CreateFeatureAreaAsync(new CreateFeatureAreaCommand(
            "documents",
            "Documents",
            FeatureAreaCategory.Documents,
            FeatureAreaVisibilityScope.Staff))).Value;
        var sensitiveOverride = await service.UpsertBusinessOverrideAsync(new UpsertBusinessFeatureOverrideCommand(
            Guid.NewGuid(),
            featureId,
            true,
            Reason: "contains password"));

        sensitiveFeature.Succeeded.Should().BeFalse();
        sensitiveOverride.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task AiGovernanceService_Should_DefaultDeny_And_RejectRawAccessForRestrictedSensitiveFields()
    {
        await using var db = FoundationTestDbContext.Create();
        var service = new AiGovernanceService(db, new MutableClock(FixedNow));

        var beforePolicy = await service.EvaluateFieldAccessAsync("PayrollPayslip", "BankAccount", "payroll-summary");
        var rawRestricted = await service.UpsertSensitiveFieldPolicyAsync(new UpsertAiSensitiveFieldPolicyCommand(
            null,
            "PayrollPayslip",
            "BankAccount",
            "payroll-summary",
            AiSensitiveDataCategory.Bank,
            AiSensitivityLevel.Restricted,
            AiAccessDecision.AllowRaw));
        var summaryPolicy = await service.UpsertSensitiveFieldPolicyAsync(new UpsertAiSensitiveFieldPolicyCommand(
            null,
            "PayrollPayslip",
            "PaymentStatus",
            "payroll-summary",
            AiSensitiveDataCategory.Payroll,
            AiSensitivityLevel.Confidential,
            AiAccessDecision.AllowSummary));
        var afterPolicy = await service.EvaluateFieldAccessAsync("PayrollPayslip", "PaymentStatus", "payroll-summary");

        beforePolicy.Should().Be(AiAccessDecision.Deny);
        rawRestricted.Succeeded.Should().BeFalse();
        summaryPolicy.Succeeded.Should().BeTrue();
        afterPolicy.Should().Be(AiAccessDecision.AllowSummary);
    }

    [Fact]
    public async Task AiGovernanceService_Should_CreateRecommendationAndActionDraft_WithoutExecutingCommands()
    {
        await using var db = FoundationTestDbContext.Create();
        var service = new AiGovernanceService(db, new MutableClock(FixedNow));
        var actorId = Guid.NewGuid();

        var recommendationId = (await service.CreateRecommendationAsync(new CreateAiRecommendationCommand(
            Guid.NewGuid(),
            "Inventory",
            "shortage-attention",
            "Review shortage",
            "Picking shortages increased for one location.",
            "Shortage attention is based on scoped warehouse task summary counts.",
            80,
            SourceEntityType: "WarehouseTask",
            SourceEntityId: Guid.NewGuid(),
            ActorUserId: actorId))).Value;
        var draftId = (await service.CreateActionDraftAsync(new CreateAiActionDraftCommand(
            Guid.NewGuid(),
            recommendationId,
            "Inventory",
            "WarehouseTask",
            Guid.NewGuid(),
            "CreateReplenishmentTask",
            "{\"quantity\":5}",
            "Draft replenishment task for operator approval.",
            AiActionRiskLevel.Medium,
            ActorUserId: actorId))).Value;
        var submit = await service.SubmitActionDraftForApprovalAsync(draftId, actorId);
        var approve = await service.ApproveActionDraftAsync(draftId, actorId, "Reviewed scoped summary and approved draft.");

        submit.Succeeded.Should().BeTrue();
        approve.Succeeded.Should().BeTrue();
        db.Set<AiRecommendation>().Single().Status.Should().Be(AiRecommendationStatus.Open);
        var draft = db.Set<AiActionDraft>().Single();
        draft.Status.Should().Be(AiActionDraftStatus.Approved);
        draft.ExecutedAtUtc.Should().BeNull("AI approval records human consent but does not execute module commands");
        db.Set<AiActionApproval>().Should().ContainSingle(x => x.Decision == AiActionApprovalDecision.Approved);
    }

    [Fact]
    public async Task AiGovernanceService_Should_RejectSensitiveRecommendationDraftAndApprovalContent()
    {
        await using var db = FoundationTestDbContext.Create();
        var service = new AiGovernanceService(db, new MutableClock(FixedNow));
        var actorId = Guid.NewGuid();

        var recommendation = await service.CreateRecommendationAsync(new CreateAiRecommendationCommand(
            null,
            "Finance",
            "review",
            "Contains token",
            "Do not store api-token in recommendation.",
            "safe rationale",
            50));
        var draft = await service.CreateActionDraftAsync(new CreateAiActionDraftCommand(
            null,
            null,
            "Finance",
            "JournalEntry",
            Guid.NewGuid(),
            "UpdateJournalEntry",
            "{\"refreshToken\":\"secret\"}",
            "Draft contains sensitive payload.",
            AiActionRiskLevel.High));
        var safeDraftId = (await service.CreateActionDraftAsync(new CreateAiActionDraftCommand(
            null,
            null,
            "Finance",
            "JournalEntry",
            Guid.NewGuid(),
            "ReviewJournalEntry",
            "{\"journalEntryId\":\"11111111-1111-1111-1111-111111111111\"}",
            "Review journal entry without mutation.",
            AiActionRiskLevel.Low))).Value;
        await service.SubmitActionDraftForApprovalAsync(safeDraftId, actorId);
        var approval = await service.ApproveActionDraftAsync(safeDraftId, actorId, "contains password");

        recommendation.Succeeded.Should().BeFalse();
        draft.Succeeded.Should().BeFalse();
        approval.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task AiGovernanceService_Should_ReviewRecommendation_And_ProtectActionDraftReview_WithRowVersion()
    {
        await using var db = FoundationTestDbContext.Create();
        var service = new AiGovernanceService(db, new MutableClock(FixedNow));
        var actorId = Guid.NewGuid();

        var recommendationId = (await service.CreateRecommendationAsync(new CreateAiRecommendationCommand(
            Guid.NewGuid(),
            "Sales",
            "attention",
            "Review order attention",
            "Open invoices increased.",
            "Scoped summary indicates more open balance attention.",
            70,
            ActorUserId: actorId))).Value;
        var recommendation = db.Set<AiRecommendation>().Single();
        recommendation.RowVersion = [1, 2, 3];
        await db.SaveChangesAsync();

        var staleRecommendation = await service.ReviewRecommendationAsync(recommendationId, AiRecommendationStatus.Accepted, actorId, "Reviewed business summary.", [9, 9, 9]);
        var acceptedRecommendation = await service.ReviewRecommendationAsync(recommendationId, AiRecommendationStatus.Accepted, actorId, "Reviewed business summary.", [1, 2, 3]);

        var draftId = (await service.CreateActionDraftAsync(new CreateAiActionDraftCommand(
            Guid.NewGuid(),
            recommendationId,
            "Sales",
            "SalesQuote",
            Guid.NewGuid(),
            "UpdateSalesQuote",
            "{\"status\":\"Accepted\"}",
            "Prepare quote status update for normal handler review.",
            AiActionRiskLevel.Medium,
            ActorUserId: actorId))).Value;
        var draft = db.Set<AiActionDraft>().Single();
        draft.RowVersion = [4, 5, 6];
        await db.SaveChangesAsync();

        var staleSubmit = await service.SubmitActionDraftForApprovalAsync(draftId, actorId, [7, 7, 7]);
        var submit = await service.SubmitActionDraftForApprovalAsync(draftId, actorId, [4, 5, 6]);
        draft.RowVersion = [8, 8, 8];
        await db.SaveChangesAsync();
        var staleApprove = await service.ApproveActionDraftAsync(draftId, actorId, "Reviewed and approved for owning command handler.", [4, 5, 6]);
        var approve = await service.ApproveActionDraftAsync(draftId, actorId, "Reviewed and approved for owning command handler.", [8, 8, 8]);

        staleRecommendation.Succeeded.Should().BeFalse();
        acceptedRecommendation.Succeeded.Should().BeTrue();
        db.Set<AiRecommendation>().Single().Status.Should().Be(AiRecommendationStatus.Accepted);
        staleSubmit.Succeeded.Should().BeFalse();
        submit.Succeeded.Should().BeTrue();
        staleApprove.Succeeded.Should().BeFalse();
        approve.Succeeded.Should().BeTrue();
        db.Set<AiActionDraft>().Single().ExecutedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task AiScopedContextProjectionService_Should_ReturnPurposeBoundAggregateContext_WithoutRawSensitiveFields()
    {
        await using var db = FoundationTestDbContext.Create();
        var businessId = Guid.NewGuid();
        db.Set<Order>().Add(new Order { BusinessId = businessId, OrderNumber = "SO-1", Status = OrderStatus.Confirmed, OrderedAtUtc = FixedNow, GrandTotalGrossMinor = 12500, BillingAddressJson = "{\"street\":\"private\"}", ShippingAddressJson = "{\"street\":\"private\"}" });
        db.Set<Invoice>().Add(new Invoice { BusinessId = businessId, InvoiceNumber = "INV-1", Status = InvoiceStatus.Open, TotalGrossMinor = 12500, Currency = "EUR" });
        db.Set<SalesQuote>().Add(new SalesQuote { BusinessId = businessId, Title = "Private customer quote", Status = SalesQuoteStatus.Sent, TotalGrossMinor = 5000 });
        db.Set<Supplier>().Add(new Supplier { BusinessId = businessId, Name = "Private Supplier", Status = SupplierStatus.Active, Email = "private@example.test", Phone = "+491234" });
        db.Set<PurchaseOrder>().Add(new PurchaseOrder { BusinessId = businessId, SupplierId = Guid.NewGuid(), OrderNumber = "PO-1", Status = PurchaseOrderStatus.Issued, OrderedAtUtc = FixedNow, Currency = "EUR" });
        db.Set<WarehouseLocation>().Add(new WarehouseLocation { BusinessId = businessId, WarehouseId = Guid.NewGuid(), Code = "BIN-A", DisplayName = "Private Bin", Status = WarehouseLocationStatus.Active });
        db.Set<Employee>().Add(new Employee { BusinessId = businessId, EmployeeNumber = "E-1", FirstName = "Private", LastName = "Employee", Status = EmployeeStatus.Active, WorkEmail = "employee@example.test" });
        db.Set<PayrollRun>().Add(new PayrollRun { BusinessId = businessId, PayrollPeriodId = Guid.NewGuid(), PayrollRuleSetId = Guid.NewGuid(), RunNumber = "PR-1", Status = PayrollRunStatus.Approved, SourceSnapshotJson = "{\"salary\":\"private\"}" });
        db.Set<BankAccount>().Add(new BankAccount { BusinessId = businessId, Code = "BANK", DisplayName = "Private Bank", Currency = "EUR", Status = BankAccountStatus.Active, MaskedAccountIdentifier = "****1234" });
        await db.SaveChangesAsync();

        var service = new AiScopedContextProjectionService(db, new MutableClock(FixedNow));
        var missingPurpose = await service.BuildAsync(new AiScopedContextProjectionRequest(businessId, null, ["Sales"]));
        var invalidModule = await service.BuildAsync(new AiScopedContextProjectionRequest(businessId, "ops-review", ["Unsupported"]));
        var context = await service.BuildAsync(new AiScopedContextProjectionRequest(businessId, "ops-review", ["Sales", "Purchasing", "Inventory", "HR", "Payroll", "Treasury"]));

        missingPurpose.Succeeded.Should().BeFalse();
        invalidModule.Succeeded.Should().BeFalse();
        context.Succeeded.Should().BeTrue();
        context.Value!.Modules.Select(x => x.ModuleKey).Should().BeEquivalentTo("Sales", "Purchasing", "Inventory", "HR", "Payroll", "Treasury");
        context.Value.Modules.SelectMany(x => x.Metrics).Should().OnlyContain(x => !string.IsNullOrWhiteSpace(x.Key));
        context.Value.Modules.Single(x => x.ModuleKey == "Sales").Metrics.Should().Contain(x => x.Key == "orders.open" && x.Value == 1);

        var json = JsonSerializer.Serialize(context.Value);
        json.Should().NotContain("Private");
        json.Should().NotContain("example.test");
        json.Should().NotContain("street");
        json.Should().NotContain("salary");
        json.Should().NotContain("****1234");
    }

    [Fact]
    public async Task AiProviderAdapterFoundationService_Should_CreateGovernedRecommendationAndDraft_FromScopedContextOnly()
    {
        await using var db = FoundationTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        db.Set<Order>().Add(new Order { BusinessId = businessId, OrderNumber = "SO-1", Status = OrderStatus.Confirmed, OrderedAtUtc = FixedNow, GrandTotalGrossMinor = 25000, BillingAddressJson = "{\"street\":\"private\"}" });
        await db.SaveChangesAsync();

        var adapter = new RecordingAiProviderAdapter(new AiProviderAdapterResponse(
            new AiProviderRecommendationResponse(
                "Review sales attention",
                "Open order attention increased.",
                "Scoped aggregate sales metrics show one open order.",
                82),
            new AiProviderActionDraftResponse(
                "Order",
                Guid.NewGuid(),
                "ReviewOrder",
                "{\"mode\":\"review\"}",
                "Prepare a review task for an operator.",
                AiActionRiskLevel.Low),
            ModelCode: "test-model",
            ProviderRequestId: "provider-run-1",
            InputTokenCount: 12,
            OutputTokenCount: 8,
            SafeSummary: "Generated from aggregate Sales context."));
        var service = CreateAiProviderFoundationService(db, adapter);

        var result = await service.GenerateRecommendationAsync(new AiProviderGenerationCommand(
            businessId,
            "ops-review",
            "Review aggregate sales attention.",
            "Sales",
            "attention",
            ["Sales"],
            ActorUserId: actorId));

        result.Succeeded.Should().BeTrue();
        result.Value!.RecommendationId.Should().NotBeEmpty();
        result.Value.ActionDraftId.Should().NotBeNull();
        result.Value.ModuleKeys.Should().BeEquivalentTo("Sales");
        adapter.LastRequest.Should().NotBeNull();
        adapter.LastRequest!.ScopedContext.Modules.Should().ContainSingle(x => x.ModuleKey == "Sales");

        db.Set<AiRecommendation>().Should().ContainSingle(x =>
            x.Id == result.Value.RecommendationId &&
            x.FeatureAreaCode == "sales" &&
            x.RecommendationType == "attention" &&
            x.MetadataJson.Contains("test-model"));
        db.Set<AiActionDraft>().Should().ContainSingle(x =>
            x.Id == result.Value.ActionDraftId &&
            x.Status == AiActionDraftStatus.Draft &&
            x.ExecutedAtUtc == null);

        var persistedJson = JsonSerializer.Serialize(new
        {
            Recommendations = db.Set<AiRecommendation>().Select(x => new { x.Title, x.Summary, x.Rationale, x.MetadataJson }).ToList(),
            Drafts = db.Set<AiActionDraft>().Select(x => new { x.Summary, x.CommandPayloadJson, x.MetadataJson }).ToList()
        });
        persistedJson.Should().NotContain("street");
        persistedJson.Should().NotContain("Private");
        persistedJson.Should().NotContain("prompt");
        persistedJson.Should().NotContain("completion");
    }

    [Fact]
    public async Task AiProviderAdapterFoundationService_Should_BlockMissingAdapter_And_SensitiveProviderOutput()
    {
        await using var db = FoundationTestDbContext.Create();
        var businessId = Guid.NewGuid();
        db.Set<Order>().Add(new Order { BusinessId = businessId, OrderNumber = "SO-1", Status = OrderStatus.Confirmed, OrderedAtUtc = FixedNow, GrandTotalGrossMinor = 1000 });
        await db.SaveChangesAsync();

        var missing = await CreateAiProviderFoundationService(db).GenerateRecommendationAsync(new AiProviderGenerationCommand(
            businessId,
            "ops-review",
            "Review aggregate sales attention.",
            "Sales",
            "attention",
            ["Sales"]));
        var sensitiveAdapter = new RecordingAiProviderAdapter(new AiProviderAdapterResponse(
            new AiProviderRecommendationResponse(
                "Contains token",
                "api-token leaked",
                "Unsafe provider output.",
                50)));
        var sensitive = await CreateAiProviderFoundationService(db, sensitiveAdapter).GenerateRecommendationAsync(new AiProviderGenerationCommand(
            businessId,
            "ops-review",
            "Review aggregate sales attention.",
            "Sales",
            "attention",
            ["Sales"]));
        var sensitiveRequest = await CreateAiProviderFoundationService(db, sensitiveAdapter).GenerateRecommendationAsync(new AiProviderGenerationCommand(
            businessId,
            "ops-review",
            "Use password from operator note.",
            "Sales",
            "attention",
            ["Sales"]));

        missing.Succeeded.Should().BeFalse();
        sensitive.Succeeded.Should().BeFalse();
        sensitiveRequest.Succeeded.Should().BeFalse();
        db.Set<AiRecommendation>().Should().BeEmpty();
        db.Set<AiActionDraft>().Should().BeEmpty();
    }

    [Fact]
    public async Task AiActionHandoffService_Should_ExecuteApprovedDraft_ThroughRegisteredExecutorOnly()
    {
        await using var db = FoundationTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var governance = new AiGovernanceService(db, new MutableClock(FixedNow));
        var draftId = (await governance.CreateActionDraftAsync(new CreateAiActionDraftCommand(
            businessId,
            null,
            "Inventory",
            "WarehouseTask",
            Guid.NewGuid(),
            "CreateReviewTask",
            "{\"mode\":\"review\"}",
            "Create a review task through the owning handler.",
            AiActionRiskLevel.Low,
            ActorUserId: actorId))).Value;
        await governance.SubmitActionDraftForApprovalAsync(draftId, actorId);
        await governance.ApproveActionDraftAsync(draftId, actorId, "Approved for handoff test.");
        var draft = await db.Set<AiActionDraft>().SingleAsync(x => x.Id == draftId);
        draft.RowVersion = [1, 2, 3];
        await db.SaveChangesAsync();

        var missingExecutor = await CreateAiActionHandoffService(db).ExecuteApprovedDraftAsync(new ExecuteAiActionDraftCommand(
            draftId,
            actorId,
            [1, 2, 3]));
        var executor = new RecordingAiActionDraftExecutor("inventory", "createreviewtask");
        var service = CreateAiActionHandoffService(db, executor);
        var stale = await service.ExecuteApprovedDraftAsync(new ExecuteAiActionDraftCommand(
            draftId,
            actorId,
            [9, 9, 9]));
        var executed = await service.ExecuteApprovedDraftAsync(new ExecuteAiActionDraftCommand(
            draftId,
            actorId,
            [1, 2, 3]));
        var retry = await service.ExecuteApprovedDraftAsync(new ExecuteAiActionDraftCommand(
            draftId,
            actorId,
            [1, 2, 3]));

        missingExecutor.Succeeded.Should().BeFalse();
        stale.Succeeded.Should().BeFalse();
        executed.Succeeded.Should().BeTrue();
        executed.Value!.ExecutionEventId.Should().NotBeNull();
        retry.Succeeded.Should().BeTrue();
        retry.Value!.AlreadyExecuted.Should().BeTrue();
        executor.ExecutionCount.Should().Be(1);
        db.Set<AiActionDraft>().Single(x => x.Id == draftId).ExecutedAtUtc.Should().Be(FixedNow);
        db.Set<BusinessEvent>().Should().ContainSingle(x => x.EventType == "ai.action_draft.executed");
    }

    [Fact]
    public async Task AiActionHandoffService_Should_BlockHighRiskDrafts_And_SensitiveExecutorOutput()
    {
        await using var db = FoundationTestDbContext.Create();
        var actorId = Guid.NewGuid();
        var governance = new AiGovernanceService(db, new MutableClock(FixedNow));
        var highRiskDraftId = (await governance.CreateActionDraftAsync(new CreateAiActionDraftCommand(
            Guid.NewGuid(),
            null,
            "Finance",
            "JournalEntry",
            Guid.NewGuid(),
            "PostJournal",
            "{\"mode\":\"review\"}",
            "Review high risk finance action.",
            AiActionRiskLevel.High,
            ActorUserId: actorId))).Value;
        await governance.SubmitActionDraftForApprovalAsync(highRiskDraftId, actorId);
        await governance.ApproveActionDraftAsync(highRiskDraftId, actorId, "Approved for high risk guard test.");

        var sensitiveDraftId = (await governance.CreateActionDraftAsync(new CreateAiActionDraftCommand(
            Guid.NewGuid(),
            null,
            "Inventory",
            "WarehouseTask",
            Guid.NewGuid(),
            "CreateReviewTask",
            "{\"mode\":\"review\"}",
            "Review inventory action.",
            AiActionRiskLevel.Low,
            ActorUserId: actorId))).Value;
        await governance.SubmitActionDraftForApprovalAsync(sensitiveDraftId, actorId);
        await governance.ApproveActionDraftAsync(sensitiveDraftId, actorId, "Approved for sensitive output guard test.");

        var highRisk = await CreateAiActionHandoffService(db, new RecordingAiActionDraftExecutor("finance", "postjournal"))
            .ExecuteApprovedDraftAsync(new ExecuteAiActionDraftCommand(highRiskDraftId, actorId));
        var sensitive = await CreateAiActionHandoffService(db, new RecordingAiActionDraftExecutor("inventory", "createreviewtask", safeSummary: "contains password"))
            .ExecuteApprovedDraftAsync(new ExecuteAiActionDraftCommand(sensitiveDraftId, actorId));

        highRisk.Succeeded.Should().BeFalse();
        sensitive.Succeeded.Should().BeFalse();
        db.Set<AiActionDraft>().Where(x => x.ExecutedAtUtc != null).Should().BeEmpty();
    }

    [Fact]
    public async Task AiTimelineActionDraftExecutor_Should_CreateInternalNoteAndActivityOnly()
    {
        await using var db = FoundationTestDbContext.Create();
        var actorId = Guid.NewGuid();
        var governance = new AiGovernanceService(db, new MutableClock(FixedNow));

        var noteDraftId = (await governance.CreateActionDraftAsync(new CreateAiActionDraftCommand(
            Guid.NewGuid(),
            null,
            "Sales",
            "Order",
            Guid.NewGuid(),
            AiTimelineActionDraftExecutor.TimelineCommandType,
            "{\"entryType\":\"Note\",\"body\":\"Internal follow-up for operator review.\"}",
            "Create internal note.",
            AiActionRiskLevel.Low,
            ActorUserId: actorId))).Value;
        await governance.SubmitActionDraftForApprovalAsync(noteDraftId, actorId);
        await governance.ApproveActionDraftAsync(noteDraftId, actorId, "Approved note handoff.");

        var activityDraftId = (await governance.CreateActionDraftAsync(new CreateAiActionDraftCommand(
            Guid.NewGuid(),
            null,
            "Inventory",
            "WarehouseTask",
            Guid.NewGuid(),
            AiTimelineActionDraftExecutor.TimelineCommandType,
            "{\"entryType\":\"Activity\",\"activityType\":\"ai-review\",\"title\":\"Review inventory attention\",\"summary\":\"Operator should review scoped inventory attention.\"}",
            "Create internal activity.",
            AiActionRiskLevel.Medium,
            ActorUserId: actorId))).Value;
        await governance.SubmitActionDraftForApprovalAsync(activityDraftId, actorId);
        await governance.ApproveActionDraftAsync(activityDraftId, actorId, "Approved activity handoff.");

        var service = CreateAiActionHandoffService(db, new AiTimelineActionDraftExecutor(db, new MutableClock(FixedNow)));
        var note = await service.ExecuteApprovedDraftAsync(new ExecuteAiActionDraftCommand(noteDraftId, actorId, OperatorReason: "Operator reviewed note draft."));
        var activity = await service.ExecuteApprovedDraftAsync(new ExecuteAiActionDraftCommand(activityDraftId, actorId, OperatorReason: "Operator reviewed activity draft."));

        note.Succeeded.Should().BeTrue(note.Error);
        activity.Succeeded.Should().BeTrue(activity.Error);
        db.Set<Note>().Should().ContainSingle(x => x.Body == "Internal follow-up for operator review." && x.Visibility == FoundationVisibility.Internal);
        db.Set<Activity>().Should().ContainSingle(x => x.Title == "Review inventory attention" && x.Visibility == FoundationVisibility.Internal);
        db.Set<BusinessEvent>().Count(x => x.EventType == "ai.action_draft.executed").Should().Be(2);
    }

    [Fact]
    public async Task InternalFollowUpTaskService_Should_Create_Complete_And_Cancel_WithSafeEvidence()
    {
        await using var db = FoundationTestDbContext.Create();
        var actorId = Guid.NewGuid();
        var service = new InternalFollowUpTaskService(db, new MutableClock(FixedNow), new BusinessEventService(db, new MutableClock(FixedNow)));

        var created = await service.CreateAsync(new CreateInternalFollowUpTaskCommand(
            Guid.NewGuid(),
            " Inventory ",
            "WarehouseLocation",
            Guid.NewGuid(),
            " Review bin capacity ",
            "Check whether this bin needs cleanup.",
            InternalFollowUpTaskPriority.High,
            FixedNow.AddHours(4),
            actorId,
            Guid.NewGuid(),
            "{}",
            actorId));

        created.Succeeded.Should().BeTrue(created.Error);
        var task = await db.Set<InternalFollowUpTask>().SingleAsync(x => x.Id == created.Value);
        task.FeatureAreaCode.Should().Be("inventory");
        task.Status.Should().Be(InternalFollowUpTaskStatus.Open);
        task.Priority.Should().Be(InternalFollowUpTaskPriority.High);

        var completed = await service.CompleteAsync(new InternalFollowUpTaskLifecycleCommand(task.Id, null, actorId, "Reviewed by operator."));
        completed.Succeeded.Should().BeTrue(completed.Error);
        db.Set<InternalFollowUpTask>().Single(x => x.Id == task.Id).Status.Should().Be(InternalFollowUpTaskStatus.Completed);

        var cancelCompleted = await service.CancelAsync(new InternalFollowUpTaskLifecycleCommand(task.Id, null, actorId, "No longer needed."));
        cancelCompleted.Succeeded.Should().BeFalse();
        db.Set<BusinessEvent>().Should().Contain(x => x.EventType == "internal_follow_up_task.created");
        db.Set<BusinessEvent>().Should().Contain(x => x.EventType == "internal_follow_up_task.completed");
    }

    [Fact]
    public async Task AiInternalFollowUpTaskActionDraftExecutor_Should_CreateInternalTaskOnly_And_BeIdempotent()
    {
        await using var db = FoundationTestDbContext.Create();
        var actorId = Guid.NewGuid();
        var governance = new AiGovernanceService(db, new MutableClock(FixedNow));
        var draftId = (await governance.CreateActionDraftAsync(new CreateAiActionDraftCommand(
            Guid.NewGuid(),
            null,
            "Inventory",
            "WarehouseLocation",
            Guid.NewGuid(),
            AiInternalFollowUpTaskActionDraftExecutor.FollowUpTaskCommandType,
            "{\"title\":\"Review warehouse attention\",\"description\":\"Check the scoped bin attention before tomorrow.\",\"priority\":\"High\",\"dueInHours\":8}",
            "Create internal follow-up task.",
            AiActionRiskLevel.Low,
            ActorUserId: actorId))).Value;
        await governance.SubmitActionDraftForApprovalAsync(draftId, actorId);
        await governance.ApproveActionDraftAsync(draftId, actorId, "Approved follow-up task handoff.");

        var taskService = new InternalFollowUpTaskService(db, new MutableClock(FixedNow), new BusinessEventService(db, new MutableClock(FixedNow)));
        var executor = new AiInternalFollowUpTaskActionDraftExecutor(taskService, new MutableClock(FixedNow));
        var service = CreateAiActionHandoffService(db, executor);

        var executed = await service.ExecuteApprovedDraftAsync(new ExecuteAiActionDraftCommand(draftId, actorId, OperatorReason: "Operator reviewed follow-up draft."));
        var retry = await service.ExecuteApprovedDraftAsync(new ExecuteAiActionDraftCommand(draftId, actorId, OperatorReason: "Operator retried follow-up draft."));

        executed.Succeeded.Should().BeTrue(executed.Error);
        retry.Succeeded.Should().BeTrue(retry.Error);
        retry.Value!.AlreadyExecuted.Should().BeTrue();
        var task = db.Set<InternalFollowUpTask>().Should().ContainSingle().Subject;
        task.Title.Should().Be("Review warehouse attention");
        task.Priority.Should().Be(InternalFollowUpTaskPriority.High);
        task.DueAtUtc.Should().Be(FixedNow.AddHours(8));
        task.SourceAiActionDraftId.Should().Be(draftId);
        db.Set<BusinessEvent>().Count(x => x.EventType == "ai.action_draft.executed").Should().Be(1);
    }

    [Fact]
    public async Task AiModuleReviewTaskActionDraftExecutor_Should_RouteModuleReview_ToInternalTaskOnly()
    {
        await using var db = FoundationTestDbContext.Create();
        var actorId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var governance = new AiGovernanceService(db, new MutableClock(FixedNow));
        var draftId = (await governance.CreateActionDraftAsync(new CreateAiActionDraftCommand(
            Guid.NewGuid(),
            null,
            "Finance",
            "SupplierInvoice",
            targetId,
            AiModuleReviewTaskActionDraftExecutor.ModuleReviewTaskCommandType,
            "{\"title\":\"Review supplier invoice matching\",\"description\":\"Check the supplier invoice discrepancy and route it to the responsible operator.\",\"priority\":\"Normal\",\"dueInHours\":24}",
            "Create module review task.",
            AiActionRiskLevel.Medium,
            ActorUserId: actorId))).Value;
        await governance.SubmitActionDraftForApprovalAsync(draftId, actorId);
        await governance.ApproveActionDraftAsync(draftId, actorId, "Approved module review routing.");

        var taskService = new InternalFollowUpTaskService(db, new MutableClock(FixedNow), new BusinessEventService(db, new MutableClock(FixedNow)));
        var executor = new AiModuleReviewTaskActionDraftExecutor(taskService, new MutableClock(FixedNow));
        var service = CreateAiActionHandoffService(db, executor);

        var executed = await service.ExecuteApprovedDraftAsync(new ExecuteAiActionDraftCommand(draftId, actorId));
        var retry = await service.ExecuteApprovedDraftAsync(new ExecuteAiActionDraftCommand(draftId, actorId));

        executed.Succeeded.Should().BeTrue(executed.Error);
        executed.Value!.ReferenceEntityType.Should().Be("InternalFollowUpTask");
        retry.Succeeded.Should().BeTrue(retry.Error);
        retry.Value!.AlreadyExecuted.Should().BeTrue();
        var task = db.Set<InternalFollowUpTask>().Should().ContainSingle().Subject;
        task.FeatureAreaCode.Should().Be("finance");
        task.TargetEntityType.Should().Be("supplierinvoice");
        task.TargetEntityId.Should().Be(targetId);
        task.Title.Should().Be("Review supplier invoice matching");
        task.SourceAiActionDraftId.Should().Be(draftId);
        db.Set<BusinessEvent>().Should().ContainSingle(x => x.EventType == "ai.action_draft.executed");
        db.Set<BusinessEvent>().Should().Contain(x => x.EventType == "internal_follow_up_task.created");
    }

    [Fact]
    public async Task CreateOrderHandler_Should_UseNumberSequence_WhenActiveSequenceExists()
    {
        await using var db = FoundationTestDbContext.Create();
        var clock = new MutableClock(FixedNow);
        var sequenceService = new NumberSequenceService(db, clock);
        await sequenceService.CreateSequenceAsync(new CreateNumberSequenceCommand(
            null,
            NumberSequenceDocumentType.Order,
            NumberSequenceService.GlobalScopeKey,
            "SO-{yyyy}{MM}-{seq}",
            42,
            4,
            NumberSequenceResetPolicy.Monthly));
        var handler = new CreateOrderHandler(
            db,
            clock,
            new OrderCreateValidator(new TestStringLocalizer()),
            sequenceService);

        var orderId = await handler.HandleAsync(BuildOrderCreateDto());

        var order = await db.Set<Order>().SingleAsync(x => x.Id == orderId);
        order.OrderNumber.Should().Be("SO-203004-0042");
    }

    [Fact]
    public async Task CreateOrderHandler_Should_Fallback_WhenNoActiveSequenceExists()
    {
        await using var db = FoundationTestDbContext.Create();
        var clock = new MutableClock(FixedNow);
        var handler = new CreateOrderHandler(
            db,
            clock,
            new OrderCreateValidator(new TestStringLocalizer()),
            new NumberSequenceService(db, clock));

        var orderId = await handler.HandleAsync(BuildOrderCreateDto());

        var order = await db.Set<Order>().SingleAsync(x => x.Id == orderId);
        order.OrderNumber.Should().Be("D-20300430-00001");
    }

    private static OrderCreateDto BuildOrderCreateDto()
        => new()
        {
            Currency = "EUR",
            BillingAddressJson = "{}",
            ShippingAddressJson = "{}",
            Lines =
            [
                new OrderLineCreateDto
                {
                    VariantId = Guid.NewGuid(),
                    Name = "Test product",
                    Sku = "SKU-1",
                    Quantity = 2,
                    UnitPriceNetMinor = 1000,
                    VatRate = 0.19m
                }
            ]
        };

    private static AiProviderAdapterFoundationService CreateAiProviderFoundationService(
        FoundationTestDbContext db,
        params IAiProviderAdapter[] adapters)
        => new(
            new AiScopedContextProjectionService(db, new MutableClock(FixedNow)),
            new AiGovernanceService(db, new MutableClock(FixedNow)),
            adapters);

    private static AiActionHandoffService CreateAiActionHandoffService(
        FoundationTestDbContext db,
        params IAiActionDraftExecutor[] executors)
        => new(
            db,
            new MutableClock(FixedNow),
            executors,
            new BusinessEventService(db, new MutableClock(FixedNow)));


    private sealed class FoundationTestDbContext : DbContext, IAppDbContext
    {
        private FoundationTestDbContext(DbContextOptions<FoundationTestDbContext> options)
            : base(options)
        {
        }

        public static FoundationTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<FoundationTestDbContext>()
                .UseInMemoryDatabase($"darwin_foundation_tests_{Guid.NewGuid()}")
                .Options;
            return new FoundationTestDbContext(options);
        }

        public DbSet<CustomFieldDefinition> CustomFieldDefinitions => Set<CustomFieldDefinition>();
        public DbSet<CustomFieldValue> CustomFieldValues => Set<CustomFieldValue>();
        public DbSet<Activity> Activities => Set<Activity>();
        public DbSet<Note> Notes => Set<Note>();
        public DbSet<DocumentRecord> DocumentRecords => Set<DocumentRecord>();
        public DbSet<NumberSequence> NumberSequences => Set<NumberSequence>();
        public DbSet<BusinessEvent> BusinessEvents => Set<BusinessEvent>();
        public DbSet<AuditTrail> AuditTrails => Set<AuditTrail>();
        public DbSet<FeatureArea> FeatureAreas => Set<FeatureArea>();
        public DbSet<BusinessFeatureOverride> BusinessFeatureOverrides => Set<BusinessFeatureOverride>();
        public DbSet<AiSensitiveFieldPolicy> AiSensitiveFieldPolicies => Set<AiSensitiveFieldPolicy>();
        public DbSet<AiRecommendation> AiRecommendations => Set<AiRecommendation>();
        public DbSet<AiActionDraft> AiActionDrafts => Set<AiActionDraft>();
        public DbSet<AiActionApproval> AiActionApprovals => Set<AiActionApproval>();
        public DbSet<InternalFollowUpTask> InternalFollowUpTasks => Set<InternalFollowUpTask>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderLine> OrderLines => Set<OrderLine>();
        public DbSet<Invoice> Invoices => Set<Invoice>();
        public DbSet<SalesQuote> SalesQuotes => Set<SalesQuote>();
        public DbSet<DeliveryNote> DeliveryNotes => Set<DeliveryNote>();
        public DbSet<ReturnOrder> ReturnOrders => Set<ReturnOrder>();
        public DbSet<CreditNote> CreditNotes => Set<CreditNote>();
        public DbSet<Supplier> Suppliers => Set<Supplier>();
        public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
        public DbSet<GoodsReceipt> GoodsReceipts => Set<GoodsReceipt>();
        public DbSet<WarehouseLocation> WarehouseLocations => Set<WarehouseLocation>();
        public DbSet<WarehouseTask> WarehouseTasks => Set<WarehouseTask>();
        public DbSet<StockCountSession> StockCountSessions => Set<StockCountSession>();
        public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
        public DbSet<SupplierInvoice> SupplierInvoices => Set<SupplierInvoice>();
        public DbSet<SupplierPayment> SupplierPayments => Set<SupplierPayment>();
        public DbSet<SupplierAdvance> SupplierAdvances => Set<SupplierAdvance>();
        public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
        public DbSet<BankStatementImport> BankStatementImports => Set<BankStatementImport>();
        public DbSet<BankReconciliationMatch> BankReconciliationMatches => Set<BankReconciliationMatch>();
        public DbSet<Employee> Employees => Set<Employee>();
        public DbSet<PayrollRun> PayrollRuns => Set<PayrollRun>();
        public DbSet<PayrollPayment> PayrollPayments => Set<PayrollPayment>();
        public DbSet<PayrollPayslip> PayrollPayslips => Set<PayrollPayslip>();
    }

    private sealed class MutableClock : IClock
    {
        public MutableClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; set; }
    }

    private sealed class RecordingAiProviderAdapter : IAiProviderAdapter
    {
        private readonly AiProviderAdapterResponse? _response;
        private readonly string? _error;

        public RecordingAiProviderAdapter(AiProviderAdapterResponse? response = null, string? error = null, bool isReady = true)
        {
            _response = response;
            _error = error;
            IsReady = isReady;
        }

        public string AdapterCode => "unit-test";
        public bool IsReady { get; }
        public AiProviderAdapterRequest? LastRequest { get; private set; }

        public Task<Result<AiProviderAdapterResponse>> GenerateAsync(AiProviderAdapterRequest request, CancellationToken ct = default)
        {
            LastRequest = request;
            if (_error is not null)
            {
                return Task.FromResult(Result<AiProviderAdapterResponse>.Fail(_error));
            }

            return Task.FromResult(Result<AiProviderAdapterResponse>.Ok(_response ?? new AiProviderAdapterResponse(
                new AiProviderRecommendationResponse("Review attention", "Review aggregate metrics.", "Scoped context supports review.", 75))));
        }
    }

    private sealed class RecordingAiActionDraftExecutor : IAiActionDraftExecutor
    {
        private readonly string? _safeSummary;

        public RecordingAiActionDraftExecutor(string featureAreaCode, string commandType, string? safeSummary = null)
        {
            FeatureAreaCode = featureAreaCode;
            CommandType = commandType;
            _safeSummary = safeSummary;
        }

        public string FeatureAreaCode { get; }
        public string CommandType { get; }
        public int ExecutionCount { get; private set; }

        public bool CanExecute(AiActionDraft draft) => true;

        public Task<Result<AiActionDraftExecutionResult>> ExecuteAsync(AiActionDraft draft, Guid actorUserId, CancellationToken ct = default)
        {
            ExecutionCount++;
            return Task.FromResult(Result<AiActionDraftExecutionResult>.Ok(new AiActionDraftExecutionResult(
                _safeSummary ?? "Executed by test-scoped executor.",
                draft.TargetEntityType,
                draft.TargetEntityId)));
        }
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
}
