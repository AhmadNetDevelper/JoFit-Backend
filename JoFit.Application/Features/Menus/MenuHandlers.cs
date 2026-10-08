using System.Text.Json;
using JoFit.Application.Common.Extensions;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Features.Courses.Catalogue;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Menus;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.Menus;

// ---- the menu document (what the back-office reads and writes) -----------------

public sealed record MenuTargetDoc(string Kind, long? PageId, string? Href);

public sealed record MenuItemDoc(string Id, LocalizedText Label, bool IsVisible, MenuTargetDoc Target);

public sealed record MenuGroupDoc(string Id, LocalizedText Label, List<MenuItemDoc> Items);

public sealed record MenuDoc(LocalizedText Label, string? Href, List<MenuGroupDoc> Groups);

public sealed record AdminMenuDto(string Key, LocalizedText Label, string? Href, IReadOnlyList<MenuGroupDoc> Groups, AuditDto Audit);

// ---- what the storefront reads --------------------------------------------------

public sealed record PublicMenuItemDto(string Id, string Label, string Href);

public sealed record PublicMenuGroupDto(string Id, string Label, IReadOnlyList<PublicMenuItemDto> Items);

public sealed record PublicMenuDto(string Key, string Label, string? Href, IReadOnlyList<PublicMenuGroupDto> Groups);

// ---- pages ---------------------------------------------------------------------

public sealed record PageSectionDto(LocalizedText Heading, LocalizedText Body);

public sealed record PageCtaDto(LocalizedText Label, string Href);

public sealed record AdminMenuPageDto(
    long Id, string Slug, LocalizedText? Eyebrow, LocalizedText Title, LocalizedText Summary, IReadOnlyList<PageSectionDto> Sections,
    PageCtaDto? Cta, string Status, AuditDto Audit);

public sealed record PublicMenuPageDto(
    string Slug, string? Eyebrow, string Title, string Summary, IReadOnlyList<PublicSectionDto> Sections, PublicCtaDto? Cta);

public sealed record PublicSectionDto(string Heading, string Body);

public sealed record PublicCtaDto(string Label, string Href);

public sealed record GetAdminMenuQuery(string Key) : IRequest<Result<AdminMenuDto>>;

public sealed record SaveMenuCommand(string Key, MenuDoc Document) : IRequest<Result<AdminMenuDto>>;

public sealed record GetPublicMenuQuery(string Key, string Language) : IRequest<Result<PublicMenuDto>>;

public sealed record GetAdminMenuPagesQuery(int Page, int PageSize, string? Search, string? SortField, string? SortDir) : IRequest<Result<PagedResult<AdminMenuPageDto>>>;

public sealed record GetMenuPageOptionsQuery : IRequest<Result<List<AdminMenuPageDto>>>;

public sealed record GetAdminMenuPageQuery(long Id) : IRequest<Result<AdminMenuPageDto>>;

public sealed record SaveMenuPageCommand(long? Id, PageDetails Details) : IRequest<Result<AdminMenuPageDto>>;

public sealed record DeleteMenuPageCommand(long Id) : IRequest<Result<Deleted>>;

public sealed record GetPublicMenuPageSlugsQuery : IRequest<Result<List<string>>>;

public sealed record GetPublicMenuPageQuery(string Slug, string Language) : IRequest<Result<PublicMenuPageDto>>;

