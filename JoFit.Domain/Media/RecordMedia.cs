using JoFit.Domain.Common;

namespace JoFit.Domain.Media;

/// <summary>
/// One image on a record's gallery (products, rental items, coach gallery, courses). The first position is the cover.
/// The url, width and height are copied from the uploaded <see cref="MediaAsset"/>; the alt text is per use.
/// </summary>
public class RecordMedia : Entity
{
    public string OwnerType { get; private set; } = string.Empty;
    public long OwnerId { get; private set; }
    public string Field { get; private set; } = string.Empty;
    public int Position { get; private set; }
    public string MediaId { get; private set; } = string.Empty;
    public string Url { get; private set; } = string.Empty;
    public string Alt { get; private set; } = string.Empty;
    public int? Width { get; private set; }
    public int? Height { get; private set; }

    private RecordMedia()
    {
    }

    public static RecordMedia Create(string ownerType, long ownerId, string field, int position, string mediaId, string url, string alt, int? width, int? height)
        => new()
        {
            OwnerType = ownerType,
            OwnerId = ownerId,
            Field = field,
            Position = position,
            MediaId = mediaId,
            Url = url,
            Alt = alt,
            Width = width,
            Height = height
        };
}
