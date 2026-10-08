using JoFit.Domain.Common.Results;

namespace JoFit.Domain.Common;

/// <summary>Validation shared by every record that carries an image field.</summary>
public static class MediaRules
{
    public const int MaxAltLength = 300;

    /// <summary>Returns an error keyed on <paramref name="field"/> when the image is not storable, else null.</summary>
    public static Error? Validate(string? id, string? url, string? alt, string field)
    {
        if (url is null && id is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(id))
        {
            return Error.Validation("Media.IdRequired", "The image needs an id.").ForField(field);
        }

        if (string.IsNullOrWhiteSpace(url)
            || !(url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                 || url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)))
        {
            return Error.Validation("Media.UrlInvalid", "The image needs an http(s) address.").ForField(field);
        }

        if (alt is { Length: > MaxAltLength })
        {
            return Error.Validation("Media.AltTooLong", $"The image description cannot exceed {MaxAltLength} characters.").ForField(field);
        }

        return null;
    }
}
