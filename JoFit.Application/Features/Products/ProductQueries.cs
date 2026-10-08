using JoFit.Application.Common.Extensions;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Common.Services;
using JoFit.Domain.Brands;
using JoFit.Domain.Common.Results;
using JoFit.Domain.ProductCategories;
using JoFit.Domain.Products;
using JoFit.Domain.Sports;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.Products;

// ---- admin ----------------------------------------------------------------

public sealed record GetAdminProductsQuery(
    int Page,
    int PageSize,
    string? Search,
    IReadOnlyList<string>? Statuses,
    IReadOnlyList<long>? CategoryIds,
    IReadOnlyList<long>? BrandIds,
    IReadOnlyList<long>? SportIds,
    IReadOnlyList<string>? StockStatuses,
    bool LowStockOnly,
    string? SortField,
    string? SortDir) : IRequest<Result<PagedResult<AdminProductDto>>>;

public sealed record GetAdminProductQuery(long Id) : IRequest<Result<AdminProductDto>>;

public class GetAdminProductsQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAdminProductsQuery, Result<PagedResult<AdminProductDto>>>
{
    public async Task<Result<PagedResult<AdminProductDto>>> Handle(GetAdminProductsQuery request, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var query = db.Set<Product>().AsNoTracking().AsQueryable();

        if (request.Statuses is { Count: > 0 })
        {
            var statuses = request.Statuses;
            query = query.Where(p => statuses.Contains(p.Status));
        }

        if (request.BrandIds is { Count: > 0 })
        {
            var brandIds = request.BrandIds;
            query = query.Where(p => brandIds.Contains(p.BrandId));
        }

        if (request.CategoryIds is { Count: > 0 })
        {
            var categoryIds = request.CategoryIds;
            query = query.Where(p => db.Set<ProductCategoryLink>().Any(l => l.ProductId == p.Id && categoryIds.Contains(l.ProductCategoryId)));
        }

        if (request.SportIds is { Count: > 0 })
        {
            var sportIds = request.SportIds;
            query = query.Where(p => db.Set<ProductSportLink>().Any(l => l.ProductId == p.Id && sportIds.Contains(l.SportId)));
        }

        // lowStockOnly=false means "no filter". Low means any variant at or under its threshold (inclusive).
        if (request.LowStockOnly)
        {
            query = query.Where(p => p.Variants.Any(v => v.StockOnHand <= v.LowStockThreshold));
        }

        if (request.StockStatuses is { Count: > 0 })
        {
            var wantOut = request.StockStatuses.Contains("out-of-stock");
            var wantLow = request.StockStatuses.Contains("low-stock");
            var wantIn = request.StockStatuses.Contains("in-stock");
            query = query.Where(p =>
                (wantOut && p.Variants.Sum(v => v.StockOnHand) <= 0)
                || (wantLow && p.Variants.Sum(v => v.StockOnHand) > 0 && p.Variants.Any(v => v.StockOnHand <= v.LowStockThreshold))
                || (wantIn && p.Variants.Sum(v => v.StockOnHand) > 0 && !p.Variants.Any(v => v.StockOnHand <= v.LowStockThreshold)));
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(p => p.TitleEn.ToLower().Contains(term) || p.TitleAr.ToLower().Contains(term)
                                     || p.Slug.Contains(term) || p.Variants.Any(v => v.SkuKey.ToLower().Contains(term)));
        }

        var descending = string.Equals(request.SortDir, "desc", StringComparison.OrdinalIgnoreCase);
        query = request.SortField?.ToLowerInvariant() switch
        {
            "status" => descending ? query.OrderByDescending(p => p.Status) : query.OrderBy(p => p.Status),
            "stock" => descending ? query.OrderByDescending(p => p.Variants.Sum(v => v.StockOnHand)) : query.OrderBy(p => p.Variants.Sum(v => v.StockOnHand)),
            "price" => descending ? query.OrderByDescending(p => p.Variants.Min(v => v.PriceAmount)) : query.OrderBy(p => p.Variants.Min(v => v.PriceAmount)),
            "updatedat" => descending
                ? query.OrderByDescending(p => p.LastModifiedUtc ?? p.CreatedAtUtc)
                : query.OrderBy(p => p.LastModifiedUtc ?? p.CreatedAtUtc),
            "title" => descending ? query.OrderByDescending(p => p.TitleEn) : query.OrderBy(p => p.TitleEn),
            _ => query.OrderBy(p => p.TitleEn)
        };

        var page = await query.ToPagedResultAsync(request.Page, request.PageSize, ct);
        var ids = page.Items.Select(p => p.Id).ToList();

        var products = await db.Set<Product>().AsNoTracking().Include(p => p.Variants).Where(p => ids.Contains(p.Id)).ToListAsync(ct);
        var categoryLinks = await db.Set<ProductCategoryLink>().AsNoTracking().Where(l => ids.Contains(l.ProductId)).ToListAsync(ct);
        var sportLinks = await db.Set<ProductSportLink>().AsNoTracking().Where(l => ids.Contains(l.ProductId)).ToListAsync(ct);
        var media = await GalleryService.LoadAsync(unitOfWork, "product", "media", ids, ct);

        var items = ids.Select(id => products.First(p => p.Id == id)).Select(p => AdminProductDto.From(
            p,
            categoryLinks.Where(l => l.ProductId == p.Id).Select(l => l.ProductCategoryId).OrderBy(x => x).ToList(),
            sportLinks.Where(l => l.ProductId == p.Id).Select(l => l.SportId).OrderBy(x => x).ToList(),
            media.GetValueOrDefault(p.Id) ?? [])).ToList();
        return new PagedResult<AdminProductDto>(items, page.Page, page.PageSize, page.TotalCount);
    }
}

