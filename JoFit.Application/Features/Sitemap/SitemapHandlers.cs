using JoFit.Application.Common.Interfaces;
using JoFit.Domain.Articles;
using JoFit.Domain.Coaches;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Courses;
using JoFit.Domain.Products;
using JoFit.Domain.Rental;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.Sitemap;

/// <summary>A public address and when it last changed.</summary>
public sealed record SitemapEntryDto(string Slug, DateTimeOffset? LastModified);

public sealed record GetSitemapQuery(string Area) : IRequest<Result<List<SitemapEntryDto>>>;

public static class SitemapAreas
{
    public static readonly string[] All = ["shop", "rental", "coaches", "education", "content", "careers"];
}

public class GetSitemapQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetSitemapQuery, Result<List<SitemapEntryDto>>>
{
    public async Task<Result<List<SitemapEntryDto>>> Handle(GetSitemapQuery request, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();

        // Only what is public is listed: published products, rental items, courses and articles, and approved coaches.
        switch (request.Area)
        {
            case "shop":
                return (await db.Set<Product>().AsNoTracking().Where(p => p.Status == ProductStatuses.Published)
                    .Select(p => new { p.Slug, Modified = p.LastModifiedUtc ?? p.CreatedAtUtc }).OrderBy(p => p.Slug).ToListAsync(ct))
                    .Select(p => new SitemapEntryDto(p.Slug, p.Modified)).ToList();
            case "rental":
                return (await db.Set<RentalItem>().AsNoTracking().Where(p => p.Status == "published")
                    .Select(p => new { p.Slug, Modified = p.LastModifiedUtc ?? p.CreatedAtUtc }).OrderBy(p => p.Slug).ToListAsync(ct))
                    .Select(p => new SitemapEntryDto(p.Slug, p.Modified)).ToList();
            case "coaches":
                return (await db.Set<Coach>().AsNoTracking().Where(p => p.VerificationStatus == CoachVerification.Approved)
                    .Select(p => new { p.Slug, Modified = p.LastModifiedUtc ?? p.CreatedAtUtc }).OrderBy(p => p.Slug).ToListAsync(ct))
                    .Select(p => new SitemapEntryDto(p.Slug, p.Modified)).ToList();
            case "education":
                return (await db.Set<Course>().AsNoTracking().Where(p => p.Status == CourseStatuses.Published && p.Slug != null)
                    .Select(p => new { Slug = p.Slug!, Modified = p.LastModifiedUtc ?? p.CreatedAtUtc }).OrderBy(p => p.Slug).ToListAsync(ct))
                    .Select(p => new SitemapEntryDto(p.Slug, p.Modified)).ToList();
            case "content":
                return (await db.Set<Article>().AsNoTracking().Where(p => p.Status == ArticleStatuses.Published)
                    .Select(p => new { p.Slug, Modified = p.LastModifiedUtc ?? p.CreatedAtUtc }).OrderBy(p => p.Slug).ToListAsync(ct))
                    .Select(p => new SitemapEntryDto(p.Slug, p.Modified)).ToList();
            case "careers":
                // Public jobs and verified employers only.
                var board = await Careers.PublicBoardAccess.LoadAsync(unitOfWork, ct);
                return board.Select(j => new SitemapEntryDto(j.Job.Slug, j.Job.LastModifiedUtc ?? j.Job.CreatedAtUtc))
                    .Concat(board.Select(j => j.Employer).DistinctBy(e => e.Id).Select(e => new SitemapEntryDto("employers/" + e.Slug, e.LastModifiedUtc ?? e.CreatedAtUtc)))
                    .OrderBy(e => e.Slug).ToList();
            default:
                return Error.NotFound("Sitemap.NotFound", "There is no sitemap for that area.");
        }
    }
}
