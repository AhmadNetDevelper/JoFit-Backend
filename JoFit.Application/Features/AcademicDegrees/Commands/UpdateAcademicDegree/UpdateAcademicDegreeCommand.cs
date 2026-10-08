using JoFit.Application.Features.AcademicDegrees.Dtos;
using JoFit.Domain.Common.Results;
using MediatR;

namespace JoFit.Application.Features.AcademicDegrees.Commands.UpdateAcademicDegree;

public sealed record UpdateAcademicDegreeCommand(
    long Id,
    string NameAr,
    string NameEn,
    bool IsActive) : IRequest<Result<AcademicDegreeDto>>;
