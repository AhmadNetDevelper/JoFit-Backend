using JoFit.Domain.UserJobTypes;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Infrastructure.Data.Seeders;

public class UserJobTypeSeeder(JoFitDbContext context)
{
    public async Task SeedAsync()
    {
        var exists = await context.UserJobTypes
            .AnyAsync(x => x.IsNormalUser && !x.IsDeleted);

        if (exists)
        {
            Console.WriteLine("⏭ Default normal UserJobType already exists.");
            return;
        }

        var result = UserJobType.Create("مستخدم", "user", isActive: true, isNormalUser: true);
        if (result.IsError)
            throw new InvalidOperationException($"Failed to create default UserJobType: {result.Errors}");

        context.UserJobTypes.Add(result.Value!);
        await context.SaveChangesAsync();

        Console.WriteLine("  ✅ Default normal UserJobType seeded.");
    }
}
