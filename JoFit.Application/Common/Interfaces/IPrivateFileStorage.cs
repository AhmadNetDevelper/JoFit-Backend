namespace JoFit.Application.Common.Interfaces;

/// <summary>A private file opened for download.</summary>
public sealed record PrivateFile(Stream Content, string MimeType, string FileName);

/// <summary>
/// Storage for files that are never public (CVs). They live outside the web root and are served only through an
/// endpoint that checks who is asking.
/// </summary>
public interface IPrivateFileStorage
{
    Task SaveAsync(string key, byte[] bytes, CancellationToken ct);

    /// <summary>Opens a stored file, or null when it is missing or the key is unsafe.</summary>
    Stream? Open(string key);

    Task DeleteAsync(string key, CancellationToken ct);
}
