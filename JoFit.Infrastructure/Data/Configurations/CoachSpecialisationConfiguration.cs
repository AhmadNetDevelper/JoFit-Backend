using JoFit.Domain.CoachSpecialisations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JoFit.Infrastructure.Data.Configurations;

public class CoachSpecialisationConfiguration : IEntityTypeConfiguration<CoachSpecialisation>
{
    public void Configure(EntityTypeBuilder<CoachSpecialisation> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.NameEn).IsRequired().HasMaxLength(100);
        builder.Property(x => x.NameAr).IsRequired().HasMaxLength(100);

        builder.ToTable("CoachSpecialisations");
    }
}
