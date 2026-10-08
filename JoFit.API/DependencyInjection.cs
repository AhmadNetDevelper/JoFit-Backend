using Asp.Versioning;
using JoFit.API;
using JoFit.API.Authorization;
using JoFit.API.Infrastructure;
using JoFit.API.OpenApi.Transformers;
using JoFit.Application;
using JoFit.Application.Common.Interfaces;
using JoFit.Infrastructure.Identity;
using JoFit.Infrastructure.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.StaticFiles;

namespace JoFit.API
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<AppSettings>(configuration.GetSection("AppSettings"));

            //QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            services.AddCustomProblemDetails()
                    .AddCustomApiVersioning()
                    .AddApiDocumentation()
                    .AddExceptionHandling()
                    .AddControllerWithJsonConfiguration()
                    .AddValidation()
                    .AddConfiguredCors(configuration)
                    .AddIdentityInfrastructure()
                    .AddAppForwardedHeaders(configuration)
                    .AddAppRateLimiting()
                    .AddAppOutputCaching()
                    //.AddAppOpenTelememrty()
                    .AddSignalR();

            return services;
        }

        public static IServiceCollection AddAppOutputCaching(this IServiceCollection services)
        {
            services.AddOutputCache(options =>
            {
                options.SizeLimit = 100 * 1024 * 1024; // 100 mb
                // No blanket response caching: availability, stock and moderation states must be live.
                // Handlers that can be cached do it themselves (HybridCache), and the website caches for 60 s.
                options.AddBasePolicy(policy => policy.NoCache());
            });

            return services;
        }

        //public static IServiceCollection AddAppOpenTelememrty(this IServiceCollection services)
        //{
        //    services.AddOpenTelemetry()
        //    .ConfigureResource(res => res.AddService("orderservice"))
        //    .WithTracing(tracing =>
        //    {
        //        tracing.AddAspNetCoreInstrumentation().
        //        AddHttpClientInstrumentation();

        //        tracing.AddOtlpExporter();
        //    }).
        //    WithMetrics(metrics =>
        //    {
        //        metrics.AddAspNetCoreInstrumentation().
        //        AddHttpClientInstrumentation();

        //        metrics.AddOtlpExporter().
        //        AddPrometheusExporter(); // /metrics
        //    });

        //    return services;
        //}

        /// <summary>
        /// Reads the client address from X-Forwarded-For when the request comes from a trusted reverse proxy, so the
        /// per-IP rate limits apply to the real client and not to the proxy. Only loopback is trusted by default;
        /// list the proxy under ForwardedHeaders:KnownProxies / KnownNetworks (CIDR), or set TrustAllProxies when
        /// only the proxy can reach the app.
        /// </summary>
        public static IServiceCollection AddAppForwardedHeaders(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.ForwardLimit = 1;

                var section = configuration.GetSection("ForwardedHeaders");
                if (section.GetValue<bool>("TrustAllProxies"))
                {
                    options.KnownNetworks.Clear();
                    options.KnownProxies.Clear();
                    return;
                }

                foreach (var proxy in section.GetSection("KnownProxies").Get<string[]>() ?? [])
                {
                    options.KnownProxies.Add(System.Net.IPAddress.Parse(proxy));
                }

                foreach (var network in section.GetSection("KnownNetworks").Get<string[]>() ?? [])
                {
                    var parts = network.Split('/');
                    options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(
                        System.Net.IPAddress.Parse(parts[0]), int.Parse(parts[1])));
                }
            });

            return services;
        }

        public static IServiceCollection AddAppRateLimiting(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                options.AddSlidingWindowLimiter("SlidingWindow", limiterOptions =>
                {
                    limiterOptions.PermitLimit = 100;
                    limiterOptions.Window = TimeSpan.FromMinutes(1);
                    limiterOptions.SegmentsPerWindow = 6;
                    limiterOptions.QueueLimit = 10;
                    limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    limiterOptions.AutoReplenishment = true;
                });

                // Sign-in, sign-up and the password endpoints: 10 attempts a minute per client address.
                options.AddPolicy("Auth", httpContext => RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));

                options.OnRejected = async (context, token) =>
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    {
                        context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
                    }

                    await context.HttpContext.Response.WriteAsJsonAsync(
                        new { error = new { code = "unknown", message = "Too many attempts. Try again in a few minutes." } },
                        token);
                };

                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            });

            return services;
        }

        public static IServiceCollection AddCustomProblemDetails(this IServiceCollection services)
        {
            services.AddProblemDetails(options => options.CustomizeProblemDetails = (context) =>
            {
                context.ProblemDetails.Instance = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
                context.ProblemDetails.Extensions.Add("requestId", context.HttpContext.TraceIdentifier);
            });

            return services;
        }

        public static IServiceCollection AddCustomApiVersioning(this IServiceCollection services)
        {
            services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            }).AddMvc()
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

            return services;
        }

        public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
        {
            AddOpenApiDocument(services, "website", "JoFit Website API", isAdmin: false);
            AddOpenApiDocument(services, "admin", "JoFit Admin API", isAdmin: true);

            return services;
        }

        private static void AddOpenApiDocument(IServiceCollection services, string name, string title, bool isAdmin)
        {
            services.AddOpenApi(name, options =>
            {
                options.AddDocumentTransformer((document, context, cancellationToken) =>
                {
                    document.Info.Title = title;
                    return Task.CompletedTask;
                });
                // Admin endpoints live under api/v{version}/admin/...; everything else is the public website API.
                options.ShouldInclude = description =>
                {
                    var segments = description.RelativePath?.Split('/', StringSplitOptions.RemoveEmptyEntries) ?? [];
                    var endpointIsAdmin = segments.Length > 2
                                          && segments[0].Equals("api", StringComparison.OrdinalIgnoreCase)
                                          && segments[2].Equals("admin", StringComparison.OrdinalIgnoreCase);
                    var isShared = description.ActionDescriptor.EndpointMetadata.OfType<JoFit.API.OpenApi.SharedApiAttribute>().Any();
                    return isShared || endpointIsAdmin == isAdmin;
                };
                options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
                options.AddOperationTransformer<BearerSecuritySchemeTransformer>();
                options.AddOperationTransformer<LanguageParameterTransformer>();
            });
        }

        public static IServiceCollection AddExceptionHandling(this IServiceCollection services)
        {
            services.AddExceptionHandler<GlobalExceptionHandler>();
            return services;
        }

        public static IServiceCollection AddControllerWithJsonConfiguration(this IServiceCollection services)
        {
            services.AddControllers().AddJsonOptions(options => options
                .JsonSerializerOptions
                .DefaultIgnoreCondition = JsonIgnoreCondition.Never);

            return services;
        }

        public static IServiceCollection AddValidation(this IServiceCollection services)
        {
            return services;
        }

        public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services)
        {
            // Register the current authenticated user accessor.
            services.AddScoped<CurrentUser>();
            services.AddScoped<IUser>(sp => sp.GetRequiredService<CurrentUser>());

            // Register permission-based authorization
            services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
            services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

            services.AddHttpContextAccessor();
            services.AddAuthorizationBuilder().AddPolicy("AnyStaff", policy => policy.RequireAuthenticatedUser().RequireAssertion(context => !context.User.HasClaim(c => c.Type == "account_role")));
            return services;
        }

        public static IServiceCollection AddConfiguredCors(this IServiceCollection services, IConfiguration configuration)
        {
            var appSettings = configuration.GetSection("AppSettings").Get<AppSettings>()!;

            services.AddCors(options => options.AddPolicy(
                appSettings.CorsPolicyName,
                policy => policy
                    .WithOrigins(appSettings.AllowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                    .WithExposedHeaders("Content-Disposition")));

            return services;
        }

        public static IApplicationBuilder UseCoreMiddlewares(this IApplicationBuilder app, IConfiguration configuration)
        {
            // 0. Real client address from the reverse proxy, before anything reads it (logging, rate limits)
            app.UseForwardedHeaders();

            // 1. Exception handling should be FIRST to catch all errors
            app.UseExceptionHandler();

            // 2. Status code pages for handling HTTP status codes
            app.UseStatusCodePages();

            // 3. Serilog request logging (early to log all requests)
            app.UseSerilogRequestLogging();

            // 4. CORS must come before static files so that CORS headers
            //    are included on static file responses (e.g. .glb files)
            app.UseCors(configuration["AppSettings:CorsPolicyName"]!);

            // 5. Static files (after CORS so responses carry the required headers).
            var contentTypeProvider = new FileExtensionContentTypeProvider();
            contentTypeProvider.Mappings[".glb"] = "model/gltf-binary";
            contentTypeProvider.Mappings[".gltf"] = "model/gltf+json";
            app.UseStaticFiles(new StaticFileOptions
            {
                ContentTypeProvider = contentTypeProvider
            });

            // 6. HTTPS redirection is disabled — app is deployed over HTTP
            // app.UseHttpsRedirection();

            // 7. Rate limiting (before authentication to protect auth endpoints)
            app.UseRateLimiter();

            // 7b. CSRF guard for writes that rely on the admin cookie
            app.UseMiddleware<AdminCsrfMiddleware>();

            // 8. Authentication (must come before authorization)
            app.UseAuthentication();

            // 9. Authorization (must come after authentication)
            app.UseAuthorization();

            // 10. Output caching (after auth to cache based on user context)
            app.UseOutputCache();

            return app;
        }
    }
}
