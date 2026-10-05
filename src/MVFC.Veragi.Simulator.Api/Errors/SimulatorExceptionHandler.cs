using Microsoft.AspNetCore.Diagnostics;
using MVFC.Veragi.Simulator.Api.Extensions;
using MVFC.Veragi.Simulator.Shareable.Logging;
using MVFC.Veragi.Simulator.Shareable.Results;

namespace MVFC.Veragi.Simulator.Api.Errors;

public sealed class SimulatorExceptionHandler(ILogger<SimulatorExceptionHandler> logger) : IExceptionHandler
{
    private readonly ILogger<SimulatorExceptionHandler> _logger = logger;

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (httpContext.Response.HasStarted)
            return false;

        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
            return true;

        var error = exception is BadHttpRequestException badRequest ? new SimulationFailureException(400, "VALIDATION_FAILED", badRequest.Message) : exception;
        var traceId = ResultHttpExtensions.EnsureTraceId(httpContext);

        if (exception is not BadHttpRequestException)
            _logger.LogRequestFailed(exception, traceId);

        await ResultHttpExtensions.Error(error, httpContext).ExecuteAsync(httpContext);

        return true;
    }
}
