using System.Text.RegularExpressions;
using JoFit.Domain.Common;
using JoFit.Domain.Common.Results;

namespace JoFit.Domain.ArticleCategories;

public sealed record ArticleCategoryValues(
    string? Slug,
    string NameEn,
    string NameAr,
    string? DescriptionEn,
    string? DescriptionAr);

public class ArticleCategory : AuditableEntity
{
    private static readonly Regex SlugPattern = new("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);

    public string Slug { get; private set; } = string.Empty;
    public string NameEn { get; private set; } = string.Empty;
    public string NameAr { get; private set; } = string.Empty;
    public string? DescriptionEn { get; private set; }
    public string? DescriptionAr { get; private set; }

    private ArticleCategory()
    {
    }

    public static Result<ArticleCategory> Create(ArticleCategoryValues values)
    {
        var entity = new ArticleCategory();
        var result = entity.Apply(values);
        return result.IsError ? result.Errors : entity;
    }

    public Result<Updated> Update(ArticleCategoryValues values) => Apply(values);

    public Result<Updated> SetParent(long? parentId)
    {
        return Result.Updated;
    }

    private Result<Updated> Apply(ArticleCategoryValues v)
    {
        var errors = new List<Error>();
        var slug = (v.Slug ?? string.Empty).Trim();
        if (slug.Length == 0) errors.Add(ArticleCategoryErrors.SlugRequired);
        else if (!SlugPattern.IsMatch(slug)) errors.Add(ArticleCategoryErrors.SlugInvalid);
        var nameEn = (v.NameEn ?? string.Empty).Trim();
        var nameAr = (v.NameAr ?? string.Empty).Trim();
        if (nameEn.Length == 0) errors.Add(ArticleCategoryErrors.NameEnRequired); else if (nameEn.Length > 100) errors.Add(ArticleCategoryErrors.NameTooLong);
        if (nameAr.Length == 0) errors.Add(ArticleCategoryErrors.NameArRequired); else if (nameAr.Length > 100) errors.Add(ArticleCategoryErrors.NameTooLong.ForField("name.ar"));
        var descriptionEn = string.IsNullOrWhiteSpace(v.DescriptionEn) ? null : v.DescriptionEn.Trim();
        var descriptionAr = string.IsNullOrWhiteSpace(v.DescriptionAr) ? null : v.DescriptionAr.Trim();
        if ((descriptionEn is null) != (descriptionAr is null)) errors.Add(ArticleCategoryErrors.DescriptionIncomplete);
        else if (descriptionEn?.Length > 1000 || descriptionAr?.Length > 1000) errors.Add(ArticleCategoryErrors.DescriptionTooLong);

        if (errors.Count > 0)
        {
            return errors;
        }

        Slug = slug;
        NameEn = nameEn;
        NameAr = nameAr;
        DescriptionEn = descriptionEn;
        DescriptionAr = descriptionAr;
        return Result.Updated;
    }
}
