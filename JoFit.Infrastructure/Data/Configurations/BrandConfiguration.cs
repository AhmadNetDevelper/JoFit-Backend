using JoFit.Domain.Brands;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JoFit.Infrastructure.Data.Configurations;

public class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Slug).IsRequired().HasMaxLength(100);
        builder.Property(x => x.NameEn).IsRequired().HasMaxLength(100);
        builder.Property(x => x.NameAr).IsRequired().HasMaxLength(100);
        builder.Property(x => x.LogoMediaId).HasMaxLength(64);
        builder.Property(x => x.LogoUrl).HasMaxLength(500);
        builder.Property(x => x.LogoAlt).HasMaxLength(300);

        builder.HasIndex(x => x.Slug).IsUnique().HasFilter("\"IsDeleted\" = false");

        builder.ToTable("Brands");
    }
}
