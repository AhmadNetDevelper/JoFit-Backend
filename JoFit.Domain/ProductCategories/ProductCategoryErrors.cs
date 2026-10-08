using JoFit.Domain.Common.Results;

namespace JoFit.Domain.ProductCategories;

public static class ProductCategoryErrors
{
    public static readonly Error SlugRequired = Error.Validation("ProductCategory.SlugRequired", "A slug is required.").ForField("slug");
    public static readonly Error SlugInvalid = Error.Validation("ProductCategory.SlugInvalid", "Use lowercase letters, numbers and single hyphens.").ForField("slug");
    public static readonly Error SlugTaken = Error.Validation("ProductCategory.SlugTaken", "Another product category already uses this slug.").ForField("slug");
    public static readonly Error NameEnRequired = Error.Validation("ProductCategory.NameEnRequired", "The English name is required.").ForField("name.en");
    public static readonly Error NameArRequired = Error.Validation("ProductCategory.NameArRequired", "The Arabic name is required.").ForField("name.ar");
    public static readonly Error NameTooLong = Error.Validation("ProductCategory.NameTooLong", "A name cannot exceed 100 characters.").ForField("name.en");
    public static readonly Error NameTaken = Error.Validation("ProductCategory.NameTaken", "Another product category already uses this name.").ForField("name.en");
    public static readonly Error DescriptionIncomplete = Error.Validation("ProductCategory.DescriptionIncomplete", "Give the description in both languages or in neither.").ForField("description");
    public static readonly Error DescriptionTooLong = Error.Validation("ProductCategory.DescriptionTooLong", "The description cannot exceed 1000 characters.").ForField("description");
    public static readonly Error NotFound = Error.NotFound("ProductCategory.NotFound", "That product category does not exist.");
    public static readonly Error ParentInvalid = Error.Validation("ProductCategory.ParentInvalid", "A product category cannot sit under itself or one of its own children.").ForField("parentId");
    public static readonly Error ParentMissing = Error.Validation("ProductCategory.ParentMissing", "The parent product category does not exist.").ForField("parentId");
    public static Error HasChildren(int count) => Error.Conflict("ProductCategory.HasChildren", $"This product category has {count} child categor(y/ies). Move or delete them first.");
}
