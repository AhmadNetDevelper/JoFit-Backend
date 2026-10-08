using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Services;
using JoFit.Application.Common.Utilities;
using JoFit.Domain.Brands;
using JoFit.Domain.Common.Results;
using JoFit.Domain.ProductCategories;
using JoFit.Domain.Products;
using JoFit.Domain.Sports;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.Products;

public sealed record ProductInput(
    ProductDetails Details,
    IReadOnlyList<long> CategoryIds,
    IReadOnlyList<long> SportIds,
    IReadOnlyList<GalleryItem>? Media);

/// <summary>The first variant a new product starts with. The only place stock is set to an absolute value.</summary>
public sealed record NewVariantInput(string? Sku, long PriceAmount, int StockOnHand);

public sealed record CreateProductCommand(ProductInput Input, NewVariantInput Variant) : IRequest<Result<AdminProductDto>>;

public sealed record UpdateProductCommand(long Id, ProductInput Input) : IRequest<Result<AdminProductDto>>;

public sealed record SetProductStatusCommand(long Id, string Status) : IRequest<Result<AdminProductDto>>;

public sealed record AdjustStockCommand(long ProductId, long VariantId, int Delta, string? Reason, long? UserId)
    : IRequest<Result<AdminProductDto>>;

public sealed record DeleteProductCommand(long Id) : IRequest<Result<Deleted>>;

internal static class ProductSaving
{
    public const string OwnerType = "product";
    public const string MediaField = "media";

    /// <summary>The brand, every category and every sport must exist.</summary>
    public static async Task<List<Error>> CheckReferencesAsync(IUnitOfWork unitOfWork, ProductInput input, CancellationToken ct)
    {
        var errors = new List<Error>();

        if (input.Details.BrandId > 0
            && !await unitOfWork.GetRepository<Brand>().GetTableNoTracking().AnyAsync(b => b.Id == input.Details.BrandId, ct))
        {
            errors.Add(ProductErrors.Field("brandId", "That brand does not exist."));
        }

        var categoryIds = input.CategoryIds.Distinct().ToList();
        if (categoryIds.Count == 0)
        {
            errors.Add(ProductErrors.Field("categoryIds", "Choose at least one category."));
        }
        else if (await unitOfWork.GetRepository<ProductCategory>().GetTableNoTracking().CountAsync(c => categoryIds.Contains(c.Id), ct) != categoryIds.Count)
        {
            errors.Add(ProductErrors.Field("categoryIds", "One of the categories does not exist."));
        }

        var sportIds = input.SportIds.Distinct().ToList();
        if (sportIds.Count > 0
            && await unitOfWork.GetRepository<Sport>().GetTableNoTracking().CountAsync(s => sportIds.Contains(s.Id), ct) != sportIds.Count)
        {
            errors.Add(ProductErrors.Field("sportIds", "One of the sports does not exist."));
        }

        return errors;
    }

    public static async Task ReplaceLinksAsync(IUnitOfWork unitOfWork, long productId, ProductInput input, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        db.RemoveRange(await db.Set<ProductCategoryLink>().Where(l => l.ProductId == productId).ToListAsync(ct));
        db.RemoveRange(await db.Set<ProductSportLink>().Where(l => l.ProductId == productId).ToListAsync(ct));
        db.AddRange(input.CategoryIds.Distinct().Select(id => new ProductCategoryLink { ProductId = productId, ProductCategoryId = id }));
        db.AddRange(input.SportIds.Distinct().Select(id => new ProductSportLink { ProductId = productId, SportId = id }));
    }

