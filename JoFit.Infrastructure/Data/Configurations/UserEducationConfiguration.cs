using JoFit.Domain.Identity.Users;
using JoFit.Domain.UserEducations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JoFit.Infrastructure.Data.Configurations;

public class UserEducationConfiguration : IEntityTypeConfiguration<UserEducation>
{
    public void Configure(EntityTypeBuilder<UserEducation> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.SchoolName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.AcademicDegreeId)
            .IsRequired();

        builder.Property(x => x.UniversityMajorId)
            .IsRequired();

        builder.Property(x => x.StartDate)
            .IsRequired();

        builder.Property(x => x.EndDate)
            .IsRequired();

        builder.Property(x => x.IsStillEducation)
            .IsRequired();

        builder.HasOne(x => x.AcademicDegree)
            .WithMany()
            .HasForeignKey(x => x.AcademicDegreeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.UniversityMajor)
            .WithMany()
            .HasForeignKey(x => x.UniversityMajorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.AcademicDegreeId);
        builder.HasIndex(x => x.UniversityMajorId);

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.UserId);

        builder.ToTable("UserEducations");
    }
}
