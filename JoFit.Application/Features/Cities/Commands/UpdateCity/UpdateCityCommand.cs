using JoFit.Application.Features.Cities.Dtos;
using JoFit.Domain.Common.Results;
using MediatR;

namespace JoFit.Application.Features.Cities.Commands.UpdateCity;

public sealed record UpdateCityCommand(
    long Id,
    string NameAr,
    string NameEn,
    long CountryId,
    bool IsActive) : IRequest<Result<CityDto>>;
