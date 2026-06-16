using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Billing.Commands;
using Darwin.Application.Billing.DTOs;
using Darwin.Application.Billing.Queries;
using Darwin.Application.Billing.Services;
using Darwin.Application.Foundation;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.Foundation;
using Darwin.Domain.Entities.Inventory;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Tests.Unit.Billing;

public sealed class SupplierInvoiceHandlersTests
{
    private static readonly DateTime Now = new(2034, 2, 3, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Create_Should_NormalizeAndPersistTotalsWithoutPosting()
    {
        await using var db = SupplierInvoiceTestDbContext.Create();
        var ids = await SeedPurchasingAsync(db, postedReceipt: true);
        var handler = new CreateSupplierInvoiceHandler(db, new FixedClock(Now));

        var id = await handler.HandleAsync(BuildCreate(ids), TestContext.Current.CancellationToken);

        var saved = await db.Set<SupplierInvoice>().Include(x => x.Lines).SingleAsync(TestContext.Current.CancellationToken);
        id.Should().Be(saved.Id);
        saved.SupplierInvoiceNumber.Should().Be("SUP-100");
        saved.Currency.Should().Be("EUR");
        saved.TotalNetMinor.Should().Be(2000);
        saved.TotalTaxMinor.Should().Be(380);
        saved.TotalGrossMinor.Should().Be(2380);
        saved.Lines.Should().ContainSingle();
        db.Set<JournalEntry>().Should().BeEmpty();
    }

    [Fact]
    public async Task Match_Should_UsePurchaseOrderAndPostedGoodsReceiptEvidence()
    {
        await using var db = SupplierInvoiceTestDbContext.Create();
        var ids = await SeedPurchasingAsync(db, postedReceipt: true);
        var invoiceId = await new CreateSupplierInvoiceHandler(db, new FixedClock(Now)).HandleAsync(BuildCreate(ids), TestContext.Current.CancellationToken);
        var invoice = await db.Set<SupplierInvoice>().SingleAsync(TestContext.Current.CancellationToken);
        invoice.RowVersion = [1, 2, 3];
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = CreateLifecycleHandler(db);

        await handler.HandleAsync(new SupplierInvoiceLifecycleActionDto { Id = invoiceId, RowVersion = [1, 2, 3], Action = "Match" }, TestContext.Current.CancellationToken);

        var matched = await db.Set<SupplierInvoice>().Include(x => x.Lines).SingleAsync(TestContext.Current.CancellationToken);
        matched.Status.Should().Be(SupplierInvoiceStatus.Matched);
        matched.MatchedAtUtc.Should().Be(Now);
        matched.Lines.Single().MatchStatus.Should().Be(SupplierInvoiceLineMatchStatus.Matched);
    }

    [Fact]
    public async Task Match_Should_RecordDiscrepancy_WhenGoodsReceiptIsNotPosted()
    {
        await using var db = SupplierInvoiceTestDbContext.Create();
        var ids = await SeedPurchasingAsync(db, postedReceipt: false);
        var invoiceId = await new CreateSupplierInvoiceHandler(db, new FixedClock(Now)).HandleAsync(BuildCreate(ids), TestContext.Current.CancellationToken);
        var invoice = await db.Set<SupplierInvoice>().SingleAsync(TestContext.Current.CancellationToken);
        invoice.RowVersion = [4, 5, 6];
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await CreateLifecycleHandler(db).HandleAsync(new SupplierInvoiceLifecycleActionDto { Id = invoiceId, RowVersion = [4, 5, 6], Action = "Match" }, TestContext.Current.CancellationToken);

        var matched = await db.Set<SupplierInvoice>().Include(x => x.Lines).SingleAsync(TestContext.Current.CancellationToken);
        matched.Status.Should().Be(SupplierInvoiceStatus.Draft);
        matched.Lines.Single().MatchStatus.Should().Be(SupplierInvoiceLineMatchStatus.Discrepancy);
        matched.Lines.Single().DiscrepancyReason.Should().Contain("GoodsReceiptNotPosted");
    }

    [Fact]
    public async Task Approve_Should_ReserveInternalNumberOnceAndCreateNoJournalEntry()
    {
        await using var db = SupplierInvoiceTestDbContext.Create();
        var ids = await SeedPurchasingAsync(db, postedReceipt: true);
        db.Set<NumberSequence>().Add(new NumberSequence
        {
            BusinessId = ids.BusinessId,
            DocumentType = NumberSequenceDocumentType.SupplierInvoice,
            ScopeKey = NumberSequenceService.GlobalScopeKey,
            PrefixPattern = "SI-{seq}",
            NextValue = 7,
            PaddingLength = 3,
            IsActive = true,
            MetadataJson = "{}"
        });
        var invoiceId = await new CreateSupplierInvoiceHandler(db, new FixedClock(Now)).HandleAsync(BuildCreate(ids), TestContext.Current.CancellationToken);
        var invoice = await db.Set<SupplierInvoice>().SingleAsync(TestContext.Current.CancellationToken);
        invoice.RowVersion = [7];
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var lifecycle = CreateLifecycleHandler(db);
        await lifecycle.HandleAsync(new SupplierInvoiceLifecycleActionDto { Id = invoiceId, RowVersion = [7], Action = "Match" }, TestContext.Current.CancellationToken);
        invoice = await db.Set<SupplierInvoice>().SingleAsync(TestContext.Current.CancellationToken);
        invoice.RowVersion = [8];
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await lifecycle.HandleAsync(new SupplierInvoiceLifecycleActionDto { Id = invoiceId, RowVersion = [8], Action = "Approve" }, TestContext.Current.CancellationToken);

        var approved = await db.Set<SupplierInvoice>().SingleAsync(TestContext.Current.CancellationToken);
        approved.Status.Should().Be(SupplierInvoiceStatus.Approved);
        approved.InternalInvoiceNumber.Should().Be("SI-007");
        approved.ApprovedAtUtc.Should().Be(Now);
        db.Set<JournalEntry>().Should().BeEmpty();
    }

    [Fact]
    public async Task Create_Should_RejectSensitiveMetadata()
    {
        await using var db = SupplierInvoiceTestDbContext.Create();
        var ids = await SeedPurchasingAsync(db, postedReceipt: true);
        var dto = BuildCreate(ids);
        dto.MetadataJson = "{\"accessToken\":\"secret\"}";

        var act = async () => await new CreateSupplierInvoiceHandler(db, new FixedClock(Now)).HandleAsync(dto, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*SensitiveMetadataRejected*");
    }

    [Fact]
    public async Task Post_Should_CreateHybridPayablePosting_ForApprovedMatchedInvoice()
    {
        await using var db = SupplierInvoiceTestDbContext.Create();
        var ids = await SeedPurchasingAsync(db, postedReceipt: true);
        SeedPostingAccounts(db, ids.BusinessId);
        var serviceLine = new SupplierInvoiceLineDto
        {
            Description = "Consulting",
            InvoicedQuantity = 1,
            UnitNetMinor = 500,
            UnitTaxMinor = 95,
            UnitGrossMinor = 595
        };
        var create = BuildCreate(ids);
        create.Lines.Add(serviceLine);
        var invoiceId = await new CreateSupplierInvoiceHandler(db, new FixedClock(Now)).HandleAsync(create, TestContext.Current.CancellationToken);
        var invoice = await db.Set<SupplierInvoice>().SingleAsync(TestContext.Current.CancellationToken);
        invoice.RowVersion = [10];
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var lifecycle = CreateLifecycleHandler(db);
        await lifecycle.HandleAsync(new SupplierInvoiceLifecycleActionDto { Id = invoiceId, RowVersion = [10], Action = "Match" }, TestContext.Current.CancellationToken);
        invoice = await db.Set<SupplierInvoice>().SingleAsync(TestContext.Current.CancellationToken);
        invoice.RowVersion = [11];
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        await lifecycle.HandleAsync(new SupplierInvoiceLifecycleActionDto { Id = invoiceId, RowVersion = [11], Action = "Approve" }, TestContext.Current.CancellationToken);
        invoice = await db.Set<SupplierInvoice>().SingleAsync(TestContext.Current.CancellationToken);
        invoice.RowVersion = [12];
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await CreatePostHandler(db).HandleAsync(new SupplierInvoiceLifecycleActionDto { Id = invoiceId, RowVersion = [12], Action = "Post" }, TestContext.Current.CancellationToken);

        var posted = await db.Set<SupplierInvoice>().SingleAsync(TestContext.Current.CancellationToken);
        posted.Status.Should().Be(SupplierInvoiceStatus.Posted);
        posted.PostedAtUtc.Should().Be(Now);
        posted.PostingJournalEntryId.Should().NotBeNull();
        var entry = await db.Set<JournalEntry>().Include(x => x.Lines).SingleAsync(TestContext.Current.CancellationToken);
        entry.PostingKind.Should().Be(JournalEntryPostingKind.SupplierInvoicePosted);
        entry.PostingKey.Should().Be($"{PostSupplierInvoiceHandler.PostingKeyPrefix}:{invoiceId}");
        entry.SourceEntityType.Should().Be("SupplierInvoice");
        entry.SourceEntityId.Should().Be(invoiceId);
        entry.Lines.Sum(x => x.DebitMinor).Should().Be(2975);
        entry.Lines.Sum(x => x.CreditMinor).Should().Be(2975);
        entry.Lines.Should().Contain(x => x.DebitMinor == 2000 && x.Memo == "Inventory clearing");
        entry.Lines.Should().Contain(x => x.DebitMinor == 500 && x.Memo == "Purchase expense");
        entry.Lines.Should().Contain(x => x.DebitMinor == 475 && x.Memo == "Input tax");
        entry.Lines.Should().Contain(x => x.CreditMinor == 2975 && x.Memo == "Accounts payable");
    }

    [Fact]
    public async Task Post_Should_BeIdempotent_ForAlreadyCreatedPostingKey()
    {
        await using var db = SupplierInvoiceTestDbContext.Create();
        var ids = await SeedPurchasingAsync(db, postedReceipt: true);
        SeedPostingAccounts(db, ids.BusinessId);
        var invoiceId = await CreateApprovedInvoiceAsync(db, ids, [21]);
        var invoice = await db.Set<SupplierInvoice>().SingleAsync(TestContext.Current.CancellationToken);
        invoice.RowVersion = [22];
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = CreatePostHandler(db);

        await handler.HandleAsync(new SupplierInvoiceLifecycleActionDto { Id = invoiceId, RowVersion = [22], Action = "Post" }, TestContext.Current.CancellationToken);
        invoice = await db.Set<SupplierInvoice>().SingleAsync(TestContext.Current.CancellationToken);
        invoice.Status = SupplierInvoiceStatus.Approved;
        invoice.PostedAtUtc = null;
        invoice.PostingJournalEntryId = null;
        invoice.RowVersion = [23];
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        await handler.HandleAsync(new SupplierInvoiceLifecycleActionDto { Id = invoiceId, RowVersion = [23], Action = "Post" }, TestContext.Current.CancellationToken);

        db.Set<JournalEntry>().Should().ContainSingle();
        var posted = await db.Set<SupplierInvoice>().SingleAsync(TestContext.Current.CancellationToken);
        posted.Status.Should().Be(SupplierInvoiceStatus.Posted);
        posted.PostingJournalEntryId.Should().Be(db.Set<JournalEntry>().Single().Id);
    }

    [Fact]
    public async Task Post_Should_RejectMissingPayablesAccountMapping()
    {
        await using var db = SupplierInvoiceTestDbContext.Create();
        var ids = await SeedPurchasingAsync(db, postedReceipt: true);
        var invoiceId = await CreateApprovedInvoiceAsync(db, ids, [31]);
        var invoice = await db.Set<SupplierInvoice>().SingleAsync(TestContext.Current.CancellationToken);
        invoice.RowVersion = [32];
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var act = async () => await CreatePostHandler(db).HandleAsync(new SupplierInvoiceLifecycleActionDto { Id = invoiceId, RowVersion = [32], Action = "Post" }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Missing finance account mappings*");
        db.Set<JournalEntry>().Should().BeEmpty();
    }

    [Fact]
    public async Task Match_Should_RecordDiscrepancy_WhenCumulativeApprovedInvoicesExceedReceipt()
    {
        await using var db = SupplierInvoiceTestDbContext.Create();
        var ids = await SeedPurchasingAsync(db, postedReceipt: true);
        db.Set<SupplierInvoice>().Add(new SupplierInvoice
        {
            Id = Guid.NewGuid(),
            BusinessId = ids.BusinessId,
            SupplierId = ids.SupplierId,
            PurchaseOrderId = ids.PurchaseOrderId,
            GoodsReceiptId = ids.GoodsReceiptId,
            SupplierInvoiceNumber = "SUP-OLD",
            Status = SupplierInvoiceStatus.Approved,
            InvoiceDateUtc = Now,
            Currency = "EUR",
            MetadataJson = "{}",
            Lines =
            [
                new SupplierInvoiceLine
                {
                    PurchaseOrderLineId = ids.PurchaseOrderLineId,
                    GoodsReceiptLineId = ids.GoodsReceiptLineId,
                    Description = "Old",
                    InvoicedQuantity = 1,
                    UnitNetMinor = 1000,
                    UnitGrossMinor = 1190,
                    TotalNetMinor = 1000,
                    TotalGrossMinor = 1190,
                    MatchStatus = SupplierInvoiceLineMatchStatus.Matched
                }
            ]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var create = BuildCreate(ids);
        create.Lines[0].InvoicedQuantity = 2;
        var invoiceId = await new CreateSupplierInvoiceHandler(db, new FixedClock(Now)).HandleAsync(create, TestContext.Current.CancellationToken);
        var invoice = await db.Set<SupplierInvoice>().SingleAsync(x => x.Id == invoiceId, TestContext.Current.CancellationToken);
        invoice.RowVersion = [41];
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await CreateLifecycleHandler(db).HandleAsync(new SupplierInvoiceLifecycleActionDto { Id = invoiceId, RowVersion = [41], Action = "Match" }, TestContext.Current.CancellationToken);

        var matched = await db.Set<SupplierInvoice>().Include(x => x.Lines).SingleAsync(x => x.Id == invoiceId, TestContext.Current.CancellationToken);
        matched.Status.Should().Be(SupplierInvoiceStatus.Draft);
        matched.Lines.Single().DiscrepancyReason.Should().Contain("QuantityExceedsAcceptedReceipt");
    }

    [Fact]
    public async Task Void_Should_RejectPostedInvoice()
    {
        await using var db = SupplierInvoiceTestDbContext.Create();
        var ids = await SeedPurchasingAsync(db, postedReceipt: true);
        var invoiceId = await new CreateSupplierInvoiceHandler(db, new FixedClock(Now)).HandleAsync(BuildCreate(ids), TestContext.Current.CancellationToken);
        var invoice = await db.Set<SupplierInvoice>().SingleAsync(TestContext.Current.CancellationToken);
        invoice.Status = SupplierInvoiceStatus.Posted;
        invoice.RowVersion = [51];
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var act = async () => await CreateLifecycleHandler(db).HandleAsync(new SupplierInvoiceLifecycleActionDto { Id = invoiceId, RowVersion = [51], Action = "Void" }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*SupplierInvoiceLifecycleUnsupportedAction*");
    }

    [Fact]
    public async Task SupplierPayment_Create_Should_PersistDraftAllocationsWithoutPosting()
    {
        await using var db = SupplierInvoiceTestDbContext.Create();
        var ids = await SeedPurchasingAsync(db, postedReceipt: true);
        var invoiceId = await CreatePostedSupplierInvoiceAsync(db, ids, [60]);
        var handler = new CreateSupplierPaymentHandler(db, new FixedClock(Now));

        var paymentId = await handler.HandleAsync(BuildPaymentCreate(ids, invoiceId, 1000), TestContext.Current.CancellationToken);

        var payment = await db.Set<SupplierPayment>().Include(x => x.Allocations).SingleAsync(TestContext.Current.CancellationToken);
        payment.Id.Should().Be(paymentId);
        payment.Status.Should().Be(SupplierPaymentStatus.Draft);
        payment.Currency.Should().Be("EUR");
        payment.TotalAmountMinor.Should().Be(1000);
        payment.Allocations.Should().ContainSingle(x => x.SupplierInvoiceId == invoiceId && x.AmountMinor == 1000);
        db.Set<JournalEntry>().Count().Should().Be(1);
    }

    [Fact]
    public async Task SupplierPayment_Create_Should_RejectNonPostedInvoice()
    {
        await using var db = SupplierInvoiceTestDbContext.Create();
        var ids = await SeedPurchasingAsync(db, postedReceipt: true);
        var invoiceId = await CreateApprovedInvoiceAsync(db, ids, [70]);

        var act = async () => await new CreateSupplierPaymentHandler(db, new FixedClock(Now))
            .HandleAsync(BuildPaymentCreate(ids, invoiceId, 1000), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*SupplierPaymentRequiresPostedInvoice*");
    }

    [Fact]
    public async Task SupplierPayment_Create_Should_RejectCumulativeOverpayment()
    {
        await using var db = SupplierInvoiceTestDbContext.Create();
        var ids = await SeedPurchasingAsync(db, postedReceipt: true);
        var invoiceId = await CreatePostedSupplierInvoiceAsync(db, ids, [80]);
        db.Set<SupplierPayment>().Add(new SupplierPayment
        {
            BusinessId = ids.BusinessId,
            SupplierId = ids.SupplierId,
            Status = SupplierPaymentStatus.Posted,
            PaymentDateUtc = Now,
            Currency = "EUR",
            TotalAmountMinor = 2000,
            MetadataJson = "{}",
            Allocations = [new SupplierPaymentAllocation { SupplierInvoiceId = invoiceId, AmountMinor = 2000 }]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var act = async () => await new CreateSupplierPaymentHandler(db, new FixedClock(Now))
            .HandleAsync(BuildPaymentCreate(ids, invoiceId, 381), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*SupplierPaymentOverpaymentRejected*");
    }

    [Fact]
    public async Task SupplierPayment_Post_Should_CreateBalancedSettlementPosting()
    {
        await using var db = SupplierInvoiceTestDbContext.Create();
        var ids = await SeedPurchasingAsync(db, postedReceipt: true);
        SeedPostingAccounts(db, ids.BusinessId);
        SeedMappedAccount(db, ids.BusinessId, FinancePostingAccountRole.CashClearing, AccountType.Asset);
        db.Set<NumberSequence>().Add(new NumberSequence
        {
            BusinessId = ids.BusinessId,
            DocumentType = NumberSequenceDocumentType.SupplierPayment,
            ScopeKey = NumberSequenceService.GlobalScopeKey,
            PrefixPattern = "SP-{seq}",
            NextValue = 3,
            PaddingLength = 3,
            IsActive = true,
            MetadataJson = "{}"
        });
        var invoiceId = await CreatePostedSupplierInvoiceAsync(db, ids, [90]);
        var paymentId = await new CreateSupplierPaymentHandler(db, new FixedClock(Now))
            .HandleAsync(BuildPaymentCreate(ids, invoiceId, 2380), TestContext.Current.CancellationToken);
        var payment = await db.Set<SupplierPayment>().SingleAsync(x => x.Id == paymentId, TestContext.Current.CancellationToken);
        payment.RowVersion = [91];
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await CreatePostSupplierPaymentHandler(db).HandleAsync(new SupplierPaymentLifecycleActionDto { Id = paymentId, RowVersion = [91], Action = "Post" }, TestContext.Current.CancellationToken);

        var posted = await db.Set<SupplierPayment>().SingleAsync(x => x.Id == paymentId, TestContext.Current.CancellationToken);
        posted.Status.Should().Be(SupplierPaymentStatus.Posted);
        posted.PaymentNumber.Should().Be("SP-003");
        posted.PostedAtUtc.Should().Be(Now);
        posted.PostingJournalEntryId.Should().NotBeNull();
        var entry = await db.Set<JournalEntry>().Include(x => x.Lines).SingleAsync(x => x.PostingKind == JournalEntryPostingKind.SupplierPaymentPosted, TestContext.Current.CancellationToken);
        entry.PostingKey.Should().Be($"{PostSupplierPaymentHandler.PostingKeyPrefix}:{paymentId}");
        entry.SourceEntityType.Should().Be("SupplierPayment");
        entry.Lines.Sum(x => x.DebitMinor).Should().Be(2380);
        entry.Lines.Sum(x => x.CreditMinor).Should().Be(2380);
        entry.Lines.Should().Contain(x => x.DebitMinor == 2380 && x.Memo == "Accounts payable settlement");
        entry.Lines.Should().Contain(x => x.CreditMinor == 2380 && x.Memo == "Cash clearing");
    }

    [Fact]
    public async Task SupplierPayment_Post_Should_RejectMissingCashClearingMapping()
    {
        await using var db = SupplierInvoiceTestDbContext.Create();
        var ids = await SeedPurchasingAsync(db, postedReceipt: true);
        SeedMappedAccount(db, ids.BusinessId, FinancePostingAccountRole.AccountsPayable, AccountType.Liability);
        var invoiceId = await CreatePostedSupplierInvoiceAsync(db, ids, [100]);
        var paymentId = await new CreateSupplierPaymentHandler(db, new FixedClock(Now))
            .HandleAsync(BuildPaymentCreate(ids, invoiceId, 1000), TestContext.Current.CancellationToken);
        var payment = await db.Set<SupplierPayment>().SingleAsync(x => x.Id == paymentId, TestContext.Current.CancellationToken);
        payment.RowVersion = [101];
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var act = async () => await CreatePostSupplierPaymentHandler(db).HandleAsync(new SupplierPaymentLifecycleActionDto { Id = paymentId, RowVersion = [101], Action = "Post" }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Missing finance account mappings*");
        db.Set<JournalEntry>().Count(x => x.PostingKind == JournalEntryPostingKind.SupplierPaymentPosted).Should().Be(0);
    }

    [Fact]
    public async Task SupplierPayment_Cancel_Should_RejectPostedPayment()
    {
        await using var db = SupplierInvoiceTestDbContext.Create();
        var ids = await SeedPurchasingAsync(db, postedReceipt: true);
        var invoiceId = await CreatePostedSupplierInvoiceAsync(db, ids, [110]);
        db.Set<SupplierPayment>().Add(new SupplierPayment
        {
            Id = Guid.NewGuid(),
            BusinessId = ids.BusinessId,
            SupplierId = ids.SupplierId,
            Status = SupplierPaymentStatus.Posted,
            PaymentDateUtc = Now,
            Currency = "EUR",
            TotalAmountMinor = 1000,
            RowVersion = [111],
            MetadataJson = "{}",
            Allocations = [new SupplierPaymentAllocation { SupplierInvoiceId = invoiceId, AmountMinor = 1000 }]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var payment = await db.Set<SupplierPayment>().SingleAsync(TestContext.Current.CancellationToken);

        var act = async () => await new CancelSupplierPaymentHandler(db, new FixedClock(Now), new SupplierPaymentWorkflowPolicy())
            .HandleAsync(new SupplierPaymentLifecycleActionDto { Id = payment.Id, RowVersion = [111], Action = "Cancel" }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*SupplierPaymentLifecycleUnsupportedAction*");
    }

    [Fact]
    public async Task SupplierPayment_Reverse_Should_CreateBalancedReversalPosting()
    {
        await using var db = SupplierInvoiceTestDbContext.Create();
        var ids = await SeedPurchasingAsync(db, postedReceipt: true);
        SeedPostingAccounts(db, ids.BusinessId);
        SeedMappedAccount(db, ids.BusinessId, FinancePostingAccountRole.CashClearing, AccountType.Asset);
        var invoiceId = await CreatePostedSupplierInvoiceAsync(db, ids, [120]);
        var paymentId = await CreatePostedSupplierPaymentAsync(db, ids, invoiceId, 1200, [121]);
        var payment = await db.Set<SupplierPayment>().SingleAsync(x => x.Id == paymentId, TestContext.Current.CancellationToken);
        payment.RowVersion = [122];
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await CreateReverseSupplierPaymentHandler(db).HandleAsync(new SupplierPaymentLifecycleActionDto
        {
            Id = paymentId,
            RowVersion = [122],
            Action = "Reverse",
            Reason = "Duplicate transfer correction"
        }, TestContext.Current.CancellationToken);

        var reversed = await db.Set<SupplierPayment>().SingleAsync(x => x.Id == paymentId, TestContext.Current.CancellationToken);
        reversed.Status.Should().Be(SupplierPaymentStatus.Reversed);
        reversed.ReversedAtUtc.Should().Be(Now);
        reversed.ReversalReason.Should().Be("Duplicate transfer correction");
        reversed.ReversalJournalEntryId.Should().NotBeNull();
        var entry = await db.Set<JournalEntry>().Include(x => x.Lines).SingleAsync(x => x.PostingKind == JournalEntryPostingKind.Reversal && x.SourceEntityId == paymentId, TestContext.Current.CancellationToken);
        entry.PostingKey.Should().Be($"{ReverseSupplierPaymentHandler.PostingKeyPrefix}:{paymentId}");
        entry.SourceEntityType.Should().Be("SupplierPayment");
        entry.Lines.Sum(x => x.DebitMinor).Should().Be(1200);
        entry.Lines.Sum(x => x.CreditMinor).Should().Be(1200);
        entry.Lines.Should().Contain(x => x.DebitMinor == 1200 && x.Memo == "Cash clearing reversal");
        entry.Lines.Should().Contain(x => x.CreditMinor == 1200 && x.Memo == "Accounts payable reinstatement");
    }

    [Theory]
    [InlineData(SupplierPaymentStatus.Draft)]
    [InlineData(SupplierPaymentStatus.Cancelled)]
    [InlineData(SupplierPaymentStatus.Reversed)]
    public async Task SupplierPayment_Reverse_Should_RejectInvalidStatuses(SupplierPaymentStatus status)
    {
        await using var db = SupplierInvoiceTestDbContext.Create();
        var ids = await SeedPurchasingAsync(db, postedReceipt: true);
        var invoiceId = await CreatePostedSupplierInvoiceAsync(db, ids, [130]);
        db.Set<SupplierPayment>().Add(new SupplierPayment
        {
            Id = Guid.NewGuid(),
            BusinessId = ids.BusinessId,
            SupplierId = ids.SupplierId,
            Status = status,
            PaymentDateUtc = Now,
            Currency = "EUR",
            TotalAmountMinor = 1000,
            PostingJournalEntryId = status == SupplierPaymentStatus.Draft ? null : Guid.NewGuid(),
            RowVersion = [131],
            MetadataJson = "{}",
            Allocations = [new SupplierPaymentAllocation { SupplierInvoiceId = invoiceId, AmountMinor = 1000 }]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var payment = await db.Set<SupplierPayment>().SingleAsync(TestContext.Current.CancellationToken);

        var act = async () => await CreateReverseSupplierPaymentHandler(db).HandleAsync(new SupplierPaymentLifecycleActionDto
        {
            Id = payment.Id,
            RowVersion = [131],
            Action = "Reverse",
            Reason = "Operator correction"
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*SupplierPaymentLifecycleUnsupportedAction*");
    }

    [Fact]
    public async Task SupplierPayment_Reverse_Should_RejectMissingPostingAndSensitiveReason()
    {
        await using var db = SupplierInvoiceTestDbContext.Create();
        var ids = await SeedPurchasingAsync(db, postedReceipt: true);
        var invoiceId = await CreatePostedSupplierInvoiceAsync(db, ids, [140]);
        db.Set<SupplierPayment>().Add(new SupplierPayment
        {
            Id = Guid.NewGuid(),
            BusinessId = ids.BusinessId,
            SupplierId = ids.SupplierId,
            Status = SupplierPaymentStatus.Posted,
            PaymentDateUtc = Now,
            Currency = "EUR",
            TotalAmountMinor = 1000,
            RowVersion = [141],
            MetadataJson = "{}",
            Allocations = [new SupplierPaymentAllocation { SupplierInvoiceId = invoiceId, AmountMinor = 1000 }]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var payment = await db.Set<SupplierPayment>().SingleAsync(TestContext.Current.CancellationToken);

        var missingPosting = async () => await CreateReverseSupplierPaymentHandler(db).HandleAsync(new SupplierPaymentLifecycleActionDto
        {
            Id = payment.Id,
            RowVersion = [141],
            Action = "Reverse",
            Reason = "Operator correction"
        }, TestContext.Current.CancellationToken);
        var sensitiveReason = async () => await CreateReverseSupplierPaymentHandler(db).HandleAsync(new SupplierPaymentLifecycleActionDto
        {
            Id = payment.Id,
            RowVersion = [141],
            Action = "Reverse",
            Reason = "token leaked"
        }, TestContext.Current.CancellationToken);

        await missingPosting.Should().ThrowAsync<InvalidOperationException>().WithMessage("*SupplierPaymentPostingRequired*");
        await sensitiveReason.Should().ThrowAsync<ArgumentException>().WithMessage("*SensitiveMetadataRejected*");
    }

    [Fact]
    public async Task SupplierPayment_Reverse_Should_ReopenPayableAndNotDuplicateOnRetry()
    {
        await using var db = SupplierInvoiceTestDbContext.Create();
        var ids = await SeedPurchasingAsync(db, postedReceipt: true);
        SeedPostingAccounts(db, ids.BusinessId);
        SeedMappedAccount(db, ids.BusinessId, FinancePostingAccountRole.CashClearing, AccountType.Asset);
        var invoiceId = await CreatePostedSupplierInvoiceAsync(db, ids, [150]);
        var paymentId = await CreatePostedSupplierPaymentAsync(db, ids, invoiceId, 2380, [151]);
        var beforeReverse = await SupplierPaymentQuerySupport.GetPostedPaidByInvoiceAsync(db, [invoiceId], null, TestContext.Current.CancellationToken);
        beforeReverse[invoiceId].Should().Be(2380);
        var payment = await db.Set<SupplierPayment>().SingleAsync(x => x.Id == paymentId, TestContext.Current.CancellationToken);
        payment.RowVersion = [152];
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await CreateReverseSupplierPaymentHandler(db).HandleAsync(new SupplierPaymentLifecycleActionDto
        {
            Id = paymentId,
            RowVersion = [152],
            Action = "Reverse",
            Reason = "Payment entered against wrong account"
        }, TestContext.Current.CancellationToken);
        var reversed = await db.Set<SupplierPayment>().SingleAsync(x => x.Id == paymentId, TestContext.Current.CancellationToken);
        reversed.RowVersion = [153];
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var retry = async () => await CreateReverseSupplierPaymentHandler(db).HandleAsync(new SupplierPaymentLifecycleActionDto
        {
            Id = paymentId,
            RowVersion = [153],
            Action = "Reverse",
            Reason = "Payment entered against wrong account"
        }, TestContext.Current.CancellationToken);

        await retry.Should().ThrowAsync<InvalidOperationException>().WithMessage("*SupplierPaymentLifecycleUnsupportedAction*");
        db.Set<JournalEntry>().Count(x => x.PostingKind == JournalEntryPostingKind.Reversal && x.SourceEntityId == paymentId).Should().Be(1);
        var afterReverse = await SupplierPaymentQuerySupport.GetPostedPaidByInvoiceAsync(db, [invoiceId], null, TestContext.Current.CancellationToken);
        afterReverse.Should().NotContainKey(invoiceId);
    }

    [Fact]
    public async Task SupplierPayment_BankSettlement_Should_CreateBalancedClearingPosting()
    {
        await using var db = SupplierInvoiceTestDbContext.Create();
        var ids = await SeedPurchasingAsync(db, postedReceipt: true);
        SeedPostingAccounts(db, ids.BusinessId);
        SeedMappedAccount(db, ids.BusinessId, FinancePostingAccountRole.CashClearing, AccountType.Asset);
        var invoiceId = await CreatePostedSupplierInvoiceAsync(db, ids, [160]);
        var paymentId = await CreatePostedSupplierPaymentAsync(db, ids, invoiceId, 2380, [161]);
        var payment = await db.Set<SupplierPayment>().SingleAsync(x => x.Id == paymentId, TestContext.Current.CancellationToken);
        var settlement = await SeedBankSettlementReconciliationAsync(db, ids.BusinessId, payment, 2380);
        payment.RowVersion = [162];
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await CreateSettleSupplierPaymentHandler(db).HandleAsync(new SupplierPaymentBankSettlementActionDto
        {
            Id = paymentId,
            RowVersion = [162],
            BankReconciliationMatchId = settlement.MatchId,
            Notes = "Statement line confirmed"
        }, TestContext.Current.CancellationToken);

        var settled = await db.Set<SupplierPayment>().SingleAsync(x => x.Id == paymentId, TestContext.Current.CancellationToken);
        settled.BankSettledAtUtc.Should().Be(Now);
        settled.BankSettlementReconciliationMatchId.Should().Be(settlement.MatchId);
        settled.BankSettlementJournalEntryId.Should().NotBeNull();
        settled.BankSettlementNotes.Should().Be("Statement line confirmed");
        var entry = await db.Set<JournalEntry>().Include(x => x.Lines).SingleAsync(x => x.PostingKind == JournalEntryPostingKind.SupplierPaymentBankSettled, TestContext.Current.CancellationToken);
        entry.PostingKey.Should().Be($"{SettleSupplierPaymentFromBankReconciliationHandler.PostingKeyPrefix}:{paymentId}");
        entry.SourceEntityType.Should().Be("SupplierPayment");
        entry.Lines.Sum(x => x.DebitMinor).Should().Be(2380);
        entry.Lines.Sum(x => x.CreditMinor).Should().Be(2380);
        entry.Lines.Should().Contain(x => x.DebitMinor == 2380 && x.Memo == "Cash clearing release");
        entry.Lines.Should().Contain(x => x.CreditMinor == 2380 && x.AccountId == settlement.BankFinancialAccountId && x.Memo == "Bank account settlement");
    }

    [Theory]
    [InlineData(SupplierPaymentStatus.Draft)]
    [InlineData(SupplierPaymentStatus.Cancelled)]
    [InlineData(SupplierPaymentStatus.Reversed)]
    public async Task SupplierPayment_BankSettlement_Should_RejectInvalidPaymentStatus(SupplierPaymentStatus status)
    {
        await using var db = SupplierInvoiceTestDbContext.Create();
        var ids = await SeedPurchasingAsync(db, postedReceipt: true);
        var invoiceId = await CreatePostedSupplierInvoiceAsync(db, ids, [170]);
        db.Set<SupplierPayment>().Add(new SupplierPayment
        {
            Id = Guid.NewGuid(),
            BusinessId = ids.BusinessId,
            SupplierId = ids.SupplierId,
            Status = status,
            PaymentDateUtc = Now,
            Currency = "EUR",
            TotalAmountMinor = 1000,
            PostingJournalEntryId = status == SupplierPaymentStatus.Draft ? null : Guid.NewGuid(),
            RowVersion = [171],
            MetadataJson = "{}",
            Allocations = [new SupplierPaymentAllocation { SupplierInvoiceId = invoiceId, AmountMinor = 1000 }]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var payment = await db.Set<SupplierPayment>().SingleAsync(TestContext.Current.CancellationToken);

        var act = async () => await CreateSettleSupplierPaymentHandler(db).HandleAsync(new SupplierPaymentBankSettlementActionDto
        {
            Id = payment.Id,
            RowVersion = [171],
            BankReconciliationMatchId = Guid.NewGuid()
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*SupplierPaymentLifecycleUnsupportedAction*");
    }

    [Fact]
    public async Task SupplierPayment_BankSettlement_Should_RejectInvalidReconciliationAndBankMapping()
    {
        await using var db = SupplierInvoiceTestDbContext.Create();
        var ids = await SeedPurchasingAsync(db, postedReceipt: true);
        SeedPostingAccounts(db, ids.BusinessId);
        SeedMappedAccount(db, ids.BusinessId, FinancePostingAccountRole.CashClearing, AccountType.Asset);
        var invoiceId = await CreatePostedSupplierInvoiceAsync(db, ids, [180]);
        var paymentId = await CreatePostedSupplierPaymentAsync(db, ids, invoiceId, 2380, [181]);
        var payment = await db.Set<SupplierPayment>().SingleAsync(x => x.Id == paymentId, TestContext.Current.CancellationToken);
        var partial = await SeedBankSettlementReconciliationAsync(db, ids.BusinessId, payment, 1000);
        payment.RowVersion = [182];
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var partialAct = async () => await CreateSettleSupplierPaymentHandler(db).HandleAsync(new SupplierPaymentBankSettlementActionDto
        {
            Id = paymentId,
            RowVersion = [182],
            BankReconciliationMatchId = partial.MatchId
        }, TestContext.Current.CancellationToken);

        await partialAct.Should().ThrowAsync<InvalidOperationException>().WithMessage("*RequiresFullAmount*");

        var unmapped = await SeedBankSettlementReconciliationAsync(db, ids.BusinessId, payment, 2380, mapBankAccount: false, identitySuffix: "unmapped");
        payment.RowVersion = [183];
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var unmappedAct = async () => await CreateSettleSupplierPaymentHandler(db).HandleAsync(new SupplierPaymentBankSettlementActionDto
        {
            Id = paymentId,
            RowVersion = [183],
            BankReconciliationMatchId = unmapped.MatchId
        }, TestContext.Current.CancellationToken);

        await unmappedAct.Should().ThrowAsync<InvalidOperationException>().WithMessage("*RequiresMappedBankAccount*");
        db.Set<JournalEntry>().Count(x => x.PostingKind == JournalEntryPostingKind.SupplierPaymentBankSettled).Should().Be(0);
    }

    [Fact]
    public async Task SupplierPayment_BankSettlement_Should_BlockRetryAndReversalAfterSuccess()
    {
        await using var db = SupplierInvoiceTestDbContext.Create();
        var ids = await SeedPurchasingAsync(db, postedReceipt: true);
        SeedPostingAccounts(db, ids.BusinessId);
        SeedMappedAccount(db, ids.BusinessId, FinancePostingAccountRole.CashClearing, AccountType.Asset);
        var invoiceId = await CreatePostedSupplierInvoiceAsync(db, ids, [190]);
        var paymentId = await CreatePostedSupplierPaymentAsync(db, ids, invoiceId, 2380, [191]);
        var payment = await db.Set<SupplierPayment>().SingleAsync(x => x.Id == paymentId, TestContext.Current.CancellationToken);
        var settlement = await SeedBankSettlementReconciliationAsync(db, ids.BusinessId, payment, 2380);
        payment.RowVersion = [192];
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await CreateSettleSupplierPaymentHandler(db).HandleAsync(new SupplierPaymentBankSettlementActionDto { Id = paymentId, RowVersion = [192], BankReconciliationMatchId = settlement.MatchId }, TestContext.Current.CancellationToken);
        var settled = await db.Set<SupplierPayment>().SingleAsync(x => x.Id == paymentId, TestContext.Current.CancellationToken);
        settled.RowVersion = [193];
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var retry = async () => await CreateSettleSupplierPaymentHandler(db).HandleAsync(new SupplierPaymentBankSettlementActionDto { Id = paymentId, RowVersion = [193], BankReconciliationMatchId = settlement.MatchId }, TestContext.Current.CancellationToken);
        await retry.Should().ThrowAsync<InvalidOperationException>().WithMessage("*SupplierPaymentLifecycleUnsupportedAction*");
        db.Set<JournalEntry>().Count(x => x.PostingKind == JournalEntryPostingKind.SupplierPaymentBankSettled && x.SourceEntityId == paymentId).Should().Be(1);

        var reverse = async () => await CreateReverseSupplierPaymentHandler(db).HandleAsync(new SupplierPaymentLifecycleActionDto { Id = paymentId, RowVersion = [193], Action = "Reverse", Reason = "Returned after bank settlement" }, TestContext.Current.CancellationToken);
        await reverse.Should().ThrowAsync<InvalidOperationException>().WithMessage("*BankSettledReversalBlocked*");
    }

    [Fact]
    public async Task SupplierPaymentBankCorrection_ReturnedTransfer_Should_CreateDraftAndPostBalancedCorrection()
    {
        await using var db = SupplierInvoiceTestDbContext.Create();
        var ids = await SeedPurchasingAsync(db, postedReceipt: true);
        SeedPostingAccounts(db, ids.BusinessId);
        SeedMappedAccount(db, ids.BusinessId, FinancePostingAccountRole.CashClearing, AccountType.Asset);
        var invoiceId = await CreatePostedSupplierInvoiceAsync(db, ids, [210]);
        var paymentId = await CreatePostedSupplierPaymentAsync(db, ids, invoiceId, 2380, [211]);
        var payment = await db.Set<SupplierPayment>().SingleAsync(x => x.Id == paymentId, TestContext.Current.CancellationToken);
        var settlement = await SeedBankSettlementReconciliationAsync(db, ids.BusinessId, payment, 2380, identitySuffix: "correction");
        payment.RowVersion = [212];
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        await CreateSettleSupplierPaymentHandler(db).HandleAsync(new SupplierPaymentBankSettlementActionDto { Id = paymentId, RowVersion = [212], BankReconciliationMatchId = settlement.MatchId }, TestContext.Current.CancellationToken);
        payment = await db.Set<SupplierPayment>().SingleAsync(x => x.Id == paymentId, TestContext.Current.CancellationToken);
        var correctionEvidence = await SeedBankSettlementReconciliationAsync(db, ids.BusinessId, payment, 2380, identitySuffix: "returned-correction", journalEntryId: payment.BankSettlementJournalEntryId);
        payment.RowVersion = [213];
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var correctionId = await CreateSupplierPaymentBankCorrectionHandler(db).HandleAsync(new SupplierPaymentBankCorrectionCreateDto
        {
            SupplierPaymentId = paymentId,
            SupplierPaymentRowVersion = [213],
            CorrectionType = SupplierPaymentBankCorrectionType.ReturnedTransfer,
            BankReconciliationMatchId = correctionEvidence.MatchId,
            Reason = "Bank returned the full transfer"
        }, TestContext.Current.CancellationToken);
        var correction = await db.Set<SupplierPaymentBankCorrection>().SingleAsync(x => x.Id == correctionId, TestContext.Current.CancellationToken);
        correction.Status.Should().Be(SupplierPaymentBankCorrectionStatus.Draft);
        correction.AmountMinor.Should().Be(2380);
        correction.OriginalBankSettlementJournalEntryId.Should().Be(payment.BankSettlementJournalEntryId);
        correction.RowVersion = [214];
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await CreatePostSupplierPaymentBankCorrectionHandler(db).HandleAsync(new SupplierPaymentBankCorrectionActionDto
        {
            Id = correctionId,
            RowVersion = [214]
        }, TestContext.Current.CancellationToken);

        var posted = await db.Set<SupplierPaymentBankCorrection>().SingleAsync(x => x.Id == correctionId, TestContext.Current.CancellationToken);
        posted.Status.Should().Be(SupplierPaymentBankCorrectionStatus.Posted);
        posted.PostedAtUtc.Should().Be(Now);
        posted.CorrectionJournalEntryId.Should().NotBeNull();
        var entry = await db.Set<JournalEntry>().Include(x => x.Lines).SingleAsync(x => x.PostingKind == JournalEntryPostingKind.SupplierPaymentBankCorrection, TestContext.Current.CancellationToken);
        entry.PostingKey.Should().Be($"{PostSupplierPaymentBankCorrectionHandler.PostingKeyPrefix}:{correctionId}");
        entry.SourceEntityType.Should().Be("SupplierPaymentBankCorrection");
        entry.Lines.Sum(x => x.DebitMinor).Should().Be(2380);
        entry.Lines.Sum(x => x.CreditMinor).Should().Be(2380);
        entry.Lines.Should().Contain(x => x.DebitMinor == 2380 && x.AccountId == correctionEvidence.BankFinancialAccountId);
        entry.Lines.Should().Contain(x => x.CreditMinor == 2380 && x.Memo == "Cash clearing reinstatement");
    }

    [Fact]
    public async Task SupplierPaymentBankCorrection_DuplicatePayment_Should_BeAttentionOnlyAndNotPost()
    {
        await using var db = SupplierInvoiceTestDbContext.Create();
        var ids = await SeedPurchasingAsync(db, postedReceipt: true);
        SeedPostingAccounts(db, ids.BusinessId);
        SeedMappedAccount(db, ids.BusinessId, FinancePostingAccountRole.CashClearing, AccountType.Asset);
        var invoiceId = await CreatePostedSupplierInvoiceAsync(db, ids, [220]);
        var paymentId = await CreatePostedSupplierPaymentAsync(db, ids, invoiceId, 2380, [221]);
        var payment = await db.Set<SupplierPayment>().SingleAsync(x => x.Id == paymentId, TestContext.Current.CancellationToken);
        var settlement = await SeedBankSettlementReconciliationAsync(db, ids.BusinessId, payment, 2380, identitySuffix: "duplicate");
        payment.RowVersion = [222];
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        await CreateSettleSupplierPaymentHandler(db).HandleAsync(new SupplierPaymentBankSettlementActionDto { Id = paymentId, RowVersion = [222], BankReconciliationMatchId = settlement.MatchId }, TestContext.Current.CancellationToken);
        payment = await db.Set<SupplierPayment>().SingleAsync(x => x.Id == paymentId, TestContext.Current.CancellationToken);
        var duplicateEvidence = await SeedBankSettlementReconciliationAsync(db, ids.BusinessId, payment, 2380, identitySuffix: "duplicate-evidence", journalEntryId: payment.BankSettlementJournalEntryId);
        payment.RowVersion = [223];
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var correctionId = await CreateSupplierPaymentBankCorrectionHandler(db).HandleAsync(new SupplierPaymentBankCorrectionCreateDto
        {
            SupplierPaymentId = paymentId,
            SupplierPaymentRowVersion = [223],
            CorrectionType = SupplierPaymentBankCorrectionType.DuplicatePayment,
            BankReconciliationMatchId = duplicateEvidence.MatchId,
            Reason = "Duplicate outgoing bank movement needs review"
        }, TestContext.Current.CancellationToken);
        var correction = await db.Set<SupplierPaymentBankCorrection>().SingleAsync(x => x.Id == correctionId, TestContext.Current.CancellationToken);
        correction.RowVersion = [224];
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var post = async () => await CreatePostSupplierPaymentBankCorrectionHandler(db).HandleAsync(new SupplierPaymentBankCorrectionActionDto
        {
            Id = correctionId,
            RowVersion = [224]
        }, TestContext.Current.CancellationToken);

        await post.Should().ThrowAsync<InvalidOperationException>().WithMessage("*DuplicatePaymentIsAttentionOnly*");
        db.Set<JournalEntry>().Count(x => x.PostingKind == JournalEntryPostingKind.SupplierPaymentBankCorrection).Should().Be(0);
    }

    [Fact]
    public async Task SupplierPaymentBankCorrection_Should_RejectSensitiveReasonAndPartialEvidence()
    {
        await using var db = SupplierInvoiceTestDbContext.Create();
        var ids = await SeedPurchasingAsync(db, postedReceipt: true);
        SeedPostingAccounts(db, ids.BusinessId);
        SeedMappedAccount(db, ids.BusinessId, FinancePostingAccountRole.CashClearing, AccountType.Asset);
        var invoiceId = await CreatePostedSupplierInvoiceAsync(db, ids, [230]);
        var paymentId = await CreatePostedSupplierPaymentAsync(db, ids, invoiceId, 2380, [231]);
        var payment = await db.Set<SupplierPayment>().SingleAsync(x => x.Id == paymentId, TestContext.Current.CancellationToken);
        var full = await SeedBankSettlementReconciliationAsync(db, ids.BusinessId, payment, 2380, identitySuffix: "full-correction");
        var partial = await SeedBankSettlementReconciliationAsync(db, ids.BusinessId, payment, 1000, identitySuffix: "partial-correction");
        payment.RowVersion = [232];
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        await CreateSettleSupplierPaymentHandler(db).HandleAsync(new SupplierPaymentBankSettlementActionDto { Id = paymentId, RowVersion = [232], BankReconciliationMatchId = full.MatchId }, TestContext.Current.CancellationToken);
        payment = await db.Set<SupplierPayment>().SingleAsync(x => x.Id == paymentId, TestContext.Current.CancellationToken);
        partial = await SeedBankSettlementReconciliationAsync(db, ids.BusinessId, payment, 1000, identitySuffix: "partial-after-settlement", journalEntryId: payment.BankSettlementJournalEntryId);
        payment.RowVersion = [233];
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sensitive = async () => await CreateSupplierPaymentBankCorrectionHandler(db).HandleAsync(new SupplierPaymentBankCorrectionCreateDto
        {
            SupplierPaymentId = paymentId,
            SupplierPaymentRowVersion = [233],
            CorrectionType = SupplierPaymentBankCorrectionType.ReturnedTransfer,
            BankReconciliationMatchId = full.MatchId,
            Reason = "secret token"
        }, TestContext.Current.CancellationToken);
        var partialEvidence = async () => await CreateSupplierPaymentBankCorrectionHandler(db).HandleAsync(new SupplierPaymentBankCorrectionCreateDto
        {
            SupplierPaymentId = paymentId,
            SupplierPaymentRowVersion = [233],
            CorrectionType = SupplierPaymentBankCorrectionType.ReturnedTransfer,
            BankReconciliationMatchId = partial.MatchId,
            Reason = "Returned only part of settlement"
        }, TestContext.Current.CancellationToken);

        await sensitive.Should().ThrowAsync<ArgumentException>().WithMessage("*SensitiveMetadataRejected*");
        await partialEvidence.Should().ThrowAsync<InvalidOperationException>().WithMessage("*RequiresFullSettlementEvidence*");
    }

    private static UpdateSupplierInvoiceLifecycleHandler CreateLifecycleHandler(SupplierInvoiceTestDbContext db)
        => new(db, new FixedClock(Now), new NumberSequenceService(db, new FixedClock(Now)), new SupplierInvoiceWorkflowPolicy());

    private static PostSupplierInvoiceHandler CreatePostHandler(SupplierInvoiceTestDbContext db)
        => new(db, new FixedClock(Now), new SupplierInvoiceWorkflowPolicy(), new FinanceAccountMappingService(db), new FinancePostingService(db, new FixedClock(Now)));

    private static PostSupplierPaymentHandler CreatePostSupplierPaymentHandler(SupplierInvoiceTestDbContext db)
        => new(db, new FixedClock(Now), new NumberSequenceService(db, new FixedClock(Now)), new SupplierPaymentWorkflowPolicy(), new FinanceAccountMappingService(db), new FinancePostingService(db, new FixedClock(Now)));

    private static ReverseSupplierPaymentHandler CreateReverseSupplierPaymentHandler(SupplierInvoiceTestDbContext db)
        => new(db, new FixedClock(Now), new SupplierPaymentWorkflowPolicy(), new FinanceAccountMappingService(db), new FinancePostingService(db, new FixedClock(Now)));

    private static SettleSupplierPaymentFromBankReconciliationHandler CreateSettleSupplierPaymentHandler(SupplierInvoiceTestDbContext db)
        => new(db, new FixedClock(Now), new SupplierPaymentWorkflowPolicy(), new FinanceAccountMappingService(db), new FinancePostingService(db, new FixedClock(Now)));

    private static CreateSupplierPaymentBankCorrectionHandler CreateSupplierPaymentBankCorrectionHandler(SupplierInvoiceTestDbContext db)
        => new(db, new FixedClock(Now));

    private static PostSupplierPaymentBankCorrectionHandler CreatePostSupplierPaymentBankCorrectionHandler(SupplierInvoiceTestDbContext db)
        => new(db, new FixedClock(Now), new FinanceAccountMappingService(db), new FinancePostingService(db, new FixedClock(Now)));

    private static async Task<Guid> CreatePostedSupplierPaymentAsync(SupplierInvoiceTestDbContext db, SeedIds ids, Guid invoiceId, long amountMinor, byte[] rowVersion)
    {
        var paymentId = await new CreateSupplierPaymentHandler(db, new FixedClock(Now))
            .HandleAsync(BuildPaymentCreate(ids, invoiceId, amountMinor), TestContext.Current.CancellationToken);
        var payment = await db.Set<SupplierPayment>().SingleAsync(x => x.Id == paymentId, TestContext.Current.CancellationToken);
        payment.RowVersion = rowVersion;
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        await CreatePostSupplierPaymentHandler(db).HandleAsync(new SupplierPaymentLifecycleActionDto { Id = paymentId, RowVersion = rowVersion, Action = "Post" }, TestContext.Current.CancellationToken);
        return paymentId;
    }

    private static async Task<Guid> CreateApprovedInvoiceAsync(SupplierInvoiceTestDbContext db, SeedIds ids, byte[] rowVersion)
    {
        var invoiceId = await new CreateSupplierInvoiceHandler(db, new FixedClock(Now)).HandleAsync(BuildCreate(ids), TestContext.Current.CancellationToken);
        var invoice = await db.Set<SupplierInvoice>().SingleAsync(TestContext.Current.CancellationToken);
        invoice.RowVersion = rowVersion;
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var lifecycle = CreateLifecycleHandler(db);
        await lifecycle.HandleAsync(new SupplierInvoiceLifecycleActionDto { Id = invoiceId, RowVersion = rowVersion, Action = "Match" }, TestContext.Current.CancellationToken);
        invoice = await db.Set<SupplierInvoice>().SingleAsync(TestContext.Current.CancellationToken);
        invoice.RowVersion = [99];
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        await lifecycle.HandleAsync(new SupplierInvoiceLifecycleActionDto { Id = invoiceId, RowVersion = [99], Action = "Approve" }, TestContext.Current.CancellationToken);
        return invoiceId;
    }

    private static async Task<Guid> CreatePostedSupplierInvoiceAsync(SupplierInvoiceTestDbContext db, SeedIds ids, byte[] rowVersion)
    {
        SeedPostingAccountsIfMissing(db, ids.BusinessId);
        var invoiceId = await CreateApprovedInvoiceAsync(db, ids, rowVersion);
        var invoice = await db.Set<SupplierInvoice>().SingleAsync(x => x.Id == invoiceId, TestContext.Current.CancellationToken);
        invoice.RowVersion = [200];
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        await CreatePostHandler(db).HandleAsync(new SupplierInvoiceLifecycleActionDto { Id = invoiceId, RowVersion = [200], Action = "Post" }, TestContext.Current.CancellationToken);
        return invoiceId;
    }

    private static void SeedPostingAccounts(SupplierInvoiceTestDbContext db, Guid businessId)
    {
        SeedMappedAccount(db, businessId, FinancePostingAccountRole.AccountsPayable, AccountType.Liability);
        SeedMappedAccount(db, businessId, FinancePostingAccountRole.PurchaseExpense, AccountType.Expense);
        SeedMappedAccount(db, businessId, FinancePostingAccountRole.InventoryClearing, AccountType.Asset);
        SeedMappedAccount(db, businessId, FinancePostingAccountRole.TaxReceivable, AccountType.Asset);
    }

    private static void SeedPostingAccountsIfMissing(SupplierInvoiceTestDbContext db, Guid businessId)
    {
        SeedMappedAccountIfMissing(db, businessId, FinancePostingAccountRole.AccountsPayable, AccountType.Liability);
        SeedMappedAccountIfMissing(db, businessId, FinancePostingAccountRole.PurchaseExpense, AccountType.Expense);
        SeedMappedAccountIfMissing(db, businessId, FinancePostingAccountRole.InventoryClearing, AccountType.Asset);
        SeedMappedAccountIfMissing(db, businessId, FinancePostingAccountRole.TaxReceivable, AccountType.Asset);
    }

    private static void SeedMappedAccount(SupplierInvoiceTestDbContext db, Guid businessId, FinancePostingAccountRole role, AccountType type)
    {
        var account = new FinancialAccount
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            Name = role.ToString(),
            Type = type
        };
        db.Set<FinancialAccount>().Add(account);
        db.Set<FinancePostingAccountMapping>().Add(new FinancePostingAccountMapping
        {
            BusinessId = businessId,
            Role = role,
            FinancialAccountId = account.Id,
            IsActive = true,
            MetadataJson = "{}"
        });
    }

    private static void SeedMappedAccountIfMissing(SupplierInvoiceTestDbContext db, Guid businessId, FinancePostingAccountRole role, AccountType type)
    {
        if (db.Set<FinancePostingAccountMapping>().Any(x => x.BusinessId == businessId && x.Role == role && !x.IsDeleted)) return;
        SeedMappedAccount(db, businessId, role, type);
    }

    private static async Task<BankSettlementSeed> SeedBankSettlementReconciliationAsync(
        SupplierInvoiceTestDbContext db,
        Guid businessId,
        SupplierPayment payment,
        long linkedAmountMinor,
        bool mapBankAccount = true,
        string identitySuffix = "main",
        Guid? journalEntryId = null)
    {
        var bankFinancialAccountId = mapBankAccount ? Guid.NewGuid() : (Guid?)null;
        if (bankFinancialAccountId.HasValue)
        {
            db.Set<FinancialAccount>().Add(new FinancialAccount
            {
                Id = bankFinancialAccountId.Value,
                BusinessId = businessId,
                Name = $"Bank {identitySuffix}",
                Type = AccountType.Asset
            });
        }

        var bankAccountId = Guid.NewGuid();
        var importId = Guid.NewGuid();
        var statementLineId = Guid.NewGuid();
        var matchId = Guid.NewGuid();
        db.Set<BankAccount>().Add(new BankAccount { Id = bankAccountId, BusinessId = businessId, FinancialAccountId = bankFinancialAccountId, Code = $"BANK-{identitySuffix}", DisplayName = $"Bank {identitySuffix}", Currency = payment.Currency, Status = BankAccountStatus.Active });
        db.Set<BankStatementImport>().Add(new BankStatementImport { Id = importId, BusinessId = businessId, BankAccountId = bankAccountId, StatementReference = $"ST-{identitySuffix}", PeriodStartUtc = Now.Date, PeriodEndUtc = Now.Date.AddDays(1), ImportedAtUtc = Now, Status = BankStatementImportStatus.Imported, MetadataJson = "{}" });
        db.Set<BankStatementLine>().Add(new BankStatementLine { Id = statementLineId, BusinessId = businessId, BankAccountId = bankAccountId, BankStatementImportId = importId, TransactionDateUtc = Now, Direction = BankStatementLineDirection.Debit, AmountMinor = linkedAmountMinor, Currency = payment.Currency, NormalizedIdentityKey = $"BANK-SETTLEMENT-{identitySuffix}", ReviewStatus = BankStatementLineReviewStatus.Unreviewed, MetadataJson = "{}" });
        db.Set<BankReconciliationMatch>().Add(new BankReconciliationMatch
        {
            Id = matchId,
            BusinessId = businessId,
            BankAccountId = bankAccountId,
            MatchNumber = $"BR-{identitySuffix}",
            Status = BankReconciliationMatchStatus.Matched,
            MatchDateUtc = Now,
            MatchedAtUtc = Now,
            Currency = payment.Currency,
            BankTotalMinor = linkedAmountMinor,
            FinanceTotalMinor = linkedAmountMinor,
            DifferenceMinor = 0,
            MetadataJson = "{}",
            Lines =
            [
                new BankReconciliationMatchLine
                {
                    BankStatementLineId = statementLineId,
                    JournalEntryId = journalEntryId ?? payment.PostingJournalEntryId,
                    SourceType = BankReconciliationSourceType.SupplierPayment,
                    SourceEntityType = "SupplierPayment",
                    SourceEntityId = payment.Id,
                    Direction = BankStatementLineDirection.Debit,
                    AmountMinor = linkedAmountMinor,
                    IsActive = true
                }
            ]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return new BankSettlementSeed(matchId, bankFinancialAccountId);
    }

    private static SupplierInvoiceCreateDto BuildCreate(SeedIds ids)
        => new()
        {
            BusinessId = ids.BusinessId,
            SupplierId = ids.SupplierId,
            PurchaseOrderId = ids.PurchaseOrderId,
            GoodsReceiptId = ids.GoodsReceiptId,
            SupplierInvoiceNumber = " SUP-100 ",
            InvoiceDateUtc = Now,
            Currency = " eur ",
            MetadataJson = "{}",
            Lines =
            [
                new SupplierInvoiceLineDto
                {
                    PurchaseOrderLineId = ids.PurchaseOrderLineId,
                    GoodsReceiptLineId = ids.GoodsReceiptLineId,
                    Description = "Received item",
                    InvoicedQuantity = 2,
                    UnitNetMinor = 1000,
                    UnitTaxMinor = 190,
                    UnitGrossMinor = 1190
                }
            ]
        };

    private static SupplierPaymentCreateDto BuildPaymentCreate(SeedIds ids, Guid invoiceId, long amountMinor)
        => new()
        {
            BusinessId = ids.BusinessId,
            SupplierId = ids.SupplierId,
            PaymentMethod = SupplierPaymentMethod.BankTransfer,
            PaymentDateUtc = Now,
            Currency = " eur ",
            Reference = " REF-1 ",
            MetadataJson = "{}",
            Allocations = [new SupplierPaymentAllocationDto { SupplierInvoiceId = invoiceId, AmountMinor = amountMinor, Memo = " invoice settlement " }]
        };

    private static async Task<SeedIds> SeedPurchasingAsync(SupplierInvoiceTestDbContext db, bool postedReceipt)
    {
        var businessId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();
        var poId = Guid.NewGuid();
        var poLineId = Guid.NewGuid();
        var grId = Guid.NewGuid();
        var grLineId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        db.Set<Supplier>().Add(new Supplier { Id = supplierId, BusinessId = businessId, Name = "Supplier", Email = "s@example.test", Phone = "1" });
        db.Set<PurchaseOrder>().Add(new PurchaseOrder { Id = poId, BusinessId = businessId, SupplierId = supplierId, OrderNumber = "PO-1", Currency = "EUR", OrderedAtUtc = Now, Status = PurchaseOrderStatus.Issued });
        db.Set<PurchaseOrderLine>().Add(new PurchaseOrderLine { Id = poLineId, PurchaseOrderId = poId, ProductVariantId = variantId, Quantity = 2, UnitCostMinor = 1000, TotalCostMinor = 2000 });
        db.Set<GoodsReceipt>().Add(new GoodsReceipt { Id = grId, BusinessId = businessId, SupplierId = supplierId, PurchaseOrderId = poId, WarehouseId = Guid.NewGuid(), Status = postedReceipt ? GoodsReceiptStatus.Posted : GoodsReceiptStatus.Inspected, MetadataJson = "{}" });
        db.Set<GoodsReceiptLine>().Add(new GoodsReceiptLine { Id = grLineId, GoodsReceiptId = grId, PurchaseOrderLineId = poLineId, ProductVariantId = variantId, OrderedQuantity = 2, ReceivedQuantity = 2, AcceptedQuantity = 2, UnitCostMinor = 1000, TotalCostMinor = 2000 });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return new SeedIds(businessId, supplierId, poId, poLineId, grId, grLineId);
    }

    private sealed record SeedIds(Guid BusinessId, Guid SupplierId, Guid PurchaseOrderId, Guid PurchaseOrderLineId, Guid GoodsReceiptId, Guid GoodsReceiptLineId);
    private sealed record BankSettlementSeed(Guid MatchId, Guid? BankFinancialAccountId);

    private sealed class FixedClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }

    private sealed class SupplierInvoiceTestDbContext : DbContext, IAppDbContext
    {
        private SupplierInvoiceTestDbContext(DbContextOptions<SupplierInvoiceTestDbContext> options) : base(options) { }
        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static SupplierInvoiceTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<SupplierInvoiceTestDbContext>()
                .UseInMemoryDatabase($"darwin_supplier_invoice_tests_{Guid.NewGuid()}")
                .Options;
            return new SupplierInvoiceTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Supplier>().HasKey(x => x.Id);
            modelBuilder.Entity<PurchaseOrder>().HasKey(x => x.Id);
            modelBuilder.Entity<PurchaseOrderLine>().HasKey(x => x.Id);
            modelBuilder.Entity<GoodsReceipt>(b => { b.HasKey(x => x.Id); b.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.GoodsReceiptId); });
            modelBuilder.Entity<GoodsReceiptLine>().HasKey(x => x.Id);
            modelBuilder.Entity<SupplierInvoice>(b => { b.HasKey(x => x.Id); b.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.SupplierInvoiceId); });
            modelBuilder.Entity<SupplierInvoiceLine>().HasKey(x => x.Id);
            modelBuilder.Entity<SupplierPayment>(b => { b.HasKey(x => x.Id); b.HasMany(x => x.Allocations).WithOne().HasForeignKey(x => x.SupplierPaymentId); });
            modelBuilder.Entity<SupplierPaymentAllocation>().HasKey(x => x.Id);
            modelBuilder.Entity<BankAccount>().HasKey(x => x.Id);
            modelBuilder.Entity<BankStatementImport>().HasKey(x => x.Id);
            modelBuilder.Entity<BankStatementLine>().HasKey(x => x.Id);
            modelBuilder.Entity<BankReconciliationMatch>(b => { b.HasKey(x => x.Id); b.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.BankReconciliationMatchId); });
            modelBuilder.Entity<BankReconciliationMatchLine>().HasKey(x => x.Id);
            modelBuilder.Entity<SupplierPaymentBankCorrection>().HasKey(x => x.Id);
            modelBuilder.Entity<JournalEntry>(b => { b.HasKey(x => x.Id); b.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.JournalEntryId); });
            modelBuilder.Entity<JournalEntryLine>().HasKey(x => x.Id);
            modelBuilder.Entity<FinancialAccount>().HasKey(x => x.Id);
            modelBuilder.Entity<FinancePostingAccountMapping>().HasKey(x => x.Id);
            modelBuilder.Entity<NumberSequence>().HasKey(x => x.Id);
        }
    }
}
