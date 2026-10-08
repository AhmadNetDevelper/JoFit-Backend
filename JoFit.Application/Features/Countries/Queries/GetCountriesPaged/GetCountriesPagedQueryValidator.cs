using FluentValidation;

namespace JoFit.Application.Features.Countries.Queries.GetCountriesPaged;

public sealed class GetCountriesPagedQueryValidator : AbstractValidator<GetCountriesPagedQuery>
{
    public GetCountriesPagedQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
