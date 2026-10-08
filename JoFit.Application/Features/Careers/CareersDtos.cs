using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Common.Utilities;
using JoFit.Domain.Careers;
using JoFit.Domain.Locations;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.Careers;

public sealed record CityDto(string Id, string Name);

public sealed record VerificationDto(string Status, string? Note);

public sealed record SalaryDto(MoneyDto? Min, MoneyDto? Max, string Period);

public sealed record JobCategoryRefDto(long Id, string Slug, string Name);

public sealed record JobSportRefDto(long Id, string Slug, string Name);

public sealed record EmployerRefDto(long Id, string Slug, string Name, string Kind, MediaImageDto? Logo);

/// <summary>A job as visitors read it. Text is the employer's own; the category and city are resolved to the requested language.</summary>
public sealed record JobPostingDto(
    long Id,
    string Reference,
    string Slug,
    string Title,
    string Description,
    IReadOnlyList<string> Requirements,
    IReadOnlyList<string> Benefits,
    IReadOnlyList<string> Languages,
    int Positions,
    string EmploymentType,
    string WorkMode,
    string ExperienceLevel,
    SalaryDto? Salary,
    JobCategoryRefDto? Category,
    CityDto? City,
    IReadOnlyList<JobSportRefDto> Sports,
    EmployerRefDto Employer,
    string? ClosesOn,
    DateTimeOffset? PostedAt,
    bool Featured,
    bool AcceptingApplications);

public sealed record FacetItem(string Value, string? Label, int Count);

public sealed record JobFacets(
    IReadOnlyList<FacetItem> Categories,
    IReadOnlyList<FacetItem> Cities,
    IReadOnlyList<FacetItem> EmploymentTypes,
    IReadOnlyList<FacetItem> WorkModes,
    IReadOnlyList<FacetItem> ExperienceLevels);

public sealed record JobListResultDto(IReadOnlyList<JobPostingDto> Items, PageMeta Meta, JobFacets Facets);

public sealed record EmployerSummaryDto(long Id, string Slug, string Name, string Kind, CityDto? City, MediaImageDto? Logo, int OpenJobCount);

public sealed record EmployerProfileDto(
    long Id, string Slug, string Name, string Kind, CityDto? City, MediaImageDto? Logo, string? About, string? Website, string? Size,
    int OpenJobCount, IReadOnlyList<JobPostingDto> OpenJobs);

public sealed record CareersStatsDto(int OpenJobs, int Employers, int Categories, int Cities);

public sealed record PublicJobCategoryDto(long Id, string Slug, string Name, int OpenJobCount);

/// <summary>An employer's own view of their company.</summary>
public sealed record EmployerAccountProfileDto(
    long Id, string Slug, string Name, string Kind, string CityId, string? About, string? Website, string? Email, string? Phone, string ContactName,
    string? Size, MediaImageDto? Logo, VerificationDto Verification, string LoginEmail, DateTimeOffset CreatedAt);

