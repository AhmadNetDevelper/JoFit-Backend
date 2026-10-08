namespace JoFit.Contracts.Requests.JobTypes;

public sealed record GetJobTypesPagedRequest(
    int Page = 1,
    int PageSize = 10,
    string? Search = null,
    bool? IsActive = null);
