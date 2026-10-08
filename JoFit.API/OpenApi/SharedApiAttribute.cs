namespace JoFit.API.OpenApi;

/// <summary>
/// Marks a controller or endpoint used by both the website and the admin panel.
/// It is listed in both the Website and Admin OpenAPI documents.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class SharedApiAttribute : Attribute;