public class GetAdminProductQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAdminProductQuery, Result<AdminProductDto>>
{
    public async Task<Result<AdminProductDto>> Handle(GetAdminProductQuery request, CancellationToken ct)
    {
        var exists = await unitOfWork.GetRepository<Product>().GetTableNoTracking().AnyAsync(p => p.Id == request.Id, ct);
        return exists ? await ProductSaving.ToDtoAsync(unitOfWork, request.Id, ct) : ProductErrors.NotFound;
    }
}

// ---- storefront -----------------------------------------------------------

public sealed record GetPublicProductsQuery(
    string Language,
    int Page,
    int PageSize,
    string? Search,
    IReadOnlyList<string>? CategorySlugs,
    IReadOnlyList<string>? BrandSlugs,
    IReadOnlyList<string>? SportSlugs,
    IReadOnlyList<string>? StockStatuses,
    long? PriceMin,
    long? PriceMax,
    string? Sort) : IRequest<Result<PagedResult<PublicProductDto>>>;

public sealed record GetPublicProductBySlugQuery(string Slug, string Language) : IRequest<Result<PublicProductDto>>;

public sealed record GetRelatedProductsQuery(long ProductId, string Language, int Limit) : IRequest<Result<List<PublicProductDto>>>;

public class GetPublicProductsQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localization)
    : IRequestHandler<GetPublicProductsQuery, Result<PagedResult<PublicProductDto>>>
{
    public async Task<Result<PagedResult<PublicProductDto>>> Handle(GetPublicProductsQuery request, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var arabic = localization.NormalizeLanguage(request.Language) == "ar";
        var query = db.Set<Product>().AsNoTracking().Where(p => p.Status == ProductStatuses.Published);

        if (request.CategorySlugs is { Count: > 0 })
        {
            var slugs = request.CategorySlugs;
            query = query.Where(p => db.Set<ProductCategoryLink>().Any(l => l.ProductId == p.Id
                && db.Set<ProductCategory>().Any(c => c.Id == l.ProductCategoryId && c.Slug != null && slugs.Contains(c.Slug))));
        }

        if (request.BrandSlugs is { Count: > 0 })
        {
            var slugs = request.BrandSlugs;
            query = query.Where(p => db.Set<Brand>().Any(b => b.Id == p.BrandId && slugs.Contains(b.Slug)));
        }

        if (request.SportSlugs is { Count: > 0 })
        {
            var slugs = request.SportSlugs;
            query = query.Where(p => db.Set<ProductSportLink>().Any(l => l.ProductId == p.Id
                && db.Set<Sport>().Any(s => s.Id == l.SportId && s.Slug != null && slugs.Contains(s.Slug))));
        }

        if (request.StockStatuses is { Count: > 0 })
        {
            var wantOut = request.StockStatuses.Contains("out-of-stock");
            var wantLow = request.StockStatuses.Contains("low-stock");
            var wantIn = request.StockStatuses.Contains("in-stock");
            query = query.Where(p =>
                (wantOut && !p.Variants.Any(v => v.IsActive && v.StockOnHand > 0))
                || (wantLow && p.Variants.Any(v => v.IsActive && v.StockOnHand > 0) && p.Variants.Any(v => v.IsActive && v.StockOnHand <= v.LowStockThreshold))
                || (wantIn && p.Variants.Any(v => v.IsActive && v.StockOnHand > 0) && !p.Variants.Any(v => v.IsActive && v.StockOnHand <= v.LowStockThreshold)));
        }

        // The price filters compare the lowest variant price.
        if (request.PriceMin is long min) query = query.Where(p => p.Variants.Where(v => v.IsActive).Min(v => (long?)v.PriceAmount) >= min);
        if (request.PriceMax is long max) query = query.Where(p => p.Variants.Where(v => v.IsActive).Min(v => (long?)v.PriceAmount) <= max);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = arabic
                ? query.Where(p => p.TitleAr.ToLower().Contains(term) || p.SummaryAr.ToLower().Contains(term) || (p.DescriptionAr != null && p.DescriptionAr.ToLower().Contains(term)))
                : query.Where(p => p.TitleEn.ToLower().Contains(term) || p.SummaryEn.ToLower().Contains(term) || (p.DescriptionEn != null && p.DescriptionEn.ToLower().Contains(term)));
        }

        query = request.Sort switch
        {
            "price-asc" => query.OrderBy(p => p.Variants.Min(v => v.PriceAmount)).ThenBy(p => p.Id),
            "price-desc" => query.OrderByDescending(p => p.Variants.Min(v => v.PriceAmount)).ThenBy(p => p.Id),
            "title" => arabic ? query.OrderBy(p => p.TitleAr) : query.OrderBy(p => p.TitleEn),
            _ => query.OrderByDescending(p => p.PublishedAtUtc).ThenBy(p => p.Id)
        };

        var page = await query.ToPagedResultAsync(request.Page, request.PageSize, ct, PagingExtensions.PublicMaxPageSize);
        var items = await PublicProductMapper.MapAsync(unitOfWork, localization, request.Language, page.Items.Select(p => p.Id).ToList(), ct);
        return new PagedResult<PublicProductDto>(items, page.Page, page.PageSize, page.TotalCount);
    }
}

