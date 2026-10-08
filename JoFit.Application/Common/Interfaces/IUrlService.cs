namespace JoFit.Application.Common.Interfaces;

/// <summary>
/// Interface for URL building operations
/// </summary>
public interface IUrlService
{
    /// <summary>
    /// Converts a relative file path to a full absolute URL
    /// </summary>
    /// <param name="relativePath">The relative path (e.g., /uploads/carbrands/image.jpg)</param>
    /// <returns>The full URL (e.g., https://yourdomain.com/uploads/carbrands/image.jpg)</returns>
    string? ToAbsoluteUrl(string? relativePath);
}
