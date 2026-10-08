using JoFit.Domain.Attachments;
using JoFit.Domain.Common.Results;
using Microsoft.AspNetCore.Http;

namespace JoFit.Application.Common.Interfaces;

/// <summary>
/// Interface for handling entity file uploads and attachments.
/// </summary>
public interface IAttachmentService
{
    /// <summary>
    /// Processes attachments for any entity type.
    /// </summary>
    /// <param name="entityId">The ID of the entity</param>
    /// <param name="enumEntityTypes">The type of entity</param>
    /// <param name="filesBySection">Dictionary mapping section types to file lists</param>
    /// <returns>List of created Attachment entities or error</returns>
    Task<Result<List<Attachment>>> CreateAttachmentsBySectionAsync(
        long entityId,
        Contracts.Dtos.Attachments.EnumEntityTypes enumEntityTypes,
        Dictionary<Contracts.Dtos.Attachments.EnumEntitySectionTypes, List<IFormFile>>? filesBySection);

    /// <summary>
    /// Processes attachments with metadata (e.g., descriptions) for generic file handling.
    /// Supports optional file descriptions for better file organization and documentation.
    /// </summary>
    /// <param name="entityId">The ID of the entity</param>
    /// <param name="enumEntityTypes">The type of entity</param>
    /// <param name="filesWithMetadataBySection">Dictionary mapping section types to files with metadata (description)</param>
    /// <returns>List of created Attachment entities or error</returns>
    Task<Result<List<Attachment>>> CreateAttachmentsBySectionWithMetadataAsync(
        long entityId,
        Contracts.Dtos.Attachments.EnumEntityTypes enumEntityTypes,
        Dictionary<Contracts.Dtos.Attachments.EnumEntitySectionTypes, List<Contracts.Dtos.Attachments.FileWithMetadata>>? filesWithMetadataBySection);
}
