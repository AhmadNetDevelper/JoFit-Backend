namespace JoFit.Application.Common.Models;

/// <summary>Image as it appears on a record: media id, absolute url, alt text and size.</summary>
public sealed record MediaImageDto(string Id, string Url, string Alt, int? Width = null, int? Height = null)
{
    public string Kind => "image";
}
