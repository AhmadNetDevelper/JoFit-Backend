using JoFit.Domain.Common.Results;

namespace JoFit.Domain.ArticleCategories;

public static class ArticleCategoryErrors
{
    public static readonly Error SlugRequired = Error.Validation("ArticleCategory.SlugRequired", "A slug is required.").ForField("slug");
    public static readonly Error SlugInvalid = Error.Validation("ArticleCategory.SlugInvalid", "Use lowercase letters, numbers and single hyphens.").ForField("slug");
    public static readonly Error SlugTaken = Error.Validation("ArticleCategory.SlugTaken", "Another article category already uses this slug.").ForField("slug");
    public static readonly Error NameEnRequired = Error.Validation("ArticleCategory.NameEnRequired", "The English name is required.").ForField("name.en");
    public static readonly Error NameArRequired = Error.Validation("ArticleCategory.NameArRequired", "The Arabic name is required.").ForField("name.ar");
    public static readonly Error NameTooLong = Error.Validation("ArticleCategory.NameTooLong", "A name cannot exceed 100 characters.").ForField("name.en");
    public static readonly Error NameTaken = Error.Validation("ArticleCategory.NameTaken", "Another article category already uses this name.").ForField("name.en");
    public static readonly Error DescriptionIncomplete = Error.Validation("ArticleCategory.DescriptionIncomplete", "Give the description in both languages or in neither.").ForField("description");
    public static readonly Error DescriptionTooLong = Error.Validation("ArticleCategory.DescriptionTooLong", "The description cannot exceed 1000 characters.").ForField("description");
    public static readonly Error NotFound = Error.NotFound("ArticleCategory.NotFound", "That article category does not exist.");
}
