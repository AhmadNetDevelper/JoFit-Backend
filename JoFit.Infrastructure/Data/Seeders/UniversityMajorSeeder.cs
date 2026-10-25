using JoFit.Domain.UniversityMajors;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Infrastructure.Data.Seeders;

public class UniversityMajorSeeder(JoFitDbContext context)
{
    private static readonly (string NameAr, string NameEn)[] Majors =
    [
        ("التربية الرياضية", "Physical Education"),
        ("التأهيل الرياضي", "Sports Rehabilitation"),
        ("علوم الرياضة", "Sports Science"),
        ("علوم الحركة و التدريب الرياضي", "Kinesiology and Sports Training"),
        ("الادارة و التدريب الرياضي", "Sports Management and Training"),
        ("أخرى", "Other"),
    ];

    public async Task SeedAsync()
    {
        var existing = await context.UniversityMajors
            .Where(x => !x.IsDeleted)
            .Select(x => x.NameAr)
            .ToListAsync();

        var added = 0;
        foreach (var (nameAr, nameEn) in Majors)
        {
            if (existing.Contains(nameAr))
                continue;

            var result = UniversityMajor.Create(nameAr, nameEn, isActive: true);
            if (result.IsError)
                throw new InvalidOperationException($"Failed to create UniversityMajor '{nameEn}': {result.Errors}");

            context.UniversityMajors.Add(result.Value!);
            added++;
        }

        if (added == 0)
        {
            Console.WriteLine("⏭ UniversityMajors already seeded.");
            return;
        }

        await context.SaveChangesAsync();
        Console.WriteLine($"  ✅ {added} UniversityMajor rows seeded.");
    }
}
