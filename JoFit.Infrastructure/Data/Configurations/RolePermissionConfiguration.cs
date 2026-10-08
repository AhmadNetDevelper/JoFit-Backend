using JoFit.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JoFit.Infrastructure.Data.Configurations;

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.HasKey(permission => new { permission.RoleId, permission.PermissionId });
        builder.HasOne(permission => permission.Role)
            .WithMany(role => role.RolePermissions)
            .HasForeignKey(permission => permission.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(permission => permission.Permission)
            .WithMany(permission => permission.RolePermissions)
            .HasForeignKey(permission => permission.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
