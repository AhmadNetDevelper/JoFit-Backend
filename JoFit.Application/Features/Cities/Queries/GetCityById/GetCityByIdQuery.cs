using JoFit.Application.Features.Cities.Dtos;
using JoFit.Domain.Common.Results;
using MediatR;

namespace JoFit.Application.Features.Cities.Queries.GetCityById;

public sealed record GetCityByIdQuery(long Id) : IRequest<Result<CityDto>>;
