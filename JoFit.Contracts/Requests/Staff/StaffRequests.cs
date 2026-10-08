namespace JoFit.Contracts.Requests.Staff;

public sealed record StaffDraftRequest
{
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string? Password { get; init; }
}

public sealed record GetStaffPagedRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string? SortField { get; init; }
    public string? SortDir { get; init; }
}
