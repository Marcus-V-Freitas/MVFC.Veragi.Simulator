using Microsoft.Extensions.Logging;

namespace MVFC.Veragi.Simulator.Shareable.Logging;

public static partial class LogDefinitions
{
    [LoggerMessage(LogLevel.Error, "Request {TraceId} failed")]
    public static partial void LogRequestFailed(this ILogger logger, Exception exception, string traceId);

    [LoggerMessage(LogLevel.Error, "Scheduler {Kind} cycle failed")]
    public static partial void LogSchedulerFailed(this ILogger logger, Exception exception, string kind);

    [LoggerMessage(LogLevel.Information, "Webhook {Kind} event {EventKey} returned {StatusCode}")]
    public static partial void LogDeliveryAttempted(this ILogger logger, string kind, string eventKey, int statusCode);

    [LoggerMessage(LogLevel.Warning, "Webhook {Kind} event {EventKey} failed: {ErrorType}")]
    public static partial void LogDeliveryFailed(this ILogger logger, string kind, string eventKey, string errorType);
}
