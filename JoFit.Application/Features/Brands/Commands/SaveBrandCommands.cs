using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.Brands.Dtos;
using JoFit.Domain.Brands;
using JoFit.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace JoFit.Application.Features.Brands.Commands;

public sealed record BrandInput(
    string Slug,
    string NameEn,
    string NameAr,
    string? LogoMediaId,
    string? LogoUrl,
    string? LogoAlt);

public sealed record CreateBrandCommand(BrandInput Input) : IRequest<Result<BrandDto>>;

public sealed record UpdateBrandCommand(long Id, BrandInput Input) : IRequest<Result<BrandDto>>;

public sealed record DeleteBrandCommand(long Id) : IRequest<Result<Deleted>>;

public class CreateBrandCommandHandler(IUnitOfWork unitOfWork, HybridCache cache)
    : IRequestHandler<CreateBrandCommand, Result<BrandDto>>
{
    public async Task<Result<BrandDto>> Handle(CreateBrandCommand request, CancellationToken ct)
    {
        var i = request.Input;
        var created = Brand.Create(i.Slug, i.NameEn, i.NameAr, i.LogoMediaId, i.LogoUrl, i.LogoAlt);
        if (created.IsError)
        {
            return created.Errors;
        }

        var repository = unitOfWork.GetRepository<Brand>();
        var slug = created.Value.Slug;
        if (await repository.GetTableNoTracking().AnyAsync(x => x.Slug == slug, ct))
        {
            return BrandErrors.SlugTaken;
        }

        await repository.AddAsync(created.Value, ct);
        await unitOfWork.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync(BrandCacheTags.Brands, ct);
        return BrandDto.From(created.Value);
    }
}

public class UpdateBrandCommandHandler(IUnitOfWork unitOfWork, HybridCache cache)
    : IRequestHandler<UpdateBrandCommand, Result<BrandDto>>
{
    public async Task<Result<BrandDto>> Handle(UpdateBrandCommand request, CancellationToken ct)
    {
        var repository = unitOfWork.GetRepository<Brand>();
        var entity = await repository.GetTableAsTracking().FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (entity is null)
        {
            return BrandErrors.NotFound;
        }

        var i = request.Input;
        var updated = entity.Update(i.Slug, i.NameEn, i.NameAr, i.LogoMediaId, i.LogoUrl, i.LogoAlt);
        if (updated.IsError)
        {
            return updated.Errors;
        }

        var slug = entity.Slug;
        if (await repository.GetTableNoTracking().AnyAsync(x => x.Slug == slug && x.Id != entity.Id, ct))
        {
            return BrandErrors.SlugTaken;
        }

        await unitOfWork.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync(BrandCacheTags.Brands, ct);
        return BrandDto.From(entity);
    }
}

public class DeleteBrandCommandHandler(IUnitOfWork unitOfWork, HybridCache cache)
    : IRequestHandler<DeleteBrandCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(DeleteBrandCommand request, CancellationToken ct)
    {
        var repository = unitOfWork.GetRepository<Brand>();
        var entity = await repository.GetTableAsTracking().FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (entity is null)
        {
            return BrandErrors.NotFound;
        }

        var products = await unitOfWork.GetRepository<JoFit.Domain.Products.Product>().GetTableNoTracking().CountAsync(p => p.BrandId == entity.Id, ct);
        if (products > 0)
        {
            return BrandErrors.InUse(products);
        }

        repository.DeleteAsync(entity);
        await unitOfWork.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync(BrandCacheTags.Brands, ct);
        return Result.Deleted;
    }
}
