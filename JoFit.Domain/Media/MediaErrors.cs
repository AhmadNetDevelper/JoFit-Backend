using JoFit.Domain.Common.Results;

namespace JoFit.Domain.Media;

public static class MediaErrors
{
    public static readonly Error FileRequired = Error.Validation("Media.FileRequired", "Choose an image to upload.").ForField("file");
    public static readonly Error FileTypeNotAllowed = Error.Validation("Media.FileTypeNotAllowed", "That file is not a supported image.").ForField("file");
    public static readonly Error FileUnreadable = Error.Validation("Media.FileUnreadable", "That image could not be read.").ForField("file");
    public static readonly Error PurposeInvalid = Error.Validation("Media.PurposeInvalid", "Unknown image purpose.").ForField("purpose");
    public static readonly Error AltTooLong = Error.Validation("Media.AltTooLong", "The description cannot exceed 300 characters.").ForField("alt");
    public static readonly Error NotFound = Error.NotFound("Media.NotFound", "That image does not exist.");
    public static readonly Error UploadForbidden = Error.Forbidden("Media.UploadForbidden", "Your role cannot upload images.");

    public static Error TooLarge(int megabytes, string field = "file")
        => Error.Create((int)ErrorKindsEnum.PayloadTooLarge, "Media.TooLarge", $"The image is larger than {megabytes} MB.").ForField(field);
}
