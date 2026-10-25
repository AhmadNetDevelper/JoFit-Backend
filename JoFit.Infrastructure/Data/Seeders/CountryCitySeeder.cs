using JoFit.Domain.Cities;
using JoFit.Domain.Countries;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Infrastructure.Data.Seeders;

/// <summary>Seeds Jordan as the only country, with the main city of each of its 12 governorates.</summary>
public class CountryCitySeeder(JoFitDbContext context)
{
    private static readonly (string Ar, string En)[] JordanCities =
    [
        ("عمّان", "Amman"),
        ("إربد", "Irbid"),
        ("الزرقاء", "Zarqa"),
        ("السلط", "Salt"),
        ("مادبا", "Madaba"),
        ("الكرك", "Karak"),
        ("الطفيلة", "Tafilah"),
        ("معان", "Ma'an"),
        ("العقبة", "Aqaba"),
        ("جرش", "Jerash"),
        ("عجلون", "Ajloun"),
        ("المفرق", "Mafraq"),
    ];

    public async Task SeedAsync()
    {
        var country = await context.Countries.FirstOrDefaultAsync(c => c.NameEn == "Jordan");
        if (country is null)
        {
            var created = Country.Create("الأردن", "Jordan");
            if (created.IsError)
                throw new InvalidOperationException($"Failed to create Jordan: {created.Errors}");

            country = created.Value!;
            context.Countries.Add(country);
            await context.SaveChangesAsync();
            Console.WriteLine("  ✅ Country seeded: Jordan.");
        }

        var existing = (await context.Cities
                .Where(c => c.CountryId == country.Id)
                .Select(c => c.NameEn)
                .ToListAsync())
            .ToHashSet();

        var added = 0;
        foreach (var (ar, en) in JordanCities.Where(c => !existing.Contains(c.En)))
        {
            var city = City.Create(ar, en, country.Id);
            if (city.IsError)
                throw new InvalidOperationException($"Failed to create city {en}: {city.Errors}");

            context.Cities.Add(city.Value!);
            added++;
        }

        if (added == 0)
        {
            Console.WriteLine("⏭ Jordan cities already seeded.");
            return;
        }

        await context.SaveChangesAsync();
        Console.WriteLine($"  ✅ Cities seeded: {added}.");
    }
}
