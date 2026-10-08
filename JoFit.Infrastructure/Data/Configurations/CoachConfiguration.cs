using JoFit.Domain.Coaches;
using JoFit.Domain.Locations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JoFit.Infrastructure.Data.Configurations;

public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Slug).IsRequired().HasMaxLength(50);
        builder.Property(x => x.NameEn).IsRequired().HasMaxLength(100);
        builder.Property(x => x.NameAr).IsRequired().HasMaxLength(100);
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.ToTable("Locations");
    }
}

public class CoachConfiguration : IEntityTypeConfiguration<Coach>
{
    public void Configure(EntityTypeBuilder<Coach> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Slug).IsRequired().HasMaxLength(150);
        builder.Property(x => x.NameEn).IsRequired().HasMaxLength(150);
        builder.Property(x => x.NameAr).IsRequired().HasMaxLength(150);
        builder.Property(x => x.HeadlineEn).IsRequired().HasMaxLength(250);
        builder.Property(x => x.HeadlineAr).IsRequired().HasMaxLength(250);
        builder.Property(x => x.BioEn).HasMaxLength(5000);
        builder.Property(x => x.BioAr).HasMaxLength(5000);
        builder.Property(x => x.Email).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Languages).IsRequired().HasMaxLength(20);
        builder.Property(x => x.VerificationStatus).IsRequired().HasMaxLength(20);
        builder.Property(x => x.VerificationNote).HasMaxLength(1000);
        builder.Property(x => x.WorkingDays).IsRequired().HasMaxLength(20);
        builder.Ignore(x => x.LanguageList);
        builder.Ignore(x => x.WorkingDayList);
        builder.Ignore(x => x.IsListed);

        builder.HasIndex(x => x.Slug).IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.HasIndex(x => x.Email).IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.LocationId);
        builder.HasIndex(x => x.VerificationStatus);

        builder.HasMany(x => x.Packages).WithOne().HasForeignKey(p => p.CoachId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Certifications).WithOne().HasForeignKey(c => c.CoachId).OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("Coaches");
    }
}

public class CoachPackageConfiguration : IEntityTypeConfiguration<CoachPackage>
{
    public void Configure(EntityTypeBuilder<CoachPackage> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.NameEn).IsRequired().HasMaxLength(150);
        builder.Property(x => x.NameAr).IsRequired().HasMaxLength(150);
        builder.Property(x => x.Mode).IsRequired().HasMaxLength(20);
        builder.ToTable("CoachPackages");
    }
}

public class CoachCertificationConfiguration : IEntityTypeConfiguration<CoachCertification>
{
    public void Configure(EntityTypeBuilder<CoachCertification> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.NameEn).IsRequired().HasMaxLength(200);
        builder.Property(x => x.NameAr).IsRequired().HasMaxLength(200);
        builder.Property(x => x.IssuerEn).IsRequired().HasMaxLength(200);
        builder.Property(x => x.IssuerAr).IsRequired().HasMaxLength(200);
        builder.ToTable("CoachCertifications");
    }
}

public class CoachSportLinkConfiguration : IEntityTypeConfiguration<CoachSportLink>
{
    public void Configure(EntityTypeBuilder<CoachSportLink> builder)
    {
        builder.HasKey(x => new { x.CoachId, x.SportId });
        builder.HasIndex(x => x.SportId);
        builder.ToTable("CoachSportLinks");
    }
}

public class CoachSpecialisationLinkConfiguration : IEntityTypeConfiguration<CoachSpecialisationLink>
{
    public void Configure(EntityTypeBuilder<CoachSpecialisationLink> builder)
    {
        builder.HasKey(x => new { x.CoachId, x.CoachSpecialisationId });
        builder.HasIndex(x => x.CoachSpecialisationId);
        builder.ToTable("CoachSpecialisationLinks");
    }
}

public class CoachBookingConfiguration : IEntityTypeConfiguration<CoachBooking>
{
    public void Configure(EntityTypeBuilder<CoachBooking> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.ContactName).IsRequired().HasMaxLength(150);
        builder.Property(x => x.ContactEmail).IsRequired().HasMaxLength(200);
        builder.Property(x => x.ContactPhone).HasMaxLength(30);
        builder.Property(x => x.Goal).HasMaxLength(1000);
        builder.Property(x => x.Status).IsRequired().HasMaxLength(20);
        builder.Property(x => x.Note).HasMaxLength(500);
        builder.Ignore(x => x.Reference);

        // One live booking per coach and start time: makes "slot free, then booked" atomic.
        builder.HasIndex(x => new { x.CoachId, x.StartsAt }).IsUnique().HasFilter("\"Status\" <> 'cancelled' AND \"IsDeleted\" = false");
        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => x.Status);
        builder.ToTable("CoachBookings");
    }
}
