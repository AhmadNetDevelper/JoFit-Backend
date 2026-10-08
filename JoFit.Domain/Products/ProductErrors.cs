using JoFit.Domain.Common.Results;

namespace JoFit.Domain.Products;

public static class ProductErrors
{
    public static readonly Error NotFound = Error.NotFound("Product.NotFound", "That product does not exist.");
    public static readonly Error VariantNotFound = Error.NotFound("Product.VariantNotFound", "That variant does not exist.");

    public static Error Field(string field, string message)
        => Error.Validation($"Product.{field}", message).ForField(field);

    public static readonly Error SkuTaken = Field("variant.sku", "Another product already uses this SKU.");

    public static Error InUse(int orders) => Error.Conflict(
        "Product.InUse",
        $"This product is on {orders} order(s). Archive it instead of deleting it.");
}