    public static async Task<AdminProductDto> ToDtoAsync(IUnitOfWork unitOfWork, long productId, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var product = await db.Set<Product>().AsNoTracking().Include(p => p.Variants).FirstAsync(p => p.Id == productId, ct);
        var categoryIds = await db.Set<ProductCategoryLink>().AsNoTracking().Where(l => l.ProductId == productId).Select(l => l.ProductCategoryId).OrderBy(x => x).ToListAsync(ct);
        var sportIds = await db.Set<ProductSportLink>().AsNoTracking().Where(l => l.ProductId == productId).Select(l => l.SportId).OrderBy(x => x).ToListAsync(ct);
        var media = await GalleryService.LoadAsync(unitOfWork, OwnerType, MediaField, [productId], ct);
        return AdminProductDto.From(product, categoryIds, sportIds, media.GetValueOrDefault(productId) ?? []);
    }

    public static async Task<Result<Updated>> SaveMediaAsync(
        IUnitOfWork unitOfWork, IUrlService urlService, long productId, IReadOnlyList<GalleryItem>? media, CancellationToken ct)
        => media is null
            ? Result.Updated
            : await GalleryService.SyncAsync(unitOfWork, urlService, OwnerType, productId, MediaField, media, ct);
}

public class CreateProductCommandHandler(IUnitOfWork unitOfWork, IUrlService urlService)
    : IRequestHandler<CreateProductCommand, Result<AdminProductDto>>
{
    public async Task<Result<AdminProductDto>> Handle(CreateProductCommand request, CancellationToken ct)
    {
        var input = request.Input;
        var variant = request.Variant;
        var errors = await ProductSaving.CheckReferencesAsync(unitOfWork, input, ct);

        var sku = variant.Sku?.Trim() ?? string.Empty;
        if (sku.Length == 0) errors.Add(ProductErrors.Field("variant.sku", "A SKU is required."));
        if (variant.PriceAmount <= 0) errors.Add(ProductErrors.Field("variant.price", "The price must be more than 0."));
        if (variant.StockOnHand < 0) errors.Add(ProductErrors.Field("variant.stockOnHand", "Stock cannot be negative."));
        if (sku.Length > 0 && await unitOfWork.GetRepository<ProductVariant>().GetTableNoTracking().AnyAsync(v => v.SkuKey == sku.ToUpper(), ct))
        {
            errors.Add(ProductErrors.SkuTaken);
        }

        var slugs = (await unitOfWork.GetRepository<Product>().GetTableNoTracking().Select(p => p.Slug).ToListAsync(ct)).ToHashSet();
        var baseSlug = Slugger.Slugify(input.Details.TitleEn);
        var slug = Slugger.Unique(baseSlug.Length == 0 ? "product" : baseSlug, slugs.Contains);

        var created = Product.Create(input.Details, slug);
        if (created.IsError) errors.AddRange(created.Errors);
        if (errors.Count > 0)
        {
            return errors;
        }

        var product = created.Value;
        product.AddVariant(sku, variant.PriceAmount, variant.StockOnHand);

        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            await unitOfWork.GetRepository<Product>().AddAsync(product, ct);
            await unitOfWork.SaveChangesAsync(ct);
            await ProductSaving.ReplaceLinksAsync(unitOfWork, product.Id, input, ct);

            var gallery = await ProductSaving.SaveMediaAsync(unitOfWork, urlService, product.Id, input.Media, ct);
            if (gallery.IsError)
            {
                await unitOfWork.RollbackAsync(ct);
                return gallery.Errors;
            }

            await unitOfWork.SaveChangesAsync(ct);
            await unitOfWork.CommitTransactionAsync(ct);
        }
        catch
        {
            await unitOfWork.RollbackAsync(ct);
            throw;
        }

        return await ProductSaving.ToDtoAsync(unitOfWork, product.Id, ct);
    }
}

