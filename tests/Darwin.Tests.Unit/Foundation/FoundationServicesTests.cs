using Darwin.Application;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Foundation;
using Darwin.Application.Orders.Commands;
using Darwin.Application.Orders.DTOs;
using Darwin.Application.Orders.Validators;
using Darwin.Domain.Entities.Foundation;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

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
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderLine> OrderLines => Set<OrderLine>();
    }

    private sealed class MutableClock : IClock
    {
        public MutableClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; set; }
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
