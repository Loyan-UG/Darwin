using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Billing.Commands;
using Darwin.Application.Billing.DTOs;
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

    private static UpdateSupplierInvoiceLifecycleHandler CreateLifecycleHandler(SupplierInvoiceTestDbContext db)
        => new(db, new FixedClock(Now), new NumberSequenceService(db, new FixedClock(Now)), new SupplierInvoiceWorkflowPolicy());

    private static PostSupplierInvoiceHandler CreatePostHandler(SupplierInvoiceTestDbContext db)
        => new(db, new FixedClock(Now), new SupplierInvoiceWorkflowPolicy(), new FinanceAccountMappingService(db), new FinancePostingService(db, new FixedClock(Now)));

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

    private static void SeedPostingAccounts(SupplierInvoiceTestDbContext db, Guid businessId)
    {
        SeedMappedAccount(db, businessId, FinancePostingAccountRole.AccountsPayable, AccountType.Liability);
        SeedMappedAccount(db, businessId, FinancePostingAccountRole.PurchaseExpense, AccountType.Expense);
        SeedMappedAccount(db, businessId, FinancePostingAccountRole.InventoryClearing, AccountType.Asset);
        SeedMappedAccount(db, businessId, FinancePostingAccountRole.TaxReceivable, AccountType.Asset);
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
            modelBuilder.Entity<JournalEntry>(b => { b.HasKey(x => x.Id); b.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.JournalEntryId); });
            modelBuilder.Entity<JournalEntryLine>().HasKey(x => x.Id);
            modelBuilder.Entity<FinancialAccount>().HasKey(x => x.Id);
            modelBuilder.Entity<FinancePostingAccountMapping>().HasKey(x => x.Id);
            modelBuilder.Entity<NumberSequence>().HasKey(x => x.Id);
        }
    }
}
