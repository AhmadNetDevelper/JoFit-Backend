namespace JoFit.Contracts.Requests.UniversityMajors;

public sealed record GetUniversityMajorsPagedRequest(
    int Page = 1,
    int PageSize = 10,
    string? Search = null,
    bool? IsActive = null);
