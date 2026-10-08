using JoFit.Application.Features.Coaches;
using JoFit.Application.Features.Users.Commands.CreateCoach;
using JoFit.Application.Features.Users.Commands.CreateUser;
using JoFit.Application.Features.Users.Commands.UpdateUser;
using JoFit.Contracts.Requests.Coaches;
using JoFit.Contracts.Requests.UserCertificates;
using JoFit.Contracts.Requests.UserEducations;
using JoFit.Contracts.Requests.UserExperiences;
using JoFit.Contracts.Requests.UserSkills;
using JoFit.Contracts.Requests.Users;
using JoFit.Domain.Coaches;
using JoFit.Domain.Identity.Users;
using JoFit.Infrastructure.Data.Seeders;

namespace JoFit.API.Mapping;

/// <summary>Turns the multipart coach forms into commands, shared by every endpoint that creates or updates a coach.</summary>
public static class CreateCoachMapper
{
    public static CreateCoachCommand ToCommand(this CreateCoachRequest request)
    {
        var user = request.User;

        return new CreateCoachCommand(
            new CreateUserCommand(
                user.UserName,
                user.Email,
                user.Password,
                user.PhoneNumber,
                user.FullName,
                user.BirthOfDate,
                (EnumGender)user.GenderId,
                user.CountryId,
                user.CityId,
                user.JobStatusId,
                SeedData.Roles.Coach,
                user.ProfileImage),
            ToCoachInput(request.Profile, user.Email),
            ToEducations(request.Educations),
            ToCertificates(request.Certificates),
            ToExperiences(request.Experiences),
            ToSkills(request.Skills));
    }

    public static UpdateCoachCommand ToCommand(this UpdateCoachRequest request, long coachId)
    {
        var user = request.User;

        return new UpdateCoachCommand(
            coachId,
            new UpdateUserCommand(
                0,
                user.UserName,
                user.Email,
                user.PhoneNumber,
                user.FullName,
                user.BirthOfDate,
                (EnumGender)user.GenderId,
                user.CountryId,
                user.CityId,
                user.JobStatusId,
                user.IsActive,
                user.ProfileImage),
            ToCoachInput(request.Profile, user.Email),
            ToEducations(request.Educations),
            ToCertificates(request.Certificates),
            ToExperiences(request.Experiences),
            ToSkills(request.Skills));
    }

    private static CoachInput ToCoachInput(CoachProfileRequest p, string email)
    {
        var details = new CoachDetails(
            p.Slug, p.Name.En, p.Name.Ar, p.Headline.En, p.Headline.Ar, p.Bio?.En, p.Bio?.Ar, email, p.LocationId,
            p.Languages, p.YearsOfExperience, p.ResponseTimeHours, p.SessionRate?.Amount,
            p.Packages.Select(x => new CoachPackageInput(x.Id, x.Name.En, x.Name.Ar, x.DurationMinutes, x.SessionCount, x.Mode, x.Price.Amount, x.CompareAtPrice?.Amount)).ToList());

        return new CoachInput(details, p.SportIds, p.Specializations, null, null, null);
    }

    private static List<CoachEducationInput> ToEducations(List<UserEducationRequest> items)
        => [.. items.Select(e => new CoachEducationInput(
            e.SchoolName, e.AcademicDegreeId, e.UniversityMajorId, e.StartDate, e.EndDate, e.IsStillEducation))];

    private static List<CoachCertificateInput> ToCertificates(List<UserCertificateRequest> items)
        => [.. items.Select(c => new CoachCertificateInput(
            c.CertificateName, c.OrganizationName, c.IssuedDate, c.ExpiryDate, c.CredentialId, c.CredentialUrl, c.CertificateFile))];

    private static List<CoachExperienceInput> ToExperiences(List<UserExperienceRequest> items)
        => [.. items.Select(e => new CoachExperienceInput(
            e.Address, e.JobTypeId, e.CompanyName, e.CompanyAddress, e.StartDate, e.EndDate, e.IsStillWorking))];

    private static List<CoachSkillInput> ToSkills(List<UserSkillRequest> items)
        => [.. items.Select(s => new CoachSkillInput(s.CourseId, s.PercentageScore))];
}
