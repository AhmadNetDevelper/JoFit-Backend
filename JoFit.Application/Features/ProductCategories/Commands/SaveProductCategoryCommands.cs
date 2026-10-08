using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.ProductCategories.Dtos;
using JoFit.Domain.ProductCategories;
using JoFit.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.ProductCategories.Commands;

public sealed record CreateProductCategoryCommand(ProductCategoryValues Values) : IRequest<Result<ProductCategoryDto>>;

public sealed record UpdateProductCategoryCommand(long Id, ProductCategoryValues Values) : IRequest<Result<ProductCategoryDto>>;

public sealed record DeleteProductCategoryCommand(long Id) : IRequest<Result<Deleted>>;

public class CreateProductCategoryCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CreateProductCategoryCommand, Result<ProductCategoryDto>>
{
    public async Task<Result<ProductCategoryDto>> Handle(CreateProductCategoryCommand request, CancellationToken ct)
    {
        var created = ProductCategory.Create(request.Values);
        if (created.IsError)
        {
            return created.Errors;
        }

        var entity = created.Value;
        var repository = unitOfWork.GetRepository<ProductCategory>();
        var slugValue = entity.Slug;
        if (slugValue is not null && await repository.GetTableNoTracking().AnyAsync(x => x.Slug == slugValue, ct))
        {
            return ProductCategoryErrors.SlugTaken;
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
        return ProductCategoryDto.From(entity);
    }

    private static async Task<Error?> CheckParentAsync(IGenericRepository<ProductCategory> repository, long parentId, long selfId, CancellationToken ct)
    {
        // Walk up from the proposed parent; meeting the record itself means a cycle.
        long? current = parentId;
        var guard = 0;
        while (current is long id && guard++ < 100)
        {
            if (id == selfId && selfId != 0)
            {
                return ProductCategoryErrors.ParentInvalid;
            }

            var parent = await repository.GetTableNoTracking().Where(x => x.Id == id).Select(x => new { x.Id, x.ParentId }).FirstOrDefaultAsync(ct);
            if (parent is null)
            {
                return guard == 1 ? ProductCategoryErrors.ParentMissing : null;
            }

            current = parent.ParentId;
        }

        return null;
    }
}

public class UpdateProductCategoryCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateProductCategoryCommand, Result<ProductCategoryDto>>
{
    public async Task<Result<ProductCategoryDto>> Handle(UpdateProductCategoryCommand request, CancellationToken ct)
    {
        var repository = unitOfWork.GetRepository<ProductCategory>();
        var entity = await repository.GetTableAsTracking().FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (entity is null)
        {
            return ProductCategoryErrors.NotFound;
        }

        var updated = entity.Update(request.Values);
        if (updated.IsError)
        {
            return updated.Errors;
        }
        var slugValue = entity.Slug;
        if (slugValue is not null && await repository.GetTableNoTracking().AnyAsync(x => x.Slug == slugValue && x.Id != entity.Id, ct))
        {
            return ProductCategoryErrors.SlugTaken;
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
        return ProductCategoryDto.From(entity);
    }

    private static async Task<Error?> CheckParentAsync(IGenericRepository<ProductCategory> repository, long parentId, long selfId, CancellationToken ct)
    {
        // Walk up from the proposed parent; meeting the record itself means a cycle.
        long? current = parentId;
        var guard = 0;
        while (current is long id && guard++ < 100)
        {
            if (id == selfId && selfId != 0)
            {
                return ProductCategoryErrors.ParentInvalid;
            }

            var parent = await repository.GetTableNoTracking().Where(x => x.Id == id).Select(x => new { x.Id, x.ParentId }).FirstOrDefaultAsync(ct);
            if (parent is null)
            {
                return guard == 1 ? ProductCategoryErrors.ParentMissing : null;
            }

            current = parent.ParentId;
        }

        return null;
    }
}

public class DeleteProductCategoryCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteProductCategoryCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(DeleteProductCategoryCommand request, CancellationToken ct)
    {
        var repository = unitOfWork.GetRepository<ProductCategory>();
        var entity = await repository.GetTableAsTracking().FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (entity is null)
        {
            return ProductCategoryErrors.NotFound;
        }

        var children = await repository.GetTableNoTracking().CountAsync(x => x.ParentId == entity.Id, ct);
        if (children > 0)
        {
            return ProductCategoryErrors.HasChildren(children);
        }

        var usedProductCategoryLink = await unitOfWork.GetDbContext().Set<JoFit.Domain.Products.ProductCategoryLink>().CountAsync(x => x.ProductCategoryId == entity.Id, ct);
        if (usedProductCategoryLink > 0)
        {
            return Error.Conflict("ProductCategory.InUse", $"This product category is used by {usedProductCategoryLink} product(s). Move them first.");
        }

        // Records that reference this one add their own in-use check here when they exist.
        repository.DeleteAsync(entity);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Deleted;
    }
}
