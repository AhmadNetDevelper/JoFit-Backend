using JoFit.Domain.Common.Results;

namespace JoFit.Domain.Brands;

public static class BrandErrors
{
    public static readonly Error SlugRequired = Error.Validation("Brand.SlugRequired", "A slug is required.").ForField("slug");
    public static readonly Error SlugInvalid = Error.Validation("Brand.SlugInvalid", "Use lowercase letters, numbers and single hyphens.").ForField("slug");
    public static readonly Error SlugTaken = Error.Validation("Brand.SlugTaken", "Another brand already uses this slug.").ForField("slug");
    public static readonly Error NameEnRequired = Error.Validation("Brand.NameEnRequired", "The English name is required.").ForField("name.en");
    public static readonly Error NameArRequired = Error.Validation("Brand.NameArRequired", "The Arabic name is required.").ForField("name.ar");
    public static readonly Error NameTooLong = Error.Validation("Brand.NameTooLong", "A name cannot exceed 100 characters.").ForField("name.en");
    public static readonly Error LogoAltTooLong = Error.Validation("Brand.LogoAltTooLong", "The logo description cannot exceed 300 characters.").ForField("logo");
    public static readonly Error NotFound = Error.NotFound("Brand.NotFound", "That brand does not exist.");
    public static Error InUse(int products) => Error.Conflict("Brand.InUse", $"This brand is used by {products} product(s). Move them to another brand first.");
}
