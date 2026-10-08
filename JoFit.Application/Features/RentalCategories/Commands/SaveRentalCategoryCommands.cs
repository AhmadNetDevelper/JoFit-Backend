using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.RentalCategories.Dtos;
using JoFit.Domain.RentalCategories;
using JoFit.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.RentalCategories.Commands;

public sealed record CreateRentalCategoryCommand(RentalCategoryValues Values) : IRequest<Result<RentalCategoryDto>>;

public sealed record UpdateRentalCategoryCommand(long Id, RentalCategoryValues Values) : IRequest<Result<RentalCategoryDto>>;

public sealed record DeleteRentalCategoryCommand(long Id) : IRequest<Result<Deleted>>;

public class CreateRentalCategoryCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CreateRentalCategoryCommand, Result<RentalCategoryDto>>
{
    public async Task<Result<RentalCategoryDto>> Handle(CreateRentalCategoryCommand request, CancellationToken ct)
    {
        var created = RentalCategory.Create(request.Values);
        if (created.IsError)
        {
            return created.Errors;
        }

        var entity = created.Value;
        var repository = unitOfWork.GetRepository<RentalCategory>();
        var slugValue = entity.Slug;
        if (slugValue is not null && await repository.GetTableNoTracking().AnyAsync(x => x.Slug == slugValue, ct))
        {
            return RentalCategoryErrors.SlugTaken;
        }

        if (entity.ParentId is long parentId)
        {
            var parentError = await CheckParentAsync(repository, parentId, 0L, ct);
            if (parentError is not null)
            {
                return parentError.Value;
            }
        }

        await repository.AddAsync(entity, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return RentalCategoryDto.From(entity);
    }

    private static async Task<Error?> CheckParentAsync(IGenericRepository<RentalCategory> repository, long parentId, long selfId, CancellationToken ct)
    {
        // Walk up from the proposed parent; meeting the record itself means a cycle.
        long? current = parentId;
        var guard = 0;
        while (current is long id && guard++ < 100)
        {
            if (id == selfId && selfId != 0)
            {
                return RentalCategoryErrors.ParentInvalid;
            }

            var parent = await repository.GetTableNoTracking().Where(x => x.Id == id).Select(x => new { x.Id, x.ParentId }).FirstOrDefaultAsync(ct);
            if (parent is null)
            {
                return guard == 1 ? RentalCategoryErrors.ParentMissing : null;
            }

            current = parent.ParentId;
        }

        return null;
    }
}

public class UpdateRentalCategoryCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateRentalCategoryCommand, Result<RentalCategoryDto>>
{
    public async Task<Result<RentalCategoryDto>> Handle(UpdateRentalCategoryCommand request, CancellationToken ct)
    {
        var repository = unitOfWork.GetRepository<RentalCategory>();
        var entity = await repository.GetTableAsTracking().FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (entity is null)
        {
            return RentalCategoryErrors.NotFound;
        }

        var updated = entity.Update(request.Values);
        if (updated.IsError)
        {
            return updated.Errors;
        }
        var slugValue = entity.Slug;
        if (slugValue is not null && await repository.GetTableNoTracking().AnyAsync(x => x.Slug == slugValue && x.Id != entity.Id, ct))
        {
            return RentalCategoryErrors.SlugTaken;
        }

        if (entity.ParentId is long parentId)
        {
            var parentError = await CheckParentAsync(repository, parentId, entity.Id, ct);
            if (parentError is not null)
            {
                return parentError.Value;
            }
        }

        await unitOfWork.SaveChangesAsync(ct);
        return RentalCategoryDto.From(entity);
    }

    private static async Task<Error?> CheckParentAsync(IGenericRepository<RentalCategory> repository, long parentId, long selfId, CancellationToken ct)
    {
        // Walk up from the proposed parent; meeting the record itself means a cycle.
        long? current = parentId;
        var guard = 0;
        while (current is long id && guard++ < 100)
        {
            if (id == selfId && selfId != 0)
            {
                return RentalCategoryErrors.ParentInvalid;
            }

            var parent = await repository.GetTableNoTracking().Where(x => x.Id == id).Select(x => new { x.Id, x.ParentId }).FirstOrDefaultAsync(ct);
            if (parent is null)
            {
                return guard == 1 ? RentalCategoryErrors.ParentMissing : null;
            }

            current = parent.ParentId;
        }

        return null;
    }
}

public class DeleteRentalCategoryCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteRentalCategoryCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(DeleteRentalCategoryCommand request, CancellationToken ct)
    {
        var repository = unitOfWork.GetRepository<RentalCategory>();
        var entity = await repository.GetTableAsTracking().FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (entity is null)
        {
            return RentalCategoryErrors.NotFound;
        }

        var children = await repository.GetTableNoTracking().CountAsync(x => x.ParentId == entity.Id, ct);
        if (children > 0)
        {
            return RentalCategoryErrors.HasChildren(children);
        }

        var usedRentalItemCategoryLink = await unitOfWork.GetDbContext().Set<JoFit.Domain.Rental.RentalItemCategoryLink>().CountAsync(x => x.RentalCategoryId == entity.Id, ct);
        if (usedRentalItemCategoryLink > 0)
        {
            return Error.Conflict("RentalCategory.InUse", $"This rental category is used by {usedRentalItemCategoryLink} rental item(s). Move them first.");
        }

        // Records that reference this one add their own in-use check here when they exist.
        repository.DeleteAsync(entity);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Deleted;
    }
}