public class GetPublicProductBySlugQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localization)
    : IRequestHandler<GetPublicProductBySlugQuery, Result<PublicProductDto>>
{
    public async Task<Result<PublicProductDto>> Handle(GetPublicProductBySlugQuery request, CancellationToken ct)
    {
        // Drafts and archived products answer not-found.
        var id = await unitOfWork.GetRepository<Product>().GetTableNoTracking()
            .Where(p => p.Slug == request.Slug && p.Status == ProductStatuses.Published)
            .Select(p => (long?)p.Id).FirstOrDefaultAsync(ct);
        if (id is null)
        {
            return ProductErrors.NotFound;
        }

        return (await PublicProductMapper.MapAsync(unitOfWork, localization, request.Language, [id.Value], ct))[0];
    }
}

public class GetRelatedProductsQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localization)
    : IRequestHandler<GetRelatedProductsQuery, Result<List<PublicProductDto>>>
{
    public async Task<Result<List<PublicProductDto>>> Handle(GetRelatedProductsQuery request, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var sportIds = await db.Set<ProductSportLink>().AsNoTracking().Where(l => l.ProductId == request.ProductId).Select(l => l.SportId).ToListAsync(ct);

        // Related means sharing a sport. An unknown product simply has no related ones.
        var ids = sportIds.Count == 0
            ? []
            : await db.Set<Product>().AsNoTracking()
                .Where(p => p.Id != request.ProductId && p.Status == ProductStatuses.Published
                            && db.Set<ProductSportLink>().Any(l => l.ProductId == p.Id && sportIds.Contains(l.SportId)))
                .OrderByDescending(p => p.PublishedAtUtc).ThenBy(p => p.Id)
                .Select(p => p.Id)
                .Take(Math.Clamp(request.Limit, 1, 50))
                .ToListAsync(ct);

        return await PublicProductMapper.MapAsync(unitOfWork, localization, request.Language, ids, ct);
    }
}

