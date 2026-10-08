using JoFit.Domain.Common;
using JoFit.Domain.Common.Results;

namespace JoFit.Domain.Courses;

public static class CourseStatuses
{
    public const string Draft = "draft";
    public const string Published = "published";
    public const string Archived = "archived";

    public static readonly string[] All = [Draft, Published, Archived];
}

public static class CourseFormats
{
    public static readonly string[] All = ["in-person", "online", "hybrid"];
    public static bool NeedsVenue(string format) => format is "in-person" or "hybrid";
}

public static class CourseLevels
{
    public static readonly string[] All = ["beginner", "intermediate", "advanced"];
}

public sealed record CourseCertification(string NameEn, string NameAr, string IssuedByEn, string IssuedByAr);

/// <summary>Everything a course draft carries, as the back-office form saves it.</summary>
public sealed record CourseDetails(
    string? Slug,
    string TitleEn,
    string TitleAr,
    string SummaryEn,
    string SummaryAr,
    string? DescriptionEn,
    string? DescriptionAr,
    string Format,
    string Level,
    decimal DurationHours,
    int ModuleCount,
    long PriceAmount,
    long? CompareAtAmount,
    IReadOnlyList<string> Languages,
    string? VenueEn,
    string? VenueAr,
    CourseCertification? Certification,
    DateOnly? IntakeStartsOn,
    int? IntakeSeatsTotal);

/// <summary>The full-catalogue side of a course (the original entity is the name, plus an active flag).</summary>
public partial class Course
{
    public string? Slug { get; private set; }
    public string Status { get; private set; } = CourseStatuses.Draft;
    public string SummaryEn { get; private set; } = string.Empty;
    public string SummaryAr { get; private set; } = string.Empty;
    public string? DescriptionEn { get; private set; }
    public string? DescriptionAr { get; private set; }
    public string Format { get; private set; } = "online";
    public string Level { get; private set; } = "beginner";
    public decimal DurationHours { get; private set; }
    public int ModuleCount { get; private set; }

    /// <summary>Minor units of JOD (3 decimals): 28000 = 28.000 JOD.</summary>
    public long PriceAmount { get; private set; }
    public long? CompareAtAmount { get; private set; }

    /// <summary>Comma-separated language codes, for example "ar,en".</summary>
    public string Languages { get; private set; } = "ar";
    public string? VenueEn { get; private set; }
    public string? VenueAr { get; private set; }
    public string? CertificationNameEn { get; private set; }
    public string? CertificationNameAr { get; private set; }
    public string? CertificationIssuedByEn { get; private set; }
    public string? CertificationIssuedByAr { get; private set; }
    public DateOnly? IntakeStartsOn { get; private set; }
    public int? IntakeSeatsTotal { get; private set; }
    public DateTimeOffset? PublishedAtUtc { get; private set; }

    public IReadOnlyList<string> LanguageList => Languages.Split(',', StringSplitOptions.RemoveEmptyEntries);

    public bool IsPublished => Status == CourseStatuses.Published;

    /// <summary>Creates a draft. The back-office never creates a published course directly.</summary>
    public static Result<Course> CreateDraft(CourseDetails details, DateOnly today)
    {
        var course = new Course { IsActive = true };
        var applied = course.ApplyDetails(details, today, intakeChanged: true);
        return applied.IsError ? applied.Errors : course;
    }

