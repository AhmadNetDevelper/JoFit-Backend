namespace JoFit.Contracts.Requests.Countries;

public sealed record GetCountriesPagedRequest(
    int Page = 1,
    int PageSize = 10,
    string? Search = null,
    bool? IsActive = null);
