using JoFit.Domain.RentalCategories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JoFit.Infrastructure.Data.Configurations;

public class RentalCategoryConfiguration : IEntityTypeConfiguration<RentalCategory>
{
    public void Configure(EntityTypeBuilder<RentalCategory> builder)
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
        builder.HasIndex(x => x.ParentId);
        builder.HasOne<RentalCategory>().WithMany().HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);

        builder.ToTable("RentalCategories");
    }
}
