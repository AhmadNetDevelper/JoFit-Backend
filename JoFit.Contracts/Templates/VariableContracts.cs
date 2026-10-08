namespace JoFit.Contracts.Templates;

public class CreateTemplateVariableRequest
{
    public string Entity { get; set; }
    public string FieldName { get; set; }
    public string DataType { get; set; }
    public string Description { get; set; }
}

public class UpdateTemplateVariableRequest
{
    public string Entity { get; set; }
    public string FieldName { get; set; }
    public string DataType { get; set; }
    public string Description { get; set; }
}

public class TemplateVariableResponse
{
    public long Id { get; set; }
    public string Entity { get; set; }
    public string FieldName { get; set; }
    public string DataType { get; set; }
    public string Description { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset LastModifiedUtc { get; set; }
}

public class TemplateVariableGroupResponse
{
    public string Entity { get; set; }
    public List<TemplateVariableFieldResponse> Fields { get; set; } = new();
}

public class TemplateVariableFieldResponse
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string DataType { get; set; }
}
