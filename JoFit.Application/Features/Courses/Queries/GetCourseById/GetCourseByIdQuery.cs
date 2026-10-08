using JoFit.Application.Features.Courses.Dtos;
using JoFit.Domain.Common.Results;
using MediatR;

namespace JoFit.Application.Features.Courses.Queries.GetCourseById;

public sealed record GetCourseByIdQuery(long Id) : IRequest<Result<CourseDto>>;
