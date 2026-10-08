using FluentValidation;

namespace JoFit.Application.Features.Cities.Queries.GetCitiesPaged;

public sealed class GetCitiesPagedQueryValidator : AbstractValidator<GetCitiesPagedQuery>
{
    public GetCitiesPagedQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
