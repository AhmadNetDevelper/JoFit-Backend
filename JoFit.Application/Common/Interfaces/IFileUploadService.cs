using Microsoft.AspNetCore.Http;

namespace JoFit.Application.Common.Interfaces;

/// <summary>
/// Interface for file upload operations
/// </summary>
public interface IFileUploadService
{
    /// <summary>
    /// Uploads a file to the storage
    /// </summary>
    /// <param name="file">The file to upload</param>
    /// <param name="folder">The folder path where the file will be stored</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The URL or path of the uploaded file</returns>
    Task<string> UploadFileAsync(IFormFile file, string folder, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from storage
    /// </summary>
    /// <param name="filePath">The path or URL of the file to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deletion was successful</returns>
    Task<bool> DeleteFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads raw bytes as a file to storage (used by background jobs where IFormFile is unavailable)
    /// </summary>
    Task<string> UploadBytesAsync(byte[] bytes, string fileName, string folder, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if the file meets the requirements
    /// </summary>
    /// <param name="file">The file to validate</param>
    /// <param name="allowedExtensions">List of allowed file extensions (e.g., ".jpg", ".png")</param>
    /// <param name="maxSizeInMB">Maximum file size in MB</param>
    /// <returns>Validation error message, or empty string if valid</returns>
    string ValidateFile(IFormFile file, string[] allowedExtensions, int maxSizeInMB = 5);
}
