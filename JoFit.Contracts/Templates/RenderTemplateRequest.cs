namespace JoFit.Contracts.Templates;

/// <summary>
/// Request to render a template with entity IDs for dynamic data fetching
/// </summary>
public class RenderTemplateRequest
{
    /// <summary>
    /// Template ID to render
    /// The template will be fetched based on this ID
    /// </summary>
    public long TemplateId { get; set; }

    /// <summary>
    /// Entity context for dynamic data fetching from database
    /// Maps entity type to entity ID
    /// Example: { "Vehicle": 123, "Employee": 456 }
    /// 
    /// If EntityIds are provided, they will be used to fetch data from database
    /// to populate the template variables
    /// </summary>
    public Dictionary<string, long> EntityIds { get; set; } = new();
}
