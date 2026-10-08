namespace JoFit.Contracts.Requests.Common;

public sealed record RegisterRequest(
    string UserName,
    string Email,
    string Password,
    string? PhoneNumber = null);
