using Darwin.Domain.Entities.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Notifications;

/// <summary>
/// EF Core mapping for internal notification inbox entities.
/// </summary>
public sealed class NotificationConfiguration :
    IEntityTypeConfiguration<NotificationMessage>,
    IEntityTypeConfiguration<NotificationRecipient>
{
    public void Configure(EntityTypeBuilder<NotificationMessage> builder)
    {
        builder.ToTable("NotificationMessages", schema: "Notifications");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Category).IsRequired();
        builder.Property(x => x.TargetApp).IsRequired();
        builder.Property(x => x.Title).IsRequired().HasMaxLength(256);
        builder.Property(x => x.Body).HasMaxLength(4000);
        builder.Property(x => x.DeepLink).HasMaxLength(1024);
        builder.Property(x => x.SourceType).HasMaxLength(128);
        builder.Property(x => x.PublishedAtUtc).IsRequired();
        builder.Property(x => x.ExpiresAtUtc);

        builder.HasIndex(x => new { x.TargetApp, x.Category, x.PublishedAtUtc })
            .HasDatabaseName("IX_NotificationMessages_Target_Category_Published");

        builder.HasIndex(x => new { x.SourceType, x.SourceId, x.TargetApp })
            .HasDatabaseName("IX_NotificationMessages_Source_Target");
    }

    public void Configure(EntityTypeBuilder<NotificationRecipient> builder)
    {
        builder.ToTable("NotificationRecipients", schema: "Notifications");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.NotificationMessageId).IsRequired();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.DeliveredAtUtc);
        builder.Property(x => x.ReadAtUtc);
        builder.Property(x => x.ArchivedAtUtc);

        builder.HasOne(x => x.Message)
            .WithMany()
            .HasForeignKey(x => x.NotificationMessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.UserId, x.ReadAtUtc, x.ArchivedAtUtc })
            .HasDatabaseName("IX_NotificationRecipients_User_Read_Archived");

        builder.HasIndex(x => new { x.UserId, x.CreatedAtUtc })
            .HasDatabaseName("IX_NotificationRecipients_User_Created");

        builder.HasIndex(x => new { x.NotificationMessageId, x.UserId })
            .IsUnique()
            .HasDatabaseName("UX_NotificationRecipients_Message_User")
            .HasFilter("[IsDeleted] = 0");
    }
}