public class UpdateProductCommandHandler(IUnitOfWork unitOfWork, IUrlService urlService)
    : IRequestHandler<UpdateProductCommand, Result<AdminProductDto>>
{
    public async Task<Result<AdminProductDto>> Handle(UpdateProductCommand request, CancellationToken ct)
    {
        var product = await unitOfWork.GetRepository<Product>().GetTableAsTracking()
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == request.Id, ct);
        if (product is null)
        {
            return ProductErrors.NotFound;
        }

        var errors = await ProductSaving.CheckReferencesAsync(unitOfWork, request.Input, ct);
        var applied = product.Apply(request.Input.Details);
        if (applied.IsError) errors.AddRange(applied.Errors);
        if (errors.Count > 0)
        {
            return errors;
        }

        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            await ProductSaving.ReplaceLinksAsync(unitOfWork, product.Id, request.Input, ct);
            var gallery = await ProductSaving.SaveMediaAsync(unitOfWork, urlService, product.Id, request.Input.Media, ct);
            if (gallery.IsError)
            {
                await unitOfWork.RollbackAsync(ct);
                return gallery.Errors;
            }

            await unitOfWork.SaveChangesAsync(ct);
            await unitOfWork.CommitTransactionAsync(ct);
        }
        catch
        {
            await unitOfWork.RollbackAsync(ct);
            throw;
        }

        return await ProductSaving.ToDtoAsync(unitOfWork, product.Id, ct);
    }
}

public class SetProductStatusCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<SetProductStatusCommand, Result<AdminProductDto>>
{
    public async Task<Result<AdminProductDto>> Handle(SetProductStatusCommand request, CancellationToken ct)
    {
        var product = await unitOfWork.GetRepository<Product>().GetTableAsTracking().FirstOrDefaultAsync(p => p.Id == request.Id, ct);
        if (product is null)
        {
            return ProductErrors.NotFound;
        }

        var result = product.SetStatus(request.Status);
        if (result.IsError)
        {
            return result.Errors;
        }

        await unitOfWork.SaveChangesAsync(ct);
        return await ProductSaving.ToDtoAsync(unitOfWork, product.Id, ct);
    }
}

public class AdjustStockCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<AdjustStockCommand, Result<AdminProductDto>>
{
    public async Task<Result<AdminProductDto>> Handle(AdjustStockCommand request, CancellationToken ct)
    {
        var product = await unitOfWork.GetRepository<Product>().GetTableAsTracking()
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, ct);
        if (product is null)
        {
            return ProductErrors.NotFound;
        }

        var variant = product.Variants.FirstOrDefault(v => v.Id == request.VariantId);
        if (variant is null)
        {
            return ProductErrors.VariantNotFound;
        }

        var reason = request.Reason?.Trim() ?? string.Empty;
        if (reason.Length == 0)
        {
            return ProductErrors.Field("reason", "Say why the stock changed.");
        }

        // One atomic statement: the delta is added to whatever is stored right now, and never goes below 0,
        // so two staff adjusting at the same moment add up instead of overwriting each other.
        var delta = request.Delta;
        var variantId = variant.Id;
        var db = unitOfWork.GetDbContext();
        var changed = await db.Set<ProductVariant>()
            .Where(v => v.Id == variantId && v.StockOnHand + delta >= 0)
            .ExecuteUpdateAsync(s => s.SetProperty(v => v.StockOnHand, v => v.StockOnHand + delta), ct);
        if (changed == 0)
        {
            return ProductErrors.Field("delta", "That would take the stock below 0.");
        }

        var stockAfter = await db.Set<ProductVariant>().AsNoTracking().Where(v => v.Id == variantId).Select(v => v.StockOnHand).FirstAsync(ct);
        await unitOfWork.GetRepository<StockAdjustment>().AddAsync(
            StockAdjustment.Create(variantId, delta, reason.Length > 300 ? reason[..300] : reason, request.UserId, stockAfter), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return await ProductSaving.ToDtoAsync(unitOfWork, product.Id, ct);
    }
}

public class DeleteProductCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteProductCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(DeleteProductCommand request, CancellationToken ct)
    {
        var repository = unitOfWork.GetRepository<Product>();
        var product = await repository.GetTableAsTracking().FirstOrDefaultAsync(p => p.Id == request.Id, ct);
        if (product is null)
        {
            return ProductErrors.NotFound;
        }

        // Once orders exist, a product that is on any order answers ProductErrors.InUse (archive it instead).
        repository.DeleteAsync(product);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Deleted;
    }
}
