namespace JoFit.Contracts.Requests.Common;

/// <summary>Image reference saved on a record (the file was uploaded first).</summary>
public sealed record MediaImageRequest
{
    public string Kind { get; init; } = "image";
    public string Id { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string? Alt { get; init; }
}

public sealed record LocalizedTextRequest
{
    public string En { get; init; } = string.Empty;
    public string Ar { get; init; } = string.Empty;
}
