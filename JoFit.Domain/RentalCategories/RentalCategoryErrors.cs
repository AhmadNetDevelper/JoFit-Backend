using JoFit.Domain.Common.Results;

namespace JoFit.Domain.RentalCategories;

public static class RentalCategoryErrors
{
    public static readonly Error SlugRequired = Error.Validation("RentalCategory.SlugRequired", "A slug is required.").ForField("slug");
    public static readonly Error SlugInvalid = Error.Validation("RentalCategory.SlugInvalid", "Use lowercase letters, numbers and single hyphens.").ForField("slug");
    public static readonly Error SlugTaken = Error.Validation("RentalCategory.SlugTaken", "Another rental category already uses this slug.").ForField("slug");
    public static readonly Error NameEnRequired = Error.Validation("RentalCategory.NameEnRequired", "The English name is required.").ForField("name.en");
    public static readonly Error NameArRequired = Error.Validation("RentalCategory.NameArRequired", "The Arabic name is required.").ForField("name.ar");
    public static readonly Error NameTooLong = Error.Validation("RentalCategory.NameTooLong", "A name cannot exceed 100 characters.").ForField("name.en");
    public static readonly Error NameTaken = Error.Validation("RentalCategory.NameTaken", "Another rental category already uses this name.").ForField("name.en");
    public static readonly Error DescriptionIncomplete = Error.Validation("RentalCategory.DescriptionIncomplete", "Give the description in both languages or in neither.").ForField("description");
    public static readonly Error DescriptionTooLong = Error.Validation("RentalCategory.DescriptionTooLong", "The description cannot exceed 1000 characters.").ForField("description");
    public static readonly Error NotFound = Error.NotFound("RentalCategory.NotFound", "That rental category does not exist.");
    public static readonly Error ParentInvalid = Error.Validation("RentalCategory.ParentInvalid", "A rental category cannot sit under itself or one of its own children.").ForField("parentId");
    public static readonly Error ParentMissing = Error.Validation("RentalCategory.ParentMissing", "The parent rental category does not exist.").ForField("parentId");
    public static Error HasChildren(int count) => Error.Conflict("RentalCategory.HasChildren", $"This rental category has {count} child categor(y/ies). Move or delete them first.");
}
