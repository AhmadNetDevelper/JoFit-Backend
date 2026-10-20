using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Domain.Courses;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.Courses.Catalogue;

public sealed record CertificationDto(LocalizedText Name, LocalizedText IssuedBy);

/// <summary>Status is what staff chose (auto, soon, open, full); State is the effective result (soon, open, closing, full).</summary>
public sealed record NextIntakeDto(string StartsOn, int SeatsTotal, int SeatsTaken, int SeatsRemaining, string Status, string State);

public sealed record CourseCategoryRefDto(long Id, string Slug, string Name);

/// <summary>A syllabus module in both languages (back-office).</summary>
public sealed record AdminCourseModuleDto(long Id, int SortOrder, LocalizedText Title, LocalizedText? Summary, int? DurationMinutes);

/// <summary>A syllabus module resolved to one language (storefront).</summary>
public sealed record PublicCourseModuleDto(long Id, int SortOrder, string Title, string? Summary, int? DurationMinutes);

internal static class CourseModuleMapper
{
    /// <summary>Modules of the given courses, each list in teaching order.</summary>
    public static async Task<Dictionary<long, List<CourseModule>>> LoadAsync(IUnitOfWork unitOfWork, IReadOnlyList<long> courseIds, CancellationToken ct)
    {
        var rows = await unitOfWork.GetDbContext().Set<CourseModule>().AsNoTracking()
            .Where(m => courseIds.Contains(m.CourseId))
            .OrderBy(m => m.SortOrder).ThenBy(m => m.Id)
            .ToListAsync(ct);
        return rows.GroupBy(m => m.CourseId).ToDictionary(g => g.Key, g => g.ToList());
    }

    public static IReadOnlyList<AdminCourseModuleDto> ToAdmin(List<CourseModule>? modules)
        => (modules ?? []).Select(m => new AdminCourseModuleDto(
            m.Id,
            m.SortOrder,
            new LocalizedText(m.TitleEn, m.TitleAr),
            m.SummaryEn is null && m.SummaryAr is null ? null : new LocalizedText(m.SummaryEn ?? string.Empty, m.SummaryAr ?? string.Empty),
            m.DurationMinutes)).ToList();

    /// <summary>Plain strings in the requested language, falling back to the other one when it is empty.</summary>
    public static IReadOnlyList<PublicCourseModuleDto> ToPublic(List<CourseModule>? modules, string? language)
    {
        var arabic = language is not null && language.StartsWith("ar", StringComparison.OrdinalIgnoreCase);
        static string? First(string? a, string? b) => string.IsNullOrWhiteSpace(a) ? (string.IsNullOrWhiteSpace(b) ? null : b) : a;
        return (modules ?? []).Select(m => new PublicCourseModuleDto(
            m.Id,
            m.SortOrder,
            (arabic ? First(m.TitleAr, m.TitleEn) : First(m.TitleEn, m.TitleAr)) ?? string.Empty,
            arabic ? First(m.SummaryAr, m.SummaryEn) : First(m.SummaryEn, m.SummaryAr),
            m.DurationMinutes)).ToList();
    }
}

