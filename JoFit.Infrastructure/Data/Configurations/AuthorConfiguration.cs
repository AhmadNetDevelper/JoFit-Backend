using JoFit.Domain.Authors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JoFit.Infrastructure.Data.Configurations;

public class AuthorConfiguration : IEntityTypeConfiguration<Author>
{
    public void Configure(EntityTypeBuilder<Author> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Slug).HasMaxLength(100);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.RoleEn).HasMaxLength(100);
        builder.Property(x => x.RoleAr).HasMaxLength(100);
        builder.Property(x => x.BioEn).HasMaxLength(1000);
        builder.Property(x => x.BioAr).HasMaxLength(1000);
        builder.Property(x => x.AvatarMediaId).HasMaxLength(64);
        builder.Property(x => x.AvatarUrl).HasMaxLength(500);
        builder.Property(x => x.AvatarAlt).HasMaxLength(300);
        builder.HasIndex(x => x.Slug).IsUnique().HasFilter("\"IsDeleted\" = false");

        builder.ToTable("Authors");
    }
}
