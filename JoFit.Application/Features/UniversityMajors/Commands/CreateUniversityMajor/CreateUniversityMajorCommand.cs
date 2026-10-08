using JoFit.Application.Features.UniversityMajors.Dtos;
using JoFit.Domain.Common.Results;
using MediatR;

namespace JoFit.Application.Features.UniversityMajors.Commands.CreateUniversityMajor;

public sealed record CreateUniversityMajorCommand(
    string NameAr,
    string NameEn,
    bool IsActive = true) : IRequest<Result<UniversityMajorDto>>;
