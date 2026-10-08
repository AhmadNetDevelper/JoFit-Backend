using JoFit.Application.Common.Extensions;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Features.ArticleCategories.Dtos;
using JoFit.Domain.ArticleCategories;
using JoFit.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.ArticleCategories.Queries;

public sealed record GetArticleCategoriesPagedQuery(
    int Page = 1,
    int PageSize = PagingExtensions.DefaultPageSize,
    string? Search = null,
    string? SortField = null,
    string? SortDir = null,
    string? Locale = null) : IRequest<Result<PagedResult<ArticleCategoryDto>>>;

public sealed record GetArticleCategoryOptionsQuery(IReadOnlyList<long>? Ids = null, string? Locale = null)
    : IRequest<Result<List<ArticleCategoryDto>>>;

public sealed record GetArticleCategoryByIdQuery(long Id) : IRequest<Result<ArticleCategoryDto>>;

public sealed record GetPublicArticleCategoriesQuery(string Language, IReadOnlyList<long>? Ids = null)
    : IRequest<Result<List<PublicArticleCategoryDto>>>;

public sealed record GetPublicArticleCategoryQuery(string Slug, string Language)
    : IRequest<Result<PublicArticleCategoryDto>>;

public class GetArticleCategoriesPagedQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetArticleCategoriesPagedQuery, Result<PagedResult<ArticleCategoryDto>>>
{
    public async Task<Result<PagedResult<ArticleCategoryDto>>> Handle(GetArticleCategoriesPagedQuery request, CancellationToken ct)
    {
        var query = unitOfWork.GetRepository<ArticleCategory>().GetTableNoTracking();

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
        return new PagedResult<ArticleCategoryDto>(page.Items.Select(ArticleCategoryDto.From).ToList(), page.Page, page.PageSize, page.TotalCount);
    }
}

public class GetArticleCategoryOptionsQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetArticleCategoryOptionsQuery, Result<List<ArticleCategoryDto>>>
{
    public async Task<Result<List<ArticleCategoryDto>>> Handle(GetArticleCategoryOptionsQuery request, CancellationToken ct)
    {
        var query = unitOfWork.GetRepository<ArticleCategory>().GetTableNoTracking();
        if (request.Ids is { Count: > 0 })
        {
            var ids = request.Ids;
            query = query.Where(x => ids.Contains(x.Id));
        }

        var rows = await (string.Equals(request.Locale, "ar", StringComparison.OrdinalIgnoreCase)
            ? query.OrderBy(x => x.NameAr)
            : query.OrderBy(x => x.NameEn)).ToListAsync(ct);
        return rows.ConvertAll(ArticleCategoryDto.From);
    }
}

public class GetArticleCategoryByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetArticleCategoryByIdQuery, Result<ArticleCategoryDto>>
{
    public async Task<Result<ArticleCategoryDto>> Handle(GetArticleCategoryByIdQuery request, CancellationToken ct)
    {
        var entity = await unitOfWork.GetRepository<ArticleCategory>().GetTableNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        return entity is null ? ArticleCategoryErrors.NotFound : ArticleCategoryDto.From(entity);
    }
}

public class GetPublicArticleCategoriesQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localization)
    : IRequestHandler<GetPublicArticleCategoriesQuery, Result<List<PublicArticleCategoryDto>>>
{
    public async Task<Result<List<PublicArticleCategoryDto>>> Handle(GetPublicArticleCategoriesQuery request, CancellationToken ct)
    {
        var query = unitOfWork.GetRepository<ArticleCategory>().GetTableNoTracking();
        if (request.Ids is { Count: > 0 })
        {
            var ids = request.Ids;
            query = query.Where(x => ids.Contains(x.Id));
        }

        var rows = await query.ToListAsync(ct);
        var items = rows.Select(x => PublicArticleCategoryDto.From(x, localization, request.Language));
        return (request.Ids is { Count: > 0 }
            ? items.OrderBy(x => request.Ids.ToList().IndexOf(x.Id))
            : items.OrderBy(x => x.Name, StringComparer.CurrentCulture)).ToList();
    }
}

public class GetPublicArticleCategoryQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localization)
    : IRequestHandler<GetPublicArticleCategoryQuery, Result<PublicArticleCategoryDto>>
{
    public async Task<Result<PublicArticleCategoryDto>> Handle(GetPublicArticleCategoryQuery request, CancellationToken ct)
    {
        var entity = await unitOfWork.GetRepository<ArticleCategory>().GetTableNoTracking()
            .FirstOrDefaultAsync(x => x.Slug == request.Slug, ct);
        return entity is null ? ArticleCategoryErrors.NotFound : PublicArticleCategoryDto.From(entity, localization, request.Language);
    }
}
