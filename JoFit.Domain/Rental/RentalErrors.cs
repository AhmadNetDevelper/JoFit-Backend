using JoFit.Domain.Common.Results;

namespace JoFit.Domain.Rental;

public static class RentalErrors
{
    public static readonly Error ItemNotFound = Error.NotFound("Rental.ItemNotFound", "That rental item does not exist.");
    public static readonly Error BookingNotFound = Error.NotFound("Rental.BookingNotFound", "That rental booking does not exist.");

    public static Error Field(string field, string message)
        => Error.Validation($"Rental.{field}", message).ForField(field);

    public static readonly Error SlugTaken = Field("slug", "Another rental item already uses this slug.");
    public static readonly Error DepotMissing = Field("depotId", "That depot does not exist.");
    public static readonly Error CategoriesMissing = Field("categoryIds", "Choose at least one category that exists.");

    public static Error IllegalMove(string from, string to)
        => Error.Conflict("Rental.IllegalMove", $"A {from} hire cannot become {to}.");

    public static readonly Error DepositNotReleasable = Error.Conflict("Rental.DepositNotReleasable", "The deposit can be released only after the items are returned.");
    public static readonly Error DepositAlreadyReleased = Error.Conflict("Rental.DepositAlreadyReleased", "This deposit was already released.");

    public static Error ItemInUse(int bookings) => Error.Conflict(
        "Rental.ItemInUse",
        $"{bookings} hire(s) are still pending, confirmed or out. Archive the item instead.");
}
