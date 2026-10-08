using JoFit.Contracts.Requests.Coaches;
using JoFit.Contracts.Requests.UserCertificates;
using JoFit.Contracts.Requests.UserEducations;
using JoFit.Contracts.Requests.UserExperiences;
using JoFit.Contracts.Requests.UserSkills;

namespace JoFit.Contracts.Requests.Users;

/// <summary>
/// Multipart request used by the coach creation endpoint. Each part has its own request class.
/// </summary>
public sealed record CreateCoachRequest
{
    public required CreateUserWithImageRequest User { get; init; }

    /// <summary>The coach page: names, headline, location, sports, packages. Saved as the Coach record linked to the user.</summary>
    public required CoachProfileRequest Profile { get; init; }

    public List<UserEducationRequest> Educations { get; init; } = [];

    public List<UserCertificateRequest> Certificates { get; init; } = [];

    public List<UserExperienceRequest> Experiences { get; init; } = [];

    public List<UserSkillRequest> Skills { get; init; } = [];
}
