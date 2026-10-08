using System.Text.RegularExpressions;
using JoFit.Domain.Common;
using JoFit.Domain.Common.Results;

namespace JoFit.Domain.JobCategories;

public sealed record JobCategoryValues(
    string? Slug,
    string NameEn,
    string NameAr);

public class JobCategory : AuditableEntity
{
    private static readonly Regex SlugPattern = new("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);

    public string Slug { get; private set; } = string.Empty;
    public string NameEn { get; private set; } = string.Empty;
    public string NameAr { get; private set; } = string.Empty;

    private JobCategory()
    {
    }

    public static Result<JobCategory> Create(JobCategoryValues values)
    {
        var entity = new JobCategory();
        var result = entity.Apply(values);
        return result.IsError ? result.Errors : entity;
    }

    public Result<Updated> Update(JobCategoryValues values) => Apply(values);

    public Result<Updated> SetParent(long? parentId)
    {
        return Result.Updated;
    }

    private Result<Updated> Apply(JobCategoryValues v)
    {
        var errors = new List<Error>();
        var slug = (v.Slug ?? string.Empty).Trim();
        if (slug.Length == 0) errors.Add(JobCategoryErrors.SlugRequired);
        else if (!SlugPattern.IsMatch(slug)) errors.Add(JobCategoryErrors.SlugInvalid);
        var nameEn = (v.NameEn ?? string.Empty).Trim();
        var nameAr = (v.NameAr ?? string.Empty).Trim();
        if (nameEn.Length == 0) errors.Add(JobCategoryErrors.NameEnRequired); else if (nameEn.Length > 100) errors.Add(JobCategoryErrors.NameTooLong);
        if (nameAr.Length == 0) errors.Add(JobCategoryErrors.NameArRequired); else if (nameAr.Length > 100) errors.Add(JobCategoryErrors.NameTooLong.ForField("name.ar"));

        if (errors.Count > 0)
        {
            return errors;
        }

        Slug = slug;
        NameEn = nameEn;
        NameAr = nameAr;
        return Result.Updated;
    }
}
