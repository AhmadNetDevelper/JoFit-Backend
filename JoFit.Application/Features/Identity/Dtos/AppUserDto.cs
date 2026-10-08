namespace JoFit.Application.Features.Identity.Dtos;

public sealed record AppUserDto(
    long UserId,
    string Email,
    IList<string> Roles,
    IList<string> Permissions = default!);
