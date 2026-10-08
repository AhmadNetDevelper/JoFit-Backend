using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.UserJobTypes.Dtos;
using JoFit.Domain.Common.Results;
using JoFit.Domain.UserJobTypes;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace JoFit.Application.Features.UserJobTypes.Commands.CreateUserJobType;

public class CreateUserJobTypeCommandHandler(IUnitOfWork unitOfWork, HybridCache cache)
    : IRequestHandler<CreateUserJobTypeCommand, Result<UserJobTypeDto>>
{
    public async Task<Result<UserJobTypeDto>> Handle(CreateUserJobTypeCommand request, CancellationToken ct)
    {
        var createResult = UserJobType.Create(request.NameAr, request.NameEn, request.IsActive);
        if (createResult.IsError)
        {
            return createResult.Errors;
        }

        var entity = createResult.Value;

        await unitOfWork.GetRepository<UserJobType>().AddAsync(entity, ct);
        await unitOfWork.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync(UserJobTypeCacheTags.UserJobTypes, ct);

        return UserJobTypeDto.From(entity);
    }
}
