using JoFit.Domain.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JoFit.Infrastructure.Data.Configurations;

public class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
{
    public void Configure(EntityTypeBuilder<MediaAsset> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.PublicId).IsRequired().HasMaxLength(64);
        builder.Property(x => x.StorageKey).IsRequired().HasMaxLength(128);
        builder.Property(x => x.MimeType).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Alt).HasMaxLength(300);
        builder.Property(x => x.Purpose).IsRequired().HasMaxLength(50);

        builder.HasIndex(x => x.PublicId).IsUnique();

        builder.ToTable("MediaAssets");
    }
}