/// <summary>Back-office view of a course: both languages, every field.</summary>
public sealed record AdminCourseDto(
    long Id,
    string? Slug,
    LocalizedText Title,
    LocalizedText Summary,
    LocalizedText? Description,
    string Status,
    string Format,
    string Level,
    decimal DurationHours,
    int ModuleCount,
    MoneyDto Price,
    MoneyDto? CompareAtPrice,
    IReadOnlyList<long> CategoryIds,
    IReadOnlyList<long> InstructorIds,
    IReadOnlyList<long> SportIds,
    IReadOnlyList<string> Languages,
    LocalizedText? Venue,
    CertificationDto? Certification,
    NextIntakeDto? NextIntake,
    IReadOnlyList<MediaImageDto> Media,
    IReadOnlyList<AdminCourseModuleDto> Modules,
    int EnrolmentCount,
    DateTimeOffset? PublishedAt,
    int DisplayOrder,
    AuditDto Audit)
{
    public static AdminCourseDto From(Course c, IReadOnlyList<long> categoryIds, IReadOnlyList<MediaImageDto> media, int seatsTaken = 0, int enrolmentCount = 0, IReadOnlyList<long>? instructorIds = null, IReadOnlyList<long>? sportIds = null, IReadOnlyList<AdminCourseModuleDto>? modules = null)
        => new(
            c.Id,
            c.Slug,
            new LocalizedText(c.NameEn, c.NameAr),
            new LocalizedText(c.SummaryEn, c.SummaryAr),
            c.DescriptionEn is null || c.DescriptionAr is null ? null : new LocalizedText(c.DescriptionEn, c.DescriptionAr),
            c.Status,
            c.Format,
            c.Level,
            c.DurationHours,
            c.ModuleCount,
            new MoneyDto(c.PriceAmount),
            c.CompareAtAmount is long compare ? new MoneyDto(compare) : null,
            categoryIds,
            instructorIds ?? [],
            sportIds ?? [],
            c.LanguageList,
            c.VenueEn is null || c.VenueAr is null ? null : new LocalizedText(c.VenueEn, c.VenueAr),
            c.CertificationNameEn is null
                ? null
                : new CertificationDto(
                    new LocalizedText(c.CertificationNameEn, c.CertificationNameAr ?? string.Empty),
                    new LocalizedText(c.CertificationIssuedByEn ?? string.Empty, c.CertificationIssuedByAr ?? string.Empty)),
            NextIntakeOf(c, seatsTaken),
            media,
            modules ?? [],
            enrolmentCount,
            c.PublishedAtUtc,
            c.DisplayOrder,
            AuditDto.From(c));

    public static NextIntakeDto? NextIntakeOf(Course c, int seatsTaken)
        => c.IntakeStartsOn is DateOnly date && c.IntakeSeatsTotal is int total
            ? new NextIntakeDto(
                date.ToString("yyyy-MM-dd"), total, seatsTaken, Math.Max(0, total - seatsTaken),
                c.IntakeStatus, IntakeStates.Of(c.IntakeStatus, date, total, seatsTaken, DateOnly.FromDateTime(DateTime.UtcNow)))
            : null;
}

/// <summary>Who created and last changed a record, and when.</summary>
public sealed record AuditDto(DateTimeOffset CreatedAt, long? CreatedBy, DateTimeOffset? UpdatedAt, long? UpdatedBy)
{
    public static AuditDto From(JoFit.Domain.Common.AuditableEntity e)
        => new(e.CreatedAtUtc, e.CreatedBy, e.LastModifiedUtc, e.LastModifiedBy);
}

/// <summary>Storefront view: text resolved to the requested language; only published courses.</summary>
public sealed record PublicCourseDto(
    long Id,
    string Slug,
    string Title,
    string Summary,
    string? Description,
    string Format,
    string Level,
    decimal DurationHours,
    int ModuleCount,
    MoneyDto Price,
    MoneyDto? CompareAtPrice,
    IReadOnlyList<CourseCategoryRefDto> Categories,
    IReadOnlyList<CourseCategoryRefDto> Instructors,
    IReadOnlyList<string> Languages,
    string? Venue,
    string? CertificationName,
    string? CertificationIssuedBy,
    NextIntakeDto? NextIntake,
    IReadOnlyList<MediaImageDto> Media,
    DateTimeOffset? PublishedAt,
    int DisplayOrder,
    IReadOnlyList<PublicCourseModuleDto>? Modules = null)
{
    public static PublicCourseDto From(
        Course c,
        ILocalizationService loc,
        string? language,
        IReadOnlyList<CourseCategoryRefDto> categories,
        IReadOnlyList<MediaImageDto> media,
        int seatsTaken,
        IReadOnlyList<CourseCategoryRefDto>? instructors = null,
        IReadOnlyList<PublicCourseModuleDto>? modules = null)
        => new(
            c.Id,
            c.Slug!,
            loc.GetLocalizedName(c.NameAr, c.NameEn, language),
            loc.GetLocalizedName(c.SummaryAr, c.SummaryEn, language),
            c.DescriptionEn is null || c.DescriptionAr is null ? null : loc.GetLocalizedName(c.DescriptionAr, c.DescriptionEn, language),
            c.Format,
            c.Level,
            c.DurationHours,
            c.ModuleCount,
            new MoneyDto(c.PriceAmount),
            c.CompareAtAmount is long compare ? new MoneyDto(compare) : null,
            categories,
            instructors ?? [],
            c.LanguageList,
            c.VenueEn is null || c.VenueAr is null ? null : loc.GetLocalizedName(c.VenueAr, c.VenueEn, language),
            c.CertificationNameEn is null ? null : loc.GetLocalizedName(c.CertificationNameAr ?? string.Empty, c.CertificationNameEn, language),
            c.CertificationIssuedByEn is null ? null : loc.GetLocalizedName(c.CertificationIssuedByAr ?? string.Empty, c.CertificationIssuedByEn, language),
            AdminCourseDto.NextIntakeOf(c, seatsTaken),
            media,
            c.PublishedAtUtc,
            c.DisplayOrder,
            modules);
}
