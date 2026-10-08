using JoFit.Domain.Enrolments;
using JoFit.Domain.Quotations;
using JoFit.Domain.Reviews;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JoFit.Infrastructure.Data.Configurations;

public class EnrolmentConfiguration : IEntityTypeConfiguration<Enrolment>
{
    public void Configure(EntityTypeBuilder<Enrolment> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Status).IsRequired().HasMaxLength(20);
        builder.Property(x => x.Note).HasMaxLength(500);
        builder.Ignore(x => x.Reference);

        // One live (pending or active) enrolment per customer and course, even under a race.
        builder.HasIndex(x => new { x.CourseId, x.CustomerId })
            .IsUnique().HasFilter("\"Status\" IN ('pending', 'active') AND \"IsDeleted\" = false");
        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => x.Status);
        builder.ToTable("Enrolments");
    }
}

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.TargetType).IsRequired().HasMaxLength(20);
        builder.Property(x => x.Title).HasMaxLength(120);
        builder.Property(x => x.Body).IsRequired().HasMaxLength(3000);
        builder.Property(x => x.ModerationStatus).IsRequired().HasMaxLength(20);
        builder.Property(x => x.ModerationNote).HasMaxLength(1000);

        // A customer reviews each target once.
        builder.HasIndex(x => new { x.CustomerId, x.TargetType, x.TargetId }).IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.HasIndex(x => new { x.TargetType, x.TargetId, x.ModerationStatus });
        builder.ToTable("Reviews");
    }
}

public class QuotationRequestConfiguration : IEntityTypeConfiguration<QuotationRequest>
{
    public void Configure(EntityTypeBuilder<QuotationRequest> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.SubjectKind).IsRequired().HasMaxLength(40);
        builder.Property(x => x.OrganisationName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.ContactName).IsRequired().HasMaxLength(150);
        builder.Property(x => x.Email).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Phone).HasMaxLength(30);
        builder.Property(x => x.PreferredContact).IsRequired().HasMaxLength(10);
        builder.Property(x => x.Message).IsRequired().HasMaxLength(5000);
        builder.Property(x => x.QuantityEstimate).HasMaxLength(100);
        builder.Property(x => x.BudgetRange).HasMaxLength(100);
        builder.Property(x => x.Status).IsRequired().HasMaxLength(20);
        builder.Property(x => x.InternalNote).HasMaxLength(2000);
        builder.Ignore(x => x.Reference);
        builder.HasIndex(x => x.Status);
        builder.ToTable("QuotationRequests");
    }
}
