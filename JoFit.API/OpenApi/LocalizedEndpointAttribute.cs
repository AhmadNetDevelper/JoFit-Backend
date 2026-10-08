namespace JoFit.API.OpenApi;

/// <summary>
/// Marks an endpoint whose response is localized. Documents the optional <c>language</c> query parameter in OpenAPI.
/// The language itself is resolved centrally by <c>ILocalizationService.GetCurrentLanguage()</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class LocalizedEndpointAttribute : Attribute;
