using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.Courses.Catalogue;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Settings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.Settings;

public sealed record ThemeTypography(string FontFamily, string Scale);

public sealed record ThemeShape(string Skew);

public sealed record ThemeLayout(string HeaderMode);

/// <summary>What the storefront paints with. Every value is validated strictly on write, because it ends up in a style attribute.</summary>
public sealed record ThemeDoc(
    string Id,
    string Name,
    Dictionary<string, string> Colors,
    ThemeTypography Typography,
    ThemeShape Shape,
    ThemeLayout Layout);

public sealed record StorefrontThemeDto(string Id, string Name, Dictionary<string, string> Colors, ThemeTypography Typography, ThemeShape Shape, ThemeLayout Layout, AuditDto? Audit);

public sealed record GetPublicThemeQuery : IRequest<Result<StorefrontThemeDto>>;

public sealed record GetAdminThemeQuery : IRequest<Result<StorefrontThemeDto>>;

public sealed record SaveThemeCommand(ThemeDoc Draft) : IRequest<Result<StorefrontThemeDto>>;

public sealed record ResetThemeCommand : IRequest<Result<StorefrontThemeDto>>;

public static partial class ThemeRules
{
    private static readonly string[] ArabicFonts = ["tajawal", "cairo", "noto sans arabic", "noto kufi arabic", "ibm plex sans arabic"];

    public static readonly ThemeDoc Default = new(
        "jofit-default",
        "JoFit default",
        new Dictionary<string, string>
        {
            ["primary"] = "#1B5E20",
            ["onPrimary"] = "#FFFFFF",
            ["secondary"] = "#FFC107",
            ["onSecondary"] = "#1A1A1A",
            ["background"] = "#F7F7F5",
            ["surface"] = "#FFFFFF",
            ["title"] = "#111111",
            ["body"] = "#2B2B2B",
            ["muted"] = "#6B7280",
            ["border"] = "#E5E7EB",
            ["accent"] = "#00796B",
            ["danger"] = "#B42318"
        },
        new ThemeTypography("Tajawal, Cairo, sans-serif", "1"),
        new ThemeShape("0deg"),
        new ThemeLayout("fixed"));

    [GeneratedRegex(@"^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$")]
    private static partial Regex HexPattern();

    [GeneratedRegex(@"^[A-Za-z0-9 ,'""_-]+$")]
    private static partial Regex FontPattern();

    [GeneratedRegex(@"^-?\d+(\.\d+)?deg$")]
    private static partial Regex SkewPattern();

    [GeneratedRegex(@"^\d+(\.\d+)?$")]
    private static partial Regex ScalePattern();

    /// <summary>Contrast of text on its background must reach 4.5 (WCAG AA) for these pairs: text colour first.</summary>
    private static readonly (string Text, string Background)[] ContrastPairs =
    [
        ("onPrimary", "primary"), ("onSecondary", "secondary"), ("body", "surface"), ("title", "surface")
    ];

    public static List<Error> Validate(ThemeDoc doc)
    {
        var errors = new List<Error>();
        static Error Field(string field, string message) => Error.Validation($"Theme.{field}", message).ForField(field);

        if (string.IsNullOrWhiteSpace(doc.Id) || !Regex.IsMatch(doc.Id, "^[a-z0-9-]{1,40}$")) errors.Add(Field("id", "The theme id is lowercase letters, numbers and hyphens."));
        if (string.IsNullOrWhiteSpace(doc.Name) || doc.Name.Length > 60) errors.Add(Field("name", "Give the theme a name of up to 60 characters."));

        var colors = doc.Colors ?? [];
        foreach (var (key, value) in colors)
        {
            if (!Regex.IsMatch(key, "^[A-Za-z][A-Za-z0-9]{0,30}$") || !HexPattern().IsMatch(value ?? string.Empty))
                errors.Add(Field($"colors.{key}", "Use a hex colour such as #1B5E20 or #1b5."));
        }

        foreach (var (text, background) in ContrastPairs)
        {
            if (!colors.TryGetValue(text, out var fg) || !colors.TryGetValue(background, out var bg))
            {
                errors.Add(Field($"colors.{text}", $"Set both {text} and {background}."));
                continue;
            }

            if (HexPattern().IsMatch(fg) && HexPattern().IsMatch(bg) && Contrast(fg, bg) < 4.5)
                errors.Add(Field($"colors.{text}", $"{text} on {background} needs a contrast of at least 4.5 (it is {Contrast(fg, bg):0.0})."));
        }

        var font = doc.Typography?.FontFamily ?? string.Empty;
        if (!FontPattern().IsMatch(font) || !ArabicFonts.Any(f => font.Contains(f, StringComparison.OrdinalIgnoreCase)))
            errors.Add(Field("typography.fontFamily", "Use letters, numbers, commas and quotes only, and include an Arabic font (Tajawal, Cairo, Noto Sans Arabic, Noto Kufi Arabic or IBM Plex Sans Arabic)."));

        var scale = doc.Typography?.Scale ?? string.Empty;
        if (!ScalePattern().IsMatch(scale) || !(double.Parse(scale, CultureInfo.InvariantCulture) > 0))
            errors.Add(Field("typography.scale", "The scale is a positive number such as 1 or 1.125."));

        if (!SkewPattern().IsMatch(doc.Shape?.Skew ?? string.Empty)) errors.Add(Field("shape.skew", "The skew is an angle such as 0deg or -6deg."));
        if (doc.Layout?.HeaderMode is not ("fixed" or "static")) errors.Add(Field("layout.headerMode", "Choose fixed or static."));
        return errors;
    }

