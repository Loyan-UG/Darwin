using Darwin.Domain.Entities.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Billing
{
    /// <summary>
    /// Configures lightweight accounting entities.
    /// </summary>
    public sealed class AccountingConfiguration :
        IEntityTypeConfiguration<FinancialAccount>,
        IEntityTypeConfiguration<FinancePostingAccountMapping>,
        IEntityTypeConfiguration<FinanceExportBatch>,
        IEntityTypeConfiguration<FinanceExportAttempt>,
        IEntityTypeConfiguration<SupplierInvoice>,
        IEntityTypeConfiguration<SupplierInvoiceLine>,
        IEntityTypeConfiguration<SupplierPayment>,
        IEntityTypeConfiguration<SupplierPaymentAllocation>,
        IEntityTypeConfiguration<JournalEntry>,
        IEntityTypeConfiguration<JournalEntryLine>,
        IEntityTypeConfiguration<Expense>
    {
        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<FinancialAccount> builder)
        {
            builder.ToTable("FinancialAccounts", schema: "Billing");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.Type)
                .IsRequired();

            builder.Property(x => x.Code)
                .HasMaxLength(64);

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => new { x.BusinessId, x.Code })
                .IsUnique()
                .HasFilter("[Code] IS NOT NULL AND [IsDeleted] = 0");
        }

        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<FinancePostingAccountMapping> builder)
        {
            builder.ToTable("FinancePostingAccountMappings", schema: "Billing");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Role)
                .HasConversion<string>()
                .HasMaxLength(64)
                .IsRequired();

            builder.Property(x => x.IsActive)
                .IsRequired();

            builder.Property(x => x.Description)
                .HasMaxLength(500);

            builder.Property(x => x.MetadataJson)
                .IsRequired()
                .HasMaxLength(4000);

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.Role);
            builder.HasIndex(x => x.FinancialAccountId);
            builder.HasIndex(x => new { x.BusinessId, x.Role })
                .IsUnique()
                .HasDatabaseName("UX_FinancePostingAccountMappings_Business_Role_Active")
                .HasFilter("[IsActive] = 1 AND [IsDeleted] = 0");

            builder.HasOne<FinancialAccount>()
                .WithMany()
                .HasForeignKey(x => x.FinancialAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<FinanceExportBatch> builder)
        {
            builder.ToTable("FinanceExportBatches", schema: "Billing");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.ExportKey)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(x => x.PostingStatusMode)
                .HasConversion<string>()
                .HasMaxLength(64)
                .IsRequired();

            builder.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(64)
                .IsRequired();

            builder.Property(x => x.PackageHashSha256)
                .HasMaxLength(128);

            builder.Property(x => x.PackageContentType)
                .HasMaxLength(128);

            builder.Property(x => x.PackageFileName)
                .HasMaxLength(260);

            builder.Property(x => x.ErrorSummary)
                .HasMaxLength(1000);

            builder.Property(x => x.MetadataJson)
                .IsRequired()
                .HasMaxLength(4000);

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.ExternalSystemId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => new { x.BusinessId, x.PeriodStartUtc, x.PeriodEndUtc });
            builder.HasIndex(x => new { x.BusinessId, x.ExternalSystemId, x.ExportKey })
                .IsUnique()
                .HasDatabaseName("UX_FinanceExportBatches_Business_Target_Key_Active")
                .HasFilter("[IsDeleted] = 0");

            builder.HasOne<Darwin.Domain.Entities.Integration.ExternalSystem>()
                .WithMany()
                .HasForeignKey(x => x.ExternalSystemId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Attempts)
                .WithOne()
                .HasForeignKey(x => x.FinanceExportBatchId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<FinanceExportAttempt> builder)
        {
            builder.ToTable("FinanceExportAttempts", schema: "Billing");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(64)
                .IsRequired();

            builder.Property(x => x.PackageHashSha256)
                .HasMaxLength(128);

            builder.Property(x => x.ErrorSummary)
                .HasMaxLength(1000);

            builder.Property(x => x.MetadataJson)
                .IsRequired()
                .HasMaxLength(4000);

            builder.HasIndex(x => x.FinanceExportBatchId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => new { x.FinanceExportBatchId, x.AttemptNumber })
                .IsUnique()
                .HasDatabaseName("UX_FinanceExportAttempts_Batch_AttemptNumber_Active")
                .HasFilter("[IsDeleted] = 0");
        }

        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<SupplierInvoice> builder)
        {
            builder.ToTable("SupplierInvoices", schema: "Billing");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.SupplierInvoiceNumber).IsRequired().HasMaxLength(128);
            builder.Property(x => x.InternalInvoiceNumber).HasMaxLength(128);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.Currency).IsRequired().HasMaxLength(3);
            builder.Property(x => x.InternalNotes).HasMaxLength(4000);
            builder.Property(x => x.MetadataJson).IsRequired().HasMaxLength(4000);

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.SupplierId);
            builder.HasIndex(x => x.PurchaseOrderId);
            builder.HasIndex(x => x.GoodsReceiptId);
            builder.HasIndex(x => x.PostingJournalEntryId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.InvoiceDateUtc);
            builder.HasIndex(x => x.DueDateUtc);
            builder.HasIndex(x => x.PostedAtUtc);
            builder.HasIndex(x => new { x.BusinessId, x.SupplierId, x.SupplierInvoiceNumber })
                .IsUnique()
                .HasDatabaseName("UX_SupplierInvoices_Business_Supplier_Number_Active")
                .HasFilter("[IsDeleted] = 0");
            builder.HasIndex(x => new { x.BusinessId, x.InternalInvoiceNumber })
                .IsUnique()
                .HasDatabaseName("UX_SupplierInvoices_Business_InternalNumber_Active")
                .HasFilter("[InternalInvoiceNumber] IS NOT NULL AND [IsDeleted] = 0");

            builder.HasMany(x => x.Lines)
                .WithOne()
                .HasForeignKey(x => x.SupplierInvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<SupplierInvoiceLine> builder)
        {
            builder.ToTable("SupplierInvoiceLines", schema: "Billing");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.SupplierSku).HasMaxLength(100);
            builder.Property(x => x.Description).IsRequired().HasMaxLength(1000);
            builder.Property(x => x.TaxRate).HasPrecision(9, 4);
            builder.Property(x => x.MatchStatus).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.DiscrepancyReason).HasMaxLength(1000);

            builder.HasIndex(x => x.SupplierInvoiceId);
            builder.HasIndex(x => x.PurchaseOrderLineId);
            builder.HasIndex(x => x.GoodsReceiptLineId);
            builder.HasIndex(x => x.ProductVariantId);
            builder.HasIndex(x => x.MatchStatus);
        }

        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<SupplierPayment> builder)
        {
            builder.ToTable("SupplierPayments", schema: "Billing");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.PaymentNumber).HasMaxLength(128);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.PaymentMethod).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.Currency).IsRequired().HasMaxLength(3);
            builder.Property(x => x.Reference).HasMaxLength(256);
            builder.Property(x => x.ReversalReason).HasMaxLength(1000);
            builder.Property(x => x.InternalNotes).HasMaxLength(4000);
            builder.Property(x => x.MetadataJson).IsRequired().HasMaxLength(4000);

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.SupplierId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.PaymentDateUtc);
            builder.HasIndex(x => x.PostingJournalEntryId);
            builder.HasIndex(x => x.ReversalJournalEntryId);
            builder.HasIndex(x => x.ReversedAtUtc);
            builder.HasIndex(x => new { x.BusinessId, x.PaymentNumber })
                .IsUnique()
                .HasDatabaseName("UX_SupplierPayments_Business_Number_Active")
                .HasFilter("[PaymentNumber] IS NOT NULL AND [IsDeleted] = 0");

            builder.HasMany(x => x.Allocations)
                .WithOne()
                .HasForeignKey(x => x.SupplierPaymentId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<SupplierPaymentAllocation> builder)
        {
            builder.ToTable("SupplierPaymentAllocations", schema: "Billing");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Memo).HasMaxLength(1000);

            builder.HasIndex(x => x.SupplierPaymentId);
            builder.HasIndex(x => x.SupplierInvoiceId);
            builder.HasIndex(x => new { x.SupplierPaymentId, x.SupplierInvoiceId })
                .IsUnique()
                .HasDatabaseName("UX_SupplierPaymentAllocations_Payment_Invoice_Active")
                .HasFilter("[IsDeleted] = 0");
        }

        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<JournalEntry> builder)
        {
            builder.ToTable("JournalEntries", schema: "Billing");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.EntryDateUtc)
                .IsRequired();

            builder.Property(x => x.Description)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(x => x.PostingStatus)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();

            builder.Property(x => x.PostingKind)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();

            builder.Property(x => x.PostingKey)
                .HasMaxLength(256);

            builder.Property(x => x.SourceEntityType)
                .HasMaxLength(128);

            builder.Property(x => x.SourceDocumentNumber)
                .HasMaxLength(128);

            builder.Property(x => x.PostingReason)
                .HasMaxLength(1000);

            builder.Property(x => x.MetadataJson)
                .IsRequired()
                .HasMaxLength(4000);

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.EntryDateUtc);
            builder.HasIndex(x => x.PostingStatus);
            builder.HasIndex(x => x.PostingKind);
            builder.HasIndex(x => new { x.SourceEntityType, x.SourceEntityId })
                .HasDatabaseName("IX_JournalEntries_SourceEntity");
            builder.HasIndex(x => x.PostingKey)
                .IsUnique()
                .HasDatabaseName("UX_JournalEntries_PostingKey")
                .HasFilter("[PostingKey] IS NOT NULL AND [IsDeleted] = 0");

            builder.HasMany(x => x.Lines)
                .WithOne()
                .HasForeignKey(x => x.JournalEntryId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<JournalEntryLine> builder)
        {
            builder.ToTable("JournalEntryLines", schema: "Billing");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.DebitMinor)
                .IsRequired();

            builder.Property(x => x.CreditMinor)
                .IsRequired();

            builder.Property(x => x.Memo)
                .HasMaxLength(1000);

            builder.HasIndex(x => x.JournalEntryId);
            builder.HasIndex(x => x.AccountId);
        }

        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<Expense> builder)
        {
            builder.ToTable("Expenses", schema: "Billing");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Category)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(x => x.Description)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(x => x.AmountMinor)
                .IsRequired();

            builder.Property(x => x.ExpenseDateUtc)
                .IsRequired();

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.SupplierId);
            builder.HasIndex(x => x.ExpenseDateUtc);
        }
    }
}
