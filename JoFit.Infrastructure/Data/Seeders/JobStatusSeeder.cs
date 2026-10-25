using JoFit.Domain.JobStatuses;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Infrastructure.Data.Seeders;

public class JobStatusSeeder(JoFitDbContext context)
{
    private static readonly (string NameAr, string NameEn)[] Statuses =
    [
        ("دوام جزئي", "Part-time"),
        ("دوام كامل", "Full-time"),
        ("عمل مستقل", "Freelance"),
        ("جاهز للعمل", "Available for work"),
    ];

    public async Task SeedAsync()
    {
        var existing = await context.JobStatuses
            .Where(x => !x.IsDeleted)
            .Select(x => x.NameAr)
            .ToListAsync();

        var added = 0;
        foreach (var (nameAr, nameEn) in Statuses)
        {
            if (existing.Contains(nameAr))
                continue;

            var result = JobStatus.Create(nameAr, nameEn, isActive: true);
            if (result.IsError)
                throw new InvalidOperationException($"Failed to create JobStatus '{nameEn}': {result.Errors}");

            context.JobStatuses.Add(result.Value!);
            added++;
        }

        if (added == 0)
        {
            Console.WriteLine("⏭ JobStatuses already seeded.");
            return;
        }

        await context.SaveChangesAsync();
        Console.WriteLine($"  ✅ {added} JobStatus rows seeded.");
    }
}
