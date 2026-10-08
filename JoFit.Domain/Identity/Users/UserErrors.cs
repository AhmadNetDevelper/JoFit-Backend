using JoFit.Domain.Common.Results;

namespace JoFit.Domain.Identity.Users;

public static class UserErrors
{
    public static Error UserNameRequired =>
        Error.Validation("User.UserNameRequired", "The username is required.");

    public static Error EmailRequired =>
        Error.Validation("User.EmailRequired", "The email address is required.");

    public static Error PasswordRequired =>
        Error.Validation("User.PasswordRequired", "The password is required.");

    public static Error PhoneNumberRequired =>
        Error.Validation("User.PhoneNumberRequired", "The phone number is required.");

    public static Error UserExists =>
        Error.Validation("User.UserExists", "A user with the same username or email already exists.");

    public static Error InvalidEmailFormat =>
        Error.Validation("User.InvalidEmailFormat", "The email format is invalid.");

    public static Error WeakPassword =>
        Error.Validation("User.WeakPassword", "The password is too weak. It must contain at least 6 characters.");

    public static Error UserNotFound =>
        Error.NotFound("User.UserNotFound", "The specified user was not found.");

    public static Error OutsideAllowedTimeWindow =>
        Error.Conflict("User.OutsideAllowedTimeWindow", "Login is not allowed outside the permitted time window.");
}
