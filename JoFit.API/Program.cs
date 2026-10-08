using JoFit.API;
using JoFit.Application;
using JoFit.Infrastructure;
using JoFit.Infrastructure.Data;
using JoFit.Infrastructure.Data.Extensions;
using JoFit.Infrastructure.RealTime;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Path = System.IO.Path;

// Bootstrap logger: captures startup errors before full configuration loads
var logPath = Path.Combine(AppContext.BaseDirectory, "logs", "bootstrap-.txt");

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(logPath, rollingInterval: RollingInterval.Day, shared: true)
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting application");

    var builder = WebApplication.CreateBuilder(args);

    // Add services to the container.

    builder.Services
           .AddPresentation(builder.Configuration)
           .AddApplication()
           .AddInfrastructure(builder.Configuration);

    builder.Host.UseSerilog((context, services, loggerConfig) =>
        loggerConfig.ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services));

    builder.Services.AddControllers();
    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    // OpenAPI documents are configured in AddApiDocumentation() via DependencyInjection.cs

    var app = builder.Build();

    // Apply migrations
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<JoFitDbContext>();
            await dbContext.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while migrating the database.");
            throw;
        }
    }

    // Seed initial data (permissions, SuperAdmin role and user)
    await app.SeedDatabaseAsync();

    // Configure the HTTP request pipeline.
    //if (app.Environment.IsDevelopment())
    //{
    app.MapOpenApi();

        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/openapi/website.json", "Website API");
            options.SwaggerEndpoint("/openapi/admin.json", "Admin API");

            options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
            options.EnableDeepLinking();
            options.DisplayRequestDuration();
            options.EnableFilter();
        });

    app.UseCoreMiddlewares(builder.Configuration);

    app.MapControllers();

    app.MapHub<NotificationHub>(NotificationHub.HubUrl);

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
