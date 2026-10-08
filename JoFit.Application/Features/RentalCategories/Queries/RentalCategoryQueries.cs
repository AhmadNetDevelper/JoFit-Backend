using JoFit.Application.Common.Extensions;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Features.RentalCategories.Dtos;
using JoFit.Domain.RentalCategories;
using JoFit.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.RentalCategories.Queries;

public sealed record GetRentalCategoriesPagedQuery(
    int Page = 1,
    int PageSize = PagingExtensions.DefaultPageSize,
    string? Search = null,
    string? SortField = null,
    string? SortDir = null,
    string? Locale = null) : IRequest<Result<PagedResult<RentalCategoryDto>>>;

public sealed record GetRentalCategoryOptionsQuery(IReadOnlyList<long>? Ids = null, string? Locale = null)
    : IRequest<Result<List<RentalCategoryDto>>>;

public sealed record GetRentalCategoryByIdQuery(long Id) : IRequest<Result<RentalCategoryDto>>;

public sealed record GetPublicRentalCategoriesQuery(string Language, IReadOnlyList<long>? Ids = null)
    : IRequest<Result<List<PublicRentalCategoryDto>>>;

public sealed record GetPublicRentalCategoryQuery(string Slug, string Language)
    : IRequest<Result<PublicRentalCategoryDto>>;

public class GetRentalCategoriesPagedQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetRentalCategoriesPagedQuery, Result<PagedResult<RentalCategoryDto>>>
{
    public async Task<Result<PagedResult<RentalCategoryDto>>> Handle(GetRentalCategoriesPagedQuery request, CancellationToken ct)
    {
        var query = unitOfWork.GetRepository<RentalCategory>().GetTableNoTracking();

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
        return new PagedResult<RentalCategoryDto>(page.Items.Select(RentalCategoryDto.From).ToList(), page.Page, page.PageSize, page.TotalCount);
    }
}

public class GetRentalCategoryOptionsQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetRentalCategoryOptionsQuery, Result<List<RentalCategoryDto>>>
{
    public async Task<Result<List<RentalCategoryDto>>> Handle(GetRentalCategoryOptionsQuery request, CancellationToken ct)
    {
        var query = unitOfWork.GetRepository<RentalCategory>().GetTableNoTracking();
        if (request.Ids is { Count: > 0 })
        {
            var ids = request.Ids;
            query = query.Where(x => ids.Contains(x.Id));
        }

        var rows = await (string.Equals(request.Locale, "ar", StringComparison.OrdinalIgnoreCase)
            ? query.OrderBy(x => x.NameAr)
            : query.OrderBy(x => x.NameEn)).ToListAsync(ct);
        return rows.ConvertAll(RentalCategoryDto.From);
    }
}

public class GetRentalCategoryByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetRentalCategoryByIdQuery, Result<RentalCategoryDto>>
{
    public async Task<Result<RentalCategoryDto>> Handle(GetRentalCategoryByIdQuery request, CancellationToken ct)
    {
        var entity = await unitOfWork.GetRepository<RentalCategory>().GetTableNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        return entity is null ? RentalCategoryErrors.NotFound : RentalCategoryDto.From(entity);
    }
}

public class GetPublicRentalCategoriesQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localization)
    : IRequestHandler<GetPublicRentalCategoriesQuery, Result<List<PublicRentalCategoryDto>>>
{
    public async Task<Result<List<PublicRentalCategoryDto>>> Handle(GetPublicRentalCategoriesQuery request, CancellationToken ct)
    {
        var query = unitOfWork.GetRepository<RentalCategory>().GetTableNoTracking();
        if (request.Ids is { Count: > 0 })
        {
            var ids = request.Ids;
            query = query.Where(x => ids.Contains(x.Id));
        }

        var rows = await query.ToListAsync(ct);
        var items = rows.Select(x => PublicRentalCategoryDto.From(x, localization, request.Language));
        return (request.Ids is { Count: > 0 }
            ? items.OrderBy(x => request.Ids.ToList().IndexOf(x.Id))
            : items.OrderBy(x => x.Name, StringComparer.CurrentCulture)).ToList();
    }
}

public class GetPublicRentalCategoryQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localization)
    : IRequestHandler<GetPublicRentalCategoryQuery, Result<PublicRentalCategoryDto>>
{
    public async Task<Result<PublicRentalCategoryDto>> Handle(GetPublicRentalCategoryQuery request, CancellationToken ct)
    {
        var entity = await unitOfWork.GetRepository<RentalCategory>().GetTableNoTracking()
            .FirstOrDefaultAsync(x => x.Slug == request.Slug, ct);
        return entity is null ? RentalCategoryErrors.NotFound : PublicRentalCategoryDto.From(entity, localization, request.Language);
    }
}
