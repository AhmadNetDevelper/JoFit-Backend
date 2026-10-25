using JoFit.Domain.AcademicDegrees;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Infrastructure.Data.Seeders;

public class AcademicDegreeSeeder(JoFitDbContext context)
{
    private static readonly (string NameAr, string NameEn)[] Degrees =
    [
        ("دبلوم", "Diploma"),
        ("بكالوريوس", "Bachelor's degree"),
        ("ماجستير", "Master's degree"),
        ("دكتوراه", "Doctorate"),
    ];

    public async Task SeedAsync()
    {
        var existing = await context.AcademicDegrees
            .Where(x => !x.IsDeleted)
            .Select(x => x.NameAr)
            .ToListAsync();

        var added = 0;
        foreach (var (nameAr, nameEn) in Degrees)
        {
            if (existing.Contains(nameAr))
                continue;

            var result = AcademicDegree.Create(nameAr, nameEn, isActive: true);
            if (result.IsError)
                throw new InvalidOperationException($"Failed to create AcademicDegree '{nameEn}': {result.Errors}");

            context.AcademicDegrees.Add(result.Value!);
            added++;
        }

        if (added == 0)
        {
            Console.WriteLine("⏭ AcademicDegrees already seeded.");
            return;
        }

        await context.SaveChangesAsync();
        Console.WriteLine($"  ✅ {added} AcademicDegree rows seeded.");
    }
}
