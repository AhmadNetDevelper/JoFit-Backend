using JoFit.Application.Common.Extensions;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Features.Authors.Dtos;
using JoFit.Domain.Authors;
using JoFit.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.Authors.Queries;

public sealed record GetAuthorsPagedQuery(
    int Page = 1,
    int PageSize = PagingExtensions.DefaultPageSize,
    string? Search = null,
    string? SortField = null,
    string? SortDir = null,
    string? Locale = null) : IRequest<Result<PagedResult<AuthorDto>>>;

public sealed record GetAuthorOptionsQuery(IReadOnlyList<long>? Ids = null, string? Locale = null)
    : IRequest<Result<List<AuthorDto>>>;

public sealed record GetAuthorByIdQuery(long Id) : IRequest<Result<AuthorDto>>;

public sealed record GetPublicAuthorsQuery(string Language, IReadOnlyList<long>? Ids = null)
    : IRequest<Result<List<PublicAuthorDto>>>;

public sealed record GetPublicAuthorQuery(long Id, string Language)
    : IRequest<Result<PublicAuthorDto>>;

public class GetAuthorsPagedQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAuthorsPagedQuery, Result<PagedResult<AuthorDto>>>
{
    public async Task<Result<PagedResult<AuthorDto>>> Handle(GetAuthorsPagedQuery request, CancellationToken ct)
    {
        var query = unitOfWork.GetRepository<Author>().GetTableNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(x => x.Name.ToLower().Contains(term)
                                     || (x.Slug != null && x.Slug.Contains(term)));
        }

        var descending = string.Equals(request.SortDir, "desc", StringComparison.OrdinalIgnoreCase);
        var arabic = string.Equals(request.Locale, "ar", StringComparison.OrdinalIgnoreCase);
        query = request.SortField?.ToLowerInvariant() switch
        {
            "slug" => descending ? query.OrderByDescending(x => x.Slug) : query.OrderBy(x => x.Slug),
            _ when arabic => descending ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
            _ => descending ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name)
        };

        var page = await query.ToPagedResultAsync(request.Page, request.PageSize, ct);
        return new PagedResult<AuthorDto>(page.Items.Select(AuthorDto.From).ToList(), page.Page, page.PageSize, page.TotalCount);
    }
}

public class GetAuthorOptionsQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAuthorOptionsQuery, Result<List<AuthorDto>>>
{
    public async Task<Result<List<AuthorDto>>> Handle(GetAuthorOptionsQuery request, CancellationToken ct)
    {
        var query = unitOfWork.GetRepository<Author>().GetTableNoTracking();
        if (request.Ids is { Count: > 0 })
        {
            var ids = request.Ids;
            query = query.Where(x => ids.Contains(x.Id));
        }

        var rows = await (string.Equals(request.Locale, "ar", StringComparison.OrdinalIgnoreCase)
            ? query.OrderBy(x => x.Name)
            : query.OrderBy(x => x.Name)).ToListAsync(ct);
        return rows.ConvertAll(AuthorDto.From);
    }
}

public class GetAuthorByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAuthorByIdQuery, Result<AuthorDto>>
{
    public async Task<Result<AuthorDto>> Handle(GetAuthorByIdQuery request, CancellationToken ct)
    {
        var entity = await unitOfWork.GetRepository<Author>().GetTableNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        return entity is null ? AuthorErrors.NotFound : AuthorDto.From(entity);
    }
}

public class GetPublicAuthorsQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localization)
    : IRequestHandler<GetPublicAuthorsQuery, Result<List<PublicAuthorDto>>>
{
    public async Task<Result<List<PublicAuthorDto>>> Handle(GetPublicAuthorsQuery request, CancellationToken ct)
    {
        var query = unitOfWork.GetRepository<Author>().GetTableNoTracking();
        if (request.Ids is { Count: > 0 })
        {
            var ids = request.Ids;
            query = query.Where(x => ids.Contains(x.Id));
        }

        var rows = await query.ToListAsync(ct);
        var items = rows.Select(x => PublicAuthorDto.From(x, localization, request.Language));
        return (request.Ids is { Count: > 0 }
            ? items.OrderBy(x => request.Ids.ToList().IndexOf(x.Id))
            : items.OrderBy(x => x.Name, StringComparer.CurrentCulture)).ToList();
    }
}

public class GetPublicAuthorQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localization)
    : IRequestHandler<GetPublicAuthorQuery, Result<PublicAuthorDto>>
{
    public async Task<Result<PublicAuthorDto>> Handle(GetPublicAuthorQuery request, CancellationToken ct)
    {
        var entity = await unitOfWork.GetRepository<Author>().GetTableNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        return entity is null ? AuthorErrors.NotFound : PublicAuthorDto.From(entity, localization, request.Language);
    }
}
