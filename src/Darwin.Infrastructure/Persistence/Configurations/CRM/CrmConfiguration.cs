using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.CRM
{
    /// <summary>
    /// Configures CRM entities including customer, lead, opportunity, consent, and invoice structures.
    /// </summary>
    public sealed class CrmConfiguration :
        IEntityTypeConfiguration<Customer>,
        IEntityTypeConfiguration<CustomerAddress>,
        IEntityTypeConfiguration<CustomerSegment>,
        IEntityTypeConfiguration<CustomerSegmentMembership>,
        IEntityTypeConfiguration<Interaction>,
        IEntityTypeConfiguration<Consent>,
        IEntityTypeConfiguration<Lead>,
        IEntityTypeConfiguration<Opportunity>,
        IEntityTypeConfiguration<OpportunityItem>,
        IEntityTypeConfiguration<Invoice>,
        IEntityTypeConfiguration<InvoiceLine>
    {
        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<Customer> builder)
        {
            builder.ToTable("Customers", schema: "CRM");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.FirstName).IsRequired().HasMaxLength(150);
            builder.Property(x => x.LastName).IsRequired().HasMaxLength(150);
            builder.Property(x => x.Email).IsRequired().HasMaxLength(320);
            builder.Property(x => x.Phone).IsRequired().HasMaxLength(50);
            builder.Property(x => x.CompanyName).HasMaxLength(200);
            builder.Property(x => x.TaxProfileType).IsRequired();
            builder.Property(x => x.VatId).HasMaxLength(64);
            builder.Property(x => x.VatValidationStatus).IsRequired();
            builder.Property(x => x.VatValidationSource).HasMaxLength(80);
            builder.Property(x => x.VatValidationMessage).HasMaxLength(512);
            builder.Property(x => x.Notes).HasMaxLength(4000);

            builder.HasIndex(x => x.Email);
            builder.HasIndex(x => x.VatValidationStatus);
            builder.HasIndex(x => x.UserId)
                .IsUnique()
                .HasFilter("[UserId] IS NOT NULL AND [IsDeleted] = 0");

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Addresses)
                .WithOne()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Consents)
                .WithOne()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Opportunities)
                .WithOne()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Invoices)
                .WithOne()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<CustomerAddress> builder)
        {
            builder.ToTable("CustomerAddresses", schema: "CRM");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Line1).IsRequired().HasMaxLength(300);
            builder.Property(x => x.Line2).HasMaxLength(300);
            builder.Property(x => x.City).IsRequired().HasMaxLength(120);
            builder.Property(x => x.State).HasMaxLength(120);
            builder.Property(x => x.PostalCode).IsRequired().HasMaxLength(32);
            builder.Property(x => x.Country).IsRequired().HasMaxLength(2);

            builder.HasIndex(x => x.CustomerId);
            builder.HasIndex(x => x.AddressId);

            builder.HasOne<Address>()
                .WithMany()
                .HasForeignKey(x => x.AddressId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<CustomerSegment> builder)
        {
            builder.ToTable("CustomerSegments", schema: "CRM");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
            builder.Property(x => x.Description).HasMaxLength(2000);

            builder.HasIndex(x => x.Name)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
        }

        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<CustomerSegmentMembership> builder)
        {
            builder.ToTable("CustomerSegmentMemberships", schema: "CRM");
            builder.HasKey(x => x.Id);

            builder.HasIndex(x => new { x.CustomerId, x.CustomerSegmentId })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            builder.HasOne<Customer>()
                .WithMany(x => x.CustomerSegments)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<CustomerSegment>()
                .WithMany(x => x.Memberships)
                .HasForeignKey(x => x.CustomerSegmentId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<Interaction> builder)
        {
            builder.ToTable("Interactions", schema: "CRM");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Type).IsRequired();
            builder.Property(x => x.Channel).IsRequired();
            builder.Property(x => x.Subject).HasMaxLength(300);
            builder.Property(x => x.Content).HasMaxLength(4000);

            builder.HasIndex(x => x.CustomerId);
            builder.HasIndex(x => x.LeadId);
            builder.HasIndex(x => x.OpportunityId);
            builder.HasIndex(x => x.UserId);

            builder.HasOne<Customer>()
                .WithMany(x => x.Interactions)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Lead>()
                .WithMany(x => x.Interactions)
                .HasForeignKey(x => x.LeadId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Opportunity>()
                .WithMany(x => x.Interactions)
                .HasForeignKey(x => x.OpportunityId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<Consent> builder)
        {
            builder.ToTable("Consents", schema: "CRM");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Type).IsRequired();
            builder.Property(x => x.Granted).IsRequired();
            builder.Property(x => x.GrantedAtUtc).IsRequired();

            builder.HasIndex(x => new { x.CustomerId, x.Type });
        }

        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<Lead> builder)
        {
            builder.ToTable("Leads", schema: "CRM");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.FirstName).IsRequired().HasMaxLength(150);
            builder.Property(x => x.LastName).IsRequired().HasMaxLength(150);
            builder.Property(x => x.CompanyName).HasMaxLength(200);
            builder.Property(x => x.Email).IsRequired().HasMaxLength(320);
            builder.Property(x => x.Phone).IsRequired().HasMaxLength(50);
            builder.Property(x => x.Source).HasMaxLength(200);
            builder.Property(x => x.Notes).HasMaxLength(4000);
            builder.Property(x => x.Status).IsRequired();

            builder.HasIndex(x => x.Email);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.AssignedToUserId);
            builder.HasIndex(x => x.CustomerId);

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.AssignedToUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Customer>()
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<Opportunity> builder)
        {
            builder.ToTable("Opportunities", schema: "CRM");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Title).IsRequired().HasMaxLength(250);
            builder.Property(x => x.Stage).IsRequired();
            builder.Property(x => x.EstimatedValueMinor).IsRequired();

            builder.HasIndex(x => x.CustomerId);
            builder.HasIndex(x => x.Stage);
            builder.HasIndex(x => x.AssignedToUserId);

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.AssignedToUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Items)
                .WithOne()
                .HasForeignKey(x => x.OpportunityId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<OpportunityItem> builder)
        {
            builder.ToTable("OpportunityItems", schema: "CRM");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Quantity).IsRequired();
            builder.Property(x => x.UnitPriceMinor).IsRequired();

            builder.HasIndex(x => x.OpportunityId);
            builder.HasIndex(x => x.ProductVariantId);
        }

        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<Invoice> builder)
        {
            builder.ToTable("Invoices", schema: "CRM");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Currency).IsRequired().HasMaxLength(3);
            builder.Property(x => x.Status).IsRequired();
            builder.Property(x => x.IssuedSnapshotJson).HasMaxLength(16000);
            builder.Property(x => x.IssuedSnapshotHashSha256).HasMaxLength(64);
            builder.Property(x => x.ArchiveRetentionPolicyVersion).HasMaxLength(80);
            builder.Property(x => x.ArchivePurgeReason).HasMaxLength(160);
            builder.Property(x => x.ReverseChargeReviewNote).HasMaxLength(512);

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.CustomerId);
            builder.HasIndex(x => x.OrderId);
            builder.HasIndex(x => x.PaymentId);
            builder.HasIndex(x => x.IssuedAtUtc);
            builder.HasIndex(x => x.ArchiveRetainUntilUtc);
            builder.HasIndex(x => x.ArchivePurgedAtUtc);
            builder.HasIndex(x => x.ReverseChargeApplied);

            builder.HasOne<Payment>()
                .WithMany()
                .HasForeignKey(x => x.PaymentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Lines)
                .WithOne()
                .HasForeignKey(x => x.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<InvoiceLine> builder)
        {
            builder.ToTable("InvoiceLines", schema: "CRM");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Description).IsRequired().HasMaxLength(400);
            builder.Property(x => x.Quantity).IsRequired();
            builder.Property(x => x.UnitPriceNetMinor).IsRequired();
            builder.Property(x => x.TaxRate).HasPrecision(18, 4);
        }
    }
}
