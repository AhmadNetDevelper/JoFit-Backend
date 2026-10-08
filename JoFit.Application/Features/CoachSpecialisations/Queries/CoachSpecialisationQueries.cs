using JoFit.Application.Common.Extensions;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Features.CoachSpecialisations.Dtos;
using JoFit.Domain.CoachSpecialisations;
using JoFit.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.CoachSpecialisations.Queries;

public sealed record GetCoachSpecialisationsPagedQuery(
    int Page = 1,
    int PageSize = PagingExtensions.DefaultPageSize,
    string? Search = null,
    string? SortField = null,
    string? SortDir = null,
    string? Locale = null) : IRequest<Result<PagedResult<CoachSpecialisationDto>>>;

public sealed record GetCoachSpecialisationOptionsQuery(IReadOnlyList<long>? Ids = null, string? Locale = null)
    : IRequest<Result<List<CoachSpecialisationDto>>>;

public sealed record GetCoachSpecialisationByIdQuery(long Id) : IRequest<Result<CoachSpecialisationDto>>;

public sealed record GetPublicCoachSpecialisationsQuery(string Language, IReadOnlyList<long>? Ids = null)
    : IRequest<Result<List<PublicCoachSpecialisationDto>>>;

public sealed record GetPublicCoachSpecialisationQuery(long Id, string Language)
    : IRequest<Result<PublicCoachSpecialisationDto>>;

public class GetCoachSpecialisationsPagedQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetCoachSpecialisationsPagedQuery, Result<PagedResult<CoachSpecialisationDto>>>
{
    public async Task<Result<PagedResult<CoachSpecialisationDto>>> Handle(GetCoachSpecialisationsPagedQuery request, CancellationToken ct)
    {
        var query = unitOfWork.GetRepository<CoachSpecialisation>().GetTableNoTracking();

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
        return new PagedResult<CoachSpecialisationDto>(page.Items.Select(CoachSpecialisationDto.From).ToList(), page.Page, page.PageSize, page.TotalCount);
    }
}

public class GetCoachSpecialisationOptionsQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetCoachSpecialisationOptionsQuery, Result<List<CoachSpecialisationDto>>>
{
    public async Task<Result<List<CoachSpecialisationDto>>> Handle(GetCoachSpecialisationOptionsQuery request, CancellationToken ct)
    {
        var query = unitOfWork.GetRepository<CoachSpecialisation>().GetTableNoTracking();
        if (request.Ids is { Count: > 0 })
        {
            var ids = request.Ids;
            query = query.Where(x => ids.Contains(x.Id));
        }

        var rows = await (string.Equals(request.Locale, "ar", StringComparison.OrdinalIgnoreCase)
            ? query.OrderBy(x => x.NameAr)
            : query.OrderBy(x => x.NameEn)).ToListAsync(ct);
        return rows.ConvertAll(CoachSpecialisationDto.From);
    }
}

public class GetCoachSpecialisationByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetCoachSpecialisationByIdQuery, Result<CoachSpecialisationDto>>
{
    public async Task<Result<CoachSpecialisationDto>> Handle(GetCoachSpecialisationByIdQuery request, CancellationToken ct)
    {
        var entity = await unitOfWork.GetRepository<CoachSpecialisation>().GetTableNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        return entity is null ? CoachSpecialisationErrors.NotFound : CoachSpecialisationDto.From(entity);
    }
}

public class GetPublicCoachSpecialisationsQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localization)
    : IRequestHandler<GetPublicCoachSpecialisationsQuery, Result<List<PublicCoachSpecialisationDto>>>
{
    public async Task<Result<List<PublicCoachSpecialisationDto>>> Handle(GetPublicCoachSpecialisationsQuery request, CancellationToken ct)
    {
        var query = unitOfWork.GetRepository<CoachSpecialisation>().GetTableNoTracking();
        if (request.Ids is { Count: > 0 })
        {
            var ids = request.Ids;
            query = query.Where(x => ids.Contains(x.Id));
        }

        var rows = await query.ToListAsync(ct);
        var items = rows.Select(x => PublicCoachSpecialisationDto.From(x, localization, request.Language));
        return (request.Ids is { Count: > 0 }
            ? items.OrderBy(x => request.Ids.ToList().IndexOf(x.Id))
            : items.OrderBy(x => x.Name, StringComparer.CurrentCulture)).ToList();
    }
}

public class GetPublicCoachSpecialisationQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localization)
    : IRequestHandler<GetPublicCoachSpecialisationQuery, Result<PublicCoachSpecialisationDto>>
{
    public async Task<Result<PublicCoachSpecialisationDto>> Handle(GetPublicCoachSpecialisationQuery request, CancellationToken ct)
    {
        var entity = await unitOfWork.GetRepository<CoachSpecialisation>().GetTableNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        return entity is null ? CoachSpecialisationErrors.NotFound : PublicCoachSpecialisationDto.From(entity, localization, request.Language);
    }
}
