using JoFit.Application.Features.AcademicDegrees.Dtos;
using JoFit.Domain.Common.Results;
using MediatR;

namespace JoFit.Application.Features.AcademicDegrees.Queries.GetAcademicDegreeById;

public sealed record GetAcademicDegreeByIdQuery(long Id) : IRequest<Result<AcademicDegreeDto>>;
