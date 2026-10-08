namespace JoFit.Infrastructure.Settings;

public sealed class AppSettings
{
    public string CorsPolicyName { get; set; } = "DefaultCors";
    public string[] AllowedOrigins { get; set; } = [];
}