internal static class MenuJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static MenuDoc? Read(string json)
        => JsonSerializer.Deserialize<MenuDoc>(json, Options);

    public static string Write(MenuDoc doc) => JsonSerializer.Serialize(doc, Options);

    /// <summary>Every page a menu opens, so a page still in use cannot be deleted.</summary>
    public static IEnumerable<long> PageIds(MenuDoc doc)
        => doc.Groups.SelectMany(g => g.Items).Where(i => i.Target.Kind == "page" && i.Target.PageId != null).Select(i => i.Target.PageId!.Value);

    /// <summary>Validates the whole document; errors point at the box that is wrong (groups.0.items.1.label.en).</summary>
    public static async Task<List<Error>> ValidateAsync(IUnitOfWork unitOfWork, MenuDoc doc, CancellationToken ct)
    {
        var errors = new List<Error>();
        if (string.IsNullOrWhiteSpace(doc.Label?.En) || string.IsNullOrWhiteSpace(doc.Label?.Ar))
            errors.Add(MenuErrors.Field("label.en", "The menu needs a label in both languages."));
        if (!string.IsNullOrWhiteSpace(doc.Href) && !doc.Href.StartsWith('/'))
            errors.Add(MenuErrors.Field("href", "A link inside the site starts with /."));

        var pageIds = PageIds(doc).Distinct().ToList();
        var existing = (await unitOfWork.GetRepository<MenuPage>().GetTableNoTracking().Where(p => pageIds.Contains(p.Id)).Select(p => p.Id).ToListAsync(ct)).ToHashSet();

        var groups = doc.Groups ?? [];
        for (var g = 0; g < groups.Count; g++)
        {
            var group = groups[g];
            // Only the .en key is emitted, even when the Arabic is what is missing.
            if (string.IsNullOrWhiteSpace(group.Label?.En) || string.IsNullOrWhiteSpace(group.Label?.Ar))
                errors.Add(MenuErrors.Field($"groups.{g}.label.en", "Every column needs a label in both languages."));

            var items = group.Items ?? [];
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var key = $"groups.{g}.items.{i}";
                if (string.IsNullOrWhiteSpace(item.Label?.En) || string.IsNullOrWhiteSpace(item.Label?.Ar))
                    errors.Add(MenuErrors.Field($"{key}.label.en", "Every link needs a label in both languages."));

                if (item.Target?.Kind == "page")
                {
                    if (item.Target.PageId is not long id || !existing.Contains(id))
                        errors.Add(MenuErrors.Field($"{key}.page", "Choose a page that exists."));
                }
                else if (item.Target?.Kind == "link")
                {
                    if (string.IsNullOrWhiteSpace(item.Target.Href) || !item.Target.Href.StartsWith('/'))
                        errors.Add(MenuErrors.Field($"{key}.href", "A link inside the site starts with /."));
                }
                else
                {
                    errors.Add(MenuErrors.Field($"{key}.page", "Choose a page or a link."));
                }
            }
        }

        return errors;
    }
}

internal static class MenuPageMapper
{
    public static AdminMenuPageDto Admin(MenuPage p)
        => new(
            p.Id, p.Slug,
            p.EyebrowEn is null || p.EyebrowAr is null ? null : new LocalizedText(p.EyebrowEn, p.EyebrowAr),
            new LocalizedText(p.TitleEn, p.TitleAr), new LocalizedText(p.SummaryEn, p.SummaryAr),
            p.Sections.Select(s => new PageSectionDto(new LocalizedText(s.HeadingEn, s.HeadingAr), new LocalizedText(s.BodyEn, s.BodyAr))).ToList(),
            p.CtaLabelEn is null || p.CtaLabelAr is null || p.CtaHref is null ? null : new PageCtaDto(new LocalizedText(p.CtaLabelEn, p.CtaLabelAr), p.CtaHref),
            p.Status, AuditDto.From(p));
}

public class GetAdminMenuQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetAdminMenuQuery, Result<AdminMenuDto>>
{
    public async Task<Result<AdminMenuDto>> Handle(GetAdminMenuQuery request, CancellationToken ct)
    {
        var menu = await unitOfWork.GetRepository<Menu>().GetTableNoTracking().FirstOrDefaultAsync(m => m.Key == request.Key, ct);
        var doc = menu is null ? null : MenuJson.Read(menu.DocumentJson);
        return menu is null || doc is null ? MenuErrors.MenuNotFound : new AdminMenuDto(menu.Key, doc.Label, doc.Href, doc.Groups, AuditDto.From(menu));
    }
}

