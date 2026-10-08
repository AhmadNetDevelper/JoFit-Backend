using JoFit.Domain.Common.Results;
using MediatR;

namespace JoFit.Application.Features.Courses.Commands.DeleteCourse;

public sealed record DeleteCourseCommand(long Id) : IRequest<Result<Deleted>>;
