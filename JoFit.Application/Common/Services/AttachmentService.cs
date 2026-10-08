using JoFit.Application.Common.Interfaces;
using JoFit.Domain.Attachments;
using JoFit.Domain.Common.Results;
using Microsoft.AspNetCore.Http;

namespace JoFit.Application.Common.Services;

/// <summary>
/// Service for handling entity file uploads and attachments.
/// Manages validation and creation of attachment entities.
/// </summary>
public class AttachmentService : IAttachmentService
{
    private readonly IFileUploadService _fileUploadService;
    private static readonly string[] AllowedFileExtensions = { ".pdf", ".jpg", ".jpeg", ".png", ".gif", ".webp", ".jfif", ".doc", ".docx", ".mp4", ".mov", ".avi", ".webm", ".mkv", ".m4v" };
    private const long MaxFileSizeBytes = 200L * 1024 * 1024; // 200 MB

    public AttachmentService(IFileUploadService fileUploadService)
    {
        _fileUploadService = fileUploadService;
    }

    /// <summary>
    /// Processes attachments for any entity type.
    /// </summary>
    /// <param name="entityId">The ID of the entity</param>
    /// <param name="enumEntityTypes">The type of entity</param>
    /// <param name="filesBySection">Dictionary mapping section types to file lists</param>
    /// <returns>List of created Attachment entities or error</returns>
    public async Task<Result<List<Attachment>>> CreateAttachmentsBySectionAsync(
        long entityId,
        Contracts.Dtos.Attachments.EnumEntityTypes enumEntityTypes,
        Dictionary<Contracts.Dtos.Attachments.EnumEntitySectionTypes, List<IFormFile>>? filesBySection)
    {
        var attachments = new List<Attachment>();

        if (filesBySection == null || !filesBySection.Any())
        {
            return attachments; // Return empty list if no files provided
        }

        // Process files by section type
        foreach (var (sectionType, files) in filesBySection)
        {
            if (files == null || !files.Any())
                continue;

            foreach (var file in files)
            {
                // Validate file
                var validation = ValidateFile(file, $"File in {sectionType}");
                if (validation.IsError)
                    return validation.Errors;

                // Get file type from extension
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                var fileType = GetFileType(fileExtension);

                // Upload file
                var filePath = await _fileUploadService.UploadFileAsync(file, GetUploadFolder(enumEntityTypes, entityId));
                if (string.IsNullOrEmpty(filePath))
                {
                    return Error.Failure(
                        "Attachment.FileUploadFailed",
                        $"Failed to upload file: {file.FileName}");
                }

                // Create attachment entity - cast Contracts enum to Domain enum
                var domainSectionType = (EnumEntitySectionTypes)(int)sectionType;
                var attachment = Attachment.Create(
                    fileName: file.FileName,
                    fileTypeId: fileType,
                    fileSize: (int)(file.Length / 1024), // Size in KB
                    filePath: filePath,
                    entityId: entityId,
                    entityTypeId: (EnumEntityTypes)(int)enumEntityTypes,
                    entitySectionTypeId: domainSectionType);

                if (attachment.IsError)
                    return attachment.Errors;

                attachments.Add(attachment.Value);
            }
        }

        return attachments;
    }

