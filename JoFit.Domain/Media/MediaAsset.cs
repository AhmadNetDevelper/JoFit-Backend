using JoFit.Domain.Common;

namespace JoFit.Domain.Media;

/// <summary>An uploaded image. The bytes live in file storage, addressed by <see cref="StorageKey"/>.</summary>
public class MediaAsset : AuditableEntity
{
    /// <summary>Id on the wire: "media-&lt;uuid&gt;".</summary>
    public string PublicId { get; private set; } = string.Empty;
    public string StorageKey { get; private set; } = string.Empty;
    public string MimeType { get; private set; } = string.Empty;
    public long Bytes { get; private set; }
    public int? Width { get; private set; }
    public int? Height { get; private set; }
    public string? Alt { get; private set; }
    public string Purpose { get; private set; } = string.Empty;
    public long? UploadedBy { get; private set; }

    private MediaAsset()
    {
    }

    public static MediaAsset Create(
        string publicId, string storageKey, string mimeType, long bytes, int? width, int? height,
        string? alt, string purpose, long? uploadedBy)
        => new()
        {
            PublicId = publicId,
            StorageKey = storageKey,
            MimeType = mimeType,
            Bytes = bytes,
            Width = width,
            Height = height,
            Alt = alt,
            Purpose = purpose,
            UploadedBy = uploadedBy
        };
}
