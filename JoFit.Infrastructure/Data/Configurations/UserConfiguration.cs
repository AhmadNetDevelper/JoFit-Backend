using JoFit.Domain.Cities;
using JoFit.Domain.Countries;
using JoFit.Domain.Identity.Users;
using JoFit.Domain.JobStatuses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JoFit.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(x => x.FullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.ProfileImageUrl)
            .IsRequired(false)
            .HasMaxLength(300);

        builder.Property(x => x.GenderId)
            .HasConversion<short>()
            .IsRequired();

        builder.HasOne<Country>()
            .WithMany()
            .HasForeignKey(x => x.CountryId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<City>()
            .WithMany()
            .HasForeignKey(x => x.CityId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<JobStatus>()
            .WithMany()
            .HasForeignKey(x => x.JobStatusId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.CountryId);
        builder.HasIndex(x => x.CityId);
        builder.HasIndex(x => x.JobStatusId);

        builder.Property(x => x.BirthOfDate)
            .HasColumnType("timestamp without time zone");

        builder.ToTable("Users");
    }
}
