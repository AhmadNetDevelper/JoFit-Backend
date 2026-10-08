using JoFit.Domain.Common.Results;

namespace JoFit.Domain.Orders;

public static class OrderErrors
{
    public static readonly Error NotFound = Error.NotFound("Order.NotFound", "That order does not exist.");
    public static readonly Error EmptyBasket = Error.Conflict("Order.EmptyBasket", "Your basket is empty.");
    public static readonly Error NotCancellable = Error.Conflict("Order.NotCancellable", "Only a pending order can be cancelled. Contact us for anything else.");

    public static Error Field(string field, string message)
        => Error.Validation($"Order.{field}", message).ForField(field);

    public static Error IllegalMove(string from, string to)
        => Error.Conflict("Order.IllegalMove", $"A {from} order cannot become {to}.");

    public static Error OutOfStock(string title) => Error.Conflict("Order.OutOfStock", $"\"{title}\" is out of stock.");
    public static Error NotEnoughStock(string title) => Error.Conflict("Order.NotEnoughStock", $"There is not enough stock of \"{title}\".");
    public static Error RentalUnavailable(string title) => Error.Conflict("Order.RentalUnavailable", $"\"{title}\" is not available for those dates.");
    public static readonly Error LineInvalid = Error.Validation("Order.LineInvalid", "One of the items in your basket is no longer available.").ForField("lines");
}
