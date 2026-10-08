using JoFit.Application.Features.Coaches;
using JoFit.Application.Features.Users.Commands.UpdateUser;
using JoFit.Domain.Common.Results;
using MediatR;

namespace JoFit.Application.Features.Users.Commands.CreateCoach;

/// <param name="User">The coach's login user; its UserId is filled in from the coach.</param>
public sealed record UpdateCoachCommand(
    long CoachId,
    UpdateUserCommand User,
    CoachInput Profile,
    List<CoachEducationInput> Educations,
    List<CoachCertificateInput> Certificates,
    List<CoachExperienceInput> Experiences,
    List<CoachSkillInput> Skills) : IRequest<Result<AdminCoachDto>>
{
    public CoachUserData Data => new(Educations, Certificates, Experiences, Skills);
}
