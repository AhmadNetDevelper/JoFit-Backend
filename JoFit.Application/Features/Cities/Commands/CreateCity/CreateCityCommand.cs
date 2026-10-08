using JoFit.Application.Features.Cities.Dtos;
using JoFit.Domain.Common.Results;
using MediatR;

namespace JoFit.Application.Features.Cities.Commands.CreateCity;

public sealed record CreateCityCommand(
    string NameAr,
    string NameEn,
    long CountryId,
    bool IsActive = true) : IRequest<Result<CityDto>>;
