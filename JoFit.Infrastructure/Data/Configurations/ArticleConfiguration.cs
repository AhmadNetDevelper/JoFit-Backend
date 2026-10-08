using JoFit.Domain.Articles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JoFit.Infrastructure.Data.Configurations;

public class ArticleConfiguration : IEntityTypeConfiguration<Article>
{
    public void Configure(EntityTypeBuilder<Article> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Slug).IsRequired().HasMaxLength(200);
        builder.Property(x => x.TitleEn).IsRequired().HasMaxLength(200);
        builder.Property(x => x.TitleAr).IsRequired().HasMaxLength(200);
        builder.Property(x => x.ExcerptEn).IsRequired().HasMaxLength(500);
        builder.Property(x => x.ExcerptAr).IsRequired().HasMaxLength(500);
        builder.Property(x => x.BodyEn).HasColumnType("text");
        builder.Property(x => x.BodyAr).HasColumnType("text");
        builder.Property(x => x.Status).IsRequired().HasMaxLength(20);
        builder.Property(x => x.Tags).IsRequired().HasMaxLength(400);
        builder.Property(x => x.RelatedIds).IsRequired().HasMaxLength(400);
        builder.Ignore(x => x.IsPublished);
        builder.Ignore(x => x.TagList);
        builder.Ignore(x => x.RelatedIdList);

        builder.HasIndex(x => x.Slug).IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.HasIndex(x => x.AuthorId);
        builder.HasIndex(x => x.Status);
        builder.ToTable("Articles");
    }
}

public class ArticleCategoryLinkConfiguration : IEntityTypeConfiguration<ArticleCategoryLink>
{
    public void Configure(EntityTypeBuilder<ArticleCategoryLink> builder)
    {
        builder.HasKey(x => new { x.ArticleId, x.ArticleCategoryId });
        builder.HasIndex(x => x.ArticleCategoryId);
        builder.ToTable("ArticleCategoryLinks");
    }
}

public class ArticleSportLinkConfiguration : IEntityTypeConfiguration<ArticleSportLink>
{
    public void Configure(EntityTypeBuilder<ArticleSportLink> builder)
    {
        builder.HasKey(x => new { x.ArticleId, x.SportId });
        builder.HasIndex(x => x.SportId);
        builder.ToTable("ArticleSportLinks");
    }
}
