using JoFit.Domain.Common.Results;
using MediatR;

namespace JoFit.Application.Features.Cities.Commands.DeleteCity;

public sealed record DeleteCityCommand(long Id) : IRequest<Result<Deleted>>;
