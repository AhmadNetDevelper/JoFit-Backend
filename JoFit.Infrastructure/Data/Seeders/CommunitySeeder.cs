using JoFit.Domain.Community;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Infrastructure.Data.Seeders;

/// <summary>Seeds the discussion topics posts are filed under.</summary>
public class CommunitySeeder(JoFitDbContext context)
{
    public async Task SeedAsync()
    {
        var existing = (await context.Set<CommunityTopic>().Select(t => t.Slug).ToListAsync()).ToHashSet();
        var missing = CommunityTopic.Defaults.Where(t => !existing.Contains(t.Slug)).ToList();
        if (missing.Count == 0)
        {
            return;
        }

        context.Set<CommunityTopic>().AddRange(missing.Select(t => CommunityTopic.Create(t.Slug, t.En, t.Ar)));
        await context.SaveChangesAsync();
        Console.WriteLine($"  ✅ Community topics seeded: {missing.Count}.");
    }
}
