using JoFit.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace JoFit.Infrastructure.Services;

public class MediaStorage(IConfiguration configuration, ILogger<MediaStorage> logger) : IMediaStorage
{
    private static readonly Dictionary<string, string> Extensions = new()
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
        ["image/avif"] = ".avif"
    };

    private string Folder => Path.Combine(
        Directory.GetCurrentDirectory(),
        configuration["FileUpload:UploadFolder"] ?? "wwwroot/uploads",
        "media");

    public string? DetectImageMimeType(ReadOnlySpan<byte> b)
    {
        if (b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF)
        {
            return "image/jpeg";
        }

        if (b.Length >= 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47
            && b[4] == 0x0D && b[5] == 0x0A && b[6] == 0x1A && b[7] == 0x0A)
        {
            return "image/png";
        }

        // RIFF....WEBP
        if (b.Length >= 12 && b[0] == 'R' && b[1] == 'I' && b[2] == 'F' && b[3] == 'F'
            && b[8] == 'W' && b[9] == 'E' && b[10] == 'B' && b[11] == 'P')
        {
            return "image/webp";
        }

        // ....ftypavif / ....ftypavis
        if (b.Length >= 12 && b[4] == 'f' && b[5] == 't' && b[6] == 'y' && b[7] == 'p'
            && b[8] == 'a' && b[9] == 'v' && b[10] == 'i' && (b[11] == 'f' || b[11] == 's'))
        {
            return "image/avif";
        }

        return null;
    }

    public async Task<StoredImage?> StoreImageAsync(byte[] bytes, string mimeType, string publicId, CancellationToken ct)
    {
        Directory.CreateDirectory(Folder);
        var key = publicId + Extensions[mimeType];
        var path = Path.Combine(Folder, key);

        // ImageSharp does not decode AVIF: keep the bytes as uploaded and skip the dimensions.
        if (mimeType == "image/avif")
        {
            await File.WriteAllBytesAsync(path, bytes, ct);
            return new StoredImage(key, mimeType, bytes.LongLength, null, null);
        }

        try
        {
            using var image = Image.Load(bytes);
            image.Mutate(x => x.AutoOrient());
            image.Metadata.ExifProfile = null;
            image.Metadata.IccProfile = null;
            image.Metadata.IptcProfile = null;
            image.Metadata.XmpProfile = null;

            var format = image.Metadata.DecodedImageFormat ?? throw new InvalidOperationException("Unknown image format.");
            await using (var stream = File.Create(path))
            {
                await image.SaveAsync(stream, image.Configuration.ImageFormatsManager.GetEncoder(format), ct);
            }

            return new StoredImage(key, mimeType, new FileInfo(path).Length, image.Width, image.Height);
        }
        catch (Exception ex) when (ex is UnknownImageFormatException or InvalidImageContentException or NotSupportedException)
        {
            logger.LogWarning(ex, "Rejected an unreadable image upload");
            return null;
        }
    }

    public Task DeleteAsync(string storageKey, CancellationToken ct)
    {
        var path = SafePath(storageKey);
        if (path is not null && File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    public MediaFile? Open(string fileName)
    {
        var path = SafePath(fileName);
        if (path is null || !File.Exists(path))
        {
            return null;
        }

        var mime = Extensions.FirstOrDefault(x => x.Value == Path.GetExtension(path).ToLowerInvariant()).Key;
        return mime is null ? null : new MediaFile(File.OpenRead(path), mime, Path.GetFileName(path));
    }

    /// <summary>Only plain file names inside the media folder are allowed (no separators, no traversal).</summary>
    private string? SafePath(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName) || fileName != Path.GetFileName(fileName) || fileName.Contains(".."))
        {
            return null;
        }

        return Path.Combine(Folder, fileName);
    }
}
