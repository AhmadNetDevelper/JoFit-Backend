using JoFit.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace JoFit.Infrastructure.Services;

public class PrivateFileStorage(IConfiguration configuration) : IPrivateFileStorage
{
    private string Folder => Path.Combine(
        Directory.GetCurrentDirectory(),
        configuration["FileUpload:PrivateFolder"] ?? "App_Data/private");

    public async Task SaveAsync(string key, byte[] bytes, CancellationToken ct)
    {
        Directory.CreateDirectory(Folder);
        var path = SafePath(key) ?? throw new ArgumentException("Unsafe storage key.", nameof(key));
        await File.WriteAllBytesAsync(path, bytes, ct);
    }

    public Stream? Open(string key)
    {
        var path = SafePath(key);
        return path is not null && File.Exists(path) ? File.OpenRead(path) : null;
    }

    public Task DeleteAsync(string key, CancellationToken ct)
    {
        var path = SafePath(key);
        if (path is not null && File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    /// <summary>Plain file names inside the private folder only: no separators, no traversal.</summary>
    private string? SafePath(string key)
        => string.IsNullOrWhiteSpace(key) || key != Path.GetFileName(key) || key.Contains("..") ? null : Path.Combine(Folder, key);
}
