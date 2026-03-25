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
                .HasFilter("[Code] IS NOT NULL");
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

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.EntryDateUtc);

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