    /// <summary>
    /// Processes attachments with metadata (e.g., descriptions) for generic file handling.
    /// Supports optional file descriptions for better file organization and documentation.
    /// </summary>
    /// <param name="entityId">The ID of the entity</param>
    /// <param name="enumEntityTypes">The type of entity</param>
    /// <param name="filesWithMetadataBySection">Dictionary mapping section types to files with metadata (description)</param>
    /// <returns>List of created Attachment entities or error</returns>
    public async Task<Result<List<Attachment>>> CreateAttachmentsBySectionWithMetadataAsync(
        long entityId,
        Contracts.Dtos.Attachments.EnumEntityTypes enumEntityTypes,
        Dictionary<Contracts.Dtos.Attachments.EnumEntitySectionTypes, List<Contracts.Dtos.Attachments.FileWithMetadata>>? filesWithMetadataBySection)
    {
        var attachments = new List<Attachment>();

        if (filesWithMetadataBySection == null || !filesWithMetadataBySection.Any())
        {
            return attachments; // Return empty list if no files provided
        }

        // Process files by section type
        foreach (var (sectionType, filesWithMetadata) in filesWithMetadataBySection)
        {
            if (filesWithMetadata == null || !filesWithMetadata.Any())
                continue;

            foreach (var fileWithMetadata in filesWithMetadata)
            {
                var file = fileWithMetadata.File;
                var description = fileWithMetadata.Description;

                // Validate file
                var validation = ValidateFile(file, $"File in {sectionType}");
                if (validation.IsError)
                    return validation.Errors;

                // Validate description length if provided
                if (!string.IsNullOrEmpty(description) && description.Length > 150)
                {
                    return Error.Validation(
                        "Attachment.DescriptionTooLong",
                        $"Description must not exceed 150 characters");
                }

                // Get file type from extension
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                var fileType = GetFileType(fileExtension);

                // Upload file
                var filePath = await _fileUploadService.UploadFileAsync(file, GetUploadFolder(enumEntityTypes, entityId));
                if (string.IsNullOrEmpty(filePath))
                {
                    return Error.Failure(
                        "Attachment.FileUploadFailed",
                        $"Failed to upload file: {file.FileName}");
                }

                // Create attachment entity - cast Contracts enum to Domain enum
                var domainSectionType = (EnumEntitySectionTypes)(int)sectionType;
                var attachment = Attachment.Create(
                    fileName: file.FileName,
                    fileTypeId: fileType,
                    fileSize: (int)(file.Length / 1024), // Size in KB
                    filePath: filePath,
                    entityId: entityId,
                    entityTypeId: (EnumEntityTypes)(int)enumEntityTypes,
                    entitySectionTypeId: domainSectionType,
                    description: description);

                if (attachment.IsError)
                    return attachment.Errors;

                attachments.Add(attachment.Value);
            }
        }

        return attachments;
    }

    /// <summary>
    /// Validates file size and extension.
    /// </summary>
    private Result<bool> ValidateFile(IFormFile file, string fileLabel)
    {
        if (file.Length == 0)
        {
            return Error.Validation(
                "Attachment.FileEmpty",
                $"{fileLabel} is empty");
        }

        if (file.Length > MaxFileSizeBytes)
        {
            return Error.Validation(
                "Attachment.FileTooLarge",
                $"{fileLabel} exceeds maximum size of {MaxFileSizeBytes / (1024 * 1024)} MB");
        }

        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedFileExtensions.Contains(fileExtension))
        {
            return Error.Validation(
                "Attachment.InvalidFileType",
                $"{fileLabel} has unsupported file type. Allowed: {string.Join(", ", AllowedFileExtensions)}");
        }

        return true;
    }

    /// <summary>
    /// Determines file type based on extension.
    /// </summary>
    private static EnumFileTypes GetFileType(string fileExtension)
    {
        return fileExtension.ToLowerInvariant() switch
        {
            ".pdf" => EnumFileTypes.Document,
            ".doc" or ".docx" => EnumFileTypes.Document,
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" or ".jfif" => EnumFileTypes.Image,
            ".mp4" or ".avi" or ".mov" or ".webm" or ".mkv" or ".m4v" => EnumFileTypes.Video,
            _ => EnumFileTypes.Document
        };
    }

    /// <summary>
    /// Builds the upload folder for an entity, e.g. "user/15".
    /// </summary>
    private static string GetUploadFolder(Contracts.Dtos.Attachments.EnumEntityTypes enumEntityTypes, long entityId)
        => $"{enumEntityTypes.ToString().ToLowerInvariant()}/{entityId}";
}
