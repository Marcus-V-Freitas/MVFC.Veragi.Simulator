namespace MVFC.Veragi.Simulator.WebhookWorker;

internal static partial class LogDefinitionsWorker
{
    [LoggerMessage(LogLevel.Information, "Webhook received (Schedules): IdempotencyKey={Key} RequestId={RequestId} Status={Status} TraceId={TraceId}")]
    public static partial void LogSchedulesWebhook(this ILogger logger, string? key, string? requestId, string? status, string traceId);

    [LoggerMessage(LogLevel.Information, "Webhook received (Contract): IdempotencyKey={Key} RequestId={RequestId} Status={Status} TraceId={TraceId}")]
    public static partial void LogContractsWebhook(this ILogger logger, string? key, string? requestId, string status, string traceId);
}