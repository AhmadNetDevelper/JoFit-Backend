using JoFit.Application.Features.Courses.Dtos;
using JoFit.Domain.Common.Results;
using MediatR;

namespace JoFit.Application.Features.Courses.Commands.CreateCourse;

public sealed record CreateCourseCommand(
    string NameAr,
    string NameEn,
    bool IsActive = true) : IRequest<Result<CourseDto>>;
