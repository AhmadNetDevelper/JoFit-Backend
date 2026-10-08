using JoFit.Domain.Common.Results;
using MediatR;

namespace JoFit.Application.Features.AcademicDegrees.Commands.DeleteAcademicDegree;

public sealed record DeleteAcademicDegreeCommand(long Id) : IRequest<Result<Deleted>>;
