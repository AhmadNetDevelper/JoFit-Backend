using FluentValidation;

namespace JoFit.Application.Features.AcademicDegrees.Queries.GetAcademicDegreesPaged;

public sealed class GetAcademicDegreesPagedQueryValidator : AbstractValidator<GetAcademicDegreesPagedQuery>
{
    public GetAcademicDegreesPagedQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
