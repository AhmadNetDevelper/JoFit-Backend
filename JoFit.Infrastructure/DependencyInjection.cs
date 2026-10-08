using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Services;
using JoFit.Domain.Identity.Roles;
using JoFit.Domain.Identity.Users;
using JoFit.Infrastructure.Data;
using JoFit.Infrastructure.Data.Interceptors;
using JoFit.Infrastructure.Identity;
using JoFit.Infrastructure.RealTime;
using JoFit.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace JoFit.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddScoped<DbCommandInterceptor, QueryPerformanceDiagnosticsInterceptor>();

        services.AddScoped<AuditableEntityInterceptor>();

        services.AddDbContext<JoFitDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString);
            options.AddInterceptors(sp.GetServices<DbCommandInterceptor>());
            options.AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>());
        });

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            var jwt = configuration.GetSection("JwtSettings");
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwt["Issuer"],
                ValidAudience = jwt["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Secret"]!)),
                // Access tokens are short-lived and not checked against the database, so expire them on time.
                ClockSkew = TimeSpan.FromSeconds(30)
            };

            // Allow SignalR hub connections to authenticate via the query string.
            // WebSocket requests cannot send custom HTTP headers, so the SignalR
            // client passes the JWT as ?access_token=<token> and this handler
            // extracts it for the bearer middleware.
            options.Events = new JwtBearerEvents
            {
                // Answer 401 and 403 with the API error envelope instead of an empty or problem+json body.
                OnChallenge = async context =>
                {
                    context.HandleResponse();
                    context.Response.StatusCode = 401;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync("{\"error\":{\"code\":\"unauthorized\",\"message\":\"Sign in to continue.\"}}");
                },
                OnForbidden = async context =>
                {
                    context.Response.StatusCode = 403;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync("{\"error\":{\"code\":\"forbidden\",\"message\":\"Your account is not allowed to do that.\"}}");
                },

                OnMessageReceived = context =>
                {
                    // The admin cookie carries the staff token when no Authorization header is sent.
                    if (string.IsNullOrEmpty(context.Request.Headers.Authorization)
                        && context.Request.Cookies.TryGetValue("jofit.admin", out var adminToken)
                        && !string.IsNullOrEmpty(adminToken))
                    {
                        context.Token = adminToken;
                        return Task.CompletedTask;
                    }

                    var accessToken = context.Request.Query["access_token"];

                    // Only read the token from the query string for hub requests
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) &&
                        path.StartsWithSegments("/hubs"))
                    {
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                }
            };
        });

        services.AddIdentityCore<User>(options =>
        {
            options.Password.RequiredLength = 6;
            options.Password.RequireDigit = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = false;
            options.User.RequireUniqueEmail = true;

            // Staff sign-in counts failed attempts: 5 wrong passwords lock the account for 15 minutes.
            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        }).AddRoles<Role>().AddEntityFrameworkStores<JoFitDbContext>();

        services.AddHybridCache(options => options.DefaultEntryOptions = new HybridCacheEntryOptions
        {
            Expiration = TimeSpan.FromMinutes(10), // L2, L3
            LocalCacheExpiration = TimeSpan.FromSeconds(30), // L1
        });

        services.AddHttpContextAccessor();
        services.AddScoped<CurrentUser>();
        services.AddScoped<IUser>(sp => sp.GetRequiredService<CurrentUser>());
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IStaffService, StaffService>();
        services.AddScoped<ITokenProvider, TokenProvider>();
        services.AddScoped<IFileUploadService, FileUploadService>();
        services.AddScoped<IUrlService, UrlService>();
        services.AddScoped<IMediaStorage, MediaStorage>();
        services.AddScoped<IPrivateFileStorage, PrivateFileStorage>();
        services.AddScoped<IAttachmentService, AttachmentService>();
        services.AddScoped<ILocalizationService, LocalizationService>();
        services.AddScoped<IRealTimeNotifier, SignalRNotifier>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddTransient(typeof(IGenericRepository<>), typeof(GenericRepository<>));

        return services;
    }
}
