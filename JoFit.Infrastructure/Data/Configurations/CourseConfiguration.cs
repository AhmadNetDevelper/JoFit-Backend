using JoFit.Domain.Courses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JoFit.Infrastructure.Data.Configurations;

public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
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

        // Catalogue side of a course.
        builder.Property(x => x.Slug).HasMaxLength(120);
        builder.Property(x => x.Status).IsRequired().HasMaxLength(20).HasDefaultValue("draft");
        builder.Property(x => x.SummaryEn).IsRequired().HasMaxLength(500).HasDefaultValue("");
        builder.Property(x => x.SummaryAr).IsRequired().HasMaxLength(500).HasDefaultValue("");
        builder.Property(x => x.DescriptionEn).HasMaxLength(10000);
        builder.Property(x => x.DescriptionAr).HasMaxLength(10000);
        builder.Property(x => x.Format).IsRequired().HasMaxLength(20).HasDefaultValue("online");
        builder.Property(x => x.Level).IsRequired().HasMaxLength(20).HasDefaultValue("beginner");
        builder.Property(x => x.DurationHours).HasPrecision(8, 2);
        builder.Property(x => x.Languages).IsRequired().HasMaxLength(20).HasDefaultValue("ar");
        builder.Property(x => x.VenueEn).HasMaxLength(200);
        builder.Property(x => x.VenueAr).HasMaxLength(200);
        builder.Property(x => x.CertificationNameEn).HasMaxLength(200);
        builder.Property(x => x.CertificationNameAr).HasMaxLength(200);
        builder.Property(x => x.CertificationIssuedByEn).HasMaxLength(200);
        builder.Property(x => x.CertificationIssuedByAr).HasMaxLength(200);
        builder.HasIndex(x => x.Slug).IsUnique().HasFilter("\"Slug\" IS NOT NULL AND \"IsDeleted\" = false");
        builder.Ignore(x => x.LanguageList);
        builder.Ignore(x => x.IsPublished);

        builder.ToTable("Courses");
    }
}

public class CourseCategoryLinkConfiguration : IEntityTypeConfiguration<CourseCategoryLink>
{
    public void Configure(EntityTypeBuilder<CourseCategoryLink> builder)
    {
        builder.HasKey(x => new { x.CourseId, x.CourseCategoryId });
        builder.HasIndex(x => x.CourseCategoryId);
        builder.ToTable("CourseCategoryLinks");
    }
}

public class CourseInstructorLinkConfiguration : IEntityTypeConfiguration<CourseInstructorLink>
{
    public void Configure(EntityTypeBuilder<CourseInstructorLink> builder)
    {
        builder.HasKey(x => new { x.CourseId, x.CoachId });
        builder.HasIndex(x => x.CoachId);
        builder.ToTable("CourseInstructorLinks");
    }
}

public class CourseSportLinkConfiguration : IEntityTypeConfiguration<CourseSportLink>
{
    public void Configure(EntityTypeBuilder<CourseSportLink> builder)
    {
        builder.HasKey(x => new { x.CourseId, x.SportId });
        builder.HasIndex(x => x.SportId);
        builder.ToTable("CourseSportLinks");
    }
}

public class RecordMediaConfiguration : IEntityTypeConfiguration<JoFit.Domain.Media.RecordMedia>
{
    public void Configure(EntityTypeBuilder<JoFit.Domain.Media.RecordMedia> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.OwnerType).IsRequired().HasMaxLength(40);
        builder.Property(x => x.Field).IsRequired().HasMaxLength(40);
        builder.Property(x => x.MediaId).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Url).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Alt).HasMaxLength(300);
        builder.HasIndex(x => new { x.OwnerType, x.OwnerId, x.Field });
        builder.ToTable("RecordMedia");
    }
}
