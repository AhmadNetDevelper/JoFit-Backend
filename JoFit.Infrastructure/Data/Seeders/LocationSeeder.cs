using JoFit.Domain.Locations;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Infrastructure.Data.Seeders;

/// <summary>Seeds the 12 governorate cities of Jordan, used by coaches and by careers.</summary>
public class LocationSeeder(JoFitDbContext context)
{
    public async Task SeedAsync()
    {
        var existing = (await context.Set<Location>().Select(l => l.Slug).ToListAsync()).ToHashSet();
        var missing = Location.Jordan.Where(l => !existing.Contains(l.Slug)).ToList();
        if (missing.Count == 0)
        {
            Console.WriteLine("⏭ Locations already seeded.");
            return;
        }

        context.Set<Location>().AddRange(missing.Select(l => Location.Create(l.Slug, l.En, l.Ar)));
        await context.SaveChangesAsync();
        Console.WriteLine($"  ✅ Locations seeded: {missing.Count}.");
    }
}
