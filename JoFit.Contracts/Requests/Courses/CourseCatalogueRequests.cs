using JoFit.Contracts.Requests.Common;

namespace JoFit.Contracts.Requests.Courses;

public sealed record CertificationRequest
{
    public LocalizedTextRequest Name { get; init; } = new();
    public LocalizedTextRequest IssuedBy { get; init; } = new();
}

public sealed record NextIntakeRequest
{
    /// <summary>yyyy-MM-dd.</summary>
    public string? StartsOn { get; init; }
    public int? SeatsTotal { get; init; }
}

public sealed record MoneyRequest
{
    /// <summary>Minor units of JOD (3 decimals): 28000 is 28.000 JOD.</summary>
    public long Amount { get; init; }
    public string Currency { get; init; } = "JOD";
}

/// <summary>Create and update body of a course. A new course is always a draft.</summary>
public sealed record CourseDraftRequest
{
    public string? Slug { get; init; }
    public LocalizedTextRequest Title { get; init; } = new();
    public LocalizedTextRequest Summary { get; init; } = new();
    public LocalizedTextRequest? Description { get; init; }
    public string Format { get; init; } = string.Empty;
    public string Level { get; init; } = string.Empty;
    public decimal DurationHours { get; init; }
    public int ModuleCount { get; init; }
    public MoneyRequest Price { get; init; } = new();
    public MoneyRequest? CompareAtPrice { get; init; }
    public List<long> CategoryIds { get; init; } = [];

    /// <summary>Coaches who teach it. Absent keeps them, a list replaces them.</summary>
    public List<long>? InstructorIds { get; init; }

    /// <summary>Sports it is filed under. Absent keeps them, a list replaces them.</summary>
    public List<long>? SportIds { get; init; }
    public List<string> Languages { get; init; } = [];
    public LocalizedTextRequest? Venue { get; init; }
    public CertificationRequest? Certification { get; init; }
    public NextIntakeRequest? NextIntake { get; init; }

    /// <summary>Absent keeps the gallery, a list replaces it, an empty list clears it.</summary>
    public List<MediaImageRequest>? Media { get; init; }
}

public sealed record SetStatusRequest
{
    public string Status { get; init; } = string.Empty;
}

public sealed record GetAdminCoursesRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string[]? Statuses { get; init; }
    public long[]? CategoryIds { get; init; }
    public string? SortField { get; init; }
    public string? SortDir { get; init; }
}

public sealed record GetPublicCoursesRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string[]? CategorySlugs { get; init; }
    public string[]? Formats { get; init; }
    public string[]? Levels { get; init; }
    public string[]? Languages { get; init; }

    public string? Sort { get; init; }
}
