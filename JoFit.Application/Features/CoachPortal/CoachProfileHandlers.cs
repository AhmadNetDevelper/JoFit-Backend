using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Features.Coaches;
using JoFit.Application.Features.Users.Commands.CreateCoach;
using JoFit.Application.Features.Users.Commands.UpdateUser;
using JoFit.Domain.Coaches;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Identity.Users;
using JoFit.Domain.UserCertificates;
using JoFit.Domain.UserEducations;
using JoFit.Domain.UserExperiences;
using JoFit.Domain.UserSkills;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.CoachPortal;

// The shape mirrors what POST /auth/register-coach accepts, so the website maps it one to one.
public sealed record MyCoachUserDto(
    string Email, string FullName, string PhoneNumber, DateTime? BirthOfDate, int GenderId,
    long? CountryId, long? CityId, long? JobStatusId, string? ProfileImageUrl);

public sealed record MyCoachPageDto(
    string Slug, LocalizedText Name, LocalizedText Headline, LocalizedText? Bio,
    IReadOnlyList<long> SportIds, long LocationId, IReadOnlyList<string> Languages);

public sealed record MyCoachEducationDto(
    string SchoolName, long AcademicDegreeId, long UniversityMajorId, DateTime StartDate, DateTime? EndDate, bool IsStillEducation);

public sealed record MyCoachCertificateDto(
    long Id, string CertificateName, string OrganizationName, DateTime IssuedDate, DateTime? ExpiryDate,
    string CredentialId, string? CredentialUrl, string? CertificateFileUrl);

public sealed record MyCoachExperienceDto(
    string Address, long JobTypeId, string CompanyName, string CompanyAddress, DateTime StartDate, DateTime? EndDate, bool IsStillWorking);

public sealed record MyCoachSkillDto(long CourseId, short PercentageScore);

public sealed record MyCoachProfileDto(
    MyCoachUserDto User,
    MyCoachPageDto Profile,
    IReadOnlyList<MyCoachEducationDto> Educations,
    IReadOnlyList<MyCoachCertificateDto> Certificates,
    IReadOnlyList<MyCoachExperienceDto> Experiences,
    IReadOnlyList<MyCoachSkillDto> Skills);

/// <summary>What a coach may change about themselves. Username, email, password and slug are not here on purpose.</summary>
public sealed record MyCoachUserInput(
    string PhoneNumber, string FullName, DateTime? BirthOfDate, EnumGender GenderId,
    long? CountryId, long? CityId, long? JobStatusId, IFormFile? ProfileImage);

public sealed record MyCoachPageInput(
    string NameEn, string NameAr, string HeadlineEn, string HeadlineAr, string? BioEn, string? BioAr,
    long LocationId, IReadOnlyList<long> SportIds, IReadOnlyList<string> Languages);

public sealed record GetMyCoachProfileQuery(long UserId) : IRequest<Result<MyCoachProfileDto>>;

public sealed record UpdateMyCoachProfileCommand(
    long UserId,
    MyCoachUserInput User,
    MyCoachPageInput Page,
    List<CoachEducationInput> Educations,
    List<CoachCertificateInput> Certificates,
    List<CoachExperienceInput> Experiences,
    List<CoachSkillInput> Skills) : IRequest<Result<MyCoachProfileDto>>;

