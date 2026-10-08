using Microsoft.AspNetCore.Http;

namespace JoFit.Contracts.Dtos.Attachments;

/// <summary>
/// Represents a file with optional metadata (description) for attachment operations.
/// Used for generic file attachment handling across different entity types.
/// </summary>
public class FileWithMetadata
{
    /// <summary>
    /// The file to be uploaded.
    /// </summary>
    public IFormFile File { get; set; } = null!;

    /// <summary>
    /// Optional description for the file.
    /// Can be used to provide context or additional information about the file.
    /// Maximum length is 150 characters as per Attachment domain constraints.
    /// </summary>
    public string? Description { get; set; }
}
