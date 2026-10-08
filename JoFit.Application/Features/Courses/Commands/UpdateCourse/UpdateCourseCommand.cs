using JoFit.Application.Features.Courses.Dtos;
using JoFit.Domain.Common.Results;
using MediatR;

namespace JoFit.Application.Features.Courses.Commands.UpdateCourse;

public sealed record UpdateCourseCommand(
    long Id,
    string NameAr,
    string NameEn,
    bool IsActive) : IRequest<Result<CourseDto>>;
