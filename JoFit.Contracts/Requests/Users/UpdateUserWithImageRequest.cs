using Microsoft.AspNetCore.Http;

namespace JoFit.Contracts.Requests.Users;

/// <summary>Multipart request used by the coach update endpoint.</summary>
public sealed record UpdateUserWithImageRequest : UpdateUserRequest
{
    /// <summary>Optional new profile image (.jpg, .jpeg, .png, .webp; max 5 MB). Leave it out to keep the current one.</summary>
    public IFormFile? ProfileImage { get; init; }
}
