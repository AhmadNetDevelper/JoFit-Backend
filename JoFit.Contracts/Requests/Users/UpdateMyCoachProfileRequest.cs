using System.ComponentModel.DataAnnotations;
using JoFit.Contracts.Requests.Common;
using JoFit.Contracts.Requests.UserEducations;
using JoFit.Contracts.Requests.UserExperiences;
using JoFit.Contracts.Requests.UserSkills;
using Microsoft.AspNetCore.Http;

namespace JoFit.Contracts.Requests.Users;

/// <summary>
/// Multipart body of PUT /coach/profile. Same field names as register-coach, without what a coach cannot change
/// here (username, email, password, slug). The four lists replace what is stored.
/// </summary>
public sealed record UpdateMyCoachProfileRequest
{
    public required MyCoachUserRequest User { get; init; }

    public required MyCoachPageRequest Profile { get; init; }

    public List<UserEducationRequest> Educations { get; init; } = [];

    public List<MyCoachCertificateRequest> Certificates { get; init; } = [];

    public List<UserExperienceRequest> Experiences { get; init; } = [];

    public List<UserSkillRequest> Skills { get; init; } = [];
}

public sealed record MyCoachUserRequest
{
    [Required(AllowEmptyStrings = false)]
    [Phone]
    public required string PhoneNumber { get; init; }

    [Required(AllowEmptyStrings = false)]
    [StringLength(200)]
    public required string FullName { get; init; }

    public DateTime? BirthOfDate { get; init; }

    /// <summary>1 = Male, 2 = Female.</summary>
    [Range(1, 2)]
    public short GenderId { get; init; }

    public long? CountryId { get; init; }

    public long? CityId { get; init; }

    public long? JobStatusId { get; init; }

    /// <summary>Optional new profile image. Leave it out to keep the current one.</summary>
    public IFormFile? ProfileImage { get; init; }
}

public sealed record MyCoachPageRequest
{
    public LocalizedTextRequest Name { get; init; } = new();
    public LocalizedTextRequest Headline { get; init; } = new();
    public LocalizedTextRequest? Bio { get; init; }
    public long LocationId { get; init; }
    public List<long> SportIds { get; init; } = [];
    public List<string> Languages { get; init; } = [];
}

public sealed record MyCoachCertificateRequest
{
    /// <summary>Id from GET /coach/profile. Send it to keep that certificate (and its file unless a new file comes with it); leave it out to add one.</summary>
    public long? Id { get; init; }

    [Required(AllowEmptyStrings = false)]
    [StringLength(100)]
    public required string CertificateName { get; init; }

    [Required(AllowEmptyStrings = false)]
    [StringLength(100)]
    public required string OrganizationName { get; init; }

    public DateTime IssuedDate { get; init; }

    public DateTime ExpiryDate { get; init; }

    [Required(AllowEmptyStrings = false)]
    [StringLength(100)]
    public required string CredentialId { get; init; }

    [StringLength(500)]
    public string? CredentialUrl { get; init; }

    public IFormFile? CertificateFile { get; init; }
}
