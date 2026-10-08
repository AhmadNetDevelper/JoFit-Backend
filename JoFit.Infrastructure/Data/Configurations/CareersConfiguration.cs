using JoFit.Domain.Careers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JoFit.Infrastructure.Data.Configurations;

public class EmployerConfiguration : IEntityTypeConfiguration<Employer>
{
    public void Configure(EntityTypeBuilder<Employer> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Slug).IsRequired().HasMaxLength(150);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Kind).IsRequired().HasMaxLength(30);
        builder.Property(x => x.About).HasMaxLength(3000);
        builder.Property(x => x.Website).HasMaxLength(300);
        builder.Property(x => x.PublicEmail).HasMaxLength(200);
        builder.Property(x => x.PublicPhone).HasMaxLength(30);
        builder.Property(x => x.ContactName).IsRequired().HasMaxLength(150);
        builder.Property(x => x.Size).HasMaxLength(10);
        builder.Property(x => x.LogoMediaId).HasMaxLength(64);
        builder.Property(x => x.LogoUrl).HasMaxLength(500);
        builder.Property(x => x.LogoAlt).HasMaxLength(300);
        builder.Property(x => x.VerificationStatus).IsRequired().HasMaxLength(20);
        builder.Property(x => x.VerificationNote).HasMaxLength(1000);
        builder.Ignore(x => x.IsVerified);

        builder.HasIndex(x => x.Slug).IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.HasIndex(x => x.UserId).IsUnique();
        builder.HasIndex(x => x.VerificationStatus);
        builder.ToTable("Employers");
    }
}

public class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Slug).IsRequired().HasMaxLength(120);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(120);
        builder.Property(x => x.EmploymentType).IsRequired().HasMaxLength(20);
        builder.Property(x => x.WorkMode).IsRequired().HasMaxLength(20);
        builder.Property(x => x.ExperienceLevel).IsRequired().HasMaxLength(20);
        builder.Property(x => x.SalaryPeriod).HasMaxLength(10);
        builder.Property(x => x.Description).IsRequired().HasMaxLength(5000);
        builder.Property(x => x.RequirementsJson).IsRequired().HasColumnType("text");
        builder.Property(x => x.BenefitsJson).IsRequired().HasColumnType("text");
        builder.Property(x => x.Languages).IsRequired().HasMaxLength(10);
        builder.Property(x => x.Status).IsRequired().HasMaxLength(20);
        builder.Property(x => x.ModerationNote).HasMaxLength(1000);
        builder.Ignore(x => x.Reference);
        builder.Ignore(x => x.Requirements);
        builder.Ignore(x => x.Benefits);
        builder.Ignore(x => x.LanguageList);
        builder.Ignore(x => x.NeedsReviewAfterEdit);

        builder.HasIndex(x => x.Slug).IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.HasIndex(x => x.EmployerId);
        builder.HasIndex(x => x.CategoryId);
        builder.HasIndex(x => x.Status);
        builder.ToTable("Jobs");
    }
}

public class JobSportLinkConfiguration : IEntityTypeConfiguration<JobSportLink>
{
    public void Configure(EntityTypeBuilder<JobSportLink> builder)
    {
        builder.HasKey(x => new { x.JobId, x.SportId });
        builder.HasIndex(x => x.SportId);
        builder.ToTable("JobSportLinks");
    }
}

public class CvFileConfiguration : IEntityTypeConfiguration<CvFile>
{
    public void Configure(EntityTypeBuilder<CvFile> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.FileName).IsRequired().HasMaxLength(120);
        builder.Property(x => x.MimeType).IsRequired().HasMaxLength(100);
        builder.Property(x => x.StorageKey).IsRequired().HasMaxLength(120);
        builder.HasIndex(x => x.CustomerId);
        builder.ToTable("CvFiles");
    }
}

public class JobApplicationConfiguration : IEntityTypeConfiguration<JobApplication>
{
    public void Configure(EntityTypeBuilder<JobApplication> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.CandidateName).IsRequired().HasMaxLength(150);
        builder.Property(x => x.CandidateEmail).IsRequired().HasMaxLength(200);
        builder.Property(x => x.CandidatePhone).HasMaxLength(30);
        builder.Property(x => x.CoverLetter).IsRequired().HasMaxLength(3000);
        builder.Property(x => x.Certifications).HasMaxLength(500);
        builder.Property(x => x.Status).IsRequired().HasMaxLength(20);
        builder.Property(x => x.StatusNote).HasMaxLength(500);
        builder.Property(x => x.WithdrawReason).HasMaxLength(500);
        builder.Property(x => x.EmployerNote).HasMaxLength(2000);
        builder.Property(x => x.HistoryJson).IsRequired().HasColumnType("text");
        builder.Ignore(x => x.Reference);
        builder.Ignore(x => x.History);
        builder.Ignore(x => x.IsNew);

        // One live (not withdrawn) application per candidate and job.
        builder.HasIndex(x => new { x.JobId, x.CustomerId }).IsUnique().HasFilter("\"Status\" <> 'withdrawn' AND \"IsDeleted\" = false");
        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => x.Status);
        builder.ToTable("JobApplications");
    }
}
