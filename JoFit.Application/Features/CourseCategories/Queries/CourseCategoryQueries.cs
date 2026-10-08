using JoFit.Application.Common.Extensions;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Features.CourseCategories.Dtos;
using JoFit.Domain.CourseCategories;
using JoFit.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.CourseCategories.Queries;

public sealed record GetCourseCategoriesPagedQuery(
    int Page = 1,
    int PageSize = PagingExtensions.DefaultPageSize,
    string? Search = null,
    string? SortField = null,
    string? SortDir = null,
    string? Locale = null) : IRequest<Result<PagedResult<CourseCategoryDto>>>;

public sealed record GetCourseCategoryOptionsQuery(IReadOnlyList<long>? Ids = null, string? Locale = null)
    : IRequest<Result<List<CourseCategoryDto>>>;

public sealed record GetCourseCategoryByIdQuery(long Id) : IRequest<Result<CourseCategoryDto>>;

public sealed record GetPublicCourseCategoriesQuery(string Language, IReadOnlyList<long>? Ids = null)
    : IRequest<Result<List<PublicCourseCategoryDto>>>;

public sealed record GetPublicCourseCategoryQuery(string Slug, string Language)
    : IRequest<Result<PublicCourseCategoryDto>>;

public class GetCourseCategoriesPagedQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetCourseCategoriesPagedQuery, Result<PagedResult<CourseCategoryDto>>>
{
    public async Task<Result<PagedResult<CourseCategoryDto>>> Handle(GetCourseCategoriesPagedQuery request, CancellationToken ct)
    {
        var query = unitOfWork.GetRepository<CourseCategory>().GetTableNoTracking();

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
        return new PagedResult<CourseCategoryDto>(page.Items.Select(CourseCategoryDto.From).ToList(), page.Page, page.PageSize, page.TotalCount);
    }
}

public class GetCourseCategoryOptionsQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetCourseCategoryOptionsQuery, Result<List<CourseCategoryDto>>>
{
    public async Task<Result<List<CourseCategoryDto>>> Handle(GetCourseCategoryOptionsQuery request, CancellationToken ct)
    {
        var query = unitOfWork.GetRepository<CourseCategory>().GetTableNoTracking();
        if (request.Ids is { Count: > 0 })
        {
            var ids = request.Ids;
            query = query.Where(x => ids.Contains(x.Id));
        }

        var rows = await (string.Equals(request.Locale, "ar", StringComparison.OrdinalIgnoreCase)
            ? query.OrderBy(x => x.NameAr)
            : query.OrderBy(x => x.NameEn)).ToListAsync(ct);
        return rows.ConvertAll(CourseCategoryDto.From);
    }
}

public class GetCourseCategoryByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetCourseCategoryByIdQuery, Result<CourseCategoryDto>>
{
    public async Task<Result<CourseCategoryDto>> Handle(GetCourseCategoryByIdQuery request, CancellationToken ct)
    {
        var entity = await unitOfWork.GetRepository<CourseCategory>().GetTableNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        return entity is null ? CourseCategoryErrors.NotFound : CourseCategoryDto.From(entity);
    }
}

public class GetPublicCourseCategoriesQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localization)
    : IRequestHandler<GetPublicCourseCategoriesQuery, Result<List<PublicCourseCategoryDto>>>
{
    public async Task<Result<List<PublicCourseCategoryDto>>> Handle(GetPublicCourseCategoriesQuery request, CancellationToken ct)
    {
        var query = unitOfWork.GetRepository<CourseCategory>().GetTableNoTracking();
        if (request.Ids is { Count: > 0 })
        {
            var ids = request.Ids;
            query = query.Where(x => ids.Contains(x.Id));
        }

        var rows = await query.ToListAsync(ct);
        var items = rows.Select(x => PublicCourseCategoryDto.From(x, localization, request.Language));
        return (request.Ids is { Count: > 0 }
            ? items.OrderBy(x => request.Ids.ToList().IndexOf(x.Id))
            : items.OrderBy(x => x.Name, StringComparer.CurrentCulture)).ToList();
    }
}

public class GetPublicCourseCategoryQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localization)
    : IRequestHandler<GetPublicCourseCategoryQuery, Result<PublicCourseCategoryDto>>
{
    public async Task<Result<PublicCourseCategoryDto>> Handle(GetPublicCourseCategoryQuery request, CancellationToken ct)
    {
        var entity = await unitOfWork.GetRepository<CourseCategory>().GetTableNoTracking()
            .FirstOrDefaultAsync(x => x.Slug == request.Slug, ct);
        return entity is null ? CourseCategoryErrors.NotFound : PublicCourseCategoryDto.From(entity, localization, request.Language);
    }
}
