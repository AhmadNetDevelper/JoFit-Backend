using JoFit.Domain.Rental;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JoFit.Infrastructure.Data.Configurations;

public class RentalItemConfiguration : IEntityTypeConfiguration<RentalItem>
{
    public void Configure(EntityTypeBuilder<RentalItem> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Slug).IsRequired().HasMaxLength(180);
        builder.Property(x => x.Status).IsRequired().HasMaxLength(20);
        builder.Property(x => x.TitleEn).IsRequired().HasMaxLength(150);
        builder.Property(x => x.TitleAr).IsRequired().HasMaxLength(150);
        builder.Property(x => x.SummaryEn).IsRequired().HasMaxLength(500);
        builder.Property(x => x.SummaryAr).IsRequired().HasMaxLength(500);
        builder.Property(x => x.DescriptionEn).HasMaxLength(10000);
        builder.Property(x => x.DescriptionAr).HasMaxLength(10000);
        builder.Property(x => x.Condition).IsRequired().HasMaxLength(20);
        builder.Ignore(x => x.IsPublished);

        builder.HasIndex(x => x.Slug).IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.HasIndex(x => x.DepotId);
        builder.HasMany(x => x.Rates).WithOne().HasForeignKey(r => r.RentalItemId).OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("RentalItems");
    }
}

public class RentalRateConfiguration : IEntityTypeConfiguration<RentalRate>
{
    public void Configure(EntityTypeBuilder<RentalRate> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Unit).IsRequired().HasMaxLength(10);
        builder.HasIndex(x => new { x.RentalItemId, x.Unit }).IsUnique();
        builder.ToTable("RentalRates");
    }
}

public class RentalItemCategoryLinkConfiguration : IEntityTypeConfiguration<RentalItemCategoryLink>
{
    public void Configure(EntityTypeBuilder<RentalItemCategoryLink> builder)
    {
        builder.HasKey(x => new { x.RentalItemId, x.RentalCategoryId });
        builder.HasIndex(x => x.RentalCategoryId);
        builder.ToTable("RentalItemCategoryLinks");
    }
}

public class RentalBookingConfiguration : IEntityTypeConfiguration<RentalBooking>
{
    public void Configure(EntityTypeBuilder<RentalBooking> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Unit).IsRequired().HasMaxLength(10);
        builder.Property(x => x.Status).IsRequired().HasMaxLength(20);
        builder.Ignore(x => x.Reference);
        builder.HasIndex(x => new { x.RentalItemId, x.From, x.To });
        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => x.Status);
        builder.ToTable("RentalBookings");
    }
}
