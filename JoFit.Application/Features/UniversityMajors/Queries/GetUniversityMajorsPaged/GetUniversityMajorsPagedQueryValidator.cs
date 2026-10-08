using FluentValidation;

namespace JoFit.Application.Features.UniversityMajors.Queries.GetUniversityMajorsPaged;

public sealed class GetUniversityMajorsPagedQueryValidator : AbstractValidator<GetUniversityMajorsPagedQuery>
{
    public GetUniversityMajorsPagedQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
