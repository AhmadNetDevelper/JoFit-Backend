using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.Users.Dtos;
using JoFit.Application.Features.Users.Mappers;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Identity.Roles;
using JoFit.Domain.Identity.Users;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace JoFit.Application.Features.Users.Commands.CreateUser;

public class CreateUserCommandHandler(
    UserManager<User> userManager,
    RoleManager<Role> roleManager,
    IFileUploadService fileUploadService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateUserCommand, Result<UserDto>>
{
    private const int MaxProfileImageSizeInMb = 5;
    private const int MaxProfileImageUrlLength = 300;
    private static readonly string[] AllowedImageExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    public async Task<Result<UserDto>> Handle(CreateUserCommand request, CancellationToken ct)
    {
        var role = await roleManager.FindByNameAsync(request.RoleName);
        if (role is null)
        {
            return RoleErrors.RoleNotFound;
        }

        if (await userManager.FindByNameAsync(request.UserName) is not null
            || await userManager.FindByEmailAsync(request.Email) is not null)
        {
            return UserErrors.UserExists;
        }

        if (request.ProfileImage is not null)
        {
            var imageError = fileUploadService.ValidateFile(request.ProfileImage, AllowedImageExtensions, MaxProfileImageSizeInMb);
            if (!string.IsNullOrEmpty(imageError))
            {
                return Error.Validation("User.InvalidProfileImage", imageError);
            }
        }

        var referenceError = await UserReferenceChecker.CheckAsync(
            unitOfWork, request.CountryId, request.CityId, request.JobStatusId, ct);
        if (referenceError is not null)
        {
            return referenceError.Value;
        }

        var user = new User
        {
            UserName = request.UserName,
            Email = request.Email,
            EmailConfirmed = true,
            PhoneNumber = request.PhoneNumber,
            PhoneNumberConfirmed = true,
            FullName = request.FullName,
            BirthOfDate = request.BirthOfDate,
            GenderId = request.GenderId,
            CountryId = request.CountryId,
            CityId = request.CityId,
            JobStatusId = request.JobStatusId,
            IsActive = true
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return createResult.Errors
                .Select(e => Error.Validation($"Identity_{e.Code}", e.Description))
                .ToList();
        }

        var roleResult = await userManager.AddToRoleAsync(user, role.Name!);
        if (!roleResult.Succeeded)
        {
            // Do not leave a user without a role behind.
            await userManager.DeleteAsync(user);
            return Error.Failure("User.RoleAssignmentFailed", "Failed to assign the role to the user.");
        }

        if (request.ProfileImage is not null)
        {
            // The image link is kept on the user only; no Attachment record is created for it.
            var imageUrl = await fileUploadService.UploadFileAsync(request.ProfileImage, $"user/{user.Id}", ct);

            if (string.IsNullOrEmpty(imageUrl) || imageUrl.Length > MaxProfileImageUrlLength)
            {
                if (!string.IsNullOrEmpty(imageUrl))
                {
                    await fileUploadService.DeleteFileAsync(imageUrl, ct);
                }

                await userManager.DeleteAsync(user);
                return Error.Validation(
                    "User.InvalidProfileImage",
                    $"The profile image could not be saved. Use a shorter file name (the image link cannot exceed {MaxProfileImageUrlLength} characters).");
            }

            user.ProfileImageUrl = imageUrl;

            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                await fileUploadService.DeleteFileAsync(imageUrl, ct);
                await userManager.DeleteAsync(user);
                return Error.Failure("User.ProfileImageSaveFailed", "Failed to save the profile image.");
            }
        }

        return user.ToDto(role.Id, role.Name);
    }
}
