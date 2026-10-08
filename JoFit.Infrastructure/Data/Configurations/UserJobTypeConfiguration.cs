using JoFit.Domain.UserJobTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JoFit.Infrastructure.Data.Configurations;

public class UserJobTypeConfiguration : IEntityTypeConfiguration<UserJobType>
{
    public void Configure(EntityTypeBuilder<UserJobType> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.NameAr)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.NameEn)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.IsNormalUser)
            .IsRequired()
            .HasDefaultValue(false);

        builder.ToTable("UserJobTypes");
    }
}
