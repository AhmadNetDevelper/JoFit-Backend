namespace JoFit.Contracts.Requests.Courses;

public sealed record GetCoursesPagedRequest(
    int Page = 1,
    int PageSize = 10,
    string? Search = null,
    bool? IsActive = null);
