using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Data.Common;
using System.Diagnostics;

namespace JoFit.Infrastructure.Data.Interceptors;

/// <summary>
/// Interceptor to log slow database queries and command execution times.
/// Helps identify N+1 queries, missing indexes, and other performance issues.
/// </summary>
public class QueryPerformanceDiagnosticsInterceptor(ILogger<QueryPerformanceDiagnosticsInterceptor> logger) : DbCommandInterceptor
{
    private readonly ILogger<QueryPerformanceDiagnosticsInterceptor> _logger = logger;
    
    // Threshold for logging slow queries (milliseconds)
    private const int SlowQueryThresholdMs = 100;

    // Storage for timing information
    private readonly Dictionary<DbCommand, Stopwatch> _commandTimers = new();

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        if (_commandTimers.TryGetValue(command, out var stopwatch))
        {
            stopwatch.Stop();
            LogQueryIfSlow(command, stopwatch.ElapsedMilliseconds);
            _commandTimers.Remove(command);
        }

        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override DbDataReader ReaderExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result)
    {
        if (_commandTimers.TryGetValue(command, out var stopwatch))
        {
            stopwatch.Stop();
            LogQueryIfSlow(command, stopwatch.ElapsedMilliseconds);
            _commandTimers.Remove(command);
        }

        return base.ReaderExecuted(command, eventData, result);
    }

    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (_commandTimers.TryGetValue(command, out var stopwatch))
        {
            stopwatch.Stop();
            LogQueryIfSlow(command, stopwatch.ElapsedMilliseconds);
            _commandTimers.Remove(command);
        }

        return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override int NonQueryExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result)
    {
        if (_commandTimers.TryGetValue(command, out var stopwatch))
        {
            stopwatch.Stop();
            LogQueryIfSlow(command, stopwatch.ElapsedMilliseconds);
            _commandTimers.Remove(command);
        }

        return base.NonQueryExecuted(command, eventData, result);
    }

    public override ValueTask<object?> ScalarExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result,
        CancellationToken cancellationToken = default)
    {
        if (_commandTimers.TryGetValue(command, out var stopwatch))
        {
            stopwatch.Stop();
            LogQueryIfSlow(command, stopwatch.ElapsedMilliseconds);
            _commandTimers.Remove(command);
        }

        return base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override object? ScalarExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result)
    {
        if (_commandTimers.TryGetValue(command, out var stopwatch))
        {
            stopwatch.Stop();
            LogQueryIfSlow(command, stopwatch.ElapsedMilliseconds);
            _commandTimers.Remove(command);
        }

        return base.ScalarExecuted(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        StartQueryTimer(command);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        StartQueryTimer(command);
        return base.ReaderExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        StartQueryTimer(command);
        return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result)
    {
        StartQueryTimer(command);
        return base.NonQueryExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<object?>> ScalarExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object?> result,
        CancellationToken cancellationToken = default)
    {
        StartQueryTimer(command);
        return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<object?> ScalarExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object?> result)
    {
        StartQueryTimer(command);
        return base.ScalarExecuting(command, eventData, result);
    }

    private void StartQueryTimer(DbCommand command)
    {
        if (!_commandTimers.ContainsKey(command))
        {
            _commandTimers[command] = Stopwatch.StartNew();
        }
    }

    private void LogQueryIfSlow(DbCommand command, long elapsedMilliseconds)
    {
        if (elapsedMilliseconds > SlowQueryThresholdMs)
        {
            var severity = elapsedMilliseconds > 1000 ? LogLevel.Error :
                          elapsedMilliseconds > 500 ? LogLevel.Warning : LogLevel.Information;

            _logger.Log(
                severity,
                "Slow Query Detected: {ElapsedMilliseconds}ms\nSQL: {CommandText}\nParameters: {@Parameters}",
                elapsedMilliseconds,
                command.CommandText,
                command.Parameters.Cast<DbParameter>().ToDictionary(p => p.ParameterName, p => p.Value));
        }
    }
}
