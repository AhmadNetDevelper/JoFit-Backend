using JoFit.Application.Features.Countries.Dtos;
using JoFit.Domain.Common.Results;
using MediatR;

namespace JoFit.Application.Features.Countries.Commands.CreateCountry;

public sealed record CreateCountryCommand(
    string NameAr,
    string NameEn,
    bool IsActive = true) : IRequest<Result<CountryDto>>;
