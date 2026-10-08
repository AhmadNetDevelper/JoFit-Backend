using JoFit.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JoFit.Infrastructure.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Slug).IsRequired().HasMaxLength(180);
        builder.Property(x => x.TitleEn).IsRequired().HasMaxLength(150);
        builder.Property(x => x.TitleAr).IsRequired().HasMaxLength(150);
        builder.Property(x => x.SummaryEn).IsRequired().HasMaxLength(500);
        builder.Property(x => x.SummaryAr).IsRequired().HasMaxLength(500);
        builder.Property(x => x.DescriptionEn).HasMaxLength(10000);
        builder.Property(x => x.DescriptionAr).HasMaxLength(10000);
        builder.Property(x => x.Status).IsRequired().HasMaxLength(20);
        builder.Ignore(x => x.IsPublished);

        builder.HasIndex(x => x.Slug).IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.HasIndex(x => x.BrandId);
        builder.HasIndex(x => x.Status);

        builder.HasMany(x => x.Variants).WithOne().HasForeignKey(v => v.ProductId).OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("Products");
    }
}

public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Sku).IsRequired().HasMaxLength(64);
        builder.Property(x => x.TitleEn).IsRequired().HasMaxLength(100);
        builder.Property(x => x.TitleAr).IsRequired().HasMaxLength(100);
        builder.Ignore(x => x.IsLowStock);

        // SKUs are unique case-insensitively across all products.
        builder.Property(x => x.SkuKey).IsRequired().HasMaxLength(64);
        builder.HasIndex(x => x.SkuKey).IsUnique();

        builder.ToTable("ProductVariants");
    }
}

public class StockAdjustmentConfiguration : IEntityTypeConfiguration<StockAdjustment>
{
    public void Configure(EntityTypeBuilder<StockAdjustment> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Reason).IsRequired().HasMaxLength(300);
        builder.HasIndex(x => x.ProductVariantId);
        builder.ToTable("StockAdjustments");
    }
}

public class ProductCategoryLinkConfiguration : IEntityTypeConfiguration<ProductCategoryLink>
{
    public void Configure(EntityTypeBuilder<ProductCategoryLink> builder)
    {
        builder.HasKey(x => new { x.ProductId, x.ProductCategoryId });
        builder.HasIndex(x => x.ProductCategoryId);
        builder.ToTable("ProductCategoryLinks");
    }
}

public class ProductSportLinkConfiguration : IEntityTypeConfiguration<ProductSportLink>
{
    public void Configure(EntityTypeBuilder<ProductSportLink> builder)
    {
        builder.HasKey(x => new { x.ProductId, x.SportId });
        builder.HasIndex(x => x.SportId);
        builder.ToTable("ProductSportLinks");
    }
}
