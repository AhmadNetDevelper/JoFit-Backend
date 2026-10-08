using JoFit.Application.Common.Interfaces;
using JoFit.Domain.Common.Results;
using JoFit.Domain.UserJobTypes;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace JoFit.Application.Features.UserJobTypes.Commands.DeleteUserJobType;

public class DeleteUserJobTypeCommandHandler(IUnitOfWork unitOfWork, HybridCache cache)
    : IRequestHandler<DeleteUserJobTypeCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(DeleteUserJobTypeCommand request, CancellationToken ct)
    {
        var repository = unitOfWork.GetRepository<UserJobType>();

        var entity = await repository.GetTableAsTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, ct);

        if (entity is null)
        {
            return UserJobTypeErrors.UserJobTypeNotFound;
        }

        // Soft delete: flags IsDeleted and keeps the row
        repository.DeleteAsync(entity);
        await unitOfWork.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync(UserJobTypeCacheTags.UserJobTypes, ct);

        return Result.Deleted;
    }
}
