using JoFit.Domain.Sports;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JoFit.Infrastructure.Data.Configurations;

public class SportConfiguration : IEntityTypeConfiguration<Sport>
{
    public void Configure(EntityTypeBuilder<Sport> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Slug).IsRequired().HasMaxLength(100);
        builder.Property(x => x.NameEn).IsRequired().HasMaxLength(100);
        builder.Property(x => x.NameAr).IsRequired().HasMaxLength(100);
        builder.Property(x => x.DescriptionEn).HasMaxLength(1000);
        builder.Property(x => x.DescriptionAr).HasMaxLength(1000);
        builder.Property(x => x.ImageMediaId).HasMaxLength(64);
        builder.Property(x => x.ImageUrl).HasMaxLength(500);
        builder.Property(x => x.ImageAlt).HasMaxLength(300);
        builder.HasIndex(x => x.Slug).IsUnique().HasFilter("\"IsDeleted\" = false");

        builder.ToTable("Sports");
    }
}
