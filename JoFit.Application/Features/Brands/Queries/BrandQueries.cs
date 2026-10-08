using JoFit.Application.Common.Extensions;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Features.Brands.Dtos;
using JoFit.Domain.Brands;
using JoFit.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.Brands.Queries;

// ---- admin ----------------------------------------------------------------

public sealed record GetBrandsPagedQuery(
    int Page = 1,
    int PageSize = PagingExtensions.DefaultPageSize,
    string? Search = null,
    string? SortField = null,
    string? SortDir = null,
    string? Locale = null) : IRequest<Result<PagedResult<BrandDto>>>;

public sealed record GetBrandOptionsQuery(IReadOnlyList<long>? Ids = null, string? Locale = null)
    : IRequest<Result<List<BrandDto>>>;

public sealed record GetBrandByIdQuery(long Id) : IRequest<Result<BrandDto>>;

public class GetBrandsPagedQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetBrandsPagedQuery, Result<PagedResult<BrandDto>>>
{
    public async Task<Result<PagedResult<BrandDto>>> Handle(GetBrandsPagedQuery request, CancellationToken ct)
    {
        var query = unitOfWork.GetRepository<Brand>().GetTableNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(x => x.NameEn.ToLower().Contains(term)
                                     || x.NameAr.ToLower().Contains(term)
                                     || x.Slug.Contains(term));
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
        return new PagedResult<BrandDto>(page.Items.Select(x => BrandDto.From(x)).ToList(), page.Page, page.PageSize, page.TotalCount);
    }
}

public class GetBrandOptionsQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetBrandOptionsQuery, Result<List<BrandDto>>>
{
    public async Task<Result<List<BrandDto>>> Handle(GetBrandOptionsQuery request, CancellationToken ct)
    {
        var query = unitOfWork.GetRepository<Brand>().GetTableNoTracking();
        if (request.Ids is { Count: > 0 })
        {
            var ids = request.Ids;
            query = query.Where(x => ids.Contains(x.Id));
        }

        var rows = await (string.Equals(request.Locale, "ar", StringComparison.OrdinalIgnoreCase)
            ? query.OrderBy(x => x.NameAr)
            : query.OrderBy(x => x.NameEn)).ToListAsync(ct);
        return rows.ConvertAll(x => BrandDto.From(x));
    }
}

public class GetBrandByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetBrandByIdQuery, Result<BrandDto>>
{
    public async Task<Result<BrandDto>> Handle(GetBrandByIdQuery request, CancellationToken ct)
    {
        var entity = await unitOfWork.GetRepository<Brand>().GetTableNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        return entity is null ? BrandErrors.NotFound : BrandDto.From(entity);
    }
}

// ---- storefront -----------------------------------------------------------

public sealed record GetPublicBrandsQuery(string Language) : IRequest<Result<List<PublicBrandDto>>>;

public sealed record GetPublicBrandBySlugQuery(string Slug, string Language) : IRequest<Result<PublicBrandDto>>;

public class GetPublicBrandsQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localization)
    : IRequestHandler<GetPublicBrandsQuery, Result<List<PublicBrandDto>>>
{
    public async Task<Result<List<PublicBrandDto>>> Handle(GetPublicBrandsQuery request, CancellationToken ct)
    {
        var rows = await unitOfWork.GetRepository<Brand>().GetTableNoTracking().ToListAsync(ct);
        return rows
            .Select(x => PublicBrandDto.From(x, localization, request.Language))
            .OrderBy(x => x.Name, StringComparer.CurrentCulture)
            .ToList();
    }
}

public class GetPublicBrandBySlugQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localization)
    : IRequestHandler<GetPublicBrandBySlugQuery, Result<PublicBrandDto>>
{
    public async Task<Result<PublicBrandDto>> Handle(GetPublicBrandBySlugQuery request, CancellationToken ct)
    {
        var entity = await unitOfWork.GetRepository<Brand>().GetTableNoTracking()
            .FirstOrDefaultAsync(x => x.Slug == request.Slug, ct);
        return entity is null ? BrandErrors.NotFound : PublicBrandDto.From(entity, localization, request.Language);
    }
}
