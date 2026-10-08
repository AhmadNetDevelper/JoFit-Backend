using JoFit.Domain.Common.Results;
using MediatR;

namespace JoFit.Application.Features.Countries.Commands.DeleteCountry;

public sealed record DeleteCountryCommand(long Id) : IRequest<Result<Deleted>>;
