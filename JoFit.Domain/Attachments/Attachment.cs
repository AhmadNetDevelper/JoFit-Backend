using JoFit.Domain.Common;
using JoFit.Domain.Common.Results;

namespace JoFit.Domain.Attachments;

public class Attachment : AuditableEntity
{
    public string FileName { get; private set; } = string.Empty;
    public EnumFileTypes FileTypeId { get; private set; }
    public int FileSize { get; private set; }
    public string FilePath { get; private set; } = string.Empty;
    public long EntityId { get; private set; }
    public EnumEntityTypes EntityTypeId { get; private set; }
    public EnumEntitySectionTypes EntitySectionTypeId { get; private set; }
    public string? Description { get; private set; }

    private Attachment()
    {
    }

    private Attachment(
        string fileName,
        EnumFileTypes fileTypeId,
        int fileSize,
        string filePath,
        long entityId,
        EnumEntityTypes entityTypeId,
        EnumEntitySectionTypes entitySectionTypeId,
        string? description = null) : base()
    {
        FileName = fileName;
        FileTypeId = fileTypeId;
        FileSize = fileSize;
        FilePath = filePath;
        EntityId = entityId;
        EntityTypeId = entityTypeId;
        EntitySectionTypeId = entitySectionTypeId;
        Description = description;
    }

    /// <summary>
    /// Creates a new Attachment instance with validation.
    /// </summary>
    public static Result<Attachment> Create(
        string fileName,
        EnumFileTypes fileTypeId,
        int fileSize,
        string filePath,
        long entityId,
        EnumEntityTypes entityTypeId,
        EnumEntitySectionTypes entitySectionTypeId,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return AttachmentErrors.FileNameRequired;
        }

        if (fileName.Length > 100)
        {
            return AttachmentErrors.FileNameMaxLengthExceeded;
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            return AttachmentErrors.FilePathRequired;
        }

        if (filePath.Length > 200)
        {
            return AttachmentErrors.FilePathMaxLengthExceeded;
        }

        if (fileSize <= 0)
        {
            return AttachmentErrors.FileSizeInvalid;
        }

        if (entityId <= 0)
        {
            return AttachmentErrors.EntityIdInvalid;
        }

        if (description != null && description.Length > 150)
        {
            return AttachmentErrors.DescriptionMaxLengthExceeded;
        }

        return new Attachment(fileName, fileTypeId, fileSize, filePath, entityId, entityTypeId, entitySectionTypeId, description);
    }

    /// <summary>
    /// Updates the attachment information with validation.
    /// </summary>
    public Result<Updated> Update(
        string fileName,
        EnumFileTypes fileTypeId,
        int fileSize,
        string filePath,
        EnumEntitySectionTypes entitySectionTypeId,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return AttachmentErrors.FileNameRequired;
        }

        if (fileName.Length > 100)
        {
            return AttachmentErrors.FileNameMaxLengthExceeded;
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            return AttachmentErrors.FilePathRequired;
        }

        if (filePath.Length > 200)
        {
            return AttachmentErrors.FilePathMaxLengthExceeded;
        }

        if (fileSize <= 0)
        {
            return AttachmentErrors.FileSizeInvalid;
        }

        if (description != null && description.Length > 150)
        {
            return AttachmentErrors.DescriptionMaxLengthExceeded;
        }

        FileName = fileName;
        FileTypeId = fileTypeId;
        FileSize = fileSize;
        FilePath = filePath;
        EntitySectionTypeId = entitySectionTypeId;
        Description = description;

        return Result.Updated;
    }
}
