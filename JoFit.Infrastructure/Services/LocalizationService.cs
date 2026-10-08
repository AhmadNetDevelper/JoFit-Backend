using JoFit.Application.Common.Interfaces;

namespace JoFit.Infrastructure.Services;

/// <summary>
/// Implementation of localization service that handles language-based data transformation.
/// </summary>
public class LocalizationService : ILocalizationService
{
    private readonly IUser _currentUser;
    private const string DefaultLanguage = "ar";
    private const string ArabicLanguage = "ar";
    private const string EnglishLanguage = "en";

    public LocalizationService(IUser currentUser)
    {
        _currentUser = currentUser;
    }

    /// <summary>
    /// Gets the current user's language preference.
    /// Defaults to Arabic if no language is specified.
    /// </summary>
    public string? GetCurrentLanguage()
    {
        var language = _currentUser.Language;
        return NormalizeLanguage(language);
    }

    /// <summary>
    /// Gets the localized name based on the specified language.
    /// If no language is provided, uses the current user's language preference.
    /// </summary>
    public string GetLocalizedName(string nameAr, string nameEn, string? language = null)
    {
        var normalizedLanguage = NormalizeLanguage(language ?? GetCurrentLanguage());

        return normalizedLanguage == ArabicLanguage ? nameAr : nameEn;
    }

    /// <summary>
    /// Normalizes the language code to a standard format.
    /// - Converts to lowercase
    /// - Removes region (e.g., "en-US" -> "en")
    /// - Defaults to Arabic if missing or unsupported
    /// </summary>
    public string NormalizeLanguage(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return DefaultLanguage;
        }

        // Extract language code (e.g., "en" from "en-US")
        var languageCode = language.Split('-', ',')[0].ToLowerInvariant();

        // Validate against supported languages
        return languageCode == ArabicLanguage || languageCode == EnglishLanguage
            ? languageCode
            : DefaultLanguage;
    }
}
