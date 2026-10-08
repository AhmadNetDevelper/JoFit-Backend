using JoFit.Application.Features.UniversityMajors.Dtos;
using JoFit.Domain.Common.Results;
using MediatR;

namespace JoFit.Application.Features.UniversityMajors.Commands.UpdateUniversityMajor;

public sealed record UpdateUniversityMajorCommand(
    long Id,
    string NameAr,
    string NameEn,
    bool IsActive) : IRequest<Result<UniversityMajorDto>>;
