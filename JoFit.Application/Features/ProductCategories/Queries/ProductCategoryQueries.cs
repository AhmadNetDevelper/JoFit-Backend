using JoFit.Application.Common.Extensions;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Features.ProductCategories.Dtos;
using JoFit.Domain.ProductCategories;
using JoFit.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.ProductCategories.Queries;

public sealed record GetProductCategoriesPagedQuery(
    int Page = 1,
    int PageSize = PagingExtensions.DefaultPageSize,
    string? Search = null,
    string? SortField = null,
    string? SortDir = null,
    string? Locale = null) : IRequest<Result<PagedResult<ProductCategoryDto>>>;

public sealed record GetProductCategoryOptionsQuery(IReadOnlyList<long>? Ids = null, string? Locale = null)
    : IRequest<Result<List<ProductCategoryDto>>>;

public sealed record GetProductCategoryByIdQuery(long Id) : IRequest<Result<ProductCategoryDto>>;

public sealed record GetPublicProductCategoriesQuery(string Language, IReadOnlyList<long>? Ids = null)
    : IRequest<Result<List<PublicProductCategoryDto>>>;

public sealed record GetPublicProductCategoryQuery(string Slug, string Language)
    : IRequest<Result<PublicProductCategoryDto>>;

public class GetProductCategoriesPagedQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetProductCategoriesPagedQuery, Result<PagedResult<ProductCategoryDto>>>
{
    public async Task<Result<PagedResult<ProductCategoryDto>>> Handle(GetProductCategoriesPagedQuery request, CancellationToken ct)
    {
        var query = unitOfWork.GetRepository<ProductCategory>().GetTableNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(x => x.NameEn.ToLower().Contains(term)
                                     || x.NameAr.ToLower().Contains(term)
                                     || (x.Slug != null && x.Slug.Contains(term)));
        }

        var descending = string.Equals(request.SortDir, "desc", StringComparison.OrdinalIgnoreCase);
        var arabic = string.Equals(request.Locale, "ar", StringComparison.OrdinalIgnoreCase);
        query = request.SortField?.ToLowerInvariant() switch
        {
            "slug" => descending ? query.OrderByDescending(x => x.Slug) : query.OrderBy(x => x.Slug),
            _ when arabic => descending ? query.OrderByDescending(x => x.NameAr) : query.OrderBy(x => x.NameAr),
            _ => descending ? query.OrderByDescending(x => x.NameEn) : query.OrderBy(x => x.NameEn)
        };

        var page = await query.ToPagedResultAsync(request.Page, request.PageSize, ct);
        return new PagedResult<ProductCategoryDto>(page.Items.Select(ProductCategoryDto.From).ToList(), page.Page, page.PageSize, page.TotalCount);
    }
}

public class GetProductCategoryOptionsQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetProductCategoryOptionsQuery, Result<List<ProductCategoryDto>>>
{
    public async Task<Result<List<ProductCategoryDto>>> Handle(GetProductCategoryOptionsQuery request, CancellationToken ct)
    {
        var query = unitOfWork.GetRepository<ProductCategory>().GetTableNoTracking();
        if (request.Ids is { Count: > 0 })
        {
            var ids = request.Ids;
            query = query.Where(x => ids.Contains(x.Id));
        }

        var rows = await (string.Equals(request.Locale, "ar", StringComparison.OrdinalIgnoreCase)
            ? query.OrderBy(x => x.NameAr)
            : query.OrderBy(x => x.NameEn)).ToListAsync(ct);
        return rows.ConvertAll(ProductCategoryDto.From);
    }
}

public class GetProductCategoryByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetProductCategoryByIdQuery, Result<ProductCategoryDto>>
{
    public async Task<Result<ProductCategoryDto>> Handle(GetProductCategoryByIdQuery request, CancellationToken ct)
    {
        var entity = await unitOfWork.GetRepository<ProductCategory>().GetTableNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        return entity is null ? ProductCategoryErrors.NotFound : ProductCategoryDto.From(entity);
    }
}

public class GetPublicProductCategoriesQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localization)
    : IRequestHandler<GetPublicProductCategoriesQuery, Result<List<PublicProductCategoryDto>>>
{
    public async Task<Result<List<PublicProductCategoryDto>>> Handle(GetPublicProductCategoriesQuery request, CancellationToken ct)
    {
        var query = unitOfWork.GetRepository<ProductCategory>().GetTableNoTracking();
        if (request.Ids is { Count: > 0 })
        {
            var ids = request.Ids;
            query = query.Where(x => ids.Contains(x.Id));
        }

        var rows = await query.ToListAsync(ct);
        var items = rows.Select(x => PublicProductCategoryDto.From(x, localization, request.Language));
        return (request.Ids is { Count: > 0 }
            ? items.OrderBy(x => request.Ids.ToList().IndexOf(x.Id))
            : items.OrderBy(x => x.Name, StringComparer.CurrentCulture)).ToList();
    }
}

public class GetPublicProductCategoryQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localization)
    : IRequestHandler<GetPublicProductCategoryQuery, Result<PublicProductCategoryDto>>
{
    public async Task<Result<PublicProductCategoryDto>> Handle(GetPublicProductCategoryQuery request, CancellationToken ct)
    {
        var entity = await unitOfWork.GetRepository<ProductCategory>().GetTableNoTracking()
            .FirstOrDefaultAsync(x => x.Slug == request.Slug, ct);
        return entity is null ? ProductCategoryErrors.NotFound : PublicProductCategoryDto.From(entity, localization, request.Language);
    }
}
