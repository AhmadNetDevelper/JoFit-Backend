using JoFit.Domain.Common;

namespace JoFit.Domain.Locations;

/// <summary>A city where coaches work and jobs are based (the 12 governorate cities of Jordan).</summary>
public class Location : Entity
{
    /// <summary>A stable key such as "amman" or "irbid".</summary>
    public string Slug { get; private set; } = string.Empty;
    public string NameEn { get; private set; } = string.Empty;
    public string NameAr { get; private set; } = string.Empty;

    private Location()
    {
    }

    public static Location Create(string slug, string nameEn, string nameAr)
        => new() { Slug = slug, NameEn = nameEn, NameAr = nameAr };

    public static readonly (string Slug, string En, string Ar)[] Jordan =
    [
        ("amman", "Amman", "عمّان"),
        ("irbid", "Irbid", "إربد"),
        ("zarqa", "Zarqa", "الزرقاء"),
        ("aqaba", "Aqaba", "العقبة"),
        ("salt", "Salt", "السلط"),
        ("madaba", "Madaba", "مادبا"),
        ("jerash", "Jerash", "جرش"),
        ("ajloun", "Ajloun", "عجلون"),
        ("karak", "Karak", "الكرك"),
        ("mafraq", "Mafraq", "المفرق"),
        ("maan", "Ma'an", "معان"),
        ("tafilah", "Tafilah", "الطفيلة")
    ];
}
