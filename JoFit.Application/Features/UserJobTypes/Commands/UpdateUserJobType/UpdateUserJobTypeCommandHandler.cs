using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.UserJobTypes.Dtos;
using JoFit.Domain.Common.Results;
using JoFit.Domain.UserJobTypes;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace JoFit.Application.Features.UserJobTypes.Commands.UpdateUserJobType;

public class UpdateUserJobTypeCommandHandler(IUnitOfWork unitOfWork, HybridCache cache)
    : IRequestHandler<UpdateUserJobTypeCommand, Result<UserJobTypeDto>>
{
    public async Task<Result<UserJobTypeDto>> Handle(UpdateUserJobTypeCommand request, CancellationToken ct)
    {
        var entity = await unitOfWork.GetRepository<UserJobType>()
            .GetTableAsTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, ct);

        if (entity is null)
        {
            return UserJobTypeErrors.UserJobTypeNotFound;
        }

        var updateResult = entity.Update(request.NameAr, request.NameEn, request.IsActive);
        if (updateResult.IsError)
        {
            return updateResult.Errors;
        }

        await unitOfWork.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync(UserJobTypeCacheTags.UserJobTypes, ct);

        return UserJobTypeDto.From(entity);
    }
}
