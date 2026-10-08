using JoFit.Domain.Common.Results;

namespace JoFit.Domain.Authors;

public static class AuthorErrors
{
    public static readonly Error SlugRequired = Error.Validation("Author.SlugRequired", "A slug is required.").ForField("slug");
    public static readonly Error SlugInvalid = Error.Validation("Author.SlugInvalid", "Use lowercase letters, numbers and single hyphens.").ForField("slug");
    public static readonly Error SlugTaken = Error.Validation("Author.SlugTaken", "Another author already uses this slug.").ForField("slug");
    public static readonly Error NameRequired = Error.Validation("Author.NameRequired", "The name is required.").ForField("name");
    public static readonly Error NameTooLong = Error.Validation("Author.NameTooLong", "The name cannot exceed 100 characters.").ForField("name");
    public static readonly Error RoleIncomplete = Error.Validation("Author.RoleIncomplete", "Give the role in both languages or in neither.").ForField("role");
    public static readonly Error RoleTooLong = Error.Validation("Author.RoleTooLong", "The role cannot exceed 100 characters.").ForField("role");
    public static readonly Error BioIncomplete = Error.Validation("Author.BioIncomplete", "Give the bio in both languages or in neither.").ForField("bio");
    public static readonly Error BioTooLong = Error.Validation("Author.BioTooLong", "The bio cannot exceed 1000 characters.").ForField("bio");
    public static readonly Error NotFound = Error.NotFound("Author.NotFound", "That author does not exist.");
}
