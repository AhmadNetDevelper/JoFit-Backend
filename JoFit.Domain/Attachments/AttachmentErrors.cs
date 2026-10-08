using JoFit.Domain.Common.Results;

namespace JoFit.Domain.Attachments;

public static class AttachmentErrors
{
    public static readonly Error FileNameRequired = Error.Validation(
        "Attachment.FileNameRequired",
        "File name is required");

    public static readonly Error FileNameMaxLengthExceeded = Error.Validation(
        "Attachment.FileNameMaxLengthExceeded",
        "File name cannot exceed 100 characters");

    public static readonly Error FilePathRequired = Error.Validation(
        "Attachment.FilePathRequired",
        "File path is required");

    public static readonly Error FilePathMaxLengthExceeded = Error.Validation(
        "Attachment.FilePathMaxLengthExceeded",
        "File path cannot exceed 200 characters");

    public static readonly Error DescriptionMaxLengthExceeded = Error.Validation(
        "Attachment.DescriptionMaxLengthExceeded",
        "Description cannot exceed 150 characters");

    public static readonly Error FileSizeInvalid = Error.Validation(
        "Attachment.FileSizeInvalid",
        "File size must be greater than 0");

    public static readonly Error EntityIdInvalid = Error.Validation(
        "Attachment.EntityIdInvalid",
        "Entity ID must be greater than 0");

    public static readonly Error AttachmentNotFound = Error.NotFound(
        "Attachment.NotFound",
        "Attachment not found");

    public static readonly Error AttachmentAlreadyExists = Error.Conflict(
        "Attachment.AlreadyExists",
        "Attachment already exists");
}
