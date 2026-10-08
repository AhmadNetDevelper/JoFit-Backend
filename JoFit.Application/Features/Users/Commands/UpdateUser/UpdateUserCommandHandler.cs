using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.Users.Dtos;
using JoFit.Application.Features.Users.Mappers;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Identity.Roles;
using JoFit.Domain.Identity.Users;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace JoFit.Application.Features.Users.Commands.UpdateUser;

public class UpdateUserCommandHandler(
    UserManager<User> userManager, RoleManager<Role> roleManager, IUnitOfWork unitOfWork, IFileUploadService fileUploadService)
    : IRequestHandler<UpdateUserCommand, Result<UserDto>>
{
    private const int MaxProfileImageSizeInMb = 5;
    private const int MaxProfileImageUrlLength = 300;
    private static readonly string[] AllowedImageExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    public async Task<Result<UserDto>> Handle(UpdateUserCommand request, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null)
        {
            return UserErrors.UserNotFound;
        }

        if (!string.Equals(user.UserName, request.UserName, StringComparison.OrdinalIgnoreCase)
            && await userManager.FindByNameAsync(request.UserName) is not null)
        {
            return UserErrors.UserExists;
        }

        if (!string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase)
            && await userManager.FindByEmailAsync(request.Email) is not null)
        {
            return UserErrors.UserExists;
        }

        var referenceError = await UserReferenceChecker.CheckAsync(
            unitOfWork, request.CountryId, request.CityId, request.JobStatusId, ct);
        if (referenceError is not null)
        {
            return referenceError.Value;
        }

        string? oldImageUrl = null;
        string? newImageUrl = null;
        if (request.ProfileImage is not null)
        {
            var imageError = fileUploadService.ValidateFile(request.ProfileImage, AllowedImageExtensions, MaxProfileImageSizeInMb);
            if (!string.IsNullOrEmpty(imageError))
            {
                return Error.Validation("User.InvalidProfileImage", imageError);
            }

            newImageUrl = await fileUploadService.UploadFileAsync(request.ProfileImage, $"user/{user.Id}", ct);
            if (string.IsNullOrEmpty(newImageUrl) || newImageUrl.Length > MaxProfileImageUrlLength)
            {
                if (!string.IsNullOrEmpty(newImageUrl))
                {
                    await fileUploadService.DeleteFileAsync(newImageUrl, ct);
                }

                return Error.Validation(
                    "User.InvalidProfileImage",
                    $"The profile image could not be saved. Use a shorter file name (the image link cannot exceed {MaxProfileImageUrlLength} characters).");
            }

            oldImageUrl = user.ProfileImageUrl;
            user.ProfileImageUrl = newImageUrl;
        }

        user.UserName = request.UserName;
        user.Email = request.Email;
        user.PhoneNumber = request.PhoneNumber;
        user.FullName = request.FullName;
        user.BirthOfDate = request.BirthOfDate;
        user.GenderId = request.GenderId;
        user.CountryId = request.CountryId;
        user.CityId = request.CityId;
        user.JobStatusId = request.JobStatusId;
        user.IsActive = request.IsActive;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            if (newImageUrl is not null)
            {
                await fileUploadService.DeleteFileAsync(newImageUrl, ct);
            }

            return result.Errors
                .Select(e => Error.Validation($"Identity_{e.Code}", e.Description))
                .ToList();
        }

        if (!string.IsNullOrEmpty(oldImageUrl))
        {
            await fileUploadService.DeleteFileAsync(oldImageUrl, ct);
        }

        var roleName = (await userManager.GetRolesAsync(user)).FirstOrDefault();
        var role = roleName is null ? null : await roleManager.FindByNameAsync(roleName);

        return user.ToDto(role?.Id, roleName);
    }
}
