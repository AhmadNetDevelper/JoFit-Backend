using JoFit.Domain.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JoFit.Infrastructure.Data.Configurations;

public class SupportConversationConfiguration : IEntityTypeConfiguration<SupportConversation>
{
    public void Configure(EntityTypeBuilder<SupportConversation> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.GuestTokenHash).HasMaxLength(64);
        builder.Property(x => x.VisitorName).IsRequired().HasMaxLength(150);
        builder.Property(x => x.VisitorEmail).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Status).IsRequired().HasMaxLength(10);
        builder.Ignore(x => x.Reference);
        builder.HasIndex(x => x.GuestTokenHash);
        builder.HasIndex(x => x.AccountUserId);
        builder.HasIndex(x => x.LastMessageAtUtc);
        builder.HasMany(x => x.Messages).WithOne().HasForeignKey(m => m.ConversationId).OnDelete(DeleteBehavior.Cascade);
        builder.ToTable("SupportConversations");
    }
}

public class SupportMessageConfiguration : IEntityTypeConfiguration<SupportMessage>
{
    public void Configure(EntityTypeBuilder<SupportMessage> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Sender).IsRequired().HasMaxLength(10);
        builder.Property(x => x.AuthorName).IsRequired().HasMaxLength(150);
        builder.Property(x => x.Body).IsRequired().HasMaxLength(2000);
        builder.ToTable("SupportMessages");
    }
}
