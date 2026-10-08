using System.Text.RegularExpressions;
using JoFit.Domain.Common;
using JoFit.Domain.Common.Results;

namespace JoFit.Domain.CourseCategories;

public sealed record CourseCategoryValues(
    string? Slug,
    string NameEn,
    string NameAr,
    string? DescriptionEn,
    string? DescriptionAr);

public class CourseCategory : AuditableEntity
{
    private static readonly Regex SlugPattern = new("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);

    public string Slug { get; private set; } = string.Empty;
    public string NameEn { get; private set; } = string.Empty;
    public string NameAr { get; private set; } = string.Empty;
    public string? DescriptionEn { get; private set; }
    public string? DescriptionAr { get; private set; }

    private CourseCategory()
    {
    }

    public static Result<CourseCategory> Create(CourseCategoryValues values)
    {
        var entity = new CourseCategory();
        var result = entity.Apply(values);
        return result.IsError ? result.Errors : entity;
    }

    public Result<Updated> Update(CourseCategoryValues values) => Apply(values);

    public Result<Updated> SetParent(long? parentId)
    {
        return Result.Updated;
    }

    private Result<Updated> Apply(CourseCategoryValues v)
    {
        var errors = new List<Error>();
        var slug = (v.Slug ?? string.Empty).Trim();
        if (slug.Length == 0) errors.Add(CourseCategoryErrors.SlugRequired);
        else if (!SlugPattern.IsMatch(slug)) errors.Add(CourseCategoryErrors.SlugInvalid);
        var nameEn = (v.NameEn ?? string.Empty).Trim();
        var nameAr = (v.NameAr ?? string.Empty).Trim();
        if (nameEn.Length == 0) errors.Add(CourseCategoryErrors.NameEnRequired); else if (nameEn.Length > 100) errors.Add(CourseCategoryErrors.NameTooLong);
        if (nameAr.Length == 0) errors.Add(CourseCategoryErrors.NameArRequired); else if (nameAr.Length > 100) errors.Add(CourseCategoryErrors.NameTooLong.ForField("name.ar"));
        var descriptionEn = string.IsNullOrWhiteSpace(v.DescriptionEn) ? null : v.DescriptionEn.Trim();
        var descriptionAr = string.IsNullOrWhiteSpace(v.DescriptionAr) ? null : v.DescriptionAr.Trim();
        if ((descriptionEn is null) != (descriptionAr is null)) errors.Add(CourseCategoryErrors.DescriptionIncomplete);
        else if (descriptionEn?.Length > 1000 || descriptionAr?.Length > 1000) errors.Add(CourseCategoryErrors.DescriptionTooLong);

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
