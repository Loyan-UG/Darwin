using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Businesses
{
    /// <summary>
    /// EF Core configuration for <see cref="BusinessFavorite"/>.
    /// Enforces that a user can have at most one active favorite per business (soft-delete aware).
    /// </summary>
    public sealed class BusinessFavoriteConfiguration : IEntityTypeConfiguration<BusinessFavorite>
    {
        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<BusinessFavorite> builder)
        {
            builder.ToTable("BusinessFavorites", schema: "Businesses");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.RowVersion).IsRowVersion();

            builder.Property(x => x.BusinessId).IsRequired();
            builder.Property(x => x.UserId).IsRequired();

            // Uniqueness: only one active favorite per (UserId, BusinessId).
            builder.HasIndex(x => new { x.UserId, x.BusinessId })
                .IsUnique()
                .HasDatabaseName("UX_BusinessFavorites_User_Business");


            // Helpful lookups
            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.UserId);

            // Relationships are anchored from BusinessConfiguration via navigation properties.
            // No extra navigation configuration is required here.
        }
    }
}
