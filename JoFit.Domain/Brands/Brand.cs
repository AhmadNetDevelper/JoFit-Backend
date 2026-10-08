using System.Text.RegularExpressions;
using JoFit.Domain.Common;
using JoFit.Domain.Common.Results;

namespace JoFit.Domain.Brands;

public class Brand : AuditableEntity
{
    private static readonly Regex SlugPattern = new("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);

    public string Slug { get; private set; } = string.Empty;
    public string NameEn { get; private set; } = string.Empty;
    public string NameAr { get; private set; } = string.Empty;
    public string? LogoMediaId { get; private set; }
    public string? LogoUrl { get; private set; }
    public string? LogoAlt { get; private set; }

    private Brand()
    {
    }

    public static Result<Brand> Create(string slug, string nameEn, string nameAr, string? logoMediaId, string? logoUrl, string? logoAlt)
    {
        var brand = new Brand();
        var result = brand.Apply(slug, nameEn, nameAr, logoMediaId, logoUrl, logoAlt);
        return result.IsError ? result.Errors : brand;
    }

    public Result<Updated> Update(string slug, string nameEn, string nameAr, string? logoMediaId, string? logoUrl, string? logoAlt)
        => Apply(slug, nameEn, nameAr, logoMediaId, logoUrl, logoAlt);

    private Result<Updated> Apply(string slug, string nameEn, string nameAr, string? logoMediaId, string? logoUrl, string? logoAlt)
    {
        var errors = new List<Error>();
        slug = (slug ?? string.Empty).Trim();
        nameEn = (nameEn ?? string.Empty).Trim();
        nameAr = (nameAr ?? string.Empty).Trim();

        if (slug.Length == 0) errors.Add(BrandErrors.SlugRequired);
        else if (!SlugPattern.IsMatch(slug)) errors.Add(BrandErrors.SlugInvalid);

        if (nameEn.Length == 0) errors.Add(BrandErrors.NameEnRequired);
        else if (nameEn.Length > 100) errors.Add(BrandErrors.NameTooLong);
        if (nameAr.Length == 0) errors.Add(BrandErrors.NameArRequired);
        else if (nameAr.Length > 100) errors.Add(BrandErrors.NameTooLong.ForField("name.ar"));

        if (logoAlt is { Length: > 300 }) errors.Add(BrandErrors.LogoAltTooLong);

        if (errors.Count > 0) return errors;

        Slug = slug;
        NameEn = nameEn;
        NameAr = nameAr;
        LogoMediaId = logoMediaId;
        LogoUrl = logoUrl;
        LogoAlt = logoAlt;
        return Result.Updated;
    }
}
