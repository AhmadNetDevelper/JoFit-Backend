using JoFit.Domain.Common.Results;

namespace JoFit.Domain.Sports;

public static class SportErrors
{
    public static readonly Error SlugRequired = Error.Validation("Sport.SlugRequired", "A slug is required.").ForField("slug");
    public static readonly Error SlugInvalid = Error.Validation("Sport.SlugInvalid", "Use lowercase letters, numbers and single hyphens.").ForField("slug");
    public static readonly Error SlugTaken = Error.Validation("Sport.SlugTaken", "Another sport already uses this slug.").ForField("slug");
    public static readonly Error NameEnRequired = Error.Validation("Sport.NameEnRequired", "The English name is required.").ForField("name.en");
    public static readonly Error NameArRequired = Error.Validation("Sport.NameArRequired", "The Arabic name is required.").ForField("name.ar");
    public static readonly Error NameTooLong = Error.Validation("Sport.NameTooLong", "A name cannot exceed 100 characters.").ForField("name.en");
    public static readonly Error NameTaken = Error.Validation("Sport.NameTaken", "Another sport already uses this name.").ForField("name.en");
    public static readonly Error DescriptionIncomplete = Error.Validation("Sport.DescriptionIncomplete", "Give the description in both languages or in neither.").ForField("description");
    public static readonly Error DescriptionTooLong = Error.Validation("Sport.DescriptionTooLong", "The description cannot exceed 1000 characters.").ForField("description");
    public static readonly Error NotFound = Error.NotFound("Sport.NotFound", "That sport does not exist.");
}
