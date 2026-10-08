using JoFit.Domain.Common.Results;

namespace JoFit.Domain.JobCategories;

public static class JobCategoryErrors
{
    public static readonly Error SlugRequired = Error.Validation("JobCategory.SlugRequired", "A slug is required.").ForField("slug");
    public static readonly Error SlugInvalid = Error.Validation("JobCategory.SlugInvalid", "Use lowercase letters, numbers and single hyphens.").ForField("slug");
    public static readonly Error SlugTaken = Error.Validation("JobCategory.SlugTaken", "Another job category already uses this slug.").ForField("slug");
    public static readonly Error NameEnRequired = Error.Validation("JobCategory.NameEnRequired", "The English name is required.").ForField("name.en");
    public static readonly Error NameArRequired = Error.Validation("JobCategory.NameArRequired", "The Arabic name is required.").ForField("name.ar");
    public static readonly Error NameTooLong = Error.Validation("JobCategory.NameTooLong", "A name cannot exceed 100 characters.").ForField("name.en");
    public static readonly Error NameTaken = Error.Validation("JobCategory.NameTaken", "Another job category already uses this name.").ForField("name.en");
    public static readonly Error NotFound = Error.NotFound("JobCategory.NotFound", "That job category does not exist.");
}