    /// <summary>Saves the editable fields. A published course must stay complete in every language.</summary>
    public Result<Updated> ApplyDetails(CourseDetails d, DateOnly today, bool intakeChanged, int seatsTaken = 0)
    {
        var errors = new List<Error>();
        var titleEn = (d.TitleEn ?? string.Empty).Trim();
        var titleAr = (d.TitleAr ?? string.Empty).Trim();
        var summaryEn = (d.SummaryEn ?? string.Empty).Trim();
        var summaryAr = (d.SummaryAr ?? string.Empty).Trim();
        var descriptionEn = string.IsNullOrWhiteSpace(d.DescriptionEn) ? null : d.DescriptionEn.Trim();
        var descriptionAr = string.IsNullOrWhiteSpace(d.DescriptionAr) ? null : d.DescriptionAr.Trim();

        if (titleEn.Length == 0) errors.Add(CourseErrors.Field("title.en", "The English title is required."));
        else if (titleEn.Length > 100) errors.Add(CourseErrors.Field("title.en", "A title cannot exceed 100 characters."));
        if (titleAr.Length == 0) errors.Add(CourseErrors.Field("title.ar", "The Arabic title is required."));
        else if (titleAr.Length > 100) errors.Add(CourseErrors.Field("title.ar", "A title cannot exceed 100 characters."));
        if (summaryEn.Length == 0) errors.Add(CourseErrors.Field("summary.en", "The English summary is required."));
        if (summaryAr.Length == 0) errors.Add(CourseErrors.Field("summary.ar", "The Arabic summary is required."));

        if (!CourseFormats.All.Contains(d.Format)) errors.Add(CourseErrors.Field("format", "Choose a course format."));
        if (!CourseLevels.All.Contains(d.Level)) errors.Add(CourseErrors.Field("level", "Choose a course level."));
        if (d.DurationHours <= 0 || d.DurationHours > 2000) errors.Add(CourseErrors.Field("durationHours", "Duration must be more than 0 and at most 2000 hours."));
        if (d.ModuleCount is < 0 or > 200) errors.Add(CourseErrors.Field("moduleCount", "Modules must be a whole number from 0 to 200."));
        if (d.PriceAmount < 0) errors.Add(CourseErrors.Field("price", "The price cannot be negative."));
        if (d.CompareAtAmount is long compareAt && compareAt <= d.PriceAmount) errors.Add(CourseErrors.Field("compareAtPrice", "The compare-at price must be higher than the price."));

        var languages = (d.Languages ?? []).Select(l => l.Trim().ToLowerInvariant()).Where(l => l.Length > 0).Distinct().ToList();
        if (languages.Count == 0 || languages.Any(l => l is not ("ar" or "en"))) errors.Add(CourseErrors.Field("languages", "Choose at least one language (ar or en)."));

        string? venueEn = null, venueAr = null;
        if (CourseFormats.NeedsVenue(d.Format))
        {
            venueEn = string.IsNullOrWhiteSpace(d.VenueEn) ? null : d.VenueEn.Trim();
            venueAr = string.IsNullOrWhiteSpace(d.VenueAr) ? null : d.VenueAr.Trim();
            if (venueEn is null) errors.Add(CourseErrors.Field("venue.en", "The venue is required for in-person and hybrid courses."));
            if (venueAr is null) errors.Add(CourseErrors.Field("venue.ar", "The venue is required for in-person and hybrid courses."));
        }

        var certification = d.Certification;
        if (certification is not null)
        {
            if (string.IsNullOrWhiteSpace(certification.NameEn)) errors.Add(CourseErrors.Field("certification.name.en", "Give the certification a name in English."));
            if (string.IsNullOrWhiteSpace(certification.NameAr)) errors.Add(CourseErrors.Field("certification.name.ar", "Give the certification a name in Arabic."));
            if (string.IsNullOrWhiteSpace(certification.IssuedByEn)) errors.Add(CourseErrors.Field("certification.issuedBy.en", "Say who issues it, in English."));
            if (string.IsNullOrWhiteSpace(certification.IssuedByAr)) errors.Add(CourseErrors.Field("certification.issuedBy.ar", "Say who issues it, in Arabic."));
        }

        if (d.IntakeStartsOn is not null || d.IntakeSeatsTotal is not null)
        {
            if (d.IntakeStartsOn is null) errors.Add(CourseErrors.Field("nextIntake.startsOn", "Give the intake a start date."));
            else if (intakeChanged && d.IntakeStartsOn < today) errors.Add(CourseErrors.Field("nextIntake.startsOn", "The start date cannot be in the past."));
            else if (!intakeChanged && seatsTaken > 0 && d.IntakeStartsOn != IntakeStartsOn) errors.Add(CourseErrors.Field("nextIntake.startsOn", "The start date cannot change while seats are taken."));

            if (d.IntakeSeatsTotal is not int seats || seats is < 1 or > 1000) errors.Add(CourseErrors.Field("nextIntake.seatsTotal", "Seats must be a whole number from 1 to 1000."));
            else if (seats < seatsTaken) errors.Add(CourseErrors.Field("nextIntake.seatsTotal", $"{seatsTaken} seats are already taken."));
        }

        if (IsPublished && (descriptionEn is null || descriptionAr is null))
        {
            errors.Add(CourseErrors.Field("description", "A published course needs its description in every language."));
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        if (!string.IsNullOrWhiteSpace(d.Slug)) Slug = d.Slug.Trim();
        NameEn = titleEn;
        NameAr = titleAr;
        SummaryEn = summaryEn;
        SummaryAr = summaryAr;
        DescriptionEn = descriptionEn;
        DescriptionAr = descriptionAr;
        Format = d.Format;
        Level = d.Level;
        DurationHours = d.DurationHours;
        ModuleCount = d.ModuleCount;
        PriceAmount = d.PriceAmount;
        CompareAtAmount = d.CompareAtAmount;
        Languages = string.Join(',', languages);
        VenueEn = venueEn;
        VenueAr = venueAr;
        CertificationNameEn = certification?.NameEn.Trim();
        CertificationNameAr = certification?.NameAr.Trim();
        CertificationIssuedByEn = certification?.IssuedByEn.Trim();
        CertificationIssuedByAr = certification?.IssuedByAr.Trim();
        IntakeStartsOn = d.IntakeStartsOn;
        IntakeSeatsTotal = d.IntakeSeatsTotal;
        return Result.Updated;
    }

    /// <summary>Moves between draft, published and archived. Publishing needs every language filled.</summary>
    public Result<Updated> SetStatus(string status)
    {
        if (!CourseStatuses.All.Contains(status))
        {
            return CourseErrors.Field("status", "Choose draft, published or archived.");
        }

        if (status == CourseStatuses.Published && (string.IsNullOrWhiteSpace(DescriptionEn) || string.IsNullOrWhiteSpace(DescriptionAr)))
        {
            return CourseErrors.Field("description", "Add the description in every language before publishing.");
        }

        if (status == CourseStatuses.Published)
        {
            PublishedAtUtc ??= DateTimeOffset.UtcNow;
        }

        Status = status;
        return Result.Updated;
    }
}

/// <summary>Which coaches teach a course.</summary>
public class CourseInstructorLink
{
    public long CourseId { get; set; }
    public long CoachId { get; set; }
}

/// <summary>Which categories a course belongs to.</summary>
public class CourseCategoryLink
{
    public long CourseId { get; set; }
    public long CourseCategoryId { get; set; }
}
