using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.Coaches;
using JoFit.Application.Features.Users.Dtos;
using JoFit.Domain.AcademicDegrees;
using JoFit.Domain.Attachments;
using JoFit.Domain.Coaches;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Courses;
using JoFit.Domain.JobTypes;
using JoFit.Domain.UniversityMajors;
using JoFit.Domain.UserCertificates;
using JoFit.Domain.UserEducations;
using JoFit.Domain.UserExperiences;
using JoFit.Domain.UserSkills;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ContractEntitySectionTypes = JoFit.Contracts.Dtos.Attachments.EnumEntitySectionTypes;
using ContractEntityTypes = JoFit.Contracts.Dtos.Attachments.EnumEntityTypes;

namespace JoFit.Application.Features.Users.Commands.CreateCoach;

/// <summary>
/// Creates the coach user together with the education, certificates, experiences and skills,
/// all in one transaction. Certificate files are uploaded and their details stored in Attachments.
/// </summary>
public class CreateCoachCommandHandler(
    ISender sender,
    IUnitOfWork unitOfWork,
    IAttachmentService attachmentService,
    IFileUploadService fileUploadService)
    : IRequestHandler<CreateCoachCommand, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(CreateCoachCommand request, CancellationToken ct)
    {
        var referenceError = await CoachUserProfile.CheckReferencesAsync(unitOfWork, request.Data, ct);
        if (referenceError is not null)
        {
            return referenceError.Value;
        }

        // The coach page is checked before anything is written, so a bad form leaves no user behind.
        var profileErrors = await CoachSaving.CheckReferencesAsync(unitOfWork, request.Profile, null, ct);
        var created = Coach.Create(request.Profile.Details);
        if (created.IsError)
        {
            profileErrors.AddRange(created.Errors);
        }

        if (profileErrors.Count > 0)
        {
            return profileErrors;
        }

        var coach = created.Value;
        for (var i = 0; i < request.Profile.Details.Packages.Count; i++)
        {
            coach.Packages.Add(CoachPackage.Create(request.Profile.Details.Packages[i] with { Id = null }, i));
        }

        var uploadedFiles = new List<string>();

        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var userResult = await sender.Send(request.User, ct);
            if (userResult.IsError)
            {
                await unitOfWork.RollbackAsync(ct);
                return userResult.Errors;
            }

            var user = userResult.Value;
            if (!string.IsNullOrEmpty(user.ProfileImageUrl))
            {
                uploadedFiles.Add(user.ProfileImageUrl);
            }

            var errors = await CoachUserProfile.AddAsync(unitOfWork, attachmentService, user.UserId, request.Data, uploadedFiles, ct);
            if (errors is not null)
            {
                await AbortAsync(uploadedFiles, ct);
                return errors;
            }

            coach.LinkLogin(user.UserId);
            await unitOfWork.GetRepository<Coach>().AddAsync(coach, ct);
            await unitOfWork.SaveChangesAsync(ct);
            await CoachSaving.ReplaceLinksAsync(unitOfWork, coach.Id, request.Profile, ct);
            await unitOfWork.SaveChangesAsync(ct);

            await unitOfWork.CommitTransactionAsync(ct);
            return user;
        }
        catch
        {
            await AbortAsync(uploadedFiles, ct);
            throw;
        }
    }

    private async Task AbortAsync(List<string> uploadedFiles, CancellationToken ct)
    {
        await unitOfWork.RollbackAsync(ct);

        foreach (var file in uploadedFiles)
        {
            await fileUploadService.DeleteFileAsync(file, ct);
        }
    }
}
