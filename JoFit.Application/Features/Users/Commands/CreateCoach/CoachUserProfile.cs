using JoFit.Application.Common.Interfaces;
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
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ContractEntitySectionTypes = JoFit.Contracts.Dtos.Attachments.EnumEntitySectionTypes;
using ContractEntityTypes = JoFit.Contracts.Dtos.Attachments.EnumEntityTypes;

namespace JoFit.Application.Features.Users.Commands.CreateCoach;

/// <summary>The education, certificates, experiences and skills of a coach, as the create and update forms send them.</summary>
public sealed record CoachUserData(
    List<CoachEducationInput> Educations,
    List<CoachCertificateInput> Certificates,
    List<CoachExperienceInput> Experiences,
    List<CoachSkillInput> Skills);

/// <summary>Checks and writes a coach's user-side profile data; shared by the coach create and update handlers.</summary>
internal static class CoachUserProfile
{
    public static async Task<List<Error>?> AddAsync(
        IUnitOfWork unitOfWork,
        IAttachmentService attachmentService,
        long userId,
        CoachUserData request,
        List<string> uploadedFiles,
        CancellationToken ct)
    {
        var educations = new List<UserEducation>();
        foreach (var e in request.Educations)
        {
            var result = UserEducation.Create(
                userId, e.SchoolName, e.AcademicDegreeId, e.UniversityMajorId, e.StartDate, e.EndDate, e.IsStillEducation);
            if (result.IsError)
            {
                return result.Errors;
            }

            educations.Add(result.Value);
        }

        var experiences = new List<UserExperience>();
        foreach (var e in request.Experiences)
        {
            var result = UserExperience.Create(
                userId, e.Address, e.JobTypeId, e.CompanyName, e.CompanyAddress, e.StartDate, e.EndDate, e.IsStillWorking);
            if (result.IsError)
            {
                return result.Errors;
            }

            experiences.Add(result.Value);
        }

        var skills = new List<UserSkill>();
        foreach (var s in request.Skills)
        {
            var result = UserSkill.Create(userId, s.CourseId, s.PercentageScore);
            if (result.IsError)
            {
                return result.Errors;
            }

            skills.Add(result.Value);
        }

        var certificates = new List<(UserCertificate Certificate, IFormFile? File)>();
        foreach (var c in request.Certificates)
        {
            var result = UserCertificate.Create(
                userId, c.CertificateName, c.OrganizationName, c.IssuedDate, c.ExpiryDate, c.CredentialId, c.CredentialUrl);
            if (result.IsError)
            {
                return result.Errors;
            }

            certificates.Add((result.Value, c.CertificateFile));
        }

        await unitOfWork.GetRepository<UserEducation>().AddRangeAsync(educations, ct);
        await unitOfWork.GetRepository<UserExperience>().AddRangeAsync(experiences, ct);
        await unitOfWork.GetRepository<UserSkill>().AddRangeAsync(skills, ct);
        await unitOfWork.GetRepository<UserCertificate>().AddRangeAsync([.. certificates.Select(x => x.Certificate)], ct);

        // Certificates need their Id before the files can be attached to them.
        await unitOfWork.SaveChangesAsync(ct);

        var attachments = new List<Attachment>();
        foreach (var (certificate, file) in certificates)
        {
            if (file is null)
            {
                continue;
            }

            var attachmentResult = await attachmentService.CreateAttachmentsBySectionAsync(
                certificate.Id,
                ContractEntityTypes.UserCertificate,
                new Dictionary<ContractEntitySectionTypes, List<IFormFile>>
                {
                    [ContractEntitySectionTypes.Certificate] = [file]
                });

            if (attachmentResult.IsError)
            {
                return attachmentResult.Errors;
            }

            var attachment = attachmentResult.Value[0];
            uploadedFiles.Add(attachment.FilePath);

            var urlResult = certificate.SetCertificateUrl(attachment.FilePath);
            if (urlResult.IsError)
            {
                return urlResult.Errors;
            }

            attachments.Add(attachment);
        }

        if (attachments.Count > 0)
        {
            await unitOfWork.GetRepository<Attachment>().AddRangeAsync(attachments, ct);
            await unitOfWork.SaveChangesAsync(ct);
        }

        return null;
    }

    /// <summary>
    /// Mirrors the submitted certificates onto the coach page, where an admin can verify them. A certificate that is
    /// already there (same name and issuer) is kept, so a verification survives an update; the rest are added or removed.
    /// </summary>
    public static void SyncCoachCertifications(Coach coach, IEnumerable<CoachCertificateInput> inputs, IUnitOfWork unitOfWork)
    {
        var wanted = inputs
            .GroupBy(c => (Name: c.CertificateName.Trim(), Issuer: c.OrganizationName.Trim()))
            .Select(g => (g.Key.Name, g.Key.Issuer, g.First().IssuedDate.Year))
            .ToList();

        var stale = coach.Certifications
            .Where(x => !wanted.Any(w => w.Name == x.NameEn && w.Issuer == x.IssuerEn))
            .ToList();
        unitOfWork.GetDbContext().RemoveRange(stale);
        foreach (var item in stale)
        {
            coach.Certifications.Remove(item);
        }

        foreach (var (name, issuer, year) in wanted.Where(w => !coach.Certifications.Any(x => x.NameEn == w.Name && x.IssuerEn == w.Issuer)))
        {
            coach.Certifications.Add(CoachCertification.Create(name, name, issuer, issuer, year));
        }
    }

    public static async Task<Error?> CheckReferencesAsync(IUnitOfWork unitOfWork, CoachUserData request, CancellationToken ct)
    {
        var error = await MissingAsync<AcademicDegree>(unitOfWork, 
            request.Educations.Select(e => e.AcademicDegreeId), "UserEducation.AcademicDegreeNotFound", "Academic degree", ct)
            ?? await MissingAsync<UniversityMajor>(unitOfWork, 
                request.Educations.Select(e => e.UniversityMajorId), "UserEducation.UniversityMajorNotFound", "University major", ct)
            ?? await MissingAsync<JobType>(unitOfWork, 
                request.Experiences.Select(e => e.JobTypeId), "UserExperience.JobTypeNotFound", "Job type", ct)
            ?? await MissingAsync<Course>(unitOfWork, 
                request.Skills.Select(s => s.CourseId), "UserSkill.CourseNotFound", "Course", ct);

        return error;
    }

    private static async Task<Error?> MissingAsync<T>(IUnitOfWork unitOfWork, IEnumerable<long> ids, string code, string label, CancellationToken ct)
        where T : JoFit.Domain.Common.Entity
    {
        var distinct = ids.Distinct().ToList();
        if (distinct.Count == 0)
        {
            return null;
        }

        var found = await unitOfWork.GetRepository<T>().GetTableNoTracking()
            .Where(x => distinct.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(ct);

        var missing = distinct.Except(found).ToList();
        return missing.Count == 0
            ? null
            : Error.NotFound(code, $"{label} not found: {string.Join(", ", missing)}");
    }

}
