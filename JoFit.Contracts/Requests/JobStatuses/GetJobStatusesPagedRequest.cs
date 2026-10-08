namespace JoFit.Contracts.Requests.JobStatuses;

public sealed record GetJobStatusesPagedRequest(
    int Page = 1,
    int PageSize = 10,
    string? Search = null,
    bool? IsActive = null);
