using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Media;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Common.Services;

/// <summary>An image chosen for a gallery: an uploaded media id and its alt text for this use.</summary>
public sealed record GalleryItem(string Id, string? Alt);

/// <summary>Reads and saves the ordered image galleries of records. The first image is the cover.</summary>
public static class GalleryService
{
    public const int MaxImages = 12;

    /// <summary>
    /// Replaces the gallery of a record. Each id must be an uploaded image; its url and size come from the upload,
    /// never from the request, so only images on our own media origin can be stored.
    /// </summary>
    public static async Task<Result<Updated>> SyncAsync(
        IUnitOfWork unitOfWork,
        IUrlService urlService,
        string ownerType,
        long ownerId,
        string field,
        IReadOnlyList<GalleryItem> items,
        CancellationToken ct)
    {
        if (items.Count > MaxImages)
        {
            return Error.Validation("Media.TooMany", $"A gallery holds at most {MaxImages} images.").ForField(field);
        }

        var errors = new List<Error>();
        var seen = new HashSet<string>();
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (string.IsNullOrWhiteSpace(item.Id))
                errors.Add(Error.Validation("Media.IdRequired", $"Image {i + 1}: the image needs an id.").ForField(field));
            else if (!seen.Add(item.Id))
                errors.Add(Error.Validation("Media.Duplicate", $"Image {i + 1}: this image is already in the gallery.").ForField(field));
            else if (item.Alt is { Length: > 300 })
                errors.Add(Error.Validation("Media.AltTooLong", $"Image {i + 1}: the description cannot exceed 300 characters.").ForField(field));
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        var ids = items.Select(x => x.Id).ToList();
        var assets = await unitOfWork.GetRepository<MediaAsset>().GetTableNoTracking()
            .Where(a => ids.Contains(a.PublicId))
            .ToDictionaryAsync(a => a.PublicId, ct);

        for (var i = 0; i < items.Count; i++)
        {
            if (!assets.ContainsKey(items[i].Id))
            {
                errors.Add(Error.Validation("Media.Unknown", $"Image {i + 1}: that image was not uploaded.").ForField(field));
            }
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        var repository = unitOfWork.GetRepository<RecordMedia>();
        var existing = await repository.GetTableAsTracking()
            .Where(x => x.OwnerType == ownerType && x.OwnerId == ownerId && x.Field == field)
            .ToListAsync(ct);
        if (existing.Count > 0)
        {
            unitOfWork.GetDbContext().RemoveRange(existing);
        }

        var rows = items.Select((item, index) =>
        {
            var asset = assets[item.Id];
            return RecordMedia.Create(
                ownerType, ownerId, field, index, asset.PublicId,
                urlService.ToAbsoluteUrl($"/media/{asset.StorageKey}")!,
                item.Alt?.Trim() ?? asset.Alt ?? string.Empty, asset.Width, asset.Height);
        }).ToList();
        await repository.AddRangeAsync(rows, ct);
        return Result.Updated;
    }

    /// <summary>The galleries of many records in display order, keyed by owner id.</summary>
    public static async Task<Dictionary<long, List<MediaImageDto>>> LoadAsync(
        IUnitOfWork unitOfWork, string ownerType, string field, IReadOnlyCollection<long> ownerIds, CancellationToken ct)
    {
        if (ownerIds.Count == 0)
        {
            return [];
        }

        var rows = await unitOfWork.GetRepository<RecordMedia>().GetTableNoTracking()
            .Where(x => x.OwnerType == ownerType && x.Field == field && ownerIds.Contains(x.OwnerId))
            .OrderBy(x => x.OwnerId).ThenBy(x => x.Position)
            .ToListAsync(ct);

        return rows
            .GroupBy(x => x.OwnerId)
            .ToDictionary(g => g.Key, g => g.Select(x => new MediaImageDto(x.MediaId, x.Url, x.Alt, x.Width, x.Height)).ToList());
    }
}
