using Darwin.Domain.Entities.CRM;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.CRM
{
    /// <summary>
    /// EF Core configuration for CRM entities.
    /// </summary>
    public sealed class CrmConfiguration :
        IEntityTypeConfiguration<Customer>,
        IEntityTypeConfiguration<CustomerAddress>,
        IEntityTypeConfiguration<CustomerSegment>,
        IEntityTypeConfiguration<CustomerSegmentMembership>,
        IEntityTypeConfiguration<Interaction>,
        IEntityTypeConfiguration<Consent>,
        IEntityTypeConfiguration<LoyaltyPointEntry>,
        IEntityTypeConfiguration<Invoice>,
        IEntityTypeConfiguration<InvoiceLine>
    {
        public void Configure(EntityTypeBuilder<Customer> builder)
        {
            builder.ToTable("Customers", schema: "CRM");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.FirstName).IsRequired().HasMaxLength(150);
            builder.Property(x => x.LastName).IsRequired().HasMaxLength(150);
            builder.Property(x => x.Email).IsRequired().HasMaxLength(320);
            builder.Property(x => x.Phone).IsRequired().HasMaxLength(50);
            builder.HasIndex(x => x.Email);
        }

        public void Configure(EntityTypeBuilder<CustomerAddress> builder)
        {
            builder.ToTable("CustomerAddresses", schema: "CRM");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Line1).IsRequired().HasMaxLength(300);
            builder.Property(x => x.City).IsRequired().HasMaxLength(120);
            builder.Property(x => x.PostalCode).IsRequired().HasMaxLength(32);
            builder.Property(x => x.Country).IsRequired().HasMaxLength(2);
            builder.HasIndex(x => x.CustomerId);
        }

        public void Configure(EntityTypeBuilder<CustomerSegment> builder)
        {
            builder.ToTable("CustomerSegments", schema: "CRM");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
            builder.HasIndex(x => x.Name).IsUnique();
        }

        public void Configure(EntityTypeBuilder<CustomerSegmentMembership> builder)
        {
            builder.ToTable("CustomerSegmentMemberships", schema: "CRM");
            builder.HasKey(x => new { x.CustomerId, x.CustomerSegmentId });
        }

        public void Configure(EntityTypeBuilder<Interaction> builder)
        {
            builder.ToTable("Interactions", schema: "CRM");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Type).IsRequired();
            builder.Property(x => x.Channel).IsRequired();
            builder.HasIndex(x => x.CustomerId);
        }

        public void Configure(EntityTypeBuilder<Consent> builder)
        {
            builder.ToTable("Consents", schema: "CRM");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Type).IsRequired();
            builder.HasIndex(x => new { x.CustomerId, x.Type });
        }

        public void Configure(EntityTypeBuilder<LoyaltyPointEntry> builder)
        {
            builder.ToTable("LoyaltyPointEntries", schema: "CRM");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Reason).IsRequired().HasMaxLength(400);
            builder.HasIndex(x => x.CustomerId);
        }

        public void Configure(EntityTypeBuilder<Invoice> builder)
        {
            builder.ToTable("Invoices", schema: "CRM");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Currency).IsRequired().HasMaxLength(3);
            builder.Property(x => x.Status).IsRequired();
            builder.HasMany(x => x.Lines)
                   .WithOne()
                   .HasForeignKey(x => x.InvoiceId)
                   .OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(x => x.CustomerId);
        }

        public void Configure(EntityTypeBuilder<InvoiceLine> builder)
        {
            builder.ToTable("InvoiceLines", schema: "CRM");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Description).IsRequired().HasMaxLength(400);
            builder.Property(x => x.TaxRate).HasPrecision(18, 4);
        }
    }
}
