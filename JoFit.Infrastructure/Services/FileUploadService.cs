using JoFit.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JoFit.Infrastructure.Services;

public class FileUploadService(IConfiguration configuration, ILogger<FileUploadService> logger) : IFileUploadService
{
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<FileUploadService> _logger = logger;

    public async Task<string> UploadFileAsync(IFormFile file, string folder, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get upload settings from configuration
            var uploadsFolder = _configuration["FileUpload:UploadFolder"] ?? "wwwroot/uploads";
            var baseUrl = _configuration["FileUpload:BaseUrl"] ?? "/uploads";

            // Create folder if it doesn't exist
            var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), uploadsFolder, folder);
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            // Generate unique filename
            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var filePath = Path.Combine(uploadPath, fileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            // Return relative URL
            var relativeUrl = $"{baseUrl}/{folder}/{fileName}";
            _logger.LogInformation("File uploaded successfully: {FilePath}", relativeUrl);

            return relativeUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file: {FileName}", file.FileName);
            throw;
        }
    }

    public async Task<string> UploadBytesAsync(byte[] bytes, string fileName, string folder, CancellationToken cancellationToken = default)
    {
        var uploadsFolder = _configuration["FileUpload:UploadFolder"] ?? "wwwroot/uploads";
        var baseUrl = _configuration["FileUpload:BaseUrl"] ?? "/uploads";

        var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), uploadsFolder, folder);
        if (!Directory.Exists(uploadPath))
            Directory.CreateDirectory(uploadPath);

        var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
        var filePath = Path.Combine(uploadPath, uniqueFileName);

        await File.WriteAllBytesAsync(filePath, bytes, cancellationToken);

        var relativeUrl = $"{baseUrl}/{folder}/{uniqueFileName}";
        _logger.LogInformation("File uploaded successfully: {FilePath}", relativeUrl);

        return relativeUrl;
    }

    public async Task<bool> DeleteFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return false;
            }

            var uploadsFolder = _configuration["FileUpload:UploadFolder"] ?? "wwwroot/uploads";
            var baseUrl = _configuration["FileUpload:BaseUrl"] ?? "/uploads";

            // Convert URL to file path
            var relativePath = filePath.Replace(baseUrl, "").TrimStart('/');
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), uploadsFolder, relativePath);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("File deleted successfully: {FilePath}", filePath);
                return true;
            }

            _logger.LogWarning("File not found for deletion: {FilePath}", filePath);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {FilePath}", filePath);
            return false;
        }
    }

    public string ValidateFile(IFormFile file, string[] allowedExtensions, int maxSizeInMB = 300)
    {
        if (file == null || file.Length == 0)
        {
            return "File is required";
        }

        // Check file size
        var maxSizeInBytes = (long)maxSizeInMB * 1024 * 1024;
        if (file.Length > maxSizeInBytes)
        {
            return $"File size must not exceed {maxSizeInMB} MB";
        }

        // Check file extension
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(fileExtension))
        {
            return $"File type must be one of: {string.Join(", ", allowedExtensions)}";
        }

        return string.Empty;
    }
}
