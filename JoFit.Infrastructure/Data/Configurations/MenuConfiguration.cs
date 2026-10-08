using JoFit.Domain.Menus;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JoFit.Infrastructure.Data.Configurations;

public class MenuPageConfiguration : IEntityTypeConfiguration<MenuPage>
{
    public void Configure(EntityTypeBuilder<MenuPage> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Slug).IsRequired().HasMaxLength(150);
        builder.Property(x => x.EyebrowEn).HasMaxLength(150);
        builder.Property(x => x.EyebrowAr).HasMaxLength(150);
        builder.Property(x => x.TitleEn).IsRequired().HasMaxLength(200);
        builder.Property(x => x.TitleAr).IsRequired().HasMaxLength(200);
        builder.Property(x => x.SummaryEn).IsRequired().HasMaxLength(1000);
        builder.Property(x => x.SummaryAr).IsRequired().HasMaxLength(1000);
        builder.Property(x => x.SectionsJson).IsRequired().HasColumnType("text");
        builder.Property(x => x.CtaLabelEn).HasMaxLength(100);
        builder.Property(x => x.CtaLabelAr).HasMaxLength(100);
        builder.Property(x => x.CtaHref).HasMaxLength(300);
        builder.Property(x => x.Status).IsRequired().HasMaxLength(20);
        builder.Ignore(x => x.IsPublished);
        builder.Ignore(x => x.Sections);
        builder.HasIndex(x => x.Slug).IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.ToTable("MenuPages");
    }
}

public class MenuConfiguration : IEntityTypeConfiguration<Menu>
{
    public void Configure(EntityTypeBuilder<Menu> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Key).IsRequired().HasMaxLength(50);
        builder.Property(x => x.DocumentJson).IsRequired().HasColumnType("text");
        builder.HasIndex(x => x.Key).IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.ToTable("Menus");
    }
}

public class ThemeSettingConfiguration : IEntityTypeConfiguration<JoFit.Domain.Settings.ThemeSetting>
{
    public void Configure(EntityTypeBuilder<JoFit.Domain.Settings.ThemeSetting> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Key).IsRequired().HasMaxLength(50);
        builder.Property(x => x.DocumentJson).IsRequired().HasColumnType("text");
        builder.ToTable("ThemeSettings");
    }
}
