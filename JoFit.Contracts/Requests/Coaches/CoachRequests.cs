using System.Text.Json;
using JoFit.Contracts.Requests.Common;
using JoFit.Contracts.Requests.Courses;

namespace JoFit.Contracts.Requests.Coaches;

public sealed record CoachPackageRequest
{
    /// <summary>Send the id of an existing package to keep it (and the bookings pointing at it); leave it out to add one.</summary>
    public long? Id { get; init; }
    public LocalizedTextRequest Name { get; init; } = new();
    public int DurationMinutes { get; init; }
    public int SessionCount { get; init; } = 1;
    public string Mode { get; init; } = string.Empty;
    public MoneyRequest Price { get; init; } = new();
    public MoneyRequest? CompareAtPrice { get; init; }
}

/// <summary>
/// The coach page details sent with the coach sign-up / creation form. The email, name for login and password
/// come from the user part of the form; a new coach is always pending verification.
/// </summary>
public sealed record CoachProfileRequest
{
    public string Slug { get; init; } = string.Empty;
    public LocalizedTextRequest Name { get; init; } = new();
    public LocalizedTextRequest Headline { get; init; } = new();
    public LocalizedTextRequest? Bio { get; init; }
    public long LocationId { get; init; }
    public List<long> SportIds { get; init; } = [];
    public List<long> Specializations { get; init; } = [];
    public List<string> Languages { get; init; } = [];
    public int YearsOfExperience { get; init; }
    public int ResponseTimeHours { get; init; } = 24;
    public MoneyRequest? SessionRate { get; init; }
    public List<CoachPackageRequest> Packages { get; init; } = [];
}

/// <summary>Create and update body of a coach. A new coach is always pending verification.</summary>
public sealed record CoachDraftRequest
{
    public string Slug { get; init; } = string.Empty;
    public LocalizedTextRequest Name { get; init; } = new();
    public LocalizedTextRequest Headline { get; init; } = new();
    public LocalizedTextRequest? Bio { get; init; }
    public string Email { get; init; } = string.Empty;
    public long LocationId { get; init; }
    public List<long> SportIds { get; init; } = [];
    public List<long> Specializations { get; init; } = [];
    public List<string> Languages { get; init; } = [];
    public int YearsOfExperience { get; init; }
    public int ResponseTimeHours { get; init; } = 24;
    public MoneyRequest? SessionRate { get; init; }
    public List<CoachPackageRequest> Packages { get; init; } = [];

    /// <summary>Absent keeps the picture, null clears it, an image replaces it.</summary>
    public JsonElement? Avatar { get; init; }

    /// <summary>Absent keeps the gallery, a list replaces it, an empty list clears it.</summary>
    public List<MediaImageRequest>? Gallery { get; init; }

    /// <summary>On create only: gives the coach a login with this password.</summary>
    public string? Password { get; init; }
}

public sealed record CoachStatusDecisionRequest
{
    /// <summary>pending, approved, rejected or suspended. Rejecting or suspending needs a note.</summary>
    public string Status { get; init; } = string.Empty;
    public string? Note { get; init; }
}

public sealed record CertificationVerifiedRequest
{
    public bool IsVerified { get; init; }
}

public sealed record CoachBookingContactRequest
{
    public string? Name { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
}

public sealed record CreateCoachBookingRequest
{
    public long CoachId { get; init; }
    public long PackageId { get; init; }
    public string? SlotId { get; init; }
    public CoachBookingContactRequest Contact { get; init; } = new();
    public string? Goal { get; init; }
}

public sealed record CoachBookingStatusRequest
{
    public string Status { get; init; } = string.Empty;
    public string? Note { get; init; }
}

public sealed record GetAdminCoachesRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string[]? Statuses { get; init; }
    public string[]? VerificationStatuses { get; init; }
    public long[]? SportIds { get; init; }
    public long[]? Specializations { get; init; }
    public long[]? LocationIds { get; init; }
    public string? SortField { get; init; }
    public string? SortDir { get; init; }
}

public sealed record GetAdminCoachBookingsRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string[]? Statuses { get; init; }
    public long[]? CoachIds { get; init; }
    public long? CustomerId { get; init; }
    public bool UpcomingOnly { get; init; }
    public string? SortField { get; init; }
    public string? SortDir { get; init; }
}

public sealed record GetPublicCoachesRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string[]? SportSlugs { get; init; }
    public long[]? Specializations { get; init; }
    public long[]? LocationIds { get; init; }
    public string[]? Languages { get; init; }
    public string? Sort { get; init; }
}