public class SaveMenuCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<SaveMenuCommand, Result<AdminMenuDto>>
{
    public async Task<Result<AdminMenuDto>> Handle(SaveMenuCommand request, CancellationToken ct)
    {
        var errors = await MenuJson.ValidateAsync(unitOfWork, request.Document, ct);
        if (errors.Count > 0)
        {
            return errors;
        }

        // The whole menu is replaced as one document; an unknown key is created.
        var repository = unitOfWork.GetRepository<Menu>();
        var menu = await repository.GetTableAsTracking().FirstOrDefaultAsync(m => m.Key == request.Key, ct);
        var json = MenuJson.Write(request.Document);
        if (menu is null)
        {
            menu = Menu.Create(request.Key, json);
            await repository.AddAsync(menu, ct);
        }
        else
        {
            menu.Replace(json);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return new AdminMenuDto(menu.Key, request.Document.Label, request.Document.Href, request.Document.Groups, AuditDto.From(menu));
    }
}

public class GetPublicMenuQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localization) : IRequestHandler<GetPublicMenuQuery, Result<PublicMenuDto>>
{
    public async Task<Result<PublicMenuDto>> Handle(GetPublicMenuQuery request, CancellationToken ct)
    {
        var menu = await unitOfWork.GetRepository<Menu>().GetTableNoTracking().FirstOrDefaultAsync(m => m.Key == request.Key, ct);
        var doc = menu is null ? null : MenuJson.Read(menu.DocumentJson);
        if (menu is null || doc is null)
        {
            return MenuErrors.MenuNotFound;
        }

        var pageIds = MenuJson.PageIds(doc).Distinct().ToList();
        var published = await unitOfWork.GetRepository<MenuPage>().GetTableNoTracking()
            .Where(p => pageIds.Contains(p.Id) && p.Status == "published").ToDictionaryAsync(p => p.Id, p => p.Slug, ct);

        string Pick(LocalizedText t) => localization.GetLocalizedName(t.Ar, t.En, request.Language);

        // Hidden items and items whose page is not published are dropped; columns left empty go with them.
        var groups = doc.Groups.Select(g => new PublicMenuGroupDto(
            g.Id, Pick(g.Label),
            g.Items.Where(i => i.IsVisible).Select(i => i.Target.Kind == "page"
                    ? (Item: i, Href: i.Target.PageId is long id && published.TryGetValue(id, out var slug) ? $"/specialisations/{slug}" : null)
                    : (Item: i, Href: i.Target.Href))
                .Where(x => x.Href is not null)
                .Select(x => new PublicMenuItemDto(x.Item.Id, Pick(x.Item.Label), x.Href!)).ToList()))
            .Where(g => g.Items.Count > 0).ToList();

        return new PublicMenuDto(menu.Key, Pick(doc.Label), doc.Href, groups);
    }
}

public class GetAdminMenuPagesQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetAdminMenuPagesQuery, Result<PagedResult<AdminMenuPageDto>>>
{
    public async Task<Result<PagedResult<AdminMenuPageDto>>> Handle(GetAdminMenuPagesQuery request, CancellationToken ct)
    {
        var query = unitOfWork.GetRepository<MenuPage>().GetTableNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(p => p.TitleEn.ToLower().Contains(term) || p.TitleAr.ToLower().Contains(term) || p.Slug.Contains(term));
        }

        var descending = string.Equals(request.SortDir, "desc", StringComparison.OrdinalIgnoreCase);
        query = request.SortField?.ToLowerInvariant() switch
        {
            "slug" => descending ? query.OrderByDescending(p => p.Slug) : query.OrderBy(p => p.Slug),
            "status" => descending ? query.OrderByDescending(p => p.Status) : query.OrderBy(p => p.Status),
            _ => descending ? query.OrderByDescending(p => p.TitleEn) : query.OrderBy(p => p.TitleEn)
        };

        var page = await query.ToPagedResultAsync(request.Page, request.PageSize, ct);
        return new PagedResult<AdminMenuPageDto>(page.Items.Select(MenuPageMapper.Admin).ToList(), page.Page, page.PageSize, page.TotalCount);
    }
}

public class GetMenuPageOptionsQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetMenuPageOptionsQuery, Result<List<AdminMenuPageDto>>>
{
    public async Task<Result<List<AdminMenuPageDto>>> Handle(GetMenuPageOptionsQuery request, CancellationToken ct)
        => (await unitOfWork.GetRepository<MenuPage>().GetTableNoTracking().OrderBy(p => p.TitleEn).ToListAsync(ct)).Select(MenuPageMapper.Admin).ToList();
}

