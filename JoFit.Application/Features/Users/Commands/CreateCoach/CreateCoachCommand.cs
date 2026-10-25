using JoFit.Application.Features.Coaches;
using JoFit.Application.Features.Users.Commands.CreateUser;
using JoFit.Application.Features.Users.Dtos;
using JoFit.Domain.Common.Results;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace JoFit.Application.Features.Users.Commands.CreateCoach;

public sealed record CreateCoachCommand(
    CreateUserCommand User,
    CoachInput Profile,
    List<CoachEducationInput> Educations,
    List<CoachCertificateInput> Certificates,
    List<CoachExperienceInput> Experiences,
    List<CoachSkillInput> Skills) : IRequest<Result<UserDto>>
{
    public CoachUserData Data => new(Educations, Certificates, Experiences, Skills);
}

public sealed record CoachEducationInput(
    string SchoolName,
    long AcademicDegreeId,
    long UniversityMajorId,
    DateTime StartDate,
    DateTime? EndDate,
    bool IsStillEducation);

public sealed record CoachCertificateInput(
    string CertificateName,
    string OrganizationName,
    DateTime IssuedDate,
    DateTime ExpiryDate,
    string CredentialId,
    string? CredentialUrl,
    IFormFile? CertificateFile);

public sealed record CoachExperienceInput(
    string Address,
    long JobTypeId,
    string CompanyName,
    string CompanyAddress,
    DateTime StartDate,
    DateTime? EndDate,
    bool IsStillWorking);

public sealed record CoachSkillInput(long CourseId, short PercentageScore);
