using Microsoft.AspNetCore.Http;

namespace JoFit.Contracts.Requests.Users;

/// <summary>
/// Multipart request used by the normal user and coach creation endpoints.
/// </summary>
public sealed record CreateUserWithImageRequest : CreateUserRequest
{
    /// <summary>
    /// Optional profile image (.jpg, .jpeg, .png, .webp; max 5 MB).
    /// </summary>
    public IFormFile? ProfileImage { get; init; }
}
