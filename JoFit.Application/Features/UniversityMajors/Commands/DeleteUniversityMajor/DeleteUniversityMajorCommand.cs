using JoFit.Domain.Common.Results;
using MediatR;

namespace JoFit.Application.Features.UniversityMajors.Commands.DeleteUniversityMajor;

public sealed record DeleteUniversityMajorCommand(long Id) : IRequest<Result<Deleted>>;
