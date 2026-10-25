using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Features.Courses.Catalogue;
using JoFit.Domain.Coaches;

namespace JoFit.Application.Features.Coaches;

public sealed record CoachPackageDto(long Id, LocalizedText Name, int DurationMinutes, int SessionCount, string Mode, MoneyDto Price, MoneyDto? CompareAtPrice)
{
    public static CoachPackageDto From(CoachPackage p)
        => new(p.Id, new LocalizedText(p.NameEn, p.NameAr), p.DurationMinutes, p.SessionCount, p.Mode, new MoneyDto(p.PriceAmount),
            p.CompareAtAmount is long c ? new MoneyDto(c) : null);
}

public sealed record CoachCertificationDto(long Id, LocalizedText Name, LocalizedText Issuer, int? Year, bool IsVerified)
{
    public static CoachCertificationDto From(CoachCertification c)
        => new(c.Id, new LocalizedText(c.NameEn, c.NameAr), new LocalizedText(c.IssuerEn, c.IssuerAr), c.Year, c.IsVerified);
}

public sealed record LocationDto(long Id, string Slug, LocalizedText Name);

/// <summary>The login account behind a coach: the personal data entered on the sign-up form.</summary>
public sealed record CoachAccountDto(
    long UserId, string UserName, string FullName, string? PhoneNumber, DateTime? BirthOfDate, int GenderId,
    long? CountryId, long? CityId, long? JobStatusId, bool IsActive, string? ProfileImageUrl);

public sealed record CoachEducationDto(
    long Id, string SchoolName, long AcademicDegreeId, LocalizedText? AcademicDegree, long UniversityMajorId, LocalizedText? UniversityMajor,
    DateTime StartDate, DateTime? EndDate, bool IsStillEducation);

public sealed record CoachExperienceDto(
    long Id, string Address, long JobTypeId, LocalizedText? JobType, string CompanyName, string CompanyAddress,
    DateTime StartDate, DateTime? EndDate, bool IsStillWorking);

public sealed record CoachUserCertificateDto(
    long Id, string CertificateName, string OrganizationName, DateTime IssuedDate, DateTime ExpiryDate,
    string CredentialId, string? CredentialUrl, string? CertificateUrl);

public sealed record CoachSkillDto(long Id, long CourseId, LocalizedText? Course, short PercentageScore);

public sealed record AdminCoachDto(
    long Id,
    string Slug,
    LocalizedText Name,
    LocalizedText Headline,
    LocalizedText? Bio,
    string Email,
    long LocationId,
    IReadOnlyList<long> SportIds,
    IReadOnlyList<long> Specializations,
    IReadOnlyList<string> Languages,
    int YearsOfExperience,
    int ResponseTimeHours,
    MoneyDto? SessionRate,
    IReadOnlyList<CoachPackageDto> Packages,
    IReadOnlyList<CoachCertificationDto> Certifications,
    MediaImageDto? Avatar,
    IReadOnlyList<MediaImageDto> Gallery,
    string VerificationStatus,
    string? VerificationNote,
    int CompletedSessions,
    bool HasLogin,
    AuditDto Audit)
{
    // Filled by the single-coach read only; the list leaves them empty.
    public CoachAccountDto? Account { get; init; }
    public IReadOnlyList<CoachEducationDto> Educations { get; init; } = [];
    public IReadOnlyList<CoachExperienceDto> Experiences { get; init; } = [];
    public IReadOnlyList<CoachUserCertificateDto> UserCertificates { get; init; } = [];
    public IReadOnlyList<CoachSkillDto> Skills { get; init; } = [];

    public static AdminCoachDto From(
        Coach c, IReadOnlyList<long> sportIds, IReadOnlyList<long> specialisationIds, MediaImageDto? avatar,
        IReadOnlyList<MediaImageDto> gallery, int completedSessions)
        => new(
            c.Id,
            c.Slug,
            new LocalizedText(c.NameEn, c.NameAr),
            new LocalizedText(c.HeadlineEn, c.HeadlineAr),
            c.BioEn is null || c.BioAr is null ? null : new LocalizedText(c.BioEn, c.BioAr),
            c.Email,
            c.LocationId,
            sportIds,
            specialisationIds,
            c.LanguageList,
            c.YearsOfExperience,
            c.ResponseTimeHours,
            c.SessionRateAmount is long r ? new MoneyDto(r) : null,
            c.Packages.OrderBy(p => p.Position).Select(CoachPackageDto.From).ToList(),
            c.Certifications.Select(CoachCertificationDto.From).ToList(),
            avatar,
            gallery,
            c.VerificationStatus,
            c.VerificationNote,
            completedSessions,
            c.UserId is not null,
            AuditDto.From(c));
}

public sealed record PublicRef(long Id, string Slug, string Name);

public sealed record PublicCoachDto(
    long Id,
    string Slug,
    string Name,
    string Headline,
    string? Bio,
    LocationRef? Location,
    IReadOnlyList<PublicRef> Sports,
    IReadOnlyList<string> Specializations,
    IReadOnlyList<string> Languages,
    int YearsOfExperience,
    int ResponseTimeHours,
    MoneyDto? SessionRate,
    IReadOnlyList<PublicPackageDto> Packages,
    IReadOnlyList<PublicCertificationDto> Certifications,
    MediaImageDto? Avatar,
    IReadOnlyList<MediaImageDto> Gallery,
    int CompletedSessions);

public sealed record LocationRef(long Id, string Slug, string Name);

public sealed record PublicPackageDto(long Id, string Name, int DurationMinutes, int SessionCount, string Mode, MoneyDto Price, MoneyDto? CompareAtPrice);

public sealed record PublicCertificationDto(string Name, string Issuer, int? Year);

public sealed record CoachSlotDto(string Id, string StartsAt, string EndsAt);

public sealed record CoachBookingContactDto(string Name, string Email, string? Phone);

public sealed record CoachBookingDto(
    long Id,
    string Reference,
    long CoachId,
    string CoachName,
    long PackageId,
    string PackageName,
    long? CustomerId,
    CoachBookingContactDto Contact,
    string? Goal,
    string StartsAt,
    string EndsAt,
    string Status,
    MoneyDto Total,
    string? Note,
    AuditDto Audit);
