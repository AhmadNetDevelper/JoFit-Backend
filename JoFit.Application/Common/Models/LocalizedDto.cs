namespace JoFit.Application.Common.Models;

/// <summary>
/// Base class for localized DTOs that support multiple languages.
/// Provides a way to return language-specific data based on the user's language preference.
/// </summary>
public abstract record LocalizedDto
{
    public long Id { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }

    /// <summary>
    /// Gets the localized name based on the specified language.
    /// </summary>
    /// <param name="language">Language code: "ar" for Arabic or "en" for English</param>
    /// <returns>The localized name</returns>
    public abstract string GetLocalizedName(string? language);
}
