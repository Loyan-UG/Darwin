using Darwin.Domain.Entities.Businesses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Businesses
{
    /// <summary>
    /// EF Core configuration for <see cref="BusinessLike"/>.
    /// Enforces: one active like per (UserId, BusinessId) while supporting soft-delete.
    /// </summary>
    public sealed class BusinessLikeConfiguration : IEntityTypeConfiguration<BusinessLike>
    {
        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<BusinessLike> builder)
        {
            builder.ToTable("BusinessLikes", schema: "Businesses");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.RowVersion).IsRowVersion();

            builder.Property(x => x.UserId).IsRequired();
            builder.Property(x => x.BusinessId).IsRequired();

            builder.HasIndex(x => new { x.UserId, x.BusinessId })
                .IsUnique()
                .HasDatabaseName("UX_BusinessLikes_User_Business")
                .HasFilter("[IsDeleted] = 0");

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.UserId);

            builder.HasOne(x => x.Business)
                .WithMany(x => x.Likes)
                .HasForeignKey(x => x.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.User)
                .WithMany(x => x.BusinessLikes)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
