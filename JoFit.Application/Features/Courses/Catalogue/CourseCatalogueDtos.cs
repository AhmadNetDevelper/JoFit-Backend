using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Domain.Courses;

namespace JoFit.Application.Features.Courses.Catalogue;

public sealed record CertificationDto(LocalizedText Name, LocalizedText IssuedBy);

public sealed record NextIntakeDto(string StartsOn, int SeatsTotal, int SeatsTaken, int SeatsRemaining);

public sealed record CourseCategoryRefDto(long Id, string Slug, string Name);

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
    IReadOnlyList<string> Languages,
    LocalizedText? Venue,
    CertificationDto? Certification,
    NextIntakeDto? NextIntake,
    IReadOnlyList<MediaImageDto> Media,
    int EnrolmentCount,
    DateTimeOffset? PublishedAt,
    AuditDto Audit)
{
    public static AdminCourseDto From(Course c, IReadOnlyList<long> categoryIds, IReadOnlyList<MediaImageDto> media, int seatsTaken = 0, int enrolmentCount = 0, IReadOnlyList<long>? instructorIds = null)
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
            c.LanguageList,
            c.VenueEn is null || c.VenueAr is null ? null : new LocalizedText(c.VenueEn, c.VenueAr),
            c.CertificationNameEn is null
                ? null
                : new CertificationDto(
                    new LocalizedText(c.CertificationNameEn, c.CertificationNameAr ?? string.Empty),
                    new LocalizedText(c.CertificationIssuedByEn ?? string.Empty, c.CertificationIssuedByAr ?? string.Empty)),
            NextIntakeOf(c, seatsTaken),
            media,
            enrolmentCount,
            c.PublishedAtUtc,
            AuditDto.From(c));

    public static NextIntakeDto? NextIntakeOf(Course c, int seatsTaken)
        => c.IntakeStartsOn is DateOnly date && c.IntakeSeatsTotal is int total
            ? new NextIntakeDto(date.ToString("yyyy-MM-dd"), total, seatsTaken, Math.Max(0, total - seatsTaken))
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
    DateTimeOffset? PublishedAt)
{
    public static PublicCourseDto From(
        Course c,
        ILocalizationService loc,
        string? language,
        IReadOnlyList<CourseCategoryRefDto> categories,
        IReadOnlyList<MediaImageDto> media,
        int seatsTaken,
        IReadOnlyList<CourseCategoryRefDto>? instructors = null)
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
            c.PublishedAtUtc);
}
