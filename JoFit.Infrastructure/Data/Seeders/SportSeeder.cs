using JoFit.Domain.Sports;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Infrastructure.Data.Seeders;

public class SportSeeder(JoFitDbContext context)
{
    private static readonly (string Slug, string En, string Ar, bool Featured)[] Sports =
    [
        ("fitness", "Fitness", "لياقة بدنية", true),
        ("bodybuilding", "Bodybuilding", "كمال أجسام", true),
        ("crossfit", "CrossFit", "كروس فيت", true),
        ("yoga", "Yoga", "يوغا", false),
        ("pilates", "Pilates", "بيلاتس", false),
        ("boxing", "Boxing", "ملاكمة", false),
        ("swimming", "Swimming", "سباحة", false),
        ("running", "Running", "جري", false),
        ("football", "Football", "كرة القدم", false),
        ("nutrition", "Nutrition", "تغذية", false),
    ];

    public async Task SeedAsync()
    {
        var existing = (await context.Sports
            .Where(x => !x.IsDeleted)
            .Select(x => x.Slug)
            .ToListAsync()).ToHashSet();

        var added = 0;
        foreach (var (slug, en, ar, featured) in Sports)
        {
            if (existing.Contains(slug))
                continue;

            var result = Sport.Create(new SportValues(slug, en, ar, null, null, null, null, null, featured));
            if (result.IsError)
                throw new InvalidOperationException($"Failed to create Sport '{slug}': {result.Errors}");

            context.Sports.Add(result.Value!);
            added++;
        }

        if (added == 0)
        {
            Console.WriteLine("⏭ Sports already seeded.");
            return;
        }

        await context.SaveChangesAsync();
        Console.WriteLine($"  ✅ {added} sports seeded.");
    }
}
