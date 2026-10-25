using JoFit.Domain.JobTypes;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Infrastructure.Data.Seeders;

public class JobTypeSeeder(JoFitDbContext context)
{
    private static readonly (string NameAr, string NameEn)[] Types =
    [
        ("دوام كامل", "Full-time"),
        ("دوام جزئي", "Part-time"),
        ("حسابي الخاص", "Self-employed"),
    ];

    public async Task SeedAsync()
    {
        var existing = await context.JobTypes
            .Where(x => !x.IsDeleted)
            .Select(x => x.NameAr)
            .ToListAsync();

        var added = 0;
        foreach (var (nameAr, nameEn) in Types)
        {
            if (existing.Contains(nameAr))
                continue;

            var result = JobType.Create(nameAr, nameEn, isActive: true);
            if (result.IsError)
                throw new InvalidOperationException($"Failed to create JobType '{nameEn}': {result.Errors}");

            context.JobTypes.Add(result.Value!);
            added++;
        }

        if (added == 0)
        {
            Console.WriteLine("⏭ JobTypes already seeded.");
            return;
        }

        await context.SaveChangesAsync();
        Console.WriteLine($"  ✅ {added} JobType rows seeded.");
    }
}
