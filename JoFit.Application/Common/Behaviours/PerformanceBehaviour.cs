using JoFit.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace JoFit.Application.Common.Behaviours;

public class PerformanceBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly Stopwatch _timer;
    private readonly ILogger<PerformanceBehaviour<TRequest, TResponse>> _logger;
    private readonly IUser _user;
    private readonly IIdentityService _identityService;

    // Thresholds for performance alerts
    private const long WarningThresholdMs = 300;    // Log warning if > 300ms
    private const long ErrorThresholdMs = 1000;      // Log error if > 1000ms
    private const long CriticalThresholdMs = 5000;   // Log critical if > 5000ms

    public PerformanceBehaviour(
        ILogger<PerformanceBehaviour<TRequest, TResponse>> logger,
        IUser user,
        IIdentityService identityService)
    {
        _timer = new Stopwatch();
        _logger = logger;
        _user = user;
        _identityService = identityService;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _timer.Start();

        try
        {
            var response = await next();

            _timer.Stop();
            var elapsedMilliseconds = _timer.ElapsedMilliseconds;

            // Log performance based on thresholds
            if (elapsedMilliseconds >= CriticalThresholdMs)
            {
                await LogPerformanceIssueAsync(request, elapsedMilliseconds, "CRITICAL");
            }
            else if (elapsedMilliseconds >= ErrorThresholdMs)
            {
                await LogPerformanceIssueAsync(request, elapsedMilliseconds, "ERROR");
            }
            else if (elapsedMilliseconds >= WarningThresholdMs)
            {
                await LogPerformanceIssueAsync(request, elapsedMilliseconds, "WARNING");
            }

            return response;
        }
        catch (OperationCanceledException ex)
        {
            _timer.Stop();
            var elapsedMilliseconds = _timer.ElapsedMilliseconds;
            
            _logger.LogError(
                "Request Timeout/Cancellation: {Name} ({ElapsedMilliseconds}ms) - {Message}. " +
                "This may indicate a database timeout or slow query. " +
                "Check SQL Server timeout settings and query performance",
                typeof(TRequest).Name,
                elapsedMilliseconds,
                ex.Message);
            
            throw;
        }
        catch (Exception ex)
        {
            _timer.Stop();
            var elapsedMilliseconds = _timer.ElapsedMilliseconds;
            
            _logger.LogError(
                ex,
                "Request Exception: {Name} ({ElapsedMilliseconds}ms) - Exception during execution",
                typeof(TRequest).Name,
                elapsedMilliseconds);
            
            throw;
        }
    }

    private async Task LogPerformanceIssueAsync(TRequest request, long elapsedMilliseconds, string severity)
    {
        try
        {
            var requestName = typeof(TRequest).Name;
            var userId = _user.Id ?? 0;
            var userName = string.Empty;

            if (userId > 0)
            {
                userName = await _identityService.GetUserNameAsync(userId);
            }

            var recommendation = GetRecommendation(requestName, elapsedMilliseconds);

            var logMessage = new StringBuilder();
            logMessage.AppendLine($"{severity} - Long Running Request");
            logMessage.AppendLine($"  Request: {requestName}");
            logMessage.AppendLine($"  Duration: {elapsedMilliseconds}ms");
            logMessage.AppendLine($"  User ID: {userId}");
            logMessage.AppendLine($"  User: {userName}");
            logMessage.AppendLine($"  Recommendation: {recommendation}"
            );

            switch (severity)
            {
                case "CRITICAL":
                    _logger.LogCritical(
                        "Critical Performance Issue: {Name} ({ElapsedMilliseconds}ms) {@UserId} {@UserName} {@Recommendation} {@Request}",
                        requestName, elapsedMilliseconds, userId, userName, recommendation, request);
                    break;
                case "ERROR":
                    _logger.LogError(
                        "Error Performance Issue: {Name} ({ElapsedMilliseconds}ms) {@UserId} {@UserName} {@Recommendation} {@Request}",
                        requestName, elapsedMilliseconds, userId, userName, recommendation, request);
                    break;
                default:
                    _logger.LogWarning(
                        "Warning Performance Issue: {Name} ({ElapsedMilliseconds}ms) {@UserId} {@UserName} {@Recommendation} {@Request}",
                        requestName, elapsedMilliseconds, userId, userName, recommendation, request);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging performance issue for request {Name}", typeof(TRequest).Name);
        }
    }

    private static string GetRecommendation(string requestName, long elapsedMilliseconds)
    {
        if (elapsedMilliseconds > 5000)
        {
            return "URGENT: Query taking >5s. Check for missing indexes, N+1 queries, or insufficient connection pool. " +
                   "Verify SQL timeout is set >= 60s. Consider query optimization or splitting into smaller operations.";
        }

        if (elapsedMilliseconds > 1000)
        {
            return "Check query execution plan for inefficiencies. Consider adding database indexes on frequently filtered columns. " +
                   "Verify connection pool settings and database server resources.";
        }

        return "Monitor this query for future slowdowns. Consider caching if results don't change frequently.";
    }
}
