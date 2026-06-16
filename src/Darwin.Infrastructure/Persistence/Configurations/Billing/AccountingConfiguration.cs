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
        IEntityTypeConfiguration<BankAccount>,
        IEntityTypeConfiguration<BankStatementImport>,
        IEntityTypeConfiguration<BankStatementLine>,
        IEntityTypeConfiguration<BankReconciliationMatch>,
        IEntityTypeConfiguration<BankReconciliationMatchLine>,
        IEntityTypeConfiguration<SupplierInvoice>,
        IEntityTypeConfiguration<SupplierInvoiceLine>,
        IEntityTypeConfiguration<SupplierPayment>,
        IEntityTypeConfiguration<SupplierPaymentAllocation>,
        IEntityTypeConfiguration<SupplierPaymentBankCorrection>,
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

        public void Configure(EntityTypeBuilder<BankAccount> builder)
        {
            builder.ToTable("BankAccounts", schema: "Billing");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Code).IsRequired().HasMaxLength(64);
            builder.Property(x => x.DisplayName).IsRequired().HasMaxLength(200);
            builder.Property(x => x.BankName).HasMaxLength(200);
            builder.Property(x => x.Currency).IsRequired().HasMaxLength(3);
            builder.Property(x => x.MaskedAccountIdentifier).HasMaxLength(128);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.MetadataJson).IsRequired().HasMaxLength(4000);

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.FinancialAccountId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.IsDefault);
            builder.HasIndex(x => new { x.BusinessId, x.Code })
                .IsUnique()
                .HasDatabaseName("UX_BankAccounts_Business_Code_Active")
                .HasFilter("[IsDeleted] = 0");

            builder.HasOne<FinancialAccount>()
                .WithMany()
                .HasForeignKey(x => x.FinancialAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        public void Configure(EntityTypeBuilder<BankStatementImport> builder)
        {
            builder.ToTable("BankStatementImports", schema: "Billing");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.StatementReference).IsRequired().HasMaxLength(128);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.MetadataJson).IsRequired().HasMaxLength(4000);

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.BankAccountId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.ImportedAtUtc);
            builder.HasIndex(x => new { x.BusinessId, x.PeriodStartUtc, x.PeriodEndUtc });
            builder.HasIndex(x => new { x.BankAccountId, x.StatementReference })
                .IsUnique()
                .HasDatabaseName("UX_BankStatementImports_Account_Reference_Active")
                .HasFilter("[IsDeleted] = 0");

            builder.HasOne<BankAccount>()
                .WithMany()
                .HasForeignKey(x => x.BankAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Lines)
                .WithOne()
                .HasForeignKey(x => x.BankStatementImportId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        public void Configure(EntityTypeBuilder<BankStatementLine> builder)
        {
            builder.ToTable("BankStatementLines", schema: "Billing");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Direction).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.Currency).IsRequired().HasMaxLength(3);
            builder.Property(x => x.CounterpartyName).HasMaxLength(256);
            builder.Property(x => x.CounterpartyReference).HasMaxLength(256);
            builder.Property(x => x.RemittanceInformation).HasMaxLength(1000);
            builder.Property(x => x.NormalizedIdentityKey).IsRequired().HasMaxLength(256);
            builder.Property(x => x.ReviewStatus).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.MetadataJson).IsRequired().HasMaxLength(4000);

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.BankAccountId);
            builder.HasIndex(x => x.BankStatementImportId);
            builder.HasIndex(x => x.TransactionDateUtc);
            builder.HasIndex(x => x.ReviewStatus);
            builder.HasIndex(x => new { x.BankAccountId, x.NormalizedIdentityKey })
                .IsUnique()
                .HasDatabaseName("UX_BankStatementLines_Account_Identity_Active")
                .HasFilter("[IsDeleted] = 0");
        }

        public void Configure(EntityTypeBuilder<BankReconciliationMatch> builder)
        {
            builder.ToTable("BankReconciliationMatches", schema: "Billing");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.MatchNumber).HasMaxLength(128);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.Currency).IsRequired().HasMaxLength(3);
            builder.Property(x => x.ReviewNotes).HasMaxLength(4000);
            builder.Property(x => x.MetadataJson).IsRequired().HasMaxLength(4000);

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.BankAccountId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.MatchDateUtc);
            builder.HasIndex(x => x.MatchedAtUtc);
            builder.HasIndex(x => new { x.BusinessId, x.MatchNumber })
                .IsUnique()
                .HasDatabaseName("UX_BankReconciliationMatches_Business_Number_Active")
                .HasFilter("[MatchNumber] IS NOT NULL AND [IsDeleted] = 0");

            builder.HasOne<BankAccount>()
                .WithMany()
                .HasForeignKey(x => x.BankAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Lines)
                .WithOne()
                .HasForeignKey(x => x.BankReconciliationMatchId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        public void Configure(EntityTypeBuilder<BankReconciliationMatchLine> builder)
        {
            builder.ToTable("BankReconciliationMatchLines", schema: "Billing");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.SourceType).HasConversion<string>().HasMaxLength(64);
            builder.Property(x => x.SourceEntityType).HasMaxLength(128);
            builder.Property(x => x.Direction).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.Memo).HasMaxLength(1000);
            builder.Property(x => x.IsActive).IsRequired();

            builder.HasIndex(x => x.BankReconciliationMatchId);
            builder.HasIndex(x => x.BankStatementLineId);
            builder.HasIndex(x => x.JournalEntryId);
            builder.HasIndex(x => new { x.SourceEntityType, x.SourceEntityId })
                .HasDatabaseName("IX_BankReconciliationMatchLines_SourceEntity");
            builder.HasIndex(x => x.BankStatementLineId)
                .IsUnique()
                .HasDatabaseName("UX_BankReconciliationMatchLines_StatementLine_Active")
                .HasFilter("[IsActive] = 1 AND [IsDeleted] = 0");

            builder.HasOne<BankStatementLine>()
                .WithMany()
                .HasForeignKey(x => x.BankStatementLineId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<JournalEntry>()
                .WithMany()
                .HasForeignKey(x => x.JournalEntryId)
                .OnDelete(DeleteBehavior.Restrict);
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
            builder.Property(x => x.BankSettlementNotes).HasMaxLength(1000);
            builder.Property(x => x.InternalNotes).HasMaxLength(4000);
            builder.Property(x => x.MetadataJson).IsRequired().HasMaxLength(4000);

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.SupplierId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.PaymentDateUtc);
            builder.HasIndex(x => x.PostingJournalEntryId);
            builder.HasIndex(x => x.ReversalJournalEntryId);
            builder.HasIndex(x => x.ReversedAtUtc);
            builder.HasIndex(x => x.BankSettlementJournalEntryId);
            builder.HasIndex(x => x.BankSettlementReconciliationMatchId);
            builder.HasIndex(x => x.BankSettledAtUtc);
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
        public void Configure(EntityTypeBuilder<SupplierPaymentBankCorrection> builder)
        {
            builder.ToTable("SupplierPaymentBankCorrections", schema: "Billing");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.CorrectionType).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.Currency).IsRequired().HasMaxLength(3);
            builder.Property(x => x.Reason).IsRequired().HasMaxLength(1000);
            builder.Property(x => x.InternalNotes).HasMaxLength(4000);
            builder.Property(x => x.MetadataJson).IsRequired().HasMaxLength(4000);

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.SupplierPaymentId);
            builder.HasIndex(x => x.BankReconciliationMatchId);
            builder.HasIndex(x => x.BankStatementLineId);
            builder.HasIndex(x => x.OriginalBankSettlementJournalEntryId);
            builder.HasIndex(x => x.CorrectionJournalEntryId);
            builder.HasIndex(x => x.CorrectionType);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.CorrectionDateUtc);
            builder.HasIndex(x => x.PostedAtUtc);
            builder.HasIndex(x => new { x.SupplierPaymentId, x.CorrectionType, x.BankReconciliationMatchId })
                .IsUnique()
                .HasDatabaseName("UX_SupplierPaymentBankCorrections_Payment_Type_Reconciliation_Active")
                .HasFilter("[IsDeleted] = 0");

            builder.HasOne<SupplierPayment>()
                .WithMany()
                .HasForeignKey(x => x.SupplierPaymentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<BankReconciliationMatch>()
                .WithMany()
                .HasForeignKey(x => x.BankReconciliationMatchId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<BankStatementLine>()
                .WithMany()
                .HasForeignKey(x => x.BankStatementLineId)
                .OnDelete(DeleteBehavior.Restrict);
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
