using JoFit.Application.Common.Extensions;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Features.RentalDepots.Dtos;
using JoFit.Domain.RentalDepots;
using JoFit.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.RentalDepots.Queries;

public sealed record GetRentalDepotsPagedQuery(
    int Page = 1,
    int PageSize = PagingExtensions.DefaultPageSize,
    string? Search = null,
    string? SortField = null,
    string? SortDir = null,
    string? Locale = null) : IRequest<Result<PagedResult<RentalDepotDto>>>;

public sealed record GetRentalDepotOptionsQuery(IReadOnlyList<long>? Ids = null, string? Locale = null)
    : IRequest<Result<List<RentalDepotDto>>>;

public sealed record GetRentalDepotByIdQuery(long Id) : IRequest<Result<RentalDepotDto>>;

public sealed record GetPublicRentalDepotsQuery(string Language, IReadOnlyList<long>? Ids = null)
    : IRequest<Result<List<PublicRentalDepotDto>>>;

public sealed record GetPublicRentalDepotQuery(long Id, string Language)
    : IRequest<Result<PublicRentalDepotDto>>;

public class GetRentalDepotsPagedQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetRentalDepotsPagedQuery, Result<PagedResult<RentalDepotDto>>>
{
    public async Task<Result<PagedResult<RentalDepotDto>>> Handle(GetRentalDepotsPagedQuery request, CancellationToken ct)
    {
        var query = unitOfWork.GetRepository<RentalDepot>().GetTableNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(x => x.NameEn.ToLower().Contains(term)
                                     || x.NameAr.ToLower().Contains(term));
        }

        var descending = string.Equals(request.SortDir, "desc", StringComparison.OrdinalIgnoreCase);
        var arabic = string.Equals(request.Locale, "ar", StringComparison.OrdinalIgnoreCase);
        query = request.SortField?.ToLowerInvariant() switch
        {
            _ when arabic => descending ? query.OrderByDescending(x => x.NameAr) : query.OrderBy(x => x.NameAr),
            _ => descending ? query.OrderByDescending(x => x.NameEn) : query.OrderBy(x => x.NameEn)
        };

        var page = await query.ToPagedResultAsync(request.Page, request.PageSize, ct);
        return new PagedResult<RentalDepotDto>(page.Items.Select(RentalDepotDto.From).ToList(), page.Page, page.PageSize, page.TotalCount);
    }
}

public class GetRentalDepotOptionsQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetRentalDepotOptionsQuery, Result<List<RentalDepotDto>>>
{
    public async Task<Result<List<RentalDepotDto>>> Handle(GetRentalDepotOptionsQuery request, CancellationToken ct)
    {
        var query = unitOfWork.GetRepository<RentalDepot>().GetTableNoTracking();
        if (request.Ids is { Count: > 0 })
        {
            var ids = request.Ids;
            query = query.Where(x => ids.Contains(x.Id));
        }

        var rows = await (string.Equals(request.Locale, "ar", StringComparison.OrdinalIgnoreCase)
            ? query.OrderBy(x => x.NameAr)
            : query.OrderBy(x => x.NameEn)).ToListAsync(ct);
        return rows.ConvertAll(RentalDepotDto.From);
    }
}

public class GetRentalDepotByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetRentalDepotByIdQuery, Result<RentalDepotDto>>
{
    public async Task<Result<RentalDepotDto>> Handle(GetRentalDepotByIdQuery request, CancellationToken ct)
    {
        var entity = await unitOfWork.GetRepository<RentalDepot>().GetTableNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        return entity is null ? RentalDepotErrors.NotFound : RentalDepotDto.From(entity);
    }
}

public class GetPublicRentalDepotsQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localization)
    : IRequestHandler<GetPublicRentalDepotsQuery, Result<List<PublicRentalDepotDto>>>
{
    public async Task<Result<List<PublicRentalDepotDto>>> Handle(GetPublicRentalDepotsQuery request, CancellationToken ct)
    {
        var query = unitOfWork.GetRepository<RentalDepot>().GetTableNoTracking();
        if (request.Ids is { Count: > 0 })
        {
            var ids = request.Ids;
            query = query.Where(x => ids.Contains(x.Id));
        }

        var rows = await query.ToListAsync(ct);
        var items = rows.Select(x => PublicRentalDepotDto.From(x, localization, request.Language));
        return (request.Ids is { Count: > 0 }
            ? items.OrderBy(x => request.Ids.ToList().IndexOf(x.Id))
            : items.OrderBy(x => x.Name, StringComparer.CurrentCulture)).ToList();
    }
}

public class GetPublicRentalDepotQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localization)
    : IRequestHandler<GetPublicRentalDepotQuery, Result<PublicRentalDepotDto>>
{
    public async Task<Result<PublicRentalDepotDto>> Handle(GetPublicRentalDepotQuery request, CancellationToken ct)
    {
        var entity = await unitOfWork.GetRepository<RentalDepot>().GetTableNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        return entity is null ? RentalDepotErrors.NotFound : PublicRentalDepotDto.From(entity, localization, request.Language);
    }
}
