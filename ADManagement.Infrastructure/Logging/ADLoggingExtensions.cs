using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ADManagement.Infrastructure.Logging;

/// <summary>
/// Enhanced logging helper for AD operations with detailed diagnostics
/// </summary>
public static class ADLoggingExtensions
{
    /// <summary>
    /// Logs the start of an operation with timing
    /// </summary>
    public static IDisposable LogOperation(this ILogger logger, string operationName, params object[] args)
    {
        return new OperationLogger(logger, operationName, args);
    }

    /// <summary>
    /// Logs connection attempt details
    /// </summary>
    public static void LogConnectionAttempt(this ILogger logger, string server, int port, bool useSSL)
    {
        logger.LogInformation(
            "┌─────────────────────────────────────────────────────────────┐\n" +
            "│ CONNECTION ATTEMPT                                           │\n" +
            "└─────────────────────────────────────────────────────────────┘\n" +
            "Server   : {Server}\n" +
            "Port     : {Port}\n" +
            "Protocol : {Protocol}\n" +
            "Time     : {Time}",
            server, port, useSSL ? "LDAPS (SSL/TLS)" : "LDAP", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
    }

    /// <summary>
    /// Logs successful connection with details
    /// </summary>
    public static void LogConnectionSuccess(this ILogger logger, string server, TimeSpan elapsed)
    {
        logger.LogInformation(
            "✅ CONNECTION SUCCESSFUL\n" +
            "   Server: {Server}\n" +
            "   Time elapsed: {Elapsed}ms\n" +
            "   Status: Connected and authenticated",
            server, elapsed.TotalMilliseconds);
    }

    /// <summary>
    /// Logs connection failure with diagnostic information
    /// </summary>
    public static void LogConnectionFailure(this ILogger logger, Exception ex, string server, int port)
    {
        logger.LogError(ex,
            "❌ CONNECTION FAILED\n" +
            "   Server: {Server}:{Port}\n" +
            "   Error: {ErrorMessage}\n" +
            "   Type: {ExceptionType}\n" +
            "   Time: {Time}\n" +
            "   \n" +
            "   💡 TROUBLESHOOTING TIPS:\n" +
            "   {Tips}",
            server, port, ex.Message, ex.GetType().Name,
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            GetTroubleshootingTips(ex));
    }

    /// <summary>
    /// Logs query execution details
    /// </summary>
    public static void LogQueryExecution(this ILogger logger, string filter, int pageSize, string searchBase)
    {
        logger.LogDebug(
            "🔍 EXECUTING QUERY\n" +
            "   Filter     : {Filter}\n" +
            "   Search Base: {SearchBase}\n" +
            "   Page Size  : {PageSize}\n" +
            "   Time       : {Time}",
            filter, searchBase, pageSize, DateTime.Now.ToString("HH:mm:ss.fff"));
    }

    /// <summary>
    /// Logs query results
    /// </summary>
    public static void LogQueryResults(this ILogger logger, int resultCount, TimeSpan elapsed)
    {
        logger.LogInformation(
            "✅ QUERY COMPLETED\n" +
            "   Results found: {Count}\n" +
            "   Time elapsed : {Elapsed}ms",
            resultCount, elapsed.TotalMilliseconds);
    }

    /// <summary>
    /// Logs export operation progress
    /// </summary>
    public static void LogExportProgress(this ILogger logger, int current, int total, string operation)
    {
        var percentage = total > 0 ? (double)current / total * 100 : 0;
        logger.LogInformation(
            "📊 {Operation}: {Current}/{Total} ({Percentage:F1}%)",
            operation, current, total, percentage);
    }

    /// <summary>
    /// Logs detailed error with context
    /// </summary>
    public static void LogDetailedError(this ILogger logger, Exception ex, string operation, Dictionary<string, object>? context = null)
    {
        var contextString = context != null
            ? string.Join("\n   ", context.Select(kvp => $"{kvp.Key}: {kvp.Value}"))
            : "No additional context";

        logger.LogError(ex,
            "❌ ERROR IN {Operation}\n" +
            "   Message   : {Message}\n" +
            "   Type      : {Type}\n" +
            "   Source    : {Source}\n" +
            "   Stack Trace:\n{StackTrace}\n" +
            "   \n" +
            "   Context:\n   {Context}\n" +
            "   \n" +
            "   Time: {Time}",
            operation, ex.Message, ex.GetType().Name, ex.Source,
            ex.StackTrace, contextString, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));

        if (ex.InnerException != null)
        {
            logger.LogError(
                "   Inner Exception: {InnerType}\n" +
                "   Inner Message  : {InnerMessage}",
                ex.InnerException.GetType().Name, ex.InnerException.Message);
        }
    }

    /// <summary>
    /// Logs performance metrics
    /// </summary>
    public static void LogPerformanceMetrics(this ILogger logger, string operation, Dictionary<string, double> metrics)
    {
        logger.LogInformation(
            "⚡ PERFORMANCE METRICS - {Operation}\n" +
            "   {Metrics}",
            operation,
            string.Join("\n   ", metrics.Select(kvp => $"{kvp.Key}: {kvp.Value:F2}ms")));
    }

    private static string GetTroubleshootingTips(Exception ex)
    {
        return ex switch
        {
            System.DirectoryServices.DirectoryServicesCOMException comEx when comEx.ErrorCode == -2147016646 =>
                "   1. Domain controller is unreachable\n" +
                "   2. Check network connectivity\n" +
                "   3. Verify domain name is correct\n" +
                "   4. Check firewall settings",

            System.DirectoryServices.DirectoryServicesCOMException comEx when comEx.ErrorCode == -2147016672 =>
                "   1. Invalid credentials\n" +
                "   2. Account may be locked or disabled\n" +
                "   3. Password may have expired\n" +
                "   4. Check username format (DOMAIN\\user or user@domain)",

            System.Runtime.InteropServices.COMException comEx when comEx.HResult == unchecked((int)0x8007052E) =>
                "   1. Logon failure - bad username or password\n" +
                "   2. Account restrictions (time, workstation)\n" +
                "   3. Account may be disabled or locked",

            System.Runtime.InteropServices.COMException comEx when comEx.HResult == unchecked((int)0x80072020) =>
                "   1. Domain not accessible\n" +
                "   2. DNS resolution failed\n" +
                "   3. Try using DC IP address directly",

            TimeoutException =>
                "   1. Operation took too long\n" +
                "   2. Increase timeout in configuration\n" +
                "   3. Check network performance\n" +
                "   4. Reduce query scope or page size",

            UnauthorizedAccessException =>
                "   1. Insufficient permissions\n" +
                "   2. Need administrative rights for this operation\n" +
                "   3. Check AD user permissions",

            _ =>
                "   1. Check application logs for details\n" +
                "   2. Verify configuration settings\n" +
                "   3. Contact system administrator if issue persists"
        };
    }

    private class OperationLogger : IDisposable
    {
        private readonly ILogger _logger;
        private readonly string _operationName;
        private readonly Stopwatch _stopwatch;
        private readonly object[] _args;

        public OperationLogger(ILogger logger, string operationName, object[] args)
        {
            _logger = logger;
            _operationName = operationName;
            _args = args;
            _stopwatch = Stopwatch.StartNew();

            _logger.LogInformation("▶️  Starting: {Operation} {Args}",
                operationName,
                args.Length > 0 ? $"({string.Join(", ", args)})" : "");
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            _logger.LogInformation("✅ Completed: {Operation} in {Elapsed}ms",
                _operationName,
                _stopwatch.ElapsedMilliseconds);
        }
    }
}