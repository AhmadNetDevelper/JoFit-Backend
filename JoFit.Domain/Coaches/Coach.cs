using System.Text.RegularExpressions;
using JoFit.Domain.Common;
using JoFit.Domain.Common.Results;

namespace JoFit.Domain.Coaches;

public static class CoachVerification
{
    public const string Pending = "pending";
    public const string Approved = "approved";
    public const string Rejected = "rejected";
    public const string Suspended = "suspended";

    public static readonly string[] All = [Pending, Approved, Rejected, Suspended];
}

public static class CoachModes
{
    public static readonly string[] All = ["in-person", "online"];
}

public sealed record CoachPackageInput(long? Id, string NameEn, string NameAr, int DurationMinutes, int SessionCount, string Mode, long PriceAmount, long? CompareAtAmount);

/// <summary>The editable fields of a coach, as the back-office form saves them.</summary>
public sealed record CoachDetails(
    string Slug,
    string NameEn,
    string NameAr,
    string HeadlineEn,
    string HeadlineAr,
    string? BioEn,
    string? BioAr,
    string Email,
    long LocationId,
    IReadOnlyList<string> Languages,
    int YearsOfExperience,
    int ResponseTimeHours,
    long? SessionRateAmount,
    IReadOnlyList<CoachPackageInput> Packages);

