using JoFit.Application.Common.Extensions;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Features.Sports.Dtos;
using JoFit.Domain.Sports;
using JoFit.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.Sports.Queries;

public sealed record GetSportsPagedQuery(
    int Page = 1,
    int PageSize = PagingExtensions.DefaultPageSize,
    string? Search = null,
    string? SortField = null,
    string? SortDir = null,
    string? Locale = null) : IRequest<Result<PagedResult<SportDto>>>;

public sealed record GetSportOptionsQuery(IReadOnlyList<long>? Ids = null, string? Locale = null)
    : IRequest<Result<List<SportDto>>>;

public sealed record GetSportByIdQuery(long Id) : IRequest<Result<SportDto>>;

public sealed record GetPublicSportsQuery(string Language, IReadOnlyList<long>? Ids = null)
    : IRequest<Result<List<PublicSportDto>>>;

public sealed record GetPublicSportQuery(string Slug, string Language)
    : IRequest<Result<PublicSportDto>>;

public class GetSportsPagedQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetSportsPagedQuery, Result<PagedResult<SportDto>>>
{
    public async Task<Result<PagedResult<SportDto>>> Handle(GetSportsPagedQuery request, CancellationToken ct)
    {
        var query = unitOfWork.GetRepository<Sport>().GetTableNoTracking();

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
        return new PagedResult<SportDto>(page.Items.Select(SportDto.From).ToList(), page.Page, page.PageSize, page.TotalCount);
    }
}

public class GetSportOptionsQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetSportOptionsQuery, Result<List<SportDto>>>
{
    public async Task<Result<List<SportDto>>> Handle(GetSportOptionsQuery request, CancellationToken ct)
    {
        var query = unitOfWork.GetRepository<Sport>().GetTableNoTracking();
        if (request.Ids is { Count: > 0 })
        {
            var ids = request.Ids;
            query = query.Where(x => ids.Contains(x.Id));
        }

        var rows = await (string.Equals(request.Locale, "ar", StringComparison.OrdinalIgnoreCase)
            ? query.OrderBy(x => x.NameAr)
            : query.OrderBy(x => x.NameEn)).ToListAsync(ct);
        return rows.ConvertAll(SportDto.From);
    }
}

public class GetSportByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetSportByIdQuery, Result<SportDto>>
{
    public async Task<Result<SportDto>> Handle(GetSportByIdQuery request, CancellationToken ct)
    {
        var entity = await unitOfWork.GetRepository<Sport>().GetTableNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        return entity is null ? SportErrors.NotFound : SportDto.From(entity);
    }
}

public class GetPublicSportsQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localization)
    : IRequestHandler<GetPublicSportsQuery, Result<List<PublicSportDto>>>
{
    public async Task<Result<List<PublicSportDto>>> Handle(GetPublicSportsQuery request, CancellationToken ct)
    {
        var query = unitOfWork.GetRepository<Sport>().GetTableNoTracking();
        if (request.Ids is { Count: > 0 })
        {
            var ids = request.Ids;
            query = query.Where(x => ids.Contains(x.Id));
        }

        var rows = await query.ToListAsync(ct);
        var items = rows.Select(x => PublicSportDto.From(x, localization, request.Language));
        return (request.Ids is { Count: > 0 }
            ? items.OrderBy(x => request.Ids.ToList().IndexOf(x.Id))
            : items.OrderBy(x => x.Name, StringComparer.CurrentCulture)).ToList();
    }
}

public class GetPublicSportQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localization)
    : IRequestHandler<GetPublicSportQuery, Result<PublicSportDto>>
{
    public async Task<Result<PublicSportDto>> Handle(GetPublicSportQuery request, CancellationToken ct)
    {
        var entity = await unitOfWork.GetRepository<Sport>().GetTableNoTracking()
            .FirstOrDefaultAsync(x => x.Slug == request.Slug, ct);
        return entity is null ? SportErrors.NotFound : PublicSportDto.From(entity, localization, request.Language);
    }
}
