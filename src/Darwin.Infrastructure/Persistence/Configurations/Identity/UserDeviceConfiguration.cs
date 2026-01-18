using Darwin.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Identity
{
    /// <summary>
    /// EF Core mapping for <see cref="UserDevice"/>.
    /// </summary>
    public sealed class UserDeviceConfiguration : IEntityTypeConfiguration<UserDevice>
    {
        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<UserDevice> builder)
        {
            builder.ToTable("UserDevices", schema: "Identity");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.UserId)
                .IsRequired();

            builder.Property(x => x.DeviceId)
                .IsRequired()
                .HasMaxLength(128);

            builder.Property(x => x.Platform)
                .IsRequired();

            builder.Property(x => x.PushToken)
                .HasMaxLength(512);

            builder.Property(x => x.PushTokenUpdatedAtUtc);

            builder.Property(x => x.NotificationsEnabled)
                .IsRequired();

            builder.Property(x => x.LastSeenAtUtc);

            builder.Property(x => x.AppVersion)
                .HasMaxLength(64);

            builder.Property(x => x.DeviceModel)
                .HasMaxLength(128);

            builder.Property(x => x.IsActive)
                .IsRequired();

            // Relationship: configured without relying on User.Devices navigation name.
            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Uniqueness per (UserId, DeviceId)
            builder.HasIndex(x => new { x.UserId, x.DeviceId })
                .IsUnique()
                .HasDatabaseName("UX_UserDevices_User_DeviceId");

            builder.HasIndex(x => x.UserId)
                .HasDatabaseName("IX_UserDevices_UserId");

            builder.HasIndex(x => x.IsActive)
                .HasDatabaseName("IX_UserDevices_IsActive");

            builder.HasIndex(x => x.LastSeenAtUtc)
                .HasDatabaseName("IX_UserDevices_LastSeenAtUtc");
        }
    }
}