public class GetAdminMenuPageQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetAdminMenuPageQuery, Result<AdminMenuPageDto>>
{
    public async Task<Result<AdminMenuPageDto>> Handle(GetAdminMenuPageQuery request, CancellationToken ct)
    {
        var page = await unitOfWork.GetRepository<MenuPage>().GetTableNoTracking().FirstOrDefaultAsync(p => p.Id == request.Id, ct);
        return page is null ? MenuErrors.PageNotFound : MenuPageMapper.Admin(page);
    }
}

public class SaveMenuPageCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<SaveMenuPageCommand, Result<AdminMenuPageDto>>
{
    public async Task<Result<AdminMenuPageDto>> Handle(SaveMenuPageCommand request, CancellationToken ct)
    {
        var repository = unitOfWork.GetRepository<MenuPage>();
        MenuPage page;
        if (request.Id is long id)
        {
            var existing = await repository.GetTableAsTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
            if (existing is null)
            {
                return MenuErrors.PageNotFound;
            }

            page = existing;
            var applied = page.Apply(request.Details);
            if (applied.IsError)
            {
                return applied.Errors;
            }
        }
        else
        {
            var created = MenuPage.Create(request.Details);
            if (created.IsError)
            {
                return created.Errors;
            }

            page = created.Value;
            await repository.AddAsync(page, ct);
        }

        var slug = page.Slug;
        if (await repository.GetTableNoTracking().AnyAsync(p => p.Slug == slug && p.Id != page.Id, ct))
        {
            return MenuErrors.SlugTaken;
        }

        await unitOfWork.SaveChangesAsync(ct);
        return MenuPageMapper.Admin(page);
    }
}

public class DeleteMenuPageCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteMenuPageCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(DeleteMenuPageCommand request, CancellationToken ct)
    {
        var repository = unitOfWork.GetRepository<MenuPage>();
        var page = await repository.GetTableAsTracking().FirstOrDefaultAsync(p => p.Id == request.Id, ct);
        if (page is null)
        {
            return MenuErrors.PageNotFound;
        }

        // A page that a menu item still opens cannot be deleted.
        var users = new List<string>();
        foreach (var menu in await unitOfWork.GetRepository<Menu>().GetTableNoTracking().ToListAsync(ct))
        {
            var doc = MenuJson.Read(menu.DocumentJson);
            if (doc is not null && MenuJson.PageIds(doc).Contains(page.Id)) users.Add(menu.Key);
        }

        if (users.Count > 0)
        {
            return MenuErrors.PageInUse(string.Join(", ", users));
        }

        repository.DeleteAsync(page);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Deleted;
    }
}

public class GetPublicMenuPageSlugsQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetPublicMenuPageSlugsQuery, Result<List<string>>>
{
    public async Task<Result<List<string>>> Handle(GetPublicMenuPageSlugsQuery request, CancellationToken ct)
        => await unitOfWork.GetRepository<MenuPage>().GetTableNoTracking().Where(p => p.Status == "published").OrderBy(p => p.Slug).Select(p => p.Slug).ToListAsync(ct);
}

public class GetPublicMenuPageQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localization) : IRequestHandler<GetPublicMenuPageQuery, Result<PublicMenuPageDto>>
{
    public async Task<Result<PublicMenuPageDto>> Handle(GetPublicMenuPageQuery request, CancellationToken ct)
    {
        // Not published answers not-found.
        var p = await unitOfWork.GetRepository<MenuPage>().GetTableNoTracking().FirstOrDefaultAsync(x => x.Slug == request.Slug && x.Status == "published", ct);
        if (p is null)
        {
            return MenuErrors.PageNotFound;
        }

        string Pick(string ar, string en) => localization.GetLocalizedName(ar, en, request.Language);
        return new PublicMenuPageDto(
            p.Slug,
            p.EyebrowEn is null || p.EyebrowAr is null ? null : Pick(p.EyebrowAr, p.EyebrowEn),
            Pick(p.TitleAr, p.TitleEn), Pick(p.SummaryAr, p.SummaryEn),
            p.Sections.Select(s => new PublicSectionDto(Pick(s.HeadingAr, s.HeadingEn), Pick(s.BodyAr, s.BodyEn))).ToList(),
            p.CtaLabelEn is null || p.CtaLabelAr is null || p.CtaHref is null ? null : new PublicCtaDto(Pick(p.CtaLabelAr, p.CtaLabelEn), p.CtaHref));
    }
}
