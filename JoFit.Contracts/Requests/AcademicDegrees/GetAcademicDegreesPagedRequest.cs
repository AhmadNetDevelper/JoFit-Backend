namespace JoFit.Contracts.Requests.AcademicDegrees;

public sealed record GetAcademicDegreesPagedRequest(
    int Page = 1,
    int PageSize = 10,
    string? Search = null,
    bool? IsActive = null);
