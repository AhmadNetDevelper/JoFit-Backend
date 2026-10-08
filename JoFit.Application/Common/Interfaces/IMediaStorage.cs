namespace JoFit.Application.Common.Interfaces;

public sealed record StoredImage(string StorageKey, string MimeType, long Bytes, int? Width, int? Height);

public sealed record MediaFile(Stream Content, string MimeType, string FileName);

/// <summary>Stores uploaded images and serves them back.</summary>
public interface IMediaStorage
{
    /// <summary>Detects the type by magic bytes. Returns the MIME type, or null when it is not JPEG, PNG, WebP or AVIF.</summary>
    string? DetectImageMimeType(ReadOnlySpan<byte> bytes);

    /// <summary>
    /// Re-encodes the image without its metadata (EXIF and GPS), applies the orientation, and stores it.
    /// Returns null when the bytes are not a readable image.
    /// </summary>
    Task<StoredImage?> StoreImageAsync(byte[] bytes, string mimeType, string publicId, CancellationToken ct);

    Task DeleteAsync(string storageKey, CancellationToken ct);

    /// <summary>Opens a stored image, or null when the key is unknown or unsafe.</summary>
    MediaFile? Open(string fileName);
}
