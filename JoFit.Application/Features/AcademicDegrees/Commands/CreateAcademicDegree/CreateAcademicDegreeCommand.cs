using JoFit.Application.Features.AcademicDegrees.Dtos;
using JoFit.Domain.Common.Results;
using MediatR;

namespace JoFit.Application.Features.AcademicDegrees.Commands.CreateAcademicDegree;

public sealed record CreateAcademicDegreeCommand(
    string NameAr,
    string NameEn,
    bool IsActive = true) : IRequest<Result<AcademicDegreeDto>>;
