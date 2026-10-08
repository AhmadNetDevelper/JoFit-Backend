using JoFit.Domain.Common;

namespace JoFit.Domain.Settings;

/// <summary>The storefront theme, kept as one validated document (colors, typography, shape, layout).</summary>
public class ThemeSetting : AuditableEntity
{
    public string Key { get; private set; } = "storefront";
    public string DocumentJson { get; private set; } = "{}";

    private ThemeSetting()
    {
    }

    public static ThemeSetting Create(string documentJson) => new() { DocumentJson = documentJson };

    public void Replace(string documentJson) => DocumentJson = documentJson;
}
