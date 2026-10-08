using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.UserJobTypes.Dtos;
using JoFit.Domain.Common.Results;
using JoFit.Domain.UserJobTypes;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.UserJobTypes.Queries.GetUserJobTypeById;

public class GetUserJobTypeByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetUserJobTypeByIdQuery, Result<UserJobTypeDto>>
{
    public async Task<Result<UserJobTypeDto>> Handle(GetUserJobTypeByIdQuery request, CancellationToken ct)
    {
        var entity = await unitOfWork.GetRepository<UserJobType>()
            .GetTableNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, ct);

        if (entity is null)
        {
            return UserJobTypeErrors.UserJobTypeNotFound;
        }

        return UserJobTypeDto.From(entity);
    }
}
