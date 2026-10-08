using JoFit.Domain.Identity.Roles;
using JoFit.Domain.Identity.Users;
using JoFit.Infrastructure.Data.Seeders;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace JoFit.Infrastructure.Data.Extensions;

public static class DatabaseSeedingExtensions
{
    /// <summary>
    /// Seeds the database with initial data including permissions, the SuperAdmin role, and a SuperAdmin user.
    /// Call this during application startup.
    /// </summary>
    /// <param name="app">The web application builder</param>
    /// <returns>A task that completes when seeding is done</returns>
    public static async Task SeedDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        try
        {
            var context = scope.ServiceProvider.GetRequiredService<JoFitDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();

            var seeder = new IdentitySeeder(context, userManager, roleManager);
            await seeder.SeedAsync();

            await new UserJobTypeSeeder(context).SeedAsync();
            await new LocationSeeder(context).SeedAsync();
            await new CommunitySeeder(context).SeedAsync();

            Console.WriteLine("✅ Database seeding completed successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ An error occurred while seeding the database: {ex.Message}");
            throw;
        }
    }
}
