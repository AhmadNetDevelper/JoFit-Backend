namespace JoFit.Application.Common.Utilities;

/// <summary>
/// Utility class for working with localized data across the application.
/// Provides helper methods for common localization tasks.
/// </summary>
public static class LocalizationUtility
{
    /// <summary>
    /// Creates a list of localized simple DTOs from a collection of full localized DTOs.
    /// </summary>
    /// <example>
    /// var localizedDtos = LocalizationUtility.ToLocalizedSimple(
    ///     dtos, 
    ///     localizationService.GetCurrentLanguage());
    /// </example>
    public static List<T> ToLocalizedSimple<T>(
        IEnumerable<T> dtos,
        string? language) where T : class
    {
        return dtos
            .Select(dto =>
            {
                var method = dto.GetType().GetMethod("ToLocalizedSimple", [typeof(string)]);
                if (method != null)
                {
                    return method.Invoke(dto, new object[] { language }) as T;
                }
                return dto;
            })
            .OfType<T>()
            .ToList();
    }

    /// <summary>
    /// Validates if a language code is supported.
    /// </summary>
    public static bool IsValidLanguage(string? language)
    {
        var supported = new[] { "ar", "en" };
        var normalized = language?.ToLowerInvariant().Split('-')[0];
        return supported.Contains(normalized);
    }

    /// <summary>
    /// Gets the opposite language code.
    /// Useful for fetching alternative language when current language not available.
    /// </summary>
    public static string GetAlternateLanguage(string language)
    {
        var normalized = language?.ToLowerInvariant().Split('-')[0] ?? "en";
        return normalized == "ar" ? "en" : "ar";
    }
}
