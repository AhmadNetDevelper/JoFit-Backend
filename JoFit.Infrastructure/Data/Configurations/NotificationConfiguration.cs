using JoFit.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JoFit.Infrastructure.Data.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Audience).IsRequired().HasMaxLength(20);
        builder.Property(x => x.Kind).IsRequired().HasMaxLength(40);
        builder.Property(x => x.TitleEn).IsRequired().HasMaxLength(250);
        builder.Property(x => x.TitleAr).IsRequired().HasMaxLength(250);
        builder.Property(x => x.BodyEn).IsRequired().HasMaxLength(1000);
        builder.Property(x => x.BodyAr).IsRequired().HasMaxLength(1000);
        builder.Property(x => x.Href).IsRequired().HasMaxLength(300);
        builder.HasIndex(x => new { x.Audience, x.RecipientId, x.CreatedAtUtc });
        builder.ToTable("Notifications");
    }
}
