namespace JoFit.Application.Common.Interfaces;

/// <summary>
/// Service for handling localization and language-based data transformation.
/// Provides a central place to manage language preferences across the application.
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// Gets the current user's language preference.
    /// </summary>
    string? GetCurrentLanguage();

    /// <summary>
    /// Gets the localized name based on the specified language.
    /// </summary>
    /// <param name="nameAr">Arabic name</param>
    /// <param name="nameEn">English name</param>
    /// <param name="language">Language code: "ar" or "en". If null, uses current user's language.</param>
    /// <returns>The localized name</returns>
    string GetLocalizedName(string nameAr, string nameEn, string? language = null);

    /// <summary>
    /// Normalizes the language code to a standard format (lowercase, remove region).
    /// </summary>
    /// <param name="language">Language code to normalize</param>
    /// <returns>Normalized language code</returns>
    string NormalizeLanguage(string? language);
}
