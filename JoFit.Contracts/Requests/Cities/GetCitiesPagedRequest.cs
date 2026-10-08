namespace JoFit.Contracts.Requests.Cities;

public sealed record GetCitiesPagedRequest(
    int Page = 1,
    int PageSize = 10,
    string? Search = null,
    bool? IsActive = null,
    long? CountryId = null);
