using JoFit.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JoFit.Infrastructure.Data.Configurations;

public class UserPermissionConfiguration : IEntityTypeConfiguration<UserPermission>
{
    public void Configure(EntityTypeBuilder<UserPermission> builder)
    {
        builder.HasKey(permission => new { permission.UserId, permission.PermissionId });
        builder.HasOne(permission => permission.User)
            .WithMany(user => user.UserPermissions)
            .HasForeignKey(permission => permission.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(permission => permission.Permission)
            .WithMany(permission => permission.UserPermissions)
            .HasForeignKey(permission => permission.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