public class Coach : AuditableEntity
{
    private static readonly Regex SlugPattern = new("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);
    private static readonly Regex EmailPattern = new(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.Compiled);

    /// <summary>The login of this coach, if they have one.</summary>
    public long? UserId { get; private set; }
    public string Slug { get; private set; } = string.Empty;
    public string NameEn { get; private set; } = string.Empty;
    public string NameAr { get; private set; } = string.Empty;
    public string HeadlineEn { get; private set; } = string.Empty;
    public string HeadlineAr { get; private set; } = string.Empty;
    public string? BioEn { get; private set; }
    public string? BioAr { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public long LocationId { get; private set; }
    public string Languages { get; private set; } = "ar";
    public int YearsOfExperience { get; private set; }
    public int ResponseTimeHours { get; private set; } = 24;
    public long? SessionRateAmount { get; private set; }
    public string VerificationStatus { get; private set; } = CoachVerification.Pending;
    public string? VerificationNote { get; private set; }

    /// <summary>Weekly schedule, until an availability editor exists: Sunday to Thursday, 09:00 to 17:00, hourly slots (Amman time).</summary>
    public string WorkingDays { get; private set; } = "0,1,2,3,4";
    public int DayStartHour { get; private set; } = 9;
    public int DayEndHour { get; private set; } = 17;

    public List<CoachPackage> Packages { get; private set; } = [];
    public List<CoachCertification> Certifications { get; private set; } = [];

    private Coach()
    {
    }

    public IReadOnlyList<string> LanguageList => Languages.Split(',', StringSplitOptions.RemoveEmptyEntries);

    public IReadOnlyList<DayOfWeek> WorkingDayList => WorkingDays.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(d => (DayOfWeek)int.Parse(d)).ToList();

    public bool IsListed => VerificationStatus == CoachVerification.Approved;

    /// <summary>A new coach is always pending, whatever the form says, and starts with no certifications.</summary>
    public static Result<Coach> Create(CoachDetails details)
    {
        var coach = new Coach();
        var applied = coach.Apply(details);
        return applied.IsError ? applied.Errors : coach;
    }

    public void LinkLogin(long userId) => UserId = userId;

    public Result<Updated> Apply(CoachDetails d)
    {
        var errors = new List<Error>();
        var slug = (d.Slug ?? string.Empty).Trim();
        var nameEn = (d.NameEn ?? string.Empty).Trim();
        var nameAr = (d.NameAr ?? string.Empty).Trim();
        var headlineEn = (d.HeadlineEn ?? string.Empty).Trim();
        var headlineAr = (d.HeadlineAr ?? string.Empty).Trim();
        var bioEn = string.IsNullOrWhiteSpace(d.BioEn) ? null : d.BioEn.Trim();
        var bioAr = string.IsNullOrWhiteSpace(d.BioAr) ? null : d.BioAr.Trim();
        var email = (d.Email ?? string.Empty).Trim().ToLowerInvariant();

        if (slug.Length == 0) errors.Add(CoachErrors.Field("slug", "A slug is required."));
        else if (!SlugPattern.IsMatch(slug)) errors.Add(CoachErrors.Field("slug", "Use lowercase letters, numbers and single hyphens."));
        if (nameEn.Length == 0) errors.Add(CoachErrors.Field("name.en", "The English name is required."));
        if (nameAr.Length == 0) errors.Add(CoachErrors.Field("name.ar", "The Arabic name is required."));
        if (headlineEn.Length == 0) errors.Add(CoachErrors.Field("headline.en", "The English headline is required."));
        if (headlineAr.Length == 0) errors.Add(CoachErrors.Field("headline.ar", "The Arabic headline is required."));
        if (!EmailPattern.IsMatch(email)) errors.Add(CoachErrors.Field("email", "Enter a valid email address."));
        if (d.LocationId <= 0) errors.Add(CoachErrors.Field("locationId", "Choose a location."));

        var languages = (d.Languages ?? []).Select(l => l.Trim().ToLowerInvariant()).Where(l => l.Length > 0).Distinct().ToList();
        if (languages.Count == 0 || languages.Any(l => l is not ("ar" or "en"))) errors.Add(CoachErrors.Field("languages", "Choose at least one language (ar or en)."));
        if (d.YearsOfExperience is < 0 or > 60) errors.Add(CoachErrors.Field("yearsOfExperience", "Years of experience must be from 0 to 60."));
        if (d.ResponseTimeHours is < 1 or > 168) errors.Add(CoachErrors.Field("responseTimeHours", "Response time must be from 1 to 168 hours."));
        if (d.SessionRateAmount is < 0) errors.Add(CoachErrors.Field("sessionRate", "The session rate cannot be negative."));

        var packages = d.Packages ?? [];
        for (var i = 0; i < packages.Count; i++)
        {
            var p = packages[i];
            var key = $"packages.{i}";
            if (string.IsNullOrWhiteSpace(p.NameEn)) errors.Add(CoachErrors.Field($"{key}.name.en", "The English package name is required."));
            if (string.IsNullOrWhiteSpace(p.NameAr)) errors.Add(CoachErrors.Field($"{key}.name.ar", "The Arabic package name is required."));
            if (p.DurationMinutes is < 5 or > 600) errors.Add(CoachErrors.Field($"{key}.durationMinutes", "Duration must be from 5 to 600 minutes."));
            if (p.SessionCount is < 1 or > 100) errors.Add(CoachErrors.Field($"{key}.sessionCount", "Sessions must be from 1 to 100."));
            if (!CoachModes.All.Contains(p.Mode)) errors.Add(CoachErrors.Field($"{key}.mode", "Choose in-person or online."));
            if (p.PriceAmount < 0) errors.Add(CoachErrors.Field($"{key}.price", "The price cannot be negative."));
            if (p.CompareAtAmount is long compare && compare <= p.PriceAmount) errors.Add(CoachErrors.Field($"{key}.compareAtPrice", "The compare-at price must be higher than the price."));
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        Slug = slug;
        NameEn = nameEn;
        NameAr = nameAr;
        HeadlineEn = headlineEn;
        HeadlineAr = headlineAr;
        BioEn = bioEn;
        BioAr = bioAr;
        Email = email;
        LocationId = d.LocationId;
        Languages = string.Join(',', languages);
        YearsOfExperience = d.YearsOfExperience;
        ResponseTimeHours = d.ResponseTimeHours;
        SessionRateAmount = d.SessionRateAmount;
        return Result.Updated;
    }

    /// <summary>Any status is accepted; rejecting or suspending needs a note, which is what the coach is told.</summary>
    public Result<Updated> Decide(string status, string? note)
    {
        if (!CoachVerification.All.Contains(status))
        {
            return CoachErrors.Field("status", "Choose approved, rejected, suspended or pending.");
        }

        var trimmed = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (status is CoachVerification.Rejected or CoachVerification.Suspended && trimmed is null)
        {
            return CoachErrors.Field("note", "Say why, so the coach knows what to fix.");
        }

        if (trimmed is { Length: > 1000 })
        {
            return CoachErrors.Field("note", "The note cannot exceed 1000 characters.");
        }

        VerificationStatus = status;
        VerificationNote = status == CoachVerification.Approved ? null : trimmed;
        return Result.Updated;
    }
}

public class CoachPackage : AuditableEntity
{
    public long CoachId { get; private set; }
    public string NameEn { get; private set; } = string.Empty;
    public string NameAr { get; private set; } = string.Empty;
    public int DurationMinutes { get; private set; }
    public int SessionCount { get; private set; } = 1;
    public string Mode { get; private set; } = "in-person";
    public long PriceAmount { get; private set; }
    public long? CompareAtAmount { get; private set; }
    public int Position { get; private set; }

    private CoachPackage()
    {
    }

    public static CoachPackage Create(CoachPackageInput input, int position)
    {
        var package = new CoachPackage();
        package.Apply(input, position);
        return package;
    }

    public void Apply(CoachPackageInput input, int position)
    {
        NameEn = input.NameEn.Trim();
        NameAr = input.NameAr.Trim();
        DurationMinutes = input.DurationMinutes;
        SessionCount = input.SessionCount;
        Mode = input.Mode;
        PriceAmount = input.PriceAmount;
        CompareAtAmount = input.CompareAtAmount;
        Position = position;
    }
}

public class CoachCertification : AuditableEntity
{
    public long CoachId { get; private set; }
    public string NameEn { get; private set; } = string.Empty;
    public string NameAr { get; private set; } = string.Empty;
    public string IssuerEn { get; private set; } = string.Empty;
    public string IssuerAr { get; private set; } = string.Empty;
    public int? Year { get; private set; }
    public bool IsVerified { get; private set; }

    private CoachCertification()
    {
    }

    public static CoachCertification Create(string nameEn, string nameAr, string issuerEn, string issuerAr, int? year)
        => new() { NameEn = nameEn, NameAr = nameAr, IssuerEn = issuerEn, IssuerAr = issuerAr, Year = year };

    public void SetVerified(bool verified) => IsVerified = verified;
}

public class CoachSportLink
{
    public long CoachId { get; set; }
    public long SportId { get; set; }
}

public class CoachSpecialisationLink
{
    public long CoachId { get; set; }
    public long CoachSpecialisationId { get; set; }
}
