using JoFit.Domain.RentalDepots;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JoFit.Infrastructure.Data.Configurations;

public class RentalDepotConfiguration : IEntityTypeConfiguration<RentalDepot>
{
    public void Configure(EntityTypeBuilder<RentalDepot> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.NameEn).IsRequired().HasMaxLength(100);
        builder.Property(x => x.NameAr).IsRequired().HasMaxLength(100);
        builder.Property(x => x.CityEn).IsRequired().HasMaxLength(100);
        builder.Property(x => x.CityAr).IsRequired().HasMaxLength(100);
        builder.Property(x => x.CountryCode).IsRequired().HasMaxLength(2);

        builder.ToTable("RentalDepots");
    }
}
