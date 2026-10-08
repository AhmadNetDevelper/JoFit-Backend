using JoFit.Domain.Community;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JoFit.Infrastructure.Data.Configurations;

public class CommunityTopicConfiguration : IEntityTypeConfiguration<CommunityTopic>
{
    public void Configure(EntityTypeBuilder<CommunityTopic> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Slug).IsRequired().HasMaxLength(60);
        builder.Property(x => x.NameEn).IsRequired().HasMaxLength(100);
        builder.Property(x => x.NameAr).IsRequired().HasMaxLength(100);
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.ToTable("CommunityTopics");
    }
}

public class CommunityPostConfiguration : IEntityTypeConfiguration<CommunityPost>
{
    public void Configure(EntityTypeBuilder<CommunityPost> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Slug).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Excerpt).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Body).IsRequired().HasColumnType("text");
        builder.Property(x => x.Status).IsRequired().HasMaxLength(20);
        builder.Property(x => x.ModerationNote).HasMaxLength(1000);
        builder.HasIndex(x => x.Slug).IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.HasIndex(x => x.TopicId);
        builder.HasIndex(x => x.Status);
        builder.ToTable("CommunityPosts");
    }
}

public class CommunityCommentConfiguration : IEntityTypeConfiguration<CommunityComment>
{
    public void Configure(EntityTypeBuilder<CommunityComment> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Body).IsRequired().HasMaxLength(3000);
        builder.Property(x => x.Status).IsRequired().HasMaxLength(20);
        builder.Property(x => x.ModerationNote).HasMaxLength(1000);
        builder.HasIndex(x => x.PostId);
        builder.HasIndex(x => x.ParentCommentId);
        builder.ToTable("CommunityComments");
    }
}
