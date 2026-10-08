using System.Text.RegularExpressions;
using JoFit.Domain.Common;
using JoFit.Domain.Common.Results;

namespace JoFit.Domain.Authors;

public sealed record AuthorValues(
    string? Slug,
    string Name,
    string? RoleEn,
    string? RoleAr,
    string? BioEn,
    string? BioAr,
    string? AvatarMediaId,
    string? AvatarUrl,
    string? AvatarAlt);

public class Author : AuditableEntity
{
    private static readonly Regex SlugPattern = new("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);

    public string? Slug { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? RoleEn { get; private set; }
    public string? RoleAr { get; private set; }
    public string? BioEn { get; private set; }
    public string? BioAr { get; private set; }
    public string? AvatarMediaId { get; private set; }
    public string? AvatarUrl { get; private set; }
    public string? AvatarAlt { get; private set; }

    private Author()
    {
    }

    public static Result<Author> Create(AuthorValues values)
    {
        var entity = new Author();
        var result = entity.Apply(values);
        return result.IsError ? result.Errors : entity;
    }

    public Result<Updated> Update(AuthorValues values) => Apply(values);

    public Result<Updated> SetParent(long? parentId)
    {
        return Result.Updated;
    }

    private Result<Updated> Apply(AuthorValues v)
    {
        var errors = new List<Error>();
        var slug = string.IsNullOrWhiteSpace(v.Slug) ? null : v.Slug.Trim();
        if (slug is not null && !SlugPattern.IsMatch(slug)) errors.Add(AuthorErrors.SlugInvalid);
        var name = (v.Name ?? string.Empty).Trim();
        if (name.Length == 0) errors.Add(AuthorErrors.NameRequired); else if (name.Length > 100) errors.Add(AuthorErrors.NameTooLong);
        var roleEn = string.IsNullOrWhiteSpace(v.RoleEn) ? null : v.RoleEn.Trim();
        var roleAr = string.IsNullOrWhiteSpace(v.RoleAr) ? null : v.RoleAr.Trim();
        if ((roleEn is null) != (roleAr is null)) errors.Add(AuthorErrors.RoleIncomplete);
        else if (roleEn?.Length > 100 || roleAr?.Length > 100) errors.Add(AuthorErrors.RoleTooLong);
        var bioEn = string.IsNullOrWhiteSpace(v.BioEn) ? null : v.BioEn.Trim();
        var bioAr = string.IsNullOrWhiteSpace(v.BioAr) ? null : v.BioAr.Trim();
        if ((bioEn is null) != (bioAr is null)) errors.Add(AuthorErrors.BioIncomplete);
        else if (bioEn?.Length > 1000 || bioAr?.Length > 1000) errors.Add(AuthorErrors.BioTooLong);
        var imageError = MediaRules.Validate(v.AvatarMediaId, v.AvatarUrl, v.AvatarAlt, "avatar");
        if (imageError is not null) errors.Add(imageError.Value);

        if (errors.Count > 0)
        {
            return errors;
        }

        Slug = slug;
        Name = name;
        RoleEn = roleEn;
        RoleAr = roleAr;
        BioEn = bioEn;
        BioAr = bioAr;
        AvatarMediaId = v.AvatarMediaId;
        AvatarUrl = v.AvatarUrl;
        AvatarAlt = v.AvatarAlt;
        return Result.Updated;
    }
}
