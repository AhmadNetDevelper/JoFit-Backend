namespace JoFit.Contracts.Templates;

public class CreateTemplateRequest
{
    public string NameAr { get; set; }
    public string NameEn { get; set; }
    public string ContentAr { get; set; }
    public string ContentEn { get; set; }
    public string DescriptionAr { get; set; }
    public string DescriptionEn { get; set; }
    public List<long> VariableIds { get; set; } = new();
}

public class UpdateTemplateRequest
{
    public string NameAr { get; set; }
    public string NameEn { get; set; }
    public string ContentAr { get; set; }
    public string ContentEn { get; set; }
    public string DescriptionAr { get; set; }
    public string DescriptionEn { get; set; }
    public List<long> VariableIds { get; set; } = new();
}

public class TemplateResponse
{
    public long Id { get; set; }
    public string NameAr { get; set; }
    public string NameEn { get; set; }
    public string ContentAr { get; set; }
    public string ContentEn { get; set; }
    public string DescriptionAr { get; set; }
    public string DescriptionEn { get; set; }
    public bool IsActive { get; set; }
    public List<TemplateVariableResponse> TemplateVariables { get; set; } = new();
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset LastModifiedUtc { get; set; }
}
