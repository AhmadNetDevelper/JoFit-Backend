using JoFit.Application.Features.UniversityMajors.Dtos;
using JoFit.Domain.Common.Results;
using MediatR;

namespace JoFit.Application.Features.UniversityMajors.Queries.GetUniversityMajorById;

public sealed record GetUniversityMajorByIdQuery(long Id) : IRequest<Result<UniversityMajorDto>>;
