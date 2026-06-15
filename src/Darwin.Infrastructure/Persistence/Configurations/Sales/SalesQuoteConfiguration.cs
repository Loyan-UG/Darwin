using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Entities.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Sales;

public sealed class SalesQuoteConfiguration : IEntityTypeConfiguration<SalesQuote>
{
    public void Configure(EntityTypeBuilder<SalesQuote> builder)
    {
        builder.ToTable("SalesQuotes", schema: "Sales");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.QuoteNumber).HasMaxLength(50);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(250);
        builder.Property(x => x.Status).IsRequired().HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Currency).IsRequired().HasMaxLength(3);
        builder.Property(x => x.CustomerSnapshotJson).IsRequired().HasMaxLength(16000);
        builder.Property(x => x.BillingAddressJson).IsRequired().HasMaxLength(16000);
        builder.Property(x => x.ShippingAddressJson).IsRequired().HasMaxLength(16000);
        builder.Property(x => x.InternalNotes).HasMaxLength(2000);

        builder.HasIndex(x => x.QuoteNumber)
            .IsUnique()
            .HasDatabaseName("IX_SalesQuotes_QuoteNumber")
            .HasFilter("[QuoteNumber] IS NOT NULL AND [IsDeleted] = 0");
        builder.HasIndex(x => x.BusinessId).HasDatabaseName("IX_SalesQuotes_BusinessId");
        builder.HasIndex(x => x.CustomerId).HasDatabaseName("IX_SalesQuotes_CustomerId");
        builder.HasIndex(x => x.OpportunityId).HasDatabaseName("IX_SalesQuotes_OpportunityId");
        builder.HasIndex(x => x.ConvertedOrderId).HasDatabaseName("IX_SalesQuotes_ConvertedOrderId");
        builder.HasIndex(x => x.Status).HasDatabaseName("IX_SalesQuotes_Status");
        builder.HasIndex(x => x.ValidUntilUtc).HasDatabaseName("IX_SalesQuotes_ValidUntilUtc");
        builder.HasIndex(x => x.SentAtUtc).HasDatabaseName("IX_SalesQuotes_SentAtUtc");
        builder.HasIndex(x => x.CreatedAtUtc).HasDatabaseName("IX_SalesQuotes_CreatedAtUtc");

        builder.HasOne<Business>().WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Opportunity>().WithMany().HasForeignKey(x => x.OpportunityId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Order>().WithMany().HasForeignKey(x => x.ConvertedOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.OwnerUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.PreparedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.SentByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.AcceptedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.RejectedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.ConvertedByUserId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Lines)
            .WithOne()
            .HasForeignKey(x => x.SalesQuoteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class SalesQuoteLineConfiguration : IEntityTypeConfiguration<SalesQuoteLine>
{
    public void Configure(EntityTypeBuilder<SalesQuoteLine> builder)
    {
        builder.ToTable("SalesQuoteLines", schema: "Sales");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).IsRequired().HasMaxLength(250);
        builder.Property(x => x.Sku).HasMaxLength(100);
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.TaxRate).HasPrecision(18, 4);

        builder.HasIndex(x => x.SalesQuoteId).HasDatabaseName("IX_SalesQuoteLines_SalesQuoteId");
        builder.HasIndex(x => x.ProductVariantId).HasDatabaseName("IX_SalesQuoteLines_ProductVariantId");
        builder.HasIndex(x => new { x.SalesQuoteId, x.SortOrder }).HasDatabaseName("IX_SalesQuoteLines_Quote_SortOrder");
    }
}
