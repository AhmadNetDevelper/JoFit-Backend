using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.Coaches;
using JoFit.Domain.Attachments;
using JoFit.Domain.Coaches;
using JoFit.Domain.Common.Results;
using JoFit.Domain.UserCertificates;
using JoFit.Domain.UserEducations;
using JoFit.Domain.UserExperiences;
using JoFit.Domain.UserSkills;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.Users.Commands.CreateCoach;

/// <summary>
/// Updates the coach page and the coach's user together, in one transaction. The education, certificate, experience
/// and skill lists replace what is stored; certificate files are uploaded again and the old ones removed after the save.
/// </summary>
public class UpdateCoachCommandHandler(
    ISender sender,
    IUnitOfWork unitOfWork,
    IUrlService urlService,
    IAttachmentService attachmentService,
    IFileUploadService fileUploadService)
    : IRequestHandler<UpdateCoachCommand, Result<AdminCoachDto>>
{
    public async Task<Result<AdminCoachDto>> Handle(UpdateCoachCommand request, CancellationToken ct)
    {
        var coach = await unitOfWork.GetRepository<Coach>().GetTableAsTracking().Include(c => c.Packages).Include(c => c.Certifications)
            .FirstOrDefaultAsync(c => c.Id == request.CoachId, ct);
        if (coach is null)
        {
            return CoachErrors.NotFound;
        }

        if (coach.UserId is not long userId)
        {
            return CoachErrors.Field("coach", "This coach has no login account, so there is no user data to update.");
        }

        var referenceError = await CoachUserProfile.CheckReferencesAsync(unitOfWork, request.Data, ct);
        if (referenceError is not null)
        {
            return referenceError.Value;
        }

        // The coach page is checked before anything is written.
        var errors = await CoachSaving.CheckReferencesAsync(unitOfWork, request.Profile, coach.Id, ct);
        var applied = coach.Apply(request.Profile.Details);
        if (applied.IsError) errors.AddRange(applied.Errors);
        var packages = CoachSaving.SyncPackages(coach, request.Profile.Details.Packages, unitOfWork);
        if (packages.IsError) errors.AddRange(packages.Errors);
        if (errors.Count > 0)
        {
            return errors;
        }

        var uploadedFiles = new List<string>();
        var oldFiles = new List<string>();

        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            await CoachSaving.ReplaceLinksAsync(unitOfWork, coach.Id, request.Profile, ct);
            await RemoveUserDataAsync(userId, oldFiles, ct);

            var profileErrors = await CoachUserProfile.AddAsync(unitOfWork, attachmentService, userId, request.Data, uploadedFiles, ct);
            if (profileErrors is not null)
            {
                await AbortAsync(uploadedFiles, ct);
                return profileErrors;
            }

            await unitOfWork.SaveChangesAsync(ct);

            // Last, because it can replace the profile image file, which cannot be rolled back.
            var user = await sender.Send(request.User with { UserId = userId }, ct);
            if (user.IsError)
            {
                await AbortAsync(uploadedFiles, ct);
                return user.Errors;
            }

            await unitOfWork.CommitTransactionAsync(ct);
        }
        catch
        {
            await AbortAsync(uploadedFiles, ct);
            throw;
        }

        foreach (var file in oldFiles)
        {
            await fileUploadService.DeleteFileAsync(file, ct);
        }

        return await CoachSaving.ToDtoAsync(unitOfWork, coach.Id, ct);
    }

    /// <summary>Soft-deletes the user's education, experience, skills and certificates (with their files' records).</summary>
    private async Task RemoveUserDataAsync(long userId, List<string> oldFiles, CancellationToken ct)
    {
        var educations = await unitOfWork.GetRepository<UserEducation>().GetTableAsTracking().Where(x => x.UserId == userId).ToListAsync(ct);
        var experiences = await unitOfWork.GetRepository<UserExperience>().GetTableAsTracking().Where(x => x.UserId == userId).ToListAsync(ct);
        var skills = await unitOfWork.GetRepository<UserSkill>().GetTableAsTracking().Where(x => x.UserId == userId).ToListAsync(ct);
        var certificates = await unitOfWork.GetRepository<UserCertificate>().GetTableAsTracking().Where(x => x.UserId == userId).ToListAsync(ct);

        var certificateIds = certificates.Select(c => c.Id).ToList();
        var attachments = await unitOfWork.GetRepository<Attachment>().GetTableAsTracking()
            .Where(a => a.EntityTypeId == EnumEntityTypes.UserCertificate && certificateIds.Contains(a.EntityId))
            .ToListAsync(ct);
        oldFiles.AddRange(attachments.Select(a => a.FilePath));

        unitOfWork.GetRepository<UserEducation>().DeleteRangeAsync(educations);
        unitOfWork.GetRepository<UserExperience>().DeleteRangeAsync(experiences);
        unitOfWork.GetRepository<UserSkill>().DeleteRangeAsync(skills);
        unitOfWork.GetRepository<UserCertificate>().DeleteRangeAsync(certificates);
        unitOfWork.GetRepository<Attachment>().DeleteRangeAsync(attachments);
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
