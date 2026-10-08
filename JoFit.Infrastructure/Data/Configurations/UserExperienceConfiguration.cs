using JoFit.Domain.Identity.Users;
using JoFit.Domain.UserExperiences;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JoFit.Infrastructure.Data.Configurations;

public class UserExperienceConfiguration : IEntityTypeConfiguration<UserExperience>
{
    public void Configure(EntityTypeBuilder<UserExperience> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.Address)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(x => x.JobTypeId)
            .IsRequired();

        builder.Property(x => x.CompanyName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.CompanyAddress)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.StartDate)
            .IsRequired();

        builder.Property(x => x.EndDate)
            .IsRequired(false);

        builder.Property(x => x.IsStillWorking)
            .IsRequired();

        builder.HasOne(x => x.JobType)
            .WithMany()
            .HasForeignKey(x => x.JobTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.JobTypeId);

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.UserId);

        builder.ToTable("UserExperiences");
    }
}
