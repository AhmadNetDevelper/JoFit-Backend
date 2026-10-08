using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Media;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.Media.Commands;

/// <summary>Who is uploading and what they may upload.</summary>
public enum MediaUploadKind
{
    /// <summary>Staff, any :write permission. JPEG, PNG, WebP or AVIF up to 10 MB.</summary>
    Admin,

    /// <summary>An employer's logo. PNG, JPEG or WebP up to 2 MB. Authorised by the employer role at the API.</summary>
    EmployerLogo
}

public sealed record UploadMediaCommand(byte[]? Bytes, string? Purpose, string? Alt, MediaUploadKind Kind)
    : IRequest<Result<MediaImageDto>>;

public sealed record DeleteMediaCommand(string PublicId) : IRequest<Result<Deleted>>;

public class UploadMediaCommandHandler(
    IUnitOfWork unitOfWork,
    IMediaStorage storage,
    IUrlService urlService,
    IUser currentUser,
    IIdentityService identityService)
    : IRequestHandler<UploadMediaCommand, Result<MediaImageDto>>
{
    private static readonly string[] AdminPurposes =
    [
        "product", "product-category", "brand-logo", "sport", "rental-item",
        "coach-avatar", "coach-gallery", "course", "article-cover", "author-avatar"
    ];

    private static readonly string[] WritePermissionActions = ["Write", "Create", "Update", "Delete"];

    public async Task<Result<MediaImageDto>> Handle(UploadMediaCommand request, CancellationToken ct)
    {
        var employer = request.Kind == MediaUploadKind.EmployerLogo;
        var maxMegabytes = employer ? 2 : 10;
        var fileField = employer ? "logo" : "file";
        string[] allowedMimes = employer ? ["image/jpeg", "image/png", "image/webp"] : ["image/jpeg", "image/png", "image/webp", "image/avif"];
        string[] purposes = employer ? ["employer-logo"] : AdminPurposes;

        if (!employer && !await CanUploadAsync(ct))
        {
            return MediaErrors.UploadForbidden;
        }

        // Every problem is reported together in one answer.
        var errors = new List<Error>();
        if (string.IsNullOrWhiteSpace(request.Purpose) || !purposes.Contains(request.Purpose))
        {
            errors.Add(MediaErrors.PurposeInvalid);
        }

        if (request.Alt is { Length: > 300 })
        {
            // An alt over 300 characters is truncated on upload; it is only refused when saved on a record.
        }

        string? mime = null;
        if (request.Bytes is null || request.Bytes.Length == 0)
        {
            errors.Add(MediaErrors.FileRequired.ForField(fileField));
        }
        else if (request.Bytes.LongLength > maxMegabytes * 1024L * 1024L)
        {
            errors.Add(MediaErrors.TooLarge(maxMegabytes, fileField));
        }
        else
        {
            mime = storage.DetectImageMimeType(request.Bytes);
            if (mime is null || !allowedMimes.Contains(mime))
            {
                errors.Add(MediaErrors.FileTypeNotAllowed.ForField(fileField));
            }
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        var publicId = $"media-{Guid.NewGuid():N}";
        var stored = await storage.StoreImageAsync(request.Bytes!, mime!, publicId, ct);
        if (stored is null)
        {
            return MediaErrors.FileUnreadable.ForField(fileField);
        }

        var alt = request.Alt is { Length: > 300 } ? request.Alt[..300] : request.Alt?.Trim();
        var asset = MediaAsset.Create(publicId, stored.StorageKey, stored.MimeType, stored.Bytes, stored.Width, stored.Height, alt, request.Purpose!, currentUser.Id);
        await unitOfWork.GetRepository<MediaAsset>().AddAsync(asset, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return new MediaImageDto(publicId, urlService.ToAbsoluteUrl($"/media/{stored.StorageKey}")!, alt ?? string.Empty, stored.Width, stored.Height);
    }

    private async Task<bool> CanUploadAsync(CancellationToken ct)
    {
        if (currentUser.Id is not long userId)
        {
            return false;
        }

        var permissions = await identityService.GetUserPermissionsAsync(userId, ct);
        return permissions.Any(p => WritePermissionActions.Contains(p[(p.LastIndexOf('.') + 1)..]));
    }
}

public class DeleteMediaCommandHandler(IUnitOfWork unitOfWork, IMediaStorage storage)
    : IRequestHandler<DeleteMediaCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(DeleteMediaCommand request, CancellationToken ct)
    {
        var repository = unitOfWork.GetRepository<MediaAsset>();
        var asset = await repository.GetTableAsTracking().FirstOrDefaultAsync(x => x.PublicId == request.PublicId, ct);
        if (asset is null)
        {
            return MediaErrors.NotFound;
        }

        await storage.DeleteAsync(asset.StorageKey, ct);
        repository.DeleteAsync(asset);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Deleted;
    }
}
