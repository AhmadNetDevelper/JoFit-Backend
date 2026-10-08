using JoFit.Domain.Common.Results;
using MediatR;

namespace JoFit.Application.Features.JobStatuses.Commands.DeleteJobStatus;

public sealed record DeleteJobStatusCommand(long Id) : IRequest<Result<Deleted>>;
