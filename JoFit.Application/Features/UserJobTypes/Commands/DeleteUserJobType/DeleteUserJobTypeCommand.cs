using JoFit.Domain.Common.Results;
using MediatR;

namespace JoFit.Application.Features.UserJobTypes.Commands.DeleteUserJobType;

public sealed record DeleteUserJobTypeCommand(long Id) : IRequest<Result<Deleted>>;
