using JoFit.Application.Features.Countries.Dtos;
using JoFit.Domain.Common.Results;
using MediatR;

namespace JoFit.Application.Features.Countries.Commands.UpdateCountry;

public sealed record UpdateCountryCommand(
    long Id,
    string NameAr,
    string NameEn,
    bool IsActive) : IRequest<Result<CountryDto>>;
