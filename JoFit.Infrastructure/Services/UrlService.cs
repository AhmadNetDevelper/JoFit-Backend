using JoFit.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace JoFit.Infrastructure.Services;

public class UrlService : IUrlService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;

    public UrlService(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
    {
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
    }

    public string? ToAbsoluteUrl(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return relativePath;
        }

        // If it's already an absolute URL, return it as-is
        if (Uri.TryCreate(relativePath, UriKind.Absolute, out _))
        {
            return relativePath;
        }

        // Encode path segments to handle spaces and special characters
        // First decode any existing percent-encoding to avoid double-encoding
        var decodedPath = Uri.UnescapeDataString(relativePath);
        var encodedPath = string.Join("/",
            decodedPath.TrimStart('/').Split('/').Select(Uri.EscapeDataString));

        // Priority 1: Use configured PublicBaseUrl from FileUpload settings
        var publicBaseUrl = _configuration["FileUpload:PublicBaseUrl"];
        if (!string.IsNullOrWhiteSpace(publicBaseUrl))
        {
            return $"{publicBaseUrl.TrimEnd('/')}/{encodedPath}";
        }

        // Priority 2: Use HttpContext to build URL from current request
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            var request = httpContext.Request;
            var scheme = request.Scheme;
            var host = request.Host.ToUriComponent();

            return $"{scheme}://{host}/{encodedPath}";
        }

        // Priority 3: Return relative path as-is if no context available
        return relativePath;
    }
}