internal static class MyCoachProfileReader
{
    public static async Task<Result<MyCoachProfileDto>> ReadAsync(IUnitOfWork unitOfWork, IUrlService urlService, long userId, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var coach = await db.Set<Coach>().AsNoTracking().FirstOrDefaultAsync(c => c.UserId == userId, ct);
        var user = await db.Set<User>().AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (coach is null || user is null)
        {
            return CoachErrors.NotFound;
        }

        var sportIds = await db.Set<CoachSportLink>().AsNoTracking().Where(l => l.CoachId == coach.Id).Select(l => l.SportId).OrderBy(x => x).ToListAsync(ct);
        var educations = await db.Set<UserEducation>().AsNoTracking().Where(x => x.UserId == userId).OrderBy(x => x.Id).ToListAsync(ct);
        var experiences = await db.Set<UserExperience>().AsNoTracking().Where(x => x.UserId == userId).OrderBy(x => x.Id).ToListAsync(ct);
        var certificates = await db.Set<UserCertificate>().AsNoTracking().Where(x => x.UserId == userId).OrderBy(x => x.Id).ToListAsync(ct);
        var skills = await db.Set<UserSkill>().AsNoTracking().Where(x => x.UserId == userId).OrderBy(x => x.Id).ToListAsync(ct);

        return new MyCoachProfileDto(
            new MyCoachUserDto(
                user.Email ?? string.Empty, user.FullName, user.PhoneNumber ?? string.Empty, user.BirthOfDate, (int)user.GenderId,
                user.CountryId, user.CityId, user.JobStatusId, urlService.ToAbsoluteUrl(user.ProfileImageUrl)),
            new MyCoachPageDto(
                coach.Slug,
                new LocalizedText(coach.NameEn, coach.NameAr),
                new LocalizedText(coach.HeadlineEn, coach.HeadlineAr),
                coach.BioEn is null && coach.BioAr is null ? null : new LocalizedText(coach.BioEn ?? string.Empty, coach.BioAr ?? string.Empty),
                sportIds, coach.LocationId, coach.LanguageList),
            educations.Select(x => new MyCoachEducationDto(
                x.SchoolName, x.AcademicDegreeId, x.UniversityMajorId, x.StartDate, x.EndDate, x.IsStillEducation)).ToList(),
            certificates.Select(x => new MyCoachCertificateDto(
                x.Id, x.CertificateName, x.OrganizationName, x.IssuedDate, x.ExpiryDate, x.CredentialId, x.CredentialUrl,
                urlService.ToAbsoluteUrl(x.CertificateUrl))).ToList(),
            experiences.Select(x => new MyCoachExperienceDto(
                x.Address, x.JobTypeId, x.CompanyName, x.CompanyAddress, x.StartDate, x.EndDate, x.IsStillWorking)).ToList(),
            skills.Select(x => new MyCoachSkillDto(x.CourseId, x.PercentageScore)).ToList());
    }
}

public class GetMyCoachProfileQueryHandler(IUnitOfWork unitOfWork, IUrlService urlService)
    : IRequestHandler<GetMyCoachProfileQuery, Result<MyCoachProfileDto>>
{
    public Task<Result<MyCoachProfileDto>> Handle(GetMyCoachProfileQuery request, CancellationToken ct)
        => MyCoachProfileReader.ReadAsync(unitOfWork, urlService, request.UserId, ct);
}

/// <summary>
/// Saves the signed-in coach's own profile. It fills in everything the coach cannot change (username, email, slug,
/// packages, rate, specialisations, active flag) from what is stored, then runs the same update the back office uses,
/// so validation, certificate files and the single transaction behave the same.
/// </summary>
/// <remarks>
/// Approval: the verification status is left alone, so an approved coach stays published and the change is live at once.
/// To send edits back to staff instead, call coach.Decide(CoachVerification.Pending, null) after a successful save.
/// </remarks>
public class UpdateMyCoachProfileCommandHandler(ISender sender, IUnitOfWork unitOfWork, IUrlService urlService)
    : IRequestHandler<UpdateMyCoachProfileCommand, Result<MyCoachProfileDto>>
{
    public async Task<Result<MyCoachProfileDto>> Handle(UpdateMyCoachProfileCommand request, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var coach = await db.Set<Coach>().AsNoTracking().Include(c => c.Packages).FirstOrDefaultAsync(c => c.UserId == request.UserId, ct);
        var user = await db.Set<User>().AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.UserId, ct);
        if (coach is null || user is null)
        {
            return CoachErrors.NotFound;
        }

        var specialisationIds = await db.Set<CoachSpecialisationLink>().AsNoTracking()
            .Where(l => l.CoachId == coach.Id).Select(l => l.CoachSpecialisationId).ToListAsync(ct);

        var page = request.Page;
        var details = new CoachDetails(
            coach.Slug, page.NameEn, page.NameAr, page.HeadlineEn, page.HeadlineAr, page.BioEn, page.BioAr, coach.Email, page.LocationId,
            page.Languages, coach.YearsOfExperience, coach.ResponseTimeHours, coach.SessionRateAmount,
            coach.Packages.OrderBy(p => p.Position).Select(p => new CoachPackageInput(
                p.Id, p.NameEn, p.NameAr, p.DurationMinutes, p.SessionCount, p.Mode, p.PriceAmount, p.CompareAtAmount)).ToList());

        var u = request.User;
        var command = new UpdateCoachCommand(
            coach.Id,
            new UpdateUserCommand(
                user.Id, user.UserName ?? string.Empty, user.Email ?? string.Empty, u.PhoneNumber, u.FullName, u.BirthOfDate,
                u.GenderId, u.CountryId, u.CityId, u.JobStatusId, user.IsActive, u.ProfileImage),
            new CoachInput(details, page.SportIds, specialisationIds, null, null, null),
            request.Educations, request.Certificates, request.Experiences, request.Skills);

        var saved = await sender.Send(command, ct);
        if (saved.IsError)
        {
            return saved.Errors;
        }

        return await MyCoachProfileReader.ReadAsync(unitOfWork, urlService, request.UserId, ct);
    }
}