internal static class PublicProductMapper
{
    /// <summary>Maps products to storefront shape, keeping the order of <paramref name="ids"/>.</summary>
    public static async Task<List<PublicProductDto>> MapAsync(
        IUnitOfWork unitOfWork, ILocalizationService localization, string language, IReadOnlyList<long> ids, CancellationToken ct)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        var db = unitOfWork.GetDbContext();
        var products = await db.Set<Product>().AsNoTracking().Include(p => p.Variants).Where(p => ids.Contains(p.Id)).ToListAsync(ct);
        var brandIds = products.Select(p => p.BrandId).Distinct().ToList();
        var brands = await db.Set<Brand>().AsNoTracking().Where(b => brandIds.Contains(b.Id)).ToDictionaryAsync(b => b.Id, ct);
        var categoryLinks = await db.Set<ProductCategoryLink>().AsNoTracking().Where(l => ids.Contains(l.ProductId)).ToListAsync(ct);
        var sportLinks = await db.Set<ProductSportLink>().AsNoTracking().Where(l => ids.Contains(l.ProductId)).ToListAsync(ct);
        var categoryIds = categoryLinks.Select(l => l.ProductCategoryId).Distinct().ToList();
        var sportIds = sportLinks.Select(l => l.SportId).Distinct().ToList();
        var categories = await db.Set<ProductCategory>().AsNoTracking().Where(c => categoryIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, ct);
        var sports = await db.Set<Sport>().AsNoTracking().Where(s => sportIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, ct);
        var media = await GalleryService.LoadAsync(unitOfWork, "product", "media", ids, ct);

        return ids.Select(id => products.First(p => p.Id == id)).Select(p =>
        {
            var brand = brands.GetValueOrDefault(p.BrandId);
            return PublicProductDto.From(
                p,
                localization,
                language,
                brand is null ? null : new RefDto(brand.Id, brand.Slug, localization.GetLocalizedName(brand.NameAr, brand.NameEn, language)),
                categoryLinks.Where(l => l.ProductId == p.Id).Select(l => categories.GetValueOrDefault(l.ProductCategoryId)).Where(c => c is not null)
                    .Select(c => new RefDto(c!.Id, c.Slug ?? string.Empty, localization.GetLocalizedName(c.NameAr, c.NameEn, language))).OrderBy(c => c.Name).ToList(),
                sportLinks.Where(l => l.ProductId == p.Id).Select(l => sports.GetValueOrDefault(l.SportId)).Where(s => s is not null)
                    .Select(s => new RefDto(s!.Id, s.Slug ?? string.Empty, localization.GetLocalizedName(s.NameAr, s.NameEn, language))).OrderBy(s => s.Name).ToList(),
                media.GetValueOrDefault(p.Id) ?? []);
        }).ToList();
    }
}