    private static double Luminance(string hex)
    {
        var h = hex.TrimStart('#');
        if (h.Length == 3) h = string.Concat(h.Select(c => $"{c}{c}"));
        double Channel(int start)
        {
            var v = Convert.ToInt32(h.Substring(start, 2), 16) / 255.0;
            return v <= 0.03928 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
        }

        return 0.2126 * Channel(0) + 0.7152 * Channel(2) + 0.0722 * Channel(4);
    }

    public static double Contrast(string a, string b)
    {
        var (light, dark) = (Math.Max(Luminance(a), Luminance(b)), Math.Min(Luminance(a), Luminance(b)));
        return (light + 0.05) / (dark + 0.05);
    }
}

internal static class ThemeStore
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static async Task<(ThemeSetting Row, ThemeDoc Doc)> LoadAsync(IUnitOfWork unitOfWork, CancellationToken ct)
    {
        var repository = unitOfWork.GetRepository<ThemeSetting>();
        var row = await repository.GetTableAsTracking().FirstOrDefaultAsync(ct);
        if (row is null)
        {
            // The first read seeds the default theme.
            row = ThemeSetting.Create(JsonSerializer.Serialize(ThemeRules.Default, Json));
            await repository.AddAsync(row, ct);
            await unitOfWork.SaveChangesAsync(ct);
        }

        return (row, JsonSerializer.Deserialize<ThemeDoc>(row.DocumentJson, Json) ?? ThemeRules.Default);
    }

    public static async Task<StorefrontThemeDto> SaveAsync(IUnitOfWork unitOfWork, ThemeDoc doc, CancellationToken ct)
    {
        var (row, _) = await LoadAsync(unitOfWork, ct);
        row.Replace(JsonSerializer.Serialize(doc, Json));
        await unitOfWork.SaveChangesAsync(ct);
        return Dto(doc, row);
    }

    public static StorefrontThemeDto Dto(ThemeDoc doc, ThemeSetting? row)
        => new(doc.Id, doc.Name, doc.Colors, doc.Typography, doc.Shape, doc.Layout, row is null ? null : AuditDto.From(row));
}

public class GetPublicThemeQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetPublicThemeQuery, Result<StorefrontThemeDto>>
{
    public async Task<Result<StorefrontThemeDto>> Handle(GetPublicThemeQuery request, CancellationToken ct)
    {
        var (_, doc) = await ThemeStore.LoadAsync(unitOfWork, ct);
        return ThemeStore.Dto(doc, null);
    }
}

public class GetAdminThemeQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetAdminThemeQuery, Result<StorefrontThemeDto>>
{
    public async Task<Result<StorefrontThemeDto>> Handle(GetAdminThemeQuery request, CancellationToken ct)
    {
        var (row, doc) = await ThemeStore.LoadAsync(unitOfWork, ct);
        return ThemeStore.Dto(doc, row);
    }
}

public class SaveThemeCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<SaveThemeCommand, Result<StorefrontThemeDto>>
{
    public async Task<Result<StorefrontThemeDto>> Handle(SaveThemeCommand request, CancellationToken ct)
    {
        var errors = ThemeRules.Validate(request.Draft);
        return errors.Count > 0 ? errors : await ThemeStore.SaveAsync(unitOfWork, request.Draft, ct);
    }
}

public class ResetThemeCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<ResetThemeCommand, Result<StorefrontThemeDto>>
{
    public async Task<Result<StorefrontThemeDto>> Handle(ResetThemeCommand request, CancellationToken ct)
        => await ThemeStore.SaveAsync(unitOfWork, ThemeRules.Default, ct);
}
