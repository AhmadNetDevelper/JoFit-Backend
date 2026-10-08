using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;

namespace JoFit.Application.Common.Extensions;

/// <summary>
/// Extension methods for applying localization to DTOs.
/// </summary>
public static class LocalizationExtensions
{
    /// <summary>
    /// Applies localization to a single localized DTO.
    /// </summary>
    public static T Localize<T>(this T dto, ILocalizationService localizationService) where T : LocalizedDto
    {
        if (dto == null || localizationService == null)
            return dto;

        // The DTO's GetLocalizedName method will be called by the client
        // This extension is for future enhancements or additional processing
        return dto;
    }

    /// <summary>
    /// Applies localization to a collection of localized DTOs.
    /// </summary>
    public static IEnumerable<T> Localize<T>(this IEnumerable<T> dtos, ILocalizationService localizationService) where T : LocalizedDto
    {
        if (dtos == null || localizationService == null)
            return dtos;

        return dtos.Select(dto => dto.Localize(localizationService));
    }
}
