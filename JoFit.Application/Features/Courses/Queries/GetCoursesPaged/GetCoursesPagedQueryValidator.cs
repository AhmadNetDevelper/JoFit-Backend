using FluentValidation;

namespace JoFit.Application.Features.Courses.Queries.GetCoursesPaged;

public sealed class GetCoursesPagedQueryValidator : AbstractValidator<GetCoursesPagedQuery>
{
    public GetCoursesPagedQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
