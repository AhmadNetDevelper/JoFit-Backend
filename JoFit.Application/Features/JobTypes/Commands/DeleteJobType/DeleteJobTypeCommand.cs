using JoFit.Domain.Common.Results;
using MediatR;

namespace JoFit.Application.Features.JobTypes.Commands.DeleteJobType;

public sealed record DeleteJobTypeCommand(long Id) : IRequest<Result<Deleted>>;
