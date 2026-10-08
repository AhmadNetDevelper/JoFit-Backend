using JoFit.Domain.Attachments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JoFit.Infrastructure.Data.Configurations;

public class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(vi => vi.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.FileName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.FileTypeId)
            .IsRequired();

        builder.Property(x => x.FileSize)
            .IsRequired();

        builder.Property(x => x.FilePath)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Description)
               .IsRequired(false)
               .HasMaxLength(150);

        builder.Property(x => x.EntityId)
            .IsRequired();

        builder.Property(x => x.EntityTypeId)
            .IsRequired();

        builder.Property(x => x.EntitySectionTypeId)
            .IsRequired();

        // Indexes for better query performance
        builder.HasIndex(x => x.EntityId);
        builder.HasIndex(x => x.EntityTypeId);

        builder.ToTable("Attachments");
    }
}
