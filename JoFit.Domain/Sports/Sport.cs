using System.Text.RegularExpressions;
using JoFit.Domain.Common;
using JoFit.Domain.Common.Results;

namespace JoFit.Domain.Sports;

public sealed record SportValues(
    string? Slug,
    string NameEn,
    string NameAr,
    string? DescriptionEn,
    string? DescriptionAr,
    string? ImageMediaId,
    string? ImageUrl,
    string? ImageAlt,
    bool IsFeatured);

public class Sport : AuditableEntity
{
    private static readonly Regex SlugPattern = new("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);

    public string Slug { get; private set; } = string.Empty;
    public string NameEn { get; private set; } = string.Empty;
    public string NameAr { get; private set; } = string.Empty;
    public string? DescriptionEn { get; private set; }
    public string? DescriptionAr { get; private set; }
    public string? ImageMediaId { get; private set; }
    public string? ImageUrl { get; private set; }
    public string? ImageAlt { get; private set; }
    public bool IsFeatured { get; private set; }

    private Sport()
    {
    }

    public static Result<Sport> Create(SportValues values)
    {
        var entity = new Sport();
        var result = entity.Apply(values);
        return result.IsError ? result.Errors : entity;
    }

    public Result<Updated> Update(SportValues values) => Apply(values);

    public Result<Updated> SetParent(long? parentId)
    {
        return Result.Updated;
    }

    private Result<Updated> Apply(SportValues v)
    {
        var errors = new List<Error>();
        var slug = (v.Slug ?? string.Empty).Trim();
        if (slug.Length == 0) errors.Add(SportErrors.SlugRequired);
        else if (!SlugPattern.IsMatch(slug)) errors.Add(SportErrors.SlugInvalid);
        var nameEn = (v.NameEn ?? string.Empty).Trim();
        var nameAr = (v.NameAr ?? string.Empty).Trim();
        if (nameEn.Length == 0) errors.Add(SportErrors.NameEnRequired); else if (nameEn.Length > 100) errors.Add(SportErrors.NameTooLong);
        if (nameAr.Length == 0) errors.Add(SportErrors.NameArRequired); else if (nameAr.Length > 100) errors.Add(SportErrors.NameTooLong.ForField("name.ar"));
        var descriptionEn = string.IsNullOrWhiteSpace(v.DescriptionEn) ? null : v.DescriptionEn.Trim();
        var descriptionAr = string.IsNullOrWhiteSpace(v.DescriptionAr) ? null : v.DescriptionAr.Trim();
        if ((descriptionEn is null) != (descriptionAr is null)) errors.Add(SportErrors.DescriptionIncomplete);
        else if (descriptionEn?.Length > 1000 || descriptionAr?.Length > 1000) errors.Add(SportErrors.DescriptionTooLong);
        var imageError = MediaRules.Validate(v.ImageMediaId, v.ImageUrl, v.ImageAlt, "image");
        if (imageError is not null) errors.Add(imageError.Value);

        if (errors.Count > 0)
        {
            return errors;
        }

        Slug = slug;
        NameEn = nameEn;
        NameAr = nameAr;
        DescriptionEn = descriptionEn;
        DescriptionAr = descriptionAr;
        ImageMediaId = v.ImageMediaId;
        ImageUrl = v.ImageUrl;
        ImageAlt = v.ImageAlt;
        IsFeatured = v.IsFeatured;
        return Result.Updated;
    }

    public void SetFeatured(bool isFeatured) => IsFeatured = isFeatured;
}
