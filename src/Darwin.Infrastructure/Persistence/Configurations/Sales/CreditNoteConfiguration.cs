using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Entities.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Sales;

public sealed class CreditNoteConfiguration : IEntityTypeConfiguration<CreditNote>
{
    public void Configure(EntityTypeBuilder<CreditNote> builder)
    {
        builder.ToTable("CreditNotes", schema: "Sales");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CreditNoteNumber).HasMaxLength(50);
        builder.Property(x => x.Status).IsRequired().HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Reason).IsRequired().HasConversion<string>().HasMaxLength(48);
        builder.Property(x => x.Currency).IsRequired().HasMaxLength(3);
        builder.Property(x => x.OriginalInvoiceNumber).HasMaxLength(50);
        builder.Property(x => x.SourceModelJson).IsRequired().HasMaxLength(32000);
        builder.Property(x => x.SourceModelHashSha256).HasMaxLength(64);
        builder.Property(x => x.ArchiveRetentionPolicyVersion).HasMaxLength(80);
        builder.Property(x => x.ArchivePurgeReason).HasMaxLength(160);
        builder.Property(x => x.InternalNotes).HasMaxLength(2000);
        builder.Property(x => x.MetadataJson).IsRequired().HasMaxLength(16000);

        builder.HasIndex(x => x.CreditNoteNumber)
            .IsUnique()
            .HasDatabaseName("IX_CreditNotes_CreditNoteNumber")
            .HasFilter("[CreditNoteNumber] IS NOT NULL AND [IsDeleted] = 0");
        builder.HasIndex(x => x.InvoiceId).HasDatabaseName("IX_CreditNotes_InvoiceId");
        builder.HasIndex(x => x.ReturnOrderId).HasDatabaseName("IX_CreditNotes_ReturnOrderId");
        builder.HasIndex(x => x.RefundId).HasDatabaseName("IX_CreditNotes_RefundId");
        builder.HasIndex(x => x.Status).HasDatabaseName("IX_CreditNotes_Status");
        builder.HasIndex(x => x.BusinessId).HasDatabaseName("IX_CreditNotes_BusinessId");
        builder.HasIndex(x => x.CustomerId).HasDatabaseName("IX_CreditNotes_CustomerId");
        builder.HasIndex(x => x.IssuedAtUtc).HasDatabaseName("IX_CreditNotes_IssuedAtUtc");
        builder.HasIndex(x => x.ArchiveRetainUntilUtc).HasDatabaseName("IX_CreditNotes_ArchiveRetainUntilUtc");
        builder.HasIndex(x => x.ArchivePurgedAtUtc).HasDatabaseName("IX_CreditNotes_ArchivePurgedAtUtc");

        builder.HasOne<Invoice>().WithMany().HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ReturnOrder>().WithMany().HasForeignKey(x => x.ReturnOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Refund>().WithMany().HasForeignKey(x => x.RefundId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Business>().WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.IssuedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.VoidedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.CancelledByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<JournalEntry>().WithMany().HasForeignKey(x => x.PostingJournalEntryId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Lines)
            .WithOne()
            .HasForeignKey(x => x.CreditNoteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class CreditNoteLineConfiguration : IEntityTypeConfiguration<CreditNoteLine>
{
    public void Configure(EntityTypeBuilder<CreditNoteLine> builder)
    {
        builder.ToTable("CreditNoteLines", schema: "Sales");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Description).IsRequired().HasMaxLength(400);
        builder.Property(x => x.TaxRate).HasPrecision(18, 4);
        builder.Property(x => x.SourceLineJson).IsRequired().HasMaxLength(16000);

        builder.HasIndex(x => x.CreditNoteId).HasDatabaseName("IX_CreditNoteLines_CreditNoteId");
        builder.HasIndex(x => x.InvoiceLineId).HasDatabaseName("IX_CreditNoteLines_InvoiceLineId");
        builder.HasIndex(x => new { x.CreditNoteId, x.SortOrder }).HasDatabaseName("IX_CreditNoteLines_CreditNote_SortOrder");

        builder.HasOne<InvoiceLine>().WithMany().HasForeignKey(x => x.InvoiceLineId).OnDelete(DeleteBehavior.Restrict);
    }
}
