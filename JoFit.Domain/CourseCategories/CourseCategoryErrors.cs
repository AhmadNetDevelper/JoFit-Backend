using JoFit.Domain.Common.Results;

namespace JoFit.Domain.CourseCategories;

public static class CourseCategoryErrors
{
    public static readonly Error SlugRequired = Error.Validation("CourseCategory.SlugRequired", "A slug is required.").ForField("slug");
    public static readonly Error SlugInvalid = Error.Validation("CourseCategory.SlugInvalid", "Use lowercase letters, numbers and single hyphens.").ForField("slug");
    public static readonly Error SlugTaken = Error.Validation("CourseCategory.SlugTaken", "Another course category already uses this slug.").ForField("slug");
    public static readonly Error NameEnRequired = Error.Validation("CourseCategory.NameEnRequired", "The English name is required.").ForField("name.en");
    public static readonly Error NameArRequired = Error.Validation("CourseCategory.NameArRequired", "The Arabic name is required.").ForField("name.ar");
    public static readonly Error NameTooLong = Error.Validation("CourseCategory.NameTooLong", "A name cannot exceed 100 characters.").ForField("name.en");
    public static readonly Error NameTaken = Error.Validation("CourseCategory.NameTaken", "Another course category already uses this name.").ForField("name.en");
    public static readonly Error DescriptionIncomplete = Error.Validation("CourseCategory.DescriptionIncomplete", "Give the description in both languages or in neither.").ForField("description");
    public static readonly Error DescriptionTooLong = Error.Validation("CourseCategory.DescriptionTooLong", "The description cannot exceed 1000 characters.").ForField("description");
    public static readonly Error NotFound = Error.NotFound("CourseCategory.NotFound", "That course category does not exist.");
}
