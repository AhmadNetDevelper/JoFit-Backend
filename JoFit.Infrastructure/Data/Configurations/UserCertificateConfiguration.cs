using JoFit.Domain.Identity.Users;
using JoFit.Domain.UserCertificates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JoFit.Infrastructure.Data.Configurations;

public class UserCertificateConfiguration : IEntityTypeConfiguration<UserCertificate>
{
    public void Configure(EntityTypeBuilder<UserCertificate> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.CertificateName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.OrganizationName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.IssuedDate)
            .IsRequired();

        builder.Property(x => x.ExpiryDate)
            .IsRequired();

        builder.Property(x => x.CredentialId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.CredentialUrl)
            .IsRequired(false)
            .HasMaxLength(500);

        builder.Property(x => x.CertificateUrl)
            .IsRequired(false)
            .HasMaxLength(500);

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.UserId);

        builder.ToTable("UserCertificates");
    }
}
