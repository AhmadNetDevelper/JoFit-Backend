using JoFit.Domain.Customers;
using JoFit.Domain.Identity.Sessions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JoFit.Infrastructure.Data.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Name).IsRequired().HasMaxLength(150);
        builder.Property(x => x.Phone).HasMaxLength(30);
        builder.Property(x => x.Locale).IsRequired().HasMaxLength(2);

        builder.HasIndex(x => x.UserId).IsUnique();

        builder.ToTable("Customers");
    }
}

public class AccountSessionConfiguration : IEntityTypeConfiguration<AccountSession>
{
    public void Configure(EntityTypeBuilder<AccountSession> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Jti).IsRequired().HasMaxLength(64);

        builder.HasIndex(x => x.Jti).IsUnique();
        builder.HasIndex(x => x.UserId);

        builder.ToTable("AccountSessions");
    }
}
