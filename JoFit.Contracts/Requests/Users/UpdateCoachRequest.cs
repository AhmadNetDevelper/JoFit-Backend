using JoFit.Contracts.Requests.Coaches;
using JoFit.Contracts.Requests.UserCertificates;
using JoFit.Contracts.Requests.UserEducations;
using JoFit.Contracts.Requests.UserExperiences;
using JoFit.Contracts.Requests.UserSkills;

namespace JoFit.Contracts.Requests.Users;

/// <summary>
/// Multipart request used by the coach update endpoint. Same parts as the create form; the education, certificate,
/// experience and skill lists replace what is stored.
/// </summary>
public sealed record UpdateCoachRequest
{
    public required UpdateUserWithImageRequest User { get; init; }

    public required CoachProfileRequest Profile { get; init; }

    public List<UserEducationRequest> Educations { get; init; } = [];

    public List<UserCertificateRequest> Certificates { get; init; } = [];

    public List<UserExperienceRequest> Experiences { get; init; } = [];

    public List<UserSkillRequest> Skills { get; init; } = [];
}
