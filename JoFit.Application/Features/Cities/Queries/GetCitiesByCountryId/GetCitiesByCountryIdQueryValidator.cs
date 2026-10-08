using FluentValidation;

namespace JoFit.Application.Features.Cities.Queries.GetCitiesByCountryId;

public sealed class GetCitiesByCountryIdQueryValidator : AbstractValidator<GetCitiesByCountryIdQuery>
{
    public GetCitiesByCountryIdQueryValidator()
    {
        RuleFor(x => x.CountryId).GreaterThan(0);
    }
}
