namespace JoFit.Domain.Attachments;

/// <summary>
/// Extension methods for EnumEntitySectionTypes enum.
/// </summary>
public static class EnumEntitySectionTypesExtensions
{
    /// <summary>
    /// Gets the enum name for the entity section type.
    /// </summary>
    public static string GetEnumName(this EnumEntitySectionTypes sectionType)
    {
        return sectionType.ToString();
    }
}