public sealed record EmployerJobDto(
    long Id, string Reference, string Slug, string Title, long CategoryId, IReadOnlyList<long> SportIds, string CityId, string EmploymentType,
    string WorkMode, string ExperienceLevel, SalaryDto? Salary, bool ShowSalary, string Description, IReadOnlyList<string> Requirements,
    IReadOnlyList<string> Benefits, IReadOnlyList<string> Languages, int Positions, string? ClosesOn, string Status, string? ModerationNote,
    bool Featured, bool IsPublic, int ApplicationCount, int NewApplicationCount, DateTimeOffset? PostedAt, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public sealed record CvFileDto(long Id, string FileName, string MimeType, long Bytes, DateTimeOffset UploadedAt);

public sealed record HistoryDto(string Status, DateTimeOffset At, string By);

public sealed record ApplicationJobRef(long Id, string Reference, string Title, string Slug, string EmployerName);

public sealed record CandidateApplicationDto(
    long Id, string Reference, ApplicationJobRef Job, string Status, string? StatusNote, string? WithdrawReason, IReadOnlyList<HistoryDto> History,
    bool CanWithdraw, CvFileDto? Cv, string CoverLetter, int YearsExperience, string? Certifications, string? AvailableFrom, MoneyDto? ExpectedSalary, DateTimeOffset SubmittedAt);

public sealed record CandidateDto(string Name, string Email, string? Phone);

public sealed record EmployerApplicationDto(
    long Id, string Reference, ApplicationJobRef Job, CandidateDto Candidate, string Status, IReadOnlyList<string> NextStatuses, bool IsNew, string? StatusNote,
    string? WithdrawReason, string? EmployerNote, IReadOnlyList<HistoryDto> History, CvFileDto? Cv, string CoverLetter, int YearsExperience,
    string? Certifications, string? AvailableFrom, MoneyDto? ExpectedSalary, DateTimeOffset SubmittedAt, DateTimeOffset? SeenByEmployerAt);

/// <summary>Staff see everything except the employer's private note; the CV is metadata only.</summary>
public sealed record AdminJobApplicationDto(
    long Id, string Reference, ApplicationJobRef Job, long JobEmployerId, long CustomerId, CandidateDto Candidate, string Status, string? StatusNote,
    string? WithdrawReason, IReadOnlyList<HistoryDto> History, CvFileDto? Cv, string CoverLetter, int YearsExperience, string? Certifications,
    string? AvailableFrom, MoneyDto? ExpectedSalary, DateTimeOffset SubmittedAt);

public sealed record AdminEmployerDto(
    long Id, string Slug, string Name, string Kind, string CityId, LocalizedText? City, string? About, string? Website, string? Email, string? Phone, string ContactName,
    string? Size, MediaImageDto? Logo, string VerificationStatus, string? VerificationNote, string LoginEmail, int JobCount, int OpenJobCount,
    int ApplicationCount, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public sealed record EmployerOptionDto(long Id, string Name, string VerificationStatus);

public sealed record AdminJobDto(
    long Id, string Reference, string Slug, string Title, long EmployerId, string EmployerName, string EmployerVerificationStatus, long CategoryId,
    LocalizedText? CategoryName, IReadOnlyList<long> SportIds, string CityId, LocalizedText? City, string EmploymentType, string WorkMode, string ExperienceLevel,
    SalaryDto? Salary, bool ShowSalary, string Description, IReadOnlyList<string> Requirements, IReadOnlyList<string> Benefits, IReadOnlyList<string> Languages,
    int Positions, string? ClosesOn, string Status, string? ModerationNote, bool Featured, bool IsPublic, int ApplicationCount, DateTimeOffset? PostedAt,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public sealed record CareersPendingCountDto(int Employers, int Jobs);

public sealed record AdminJobCategoryDto(long Id, string Slug, LocalizedText Name, int JobCount, int OpenJobCount);

/// <summary>Shared lookups for the careers handlers.</summary>
internal static class CareersSupport
{
    public static DateOnly Today => AmmanTime.Today;

    public static async Task<Dictionary<long, Location>> LocationsByIdAsync(IUnitOfWork uow, CancellationToken ct)
        => await uow.GetDbContext().Set<Location>().AsNoTracking().ToDictionaryAsync(l => l.Id, ct);

    public static async Task<Dictionary<string, Location>> LocationsBySlugAsync(IUnitOfWork uow, CancellationToken ct)
        => await uow.GetDbContext().Set<Location>().AsNoTracking().ToDictionaryAsync(l => l.Slug, ct);

    public static MediaImageDto? Logo(Employer e)
        => e.LogoUrl is null ? null : new MediaImageDto(e.LogoMediaId ?? string.Empty, e.LogoUrl, e.LogoAlt ?? string.Empty);

    public static SalaryDto? Salary(Job j)
        => j.SalaryPeriod is null || (j.SalaryMinAmount is null && j.SalaryMaxAmount is null)
            ? null
            : new SalaryDto(j.SalaryMinAmount is long min ? new MoneyDto(min) : null, j.SalaryMaxAmount is long max ? new MoneyDto(max) : null, j.SalaryPeriod);

    /// <summary>A monthly figure for sorting: hourly pay counts as 160 hours.</summary>
    public static long MonthlyMax(Job j)
    {
        var amount = j.SalaryMaxAmount ?? j.SalaryMinAmount ?? 0;
        return j.SalaryPeriod == "hour" ? amount * CareersRules.HoursPerMonth : amount;
    }
}
