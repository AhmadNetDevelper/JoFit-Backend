using JoFit.Application.Features.Countries.Dtos;
using JoFit.Domain.Common.Results;
using MediatR;

namespace JoFit.Application.Features.Countries.Queries.GetCountryById;

public sealed record GetCountryByIdQuery(long Id) : IRequest<Result<CountryDto>>;
