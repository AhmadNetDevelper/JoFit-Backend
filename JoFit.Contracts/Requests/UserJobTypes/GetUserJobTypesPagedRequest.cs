namespace JoFit.Contracts.Requests.UserJobTypes;

public sealed record GetUserJobTypesPagedRequest(
    int Page = 1,
    int PageSize = 10,
    string? Search = null,
    bool? IsActive = null);
