using JoFit.Domain.Identity.Permissions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JoFit.Infrastructure.Data.Configurations;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.Property(permission => permission.Name).HasMaxLength(200).IsRequired();
        builder.Property(permission => permission.Resource).HasMaxLength(100).IsRequired();
        builder.Property(permission => permission.Action).HasMaxLength(100).IsRequired();
        builder.HasIndex(permission => permission.Name).IsUnique();
    }
}
