using System.Text;

namespace JoFit.Application.Common.Utilities;

public static class Slugger
{
    /// <summary>Lowercase latin slug ("Sports Nutrition 101" becomes "sports-nutrition-101"). Empty when nothing usable.</summary>
    public static string Slugify(string? text)
    {
        var builder = new StringBuilder();
        var lastHyphen = true;
        foreach (var c in (text ?? string.Empty).ToLowerInvariant())
        {
            if (c is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                builder.Append(c);
                lastHyphen = false;
            }
            else if (!lastHyphen)
            {
                builder.Append('-');
                lastHyphen = true;
            }
        }

        return builder.ToString().Trim('-');
    }

    /// <summary>The first of base, base-2, base-3... that is not taken.</summary>
    public static string Unique(string baseSlug, Func<string, bool> isTaken)
    {
        var slug = baseSlug;
        for (var n = 2; isTaken(slug); n++)
        {
            slug = $"{baseSlug}-{n}";
        }

        return slug;
    }
}
